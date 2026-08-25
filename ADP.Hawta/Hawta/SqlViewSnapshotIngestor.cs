using System.Data;
using System.Data.Common;

namespace ShiftSoftware.ADP.Hawta;

public sealed class SqlViewSnapshotIngestorOptions
{
    /// <summary>The snapshot table this source feeds.</summary>
    public required SnapshotTableDefinition Table { get; init; }

    /// <summary>
    /// The SELECT executed against the source (typically the client's wrapper SQL over a
    /// read-only view — compute keys and dedup there; read the view's public contract, never
    /// what's under it). Must return every column in <see cref="Table"/> by name.
    /// </summary>
    public required string SelectSql { get; init; }

    /// <summary>Result column whose (trimmed) text value becomes <c>_PrimaryKey</c>.</summary>
    public required string PrimaryKeyColumn { get; init; }

    /// <summary>
    /// Optional result column carrying the source row's own save date (becomes
    /// <c>_SourceModified</c>). Leave null for sources with no change column — the merge
    /// stamps the run clock instead. Values must be UTC or consistently normalized; the
    /// merge's monotonic stamps make skew safe, not invisible.
    /// </summary>
    public string? SourceModifiedColumn { get; init; }

    public int CommandTimeoutSeconds { get; init; } = 600;

    public required SnapshotMergeOptions MergeOptions { get; init; }
}

/// <summary>
/// Full-pull ingestion for SQL views and tables: executes the source SELECT, streams rows
/// through a DuckDB appender into persistent staging (no per-row reflection — ordinals are
/// resolved once), computes the uniform <see cref="RowHash"/> in-database, then hands off to
/// <see cref="SnapshotMerge"/>. Change detection is the hash diff — the source needs no
/// change column and is never written to.
///
/// <para><b>Two entry shapes, same downstream code.</b> <see cref="Ingest(SnapshotStore, DbConnection, SqlViewSnapshotIngestorOptions)"/>
/// does the whole thing on one thread. <see cref="Fetch(Func{DbConnection}, SqlViewSnapshotIngestorOptions, SnapshotSourceFetchContext)"/>
/// splits it at the only seam that pays: the remote read, measured at 95.4–97.9 % of an ingest
/// cycle, runs on a worker; staging, the in-DB hash and the merge stay on the store's single
/// connection. Both converge on the reader entry point below, so there is one implementation of
/// the behaviour and two ways to schedule it.</para>
/// </summary>
public static class SqlViewSnapshotIngestor
{
    public static SnapshotMergeResult Ingest(
        SnapshotStore store,
        DbConnection sourceConnection,
        SqlViewSnapshotIngestorOptions options)
    {
        if (sourceConnection.State != ConnectionState.Open)
            sourceConnection.Open();

        using var command = sourceConnection.CreateCommand();
        command.CommandText = options.SelectSql;
        command.CommandTimeout = options.CommandTimeoutSeconds;

        using var reader = command.ExecuteReader();
        return Ingest(store, reader, options);
    }

    /// <summary>
    /// The parallel half of the two-phase form: connect, execute, drain into memory, and hand the
    /// buffer to the serial drain. <b>Nothing here touches the store</b>, which is what makes it
    /// safe to run several of these at once while merges continue on the store's one connection.
    ///
    /// <para><b>The source connection closes here, not after the merge.</b> The one-phase form
    /// holds it open through staging, hashing and the merge, because the appender reads straight
    /// off the live reader. Under fetch-ahead the connection is done the moment the rows are in
    /// hand — strictly gentler on a dealer box than today, quite apart from the parallelism.</para>
    /// </summary>
    /// <param name="connect">
    /// Builds the source connection. A factory rather than a connection, because the fetch runs on
    /// a worker thread and must own the connection's whole life there — including disposing it
    /// when the fetch throws.
    /// </param>
    public static SnapshotSourceFetch Fetch(
        Func<DbConnection> connect,
        SqlViewSnapshotIngestorOptions options,
        SnapshotSourceFetchContext context)
    {
        var buffered = Fetch(connect, options, context.CancellationToken, context.RowsBuffered);
        return SnapshotSourceFetch.Staged(
            buffered.RowCount,
            drain => Ingest(drain.Store, buffered, options));
    }

    /// <summary>
    /// Connect, execute and drain into memory. Split out from the overload above so a caller can
    /// hold the buffer on its own — tests and diagnostics want the row set, not the carrier.
    /// </summary>
    public static BufferedRowSet Fetch(
        Func<DbConnection> connect,
        SqlViewSnapshotIngestorOptions options,
        CancellationToken cancellationToken = default,
        Action<int>? onRowsBuffered = null)
    {
        using var sourceConnection = connect();
        if (sourceConnection.State != ConnectionState.Open)
            sourceConnection.Open();

        using var command = sourceConnection.CreateCommand();
        command.CommandText = options.SelectSql;
        command.CommandTimeout = options.CommandTimeoutSeconds;

        using var reader = command.ExecuteReader();
        return BufferedRowSet.Drain(reader, cancellationToken, onRowsBuffered);
    }

