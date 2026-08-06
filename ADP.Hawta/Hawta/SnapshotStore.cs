using System.Data;
using System.Reflection;
using DuckDB.NET.Data;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// The snapshot write database: one DuckDB file, three schemas —
/// <c>data</c> (consolidated tables + bookkeeping columns), <c>src</c> (per-source state),
/// <c>meta</c> (<c>schema_info</c> sentinel, <c>SyncRuns</c> run records).
/// Single-writer by design: callers serialize writes through the write gate (a later slice);
/// the store itself holds one connection and is not thread-safe.
/// </summary>
public sealed class SnapshotStore : IDisposable
{
    public const int CurrentSchemaVersion = 1;

    /// <summary>Rows failing replication this many times leave the dirty predicate (dead-letter) until reset.</summary>
    public const int MaxReplicationAttempts = 5;

    private readonly DuckDBConnection connection;

    public DuckDBConnection Connection => connection;

    private SnapshotStore(DuckDBConnection connection) => this.connection = connection;

    public static SnapshotStore Open(SnapshotStoreOptions options)
    {
        var connection = new DuckDBConnection($"Data Source={options.DatabasePath}");

        try
        {
            connection.Open();
            var store = new SnapshotStore(connection);
            store.Bootstrap(options.SchemaVersion);
            return store;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private void Bootstrap(int expectedSchemaVersion)
    {
        Execute("CREATE SCHEMA IF NOT EXISTS data");
        Execute("CREATE SCHEMA IF NOT EXISTS src");
        Execute("CREATE SCHEMA IF NOT EXISTS meta");
        Execute("CREATE SCHEMA IF NOT EXISTS stage");

        Execute(
            """
            CREATE TABLE IF NOT EXISTS meta.schema_info (
                "SchemaVersion" INTEGER NOT NULL,
                "PackageVersion" VARCHAR NOT NULL,
                "CreatedAt" TIMESTAMP NOT NULL
            )
            """);

        var sentinel = ExecuteScalar("SELECT max(\"SchemaVersion\") FROM meta.schema_info");
        if (sentinel is null or DBNull)
        {
            var packageVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            Execute(
                "INSERT INTO meta.schema_info VALUES (?, ?, ?)",
                expectedSchemaVersion, packageVersion, DateTime.UtcNow);
        }
        else if (Convert.ToInt32(sentinel) != expectedSchemaVersion)
        {
            throw new SnapshotSchemaMismatchException(expectedSchemaVersion, Convert.ToInt32(sentinel));
        }

        Execute(
            """
            CREATE TABLE IF NOT EXISTS meta.ReconOps (
                "RunId" VARCHAR NOT NULL,
                "TargetTable" VARCHAR NOT NULL,
                "Family" VARCHAR NOT NULL,
                "PrimaryKey" VARCHAR NOT NULL,
                "Op" VARCHAR NOT NULL,
                "CosmosId" VARCHAR,
                "PartitionKey" VARCHAR,
                "DocHash" VARCHAR,
                "CapturedLastModified" TIMESTAMP NOT NULL,
                "EmittedAt" TIMESTAMP NOT NULL
            )
            """);

        Execute(
            """
            CREATE TABLE IF NOT EXISTS meta.PublishRuns (
                "PublishId" VARCHAR NOT NULL,
                "SnapshotName" VARCHAR NOT NULL,
                "StartedAt" TIMESTAMP NOT NULL,
                "FinishedAt" TIMESTAMP,
                "TablesExported" INTEGER NOT NULL DEFAULT 0,
                "TablesReused" INTEGER NOT NULL DEFAULT 0,
                "ShimsDeleted" INTEGER NOT NULL DEFAULT 0,
                "ParquetFilesDeleted" INTEGER NOT NULL DEFAULT 0,
                "Status" VARCHAR NOT NULL,
                "Error" VARCHAR
            )
            """);

        Execute(
            """
            CREATE TABLE IF NOT EXISTS meta.SyncRuns (
                "RunId" VARCHAR NOT NULL PRIMARY KEY,
                "Source" VARCHAR NOT NULL,
                "TargetTable" VARCHAR NOT NULL,
                "StartedAt" TIMESTAMP NOT NULL,
                "FinishedAt" TIMESTAMP,
                "RowsStaged" BIGINT NOT NULL DEFAULT 0,
                "RowsInserted" BIGINT NOT NULL DEFAULT 0,
                "RowsUpdated" BIGINT NOT NULL DEFAULT 0,
                "RowsTombstoned" BIGINT NOT NULL DEFAULT 0,
                "Status" VARCHAR NOT NULL,
                "Error" VARCHAR
            )
            """);
    }

    /// <summary>
    /// Creates the family's consolidated table (source columns + bookkeeping) if missing.
    /// Additive schema drift is applied automatically: source columns present in the
    /// definition but missing on an existing table are added via <c>ALTER TABLE ADD COLUMN</c>
    /// (as nullable), so shipping a widened definition against a live write DB just works.
    /// </summary>
    public void EnsureTable(SnapshotTableDefinition table)
    {
        var sourceColumns = string.Join(",\n    ", table.Columns.Select(c => $"\"{c.Name}\" {c.DuckDbType}"));

        Execute(
            $"""
            CREATE TABLE IF NOT EXISTS {table.QualifiedName} (
                {sourceColumns},
                {BookkeepingColumns.TableDdl},
                PRIMARY KEY ("{BookkeepingColumns.PrimaryKey}")
            )
            """);

        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                "SELECT column_name FROM information_schema.columns WHERE table_schema = 'data' AND table_name = ?";
            var parameter = command.CreateParameter();
            parameter.Value = table.Name;
            command.Parameters.Add(parameter);

            using var reader = command.ExecuteReader();
            while (reader.Read())
                existingColumns.Add(reader.GetString(0));
        }

        foreach (var column in table.Columns.Where(c => !existingColumns.Contains(c.Name)))
            Execute($"ALTER TABLE {table.QualifiedName} ADD COLUMN \"{column.Name}\" {column.DuckDbType}");
    }

    /// <summary>
    /// Creates (dropping any previous instance) the staging table for one ingest run: the
    /// family's source columns plus <c>_PrimaryKey</c>, <c>_RowHash</c> (nullable at staging
    /// time — ingestors fill it in-DB with <see cref="RowHash.Expression"/> after loading, so
    /// the canonicalization is uniform across source formats), and <c>_SourceModified</c>
    /// (optional per-row source timestamp).
    /// Temp staging (default) is connection-local; persistent staging lives in the
    /// <c>stage</c> schema — required by appender-based ingestion, which cannot target temp
    /// tables. Both are transient by contract: dropped and recreated per run.
    /// </summary>
    public StagingTable CreateStagingTable(SnapshotTableDefinition table, bool persistent = false)
    {
        var stagingName = $"staging_{table.Name}";
        var sourceColumns = string.Join(",\n    ", table.Columns.Select(c => $"\"{c.Name}\" {c.DuckDbType}"));

        // _PrimaryKey/_RowHash are nullable AT STAGING TIME so a bad source row lands and
        // fails the merge's validation with a loud Failed:InvalidStagingRows run record —
        // instead of exploding mid-append. The merge validates both before touching data.
        var columnsDdl =
            $"""
            {sourceColumns},
            "{BookkeepingColumns.PrimaryKey}" VARCHAR,
            "{BookkeepingColumns.RowHash}" VARCHAR,
            "_SourceModified" TIMESTAMP
            """;

        if (persistent)
        {
            Execute($"DROP TABLE IF EXISTS stage.\"{stagingName}\"");
            Execute($"CREATE TABLE stage.\"{stagingName}\" (\n{columnsDdl}\n)");
            return new StagingTable(stagingName, $"stage.\"{stagingName}\"");
        }

        // temp-qualified DROP: an unqualified name would resolve through the search path and
        // could hit a real table in main when no temp table exists yet.
        Execute($"DROP TABLE IF EXISTS temp.main.\"{stagingName}\"");
        Execute($"CREATE TEMP TABLE \"{stagingName}\" (\n{columnsDdl}\n)");
        return new StagingTable(stagingName, $"temp.main.\"{stagingName}\"");
    }

    // ---- Replication state ------------------------------------------------------------------
    // These carry ShiftEntity's replication watermark semantics verbatim (the contract pinned
    // by ShiftEntity.Tests/Replication/ReplicationWatermarkTests.cs and this package's
    // ReplicationWatermarkTests): the watermark written on success is the _LastModified value
    // CAPTURED WHEN THE ROW WAS LOADED — never the current time, never the row's latest value.
    // A row modified while its push was in flight therefore stays dirty.

    internal static string DirtyPredicate =>
        $"""
        ("{BookkeepingColumns.LastReplicationDate}" < "{BookkeepingColumns.LastModified}"
         OR "{BookkeepingColumns.LastReplicationDate}" IS NULL)
        AND "{BookkeepingColumns.ReplicationAttempts}" < {MaxReplicationAttempts}
        """;

    /// <summary>Loads a batch of dirty rows, capturing each row's <c>_LastModified</c> at load time.</summary>
    public IReadOnlyList<DirtyRow> ReadDirtyRows(SnapshotTableDefinition table, int limit = 1000) =>
        ReadDirtyRows(table, afterPrimaryKey: null, limit);

    /// <summary>
    /// Cursor-paged variant: loads dirty rows with <c>_PrimaryKey</c> strictly above
    /// <paramref name="afterPrimaryKey"/>. This is how a DRY-RUN covers the entire dirty set —
    /// dry-runs stamp nothing, so limit-only reads would return the same batch forever.
    /// </summary>
    public IReadOnlyList<DirtyRow> ReadDirtyRows(SnapshotTableDefinition table, string? afterPrimaryKey, int limit)
    {
        var rows = new List<DirtyRow>();

        using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT "{BookkeepingColumns.PrimaryKey}", "{BookkeepingColumns.LastModified}",
                   "{BookkeepingColumns.Deleted}", "{BookkeepingColumns.ReplicationStamp}",
                   "{BookkeepingColumns.SourceScope}",
                   {table.QuotedColumnList}
            FROM {table.QualifiedName}
            WHERE {DirtyPredicate}
              {(afterPrimaryKey is null ? "" : $"AND \"{BookkeepingColumns.PrimaryKey}\" > ?")}
            ORDER BY "{BookkeepingColumns.PrimaryKey}"
            LIMIT {limit}
            """;
        AddParameters(command, afterPrimaryKey is null ? [] : [afterPrimaryKey]);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var values = new Dictionary<string, object?>(table.Columns.Count);
            for (var i = 0; i < table.Columns.Count; i++)
            {
                var value = reader.GetValue(i + 5);
                values[table.Columns[i].Name] = value is DBNull ? null : value;
            }

            rows.Add(new DirtyRow(
                PrimaryKey: reader.GetString(0),
                // DuckDB TIMESTAMP is naive; the store's convention is UTC. Stamping the Kind
                // here keeps a publisher's .ToUniversalTime() from silently shifting the value
                // (which would wedge the row as permanently dirty).
                CapturedLastModified: DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc),
                Deleted: reader.GetBoolean(2),
                ReplicationStamp: reader.IsDBNull(3) ? null : reader.GetString(3),
                Values: values,
                SourceScope: reader.IsDBNull(4) ? null : reader.GetString(4)));
        }

        return rows;
    }

    /// <summary>
    /// Cursor-paged full scan over a table — every row, live or tombstoned, dirty or clean —
    /// for recon: comparing the snapshot's INTENDED Cosmos state against the actual one.
    /// (Dirty rows alone can't recon: a clean row's document still has to match.)
    /// </summary>
    public IReadOnlyList<SnapshotRow> ReadRows(SnapshotTableDefinition table, string? afterPrimaryKey, int limit)
    {
        var rows = new List<SnapshotRow>();

        using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT "{BookkeepingColumns.PrimaryKey}", "{BookkeepingColumns.LastModified}",
                   "{BookkeepingColumns.Deleted}", "{BookkeepingColumns.ReplicationStamp}",
                   "{BookkeepingColumns.SourceScope}",
                   ({DirtyPredicate}) AS "hawta$dirty",
                   {table.QuotedColumnList}
            FROM {table.QualifiedName}
            {(afterPrimaryKey is null ? "" : $"WHERE \"{BookkeepingColumns.PrimaryKey}\" > ?")}
            ORDER BY "{BookkeepingColumns.PrimaryKey}"
            LIMIT {limit}
            """;
        AddParameters(command, afterPrimaryKey is null ? [] : [afterPrimaryKey]);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var values = new Dictionary<string, object?>(table.Columns.Count);
            for (var i = 0; i < table.Columns.Count; i++)
            {
                var value = reader.GetValue(i + 6);
                values[table.Columns[i].Name] = value is DBNull ? null : value;
            }

            rows.Add(new SnapshotRow(
                PrimaryKey: reader.GetString(0),
                CapturedLastModified: DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc),
                Deleted: reader.GetBoolean(2),
                ReplicationStamp: reader.IsDBNull(3) ? null : reader.GetString(3),
                SourceScope: reader.IsDBNull(4) ? null : reader.GetString(4),
                Dirty: reader.GetBoolean(5),
                Values: values));
        }