    /// <summary>
    /// The serial half of the two-phase form. It is the reader entry point below, over
    /// <paramref name="buffered"/> — deliberately the SAME code path, so a two-phase source and a
    /// one-phase source resolve ordinals identically, fail on a missing column identically, and
    /// write identical run records.
    /// </summary>
    public static SnapshotMergeResult Ingest(
        SnapshotStore store,
        BufferedRowSet buffered,
        SqlViewSnapshotIngestorOptions options)
    {
        using var reader = buffered.CreateReader();
        return Ingest(store, reader, options);
    }

    /// <summary>Reader-level entry point — any <see cref="DbDataReader"/> works (SQL Server in production; in-memory readers in tests).</summary>
    public static SnapshotMergeResult Ingest(
        SnapshotStore store,
        DbDataReader reader,
        SqlViewSnapshotIngestorOptions options)
    {
        var staging = store.CreateStagingTable(options.Table, persistent: true);

        // Resolve ordinals once; a missing column throws here — by name, before any row
        // lands (the "read the view contract" tripwire for renamed/dropped columns).
        var columnOrdinals = options.Table.Columns
            .Select(c => reader.GetOrdinal(c.Name))
            .ToArray();
        var keyOrdinal = reader.GetOrdinal(options.PrimaryKeyColumn);
        var sourceModifiedOrdinal = options.SourceModifiedColumn is null
            ? -1
            : reader.GetOrdinal(options.SourceModifiedColumn);

        using (var appender = store.Connection.CreateAppender("stage", staging.Name))
        {
            while (reader.Read())
            {
                var row = appender.CreateRow();

                foreach (var ordinal in columnOrdinals)
                    AppendValue(row, reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal));

                // _PrimaryKey — trimmed text (1C nchar right-pads; a Cosmos id with trailing
                // whitespace can be written but never read or deleted by id).
                var key = reader.IsDBNull(keyOrdinal) ? null : reader.GetValue(keyOrdinal)?.ToString()?.Trim();
                AppendValue(row, string.IsNullOrEmpty(key) ? null : key);

                // Content and replication hashes — filled in-database below, so
                // canonicalization stays uniform.
                AppendValue(row, null);
                AppendValue(row, null);

                AppendValue(row, sourceModifiedOrdinal < 0 || reader.IsDBNull(sourceModifiedOrdinal)
                    ? null
                    : reader.GetValue(sourceModifiedOrdinal));

                row.EndRow();
            }
        }

        store.Execute(
            $"""
            UPDATE {staging.QualifiedName}
            SET "{BookkeepingColumns.RowHash}" = {RowHash.Expression(options.Table.Columns.Select(c => c.Name))},
                "{BookkeepingColumns.ReplicationHash}" = {RowHash.Expression(options.Table.ReplicationColumns.Select(c => c.Name))}
            """);

        // A full-universe view that returns ZERO rows is presumed torn, never a purge — the
        // same rule the file ingestor applies to a 0-byte feed. Without this the merge's
        // wipes-entire-scope guard would trip on every cycle (an empty staging deletes the
        // whole scope at any size), leaving the source permanently Aborted:MassDelete with
        // no operator-reachable escape. A dropped view, a permissions change, or a dealer DB
        // restored empty all look like this; ForceDeletes remains the intentional-wipe path.
        if (options.MergeOptions is { DeletesEnabled: true, ForceDeletes: false }
            && Convert.ToInt64(store.ExecuteScalar($"SELECT count(*) FROM {staging.QualifiedName}")) == 0)
        {
            var runId = options.MergeOptions.RunId ?? Guid.NewGuid().ToString("N");
            var skipped = new SnapshotMergeResult(runId, SnapshotMergeStatus.SkippedSourceEmpty, 0, 0, 0, 0);
            SnapshotMerge.InsertRunRecord(store, options.Table, options.MergeOptions, runId, DateTime.UtcNow, skipped,
                "The source view returned zero rows. Nothing merged; re-run with ForceDeletes for an intentional purge.");
            return skipped;
        }

        // NULL keys (blank/absent key values) fail the merge with Failed:InvalidStagingRows —
        // a loud run record instead of silently corrupted identity.
        return SnapshotMerge.Execute(store, options.Table, staging, options.MergeOptions);
    }

    private static void AppendValue(DuckDB.NET.Data.IDuckDBAppenderRow row, object? value)
    {
        switch (value)
        {
            case null or DBNull: row.AppendNullValue(); break;
            case string s: row.AppendValue(s); break;
            case bool b: row.AppendValue(b); break;
            case byte by: row.AppendValue(by); break;
            case short sh: row.AppendValue(sh); break;
            case int i: row.AppendValue(i); break;
            case long l: row.AppendValue(l); break;
            case float f: row.AppendValue(f); break;
            case double d: row.AppendValue(d); break;
            case decimal m: row.AppendValue(m); break;
            case DateTime dt: row.AppendValue(dt); break;
            case DateTimeOffset dto: row.AppendValue(dto.UtcDateTime); break;
            case Guid g: row.AppendValue(g.ToString()); break;
            case byte[] bytes: row.AppendValue(bytes); break;
            default: row.AppendValue(value.ToString()); break;
        }
    }
}