        return rows;
    }

    /// <summary>
    /// Deletes recon ops for a table (optionally sparing one run). Dry-runs stamp nothing, so
    /// repeated dry-runs would otherwise grow <c>meta.ReconOps</c> without bound.
    /// </summary>
    public int PruneReconOps(SnapshotTableDefinition table, string? keepRunId = null) =>
        Execute(
            """
            DELETE FROM meta.ReconOps
            WHERE "TargetTable" = ? AND (? IS NULL OR "RunId" <> ?)
            """,
            table.Name, keepRunId, keepRunId);

    public long CountDirtyRows(SnapshotTableDefinition table) =>
        Convert.ToInt64(ExecuteScalar($"SELECT count(*) FROM {table.QualifiedName} WHERE {DirtyPredicate}"));

    /// <summary>
    /// Records a successful push: stamps the CAPTURED <c>_LastModified</c> as the watermark and
    /// the replication stamp in the same statement (they can never drift apart), and clears the
    /// failure ledger. If a merge bumped the row mid-flight, captured &lt; current
    /// <c>_LastModified</c> → the row remains dirty and is re-pushed next cycle.
    /// </summary>
    public void MarkReplicated(SnapshotTableDefinition table, string primaryKey, DateTime capturedLastModified, string? replicationStamp)
    {
        capturedLastModified = AsUtc(capturedLastModified);
        Execute(
            $"""
            UPDATE {table.QualifiedName}
            SET "{BookkeepingColumns.LastReplicationDate}" = ?,
                "{BookkeepingColumns.ReplicationStamp}" = ?,
                "{BookkeepingColumns.ReplicatedAt}" = ?,
                "{BookkeepingColumns.ReplicationAttempts}" = 0,
                "{BookkeepingColumns.ReplicationError}" = NULL
            WHERE "{BookkeepingColumns.PrimaryKey}" = ?
            """,
            capturedLastModified, replicationStamp, DateTime.UtcNow, primaryKey);
    }

    /// <summary>
    /// Records a failed push attempt for the captured row version. A newer source version
    /// resets its own ledger during merge and must not inherit a stale in-flight failure.
    /// </summary>
    public void MarkReplicationFailed(
        SnapshotTableDefinition table,
        string primaryKey,
        DateTime capturedLastModified,
        string error)
    {
        capturedLastModified = AsUtc(capturedLastModified);
        Execute(
            $"""
            UPDATE {table.QualifiedName}
            SET "{BookkeepingColumns.ReplicationAttempts}" = "{BookkeepingColumns.ReplicationAttempts}" + 1,
                "{BookkeepingColumns.ReplicationError}" = ?
            WHERE "{BookkeepingColumns.PrimaryKey}" = ?
              AND "{BookkeepingColumns.LastModified}" = ?
              AND ({DirtyPredicate})
            """,
            error, primaryKey, capturedLastModified);
    }

    /// <summary>
    /// Atomically applies a bounded owner-produced group of terminal replication outcomes.
    /// Successes retain captured-watermark semantics: the stamp describes what landed and
    /// a newer local version remains dirty. Failures increment only the same captured dirty
    /// version. Both sets are applied with set-based SQL in one transaction.
    /// </summary>
    internal void CommitReplicationOutcomes(
        SnapshotTableDefinition table,
        IReadOnlyList<ReplicationStateOutcome> outcomes,
        Action ensureCommitAllowed)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(outcomes);
        ArgumentNullException.ThrowIfNull(ensureCommitAllowed);

        if (outcomes.Count == 0)
            return;

        var duplicateKey = outcomes
            .GroupBy(outcome => outcome.PrimaryKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateKey is not null)
        {
            throw new ArgumentException(
                $"Replication outcome group contains primary key '{duplicateKey.Key}' more than once.",
                nameof(outcomes));
        }

        foreach (var outcome in outcomes)
        {
            if (string.IsNullOrWhiteSpace(outcome.PrimaryKey))
                throw new ArgumentException("Replication outcomes require a primary key.", nameof(outcomes));
            if (outcome.Kind is not ReplicationStateOutcomeKind.Replicated
                and not ReplicationStateOutcomeKind.Failed)
            {
                throw new ArgumentException(
                    $"Replication outcome for '{outcome.PrimaryKey}' has an unsupported kind.",
                    nameof(outcomes));
            }
            if (outcome.Kind == ReplicationStateOutcomeKind.Replicated && outcome.Error is not null)
                throw new ArgumentException("A successful replication outcome cannot carry an error.", nameof(outcomes));
            if (outcome.Kind == ReplicationStateOutcomeKind.Failed && string.IsNullOrWhiteSpace(outcome.Error))
                throw new ArgumentException("A failed replication outcome requires an error.", nameof(outcomes));
            if (outcome.Kind == ReplicationStateOutcomeKind.Failed && outcome.ReplicationStamp is not null)
                throw new ArgumentException("A failed replication outcome cannot carry a stamp.", nameof(outcomes));
        }

        // The callback combines cancellation with the caller's lease/fence proof. Check at
        // the transaction boundary and again at the last possible point before commit.
        ensureCommitAllowed();
        using var transaction = connection.BeginTransaction();
        try
        {
            var successes = outcomes
                .Where(outcome => outcome.Kind == ReplicationStateOutcomeKind.Replicated)
                .ToList();
            if (successes.Count > 0)
                ApplySuccessfulReplicationOutcomes(transaction, table, successes);

            var failures = outcomes
                .Where(outcome => outcome.Kind == ReplicationStateOutcomeKind.Failed)
                .ToList();
            if (failures.Count > 0)
                ApplyFailedReplicationOutcomes(transaction, table, failures);

            ensureCommitAllowed();
            transaction.Commit();
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // Preserve the ownership, cancellation, or SQL exception that caused the
                // rollback. Disposing the transaction/connection remains the final cleanup.
            }

            throw;
        }
    }

    private void ApplySuccessfulReplicationOutcomes(
        DuckDBTransaction transaction,
        SnapshotTableDefinition table,
        IReadOnlyList<ReplicationStateOutcome> outcomes)
    {
        var values = string.Join(", ", outcomes.Select(_ =>
            "(CAST(? AS VARCHAR), CAST(? AS TIMESTAMP), CAST(? AS VARCHAR), CAST(? AS TIMESTAMP))"));
        var parameters = new List<object?>(outcomes.Count * 4);
        var replicatedAt = DateTime.UtcNow;
        foreach (var outcome in outcomes)
        {
            parameters.Add(outcome.PrimaryKey);
            parameters.Add(AsUtc(outcome.CapturedLastModified));
            parameters.Add(outcome.ReplicationStamp);
            parameters.Add(replicatedAt);
        }

        Execute(
            transaction,
            $"""
            UPDATE {table.QualifiedName} AS target
            SET "{BookkeepingColumns.LastReplicationDate}" = outcome."CapturedLastModified",
                "{BookkeepingColumns.ReplicationStamp}" = outcome."ReplicationStamp",
                "{BookkeepingColumns.ReplicatedAt}" = outcome."ReplicatedAt",
                "{BookkeepingColumns.ReplicationAttempts}" = 0,
                "{BookkeepingColumns.ReplicationError}" = NULL
            FROM (VALUES {values}) AS outcome(
                "PrimaryKey", "CapturedLastModified", "ReplicationStamp", "ReplicatedAt")
            WHERE target."{BookkeepingColumns.PrimaryKey}" = outcome."PrimaryKey"
            """,
            parameters.ToArray());
    }

    private void ApplyFailedReplicationOutcomes(
        DuckDBTransaction transaction,
        SnapshotTableDefinition table,
        IReadOnlyList<ReplicationStateOutcome> outcomes)
    {
        var values = string.Join(", ", outcomes.Select(_ =>
            "(CAST(? AS VARCHAR), CAST(? AS TIMESTAMP), CAST(? AS VARCHAR))"));
        var parameters = new List<object?>(outcomes.Count * 3);
        foreach (var outcome in outcomes)
        {
            parameters.Add(outcome.PrimaryKey);
            parameters.Add(AsUtc(outcome.CapturedLastModified));
            parameters.Add(outcome.Error);
        }

        Execute(
            transaction,
            $"""
            UPDATE {table.QualifiedName} AS target
            SET "{BookkeepingColumns.ReplicationAttempts}" = target."{BookkeepingColumns.ReplicationAttempts}" + 1,
                "{BookkeepingColumns.ReplicationError}" = outcome."Error"
            FROM (VALUES {values}) AS outcome("PrimaryKey", "CapturedLastModified", "Error")
            WHERE target."{BookkeepingColumns.PrimaryKey}" = outcome."PrimaryKey"
              AND target."{BookkeepingColumns.LastModified}" = outcome."CapturedLastModified"
              AND (target."{BookkeepingColumns.LastReplicationDate}" < target."{BookkeepingColumns.LastModified}"
                   OR target."{BookkeepingColumns.LastReplicationDate}" IS NULL)
              AND target."{BookkeepingColumns.ReplicationAttempts}" < {MaxReplicationAttempts}
            """,
            parameters.ToArray());
    }

    /// <summary>Returns dead-lettered rows to the dirty predicate (the manual reset endpoint's backing call).</summary>
    public int ResetReplicationFailures(SnapshotTableDefinition table)
    {
        var deadLettered = Convert.ToInt32(ExecuteScalar(
            $"""
            SELECT count(*) FROM {table.QualifiedName}
            WHERE "{BookkeepingColumns.ReplicationAttempts}" >= {MaxReplicationAttempts}
            """));

        Execute(
            $"""
            UPDATE {table.QualifiedName}
            SET "{BookkeepingColumns.ReplicationAttempts}" = 0,
                "{BookkeepingColumns.ReplicationError}" = NULL
            WHERE "{BookkeepingColumns.ReplicationAttempts}" >= {MaxReplicationAttempts}
            """);

        return deadLettered;
    }

    // ---- Plumbing ---------------------------------------------------------------------------

    /// <summary>The store's timestamps are UTC; Unspecified is assumed already-UTC (the DB round-trip case).</summary>
    internal static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    internal int Execute(string sql, params object?[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        return command.ExecuteNonQuery();
    }

    private int Execute(
        DuckDBTransaction transaction,
        string sql,
        params object?[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        AddParameters(command, parameters);
        return command.ExecuteNonQuery();
    }

    internal object? ExecuteScalar(string sql, params object?[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        return command.ExecuteScalar();
    }

    private static void AddParameters(IDbCommand command, object?[] parameters)
    {
        foreach (var value in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }

    public void Dispose() => connection.Dispose();
}

/// <summary>A dirty row loaded for replication, with its <c>_LastModified</c> captured at load time.</summary>
/// <param name="ReplicationStamp">The stamp JSON of the last successful push (Cosmos coordinates per family), or null if never replicated.</param>
/// <param name="SourceScope">The row's <c>_SourceScope</c> — how a mapping shared across
/// per-scope sources (e.g. one table fed by eight dealers) resolves scope-specific values
/// like CompanyID. Null when the table has a single unscoped universe.</param>
public sealed record DirtyRow(
    string PrimaryKey,
    DateTime CapturedLastModified,
    bool Deleted,
    string? ReplicationStamp,
    IReadOnlyDictionary<string, object?> Values,
    string? SourceScope = null);

/// <summary>A full-scan row for recon: bookkeeping state plus source values, dirty or not.</summary>
public sealed record SnapshotRow(
    string PrimaryKey,
    DateTime CapturedLastModified,
    bool Deleted,
    string? ReplicationStamp,
    string? SourceScope,
    bool Dirty,
    IReadOnlyDictionary<string, object?> Values)
{
    /// <summary>The row in the shape family mappings consume.</summary>
    public DirtyRow AsDirtyRow() =>
        new(PrimaryKey, CapturedLastModified, Deleted, ReplicationStamp, Values, SourceScope);
}
