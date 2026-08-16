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
    public const int CurrentSchemaVersion = 2;

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
            ApplyExtensionDirectory(connection, options.ExtensionDirectory);
            ApplyAzureCredential(connection, options.AzureConnectionString);
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

    /// <summary>
    /// Points DuckDB's extension cache somewhere durable. Applied before <see cref="Bootstrap"/>
    /// so it is in force for every statement this connection ever runs — the <c>azure</c>
    /// extension is fetched lazily on the first <c>az://</c> touch, which can be any of them.
    /// </summary>
    internal static void ApplyExtensionDirectory(DuckDBConnection connection, string? extensionDirectory)
    {
        if (string.IsNullOrWhiteSpace(extensionDirectory))
            return;

        // Created eagerly: DuckDB reports a missing extension directory as a failure to open
        // the download's temp file, which names the temp file and not the cause.
        Directory.CreateDirectory(extensionDirectory);

        using var command = connection.CreateCommand();
        command.CommandText = $"SET extension_directory = '{extensionDirectory.Replace("'", "''")}'";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Gives DuckDB its own credential for <c>az://</c>.
    ///
    /// <para>The publish tier reaches blob two ways and they authenticate SEPARATELY: listing,
    /// metadata, delete and the manifest commit go through <c>Azure.Storage.Blobs</c>, while the
    /// parquet itself moves through DuckDB's <c>COPY … TO</c> and <c>read_parquet</c>. Configuring
    /// only the SDK half produces "Invalid Input Error: No valid Azure credentials found!" at the
    /// first export — after the destination has already been proven reachable, which makes it read
    /// like a DuckDB fault rather than a missing setting.</para>
    ///
    /// <para>Applied before <see cref="Bootstrap"/>, for the same reason as the extension
    /// directory: the first <c>az://</c> touch can be any statement this connection runs.</para>
    /// </summary>
    internal static void ApplyAzureCredential(DuckDBConnection connection, string? azureConnectionString)
    {
        if (string.IsNullOrWhiteSpace(azureConnectionString))
            return;

        using var command = connection.CreateCommand();
        // Never interpolated into a log or an exception message — this value carries the account
        // key or SAS. DuckDB has no parameter binding for CREATE SECRET, so it is escaped instead.
        command.CommandText =
            $"CREATE OR REPLACE SECRET hawta_publish (TYPE azure, CONNECTION_STRING '{azureConnectionString.Replace("'", "''")}')";
        command.ExecuteNonQuery();
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

        // The source change gate's memory: what each file source looked like the last time its
        // merge SUCCEEDED. Additive, so an existing write DB gains it empty on the next open and
        // no schema-version bump (and therefore no forced cold-start rebuild) is needed — every
        // source simply re-reads once and re-stamps.
        Execute(
            """
            CREATE TABLE IF NOT EXISTS meta.SourceFileStamps (
                "SourceKey" VARCHAR NOT NULL PRIMARY KEY,
                "FilePath" VARCHAR NOT NULL,
                "Length" BIGINT NOT NULL,
                "LastWriteUtcTicks" BIGINT NOT NULL,
                "ConfigFingerprint" VARCHAR NOT NULL,
                "StampedAtUtc" TIMESTAMP NOT NULL
            )
            """);
    }

    /// <summary>
    /// What <paramref name="sourceKey"/> looked like at its last successful merge, or null if it
    /// has never had one. Null is the safe answer: the caller reads the file.
    /// </summary>
    /// <remarks>
    /// The last-write time is stored as exact <see cref="DateTime.Ticks"/>, not as a TIMESTAMP.
    /// NTFS resolves file times to 100 ns while DuckDB's TIMESTAMP holds microseconds, so a
    /// round-trip through TIMESTAMP truncates — and this value is compared for EQUALITY, where a
    /// truncated round-trip reads as "the file changed" on almost every unchanged file. (Almost:
    /// a timestamp that happens to land on a microsecond boundary compares equal, which makes the
    /// bug intermittent rather than obvious.) Ticks are exact and this field is an identity token,
    /// never something a human queries by range.
    /// </remarks>
    public SourceFileStamp? ReadSourceFileStamp(string sourceKey)
    {
        using var command = Connection.CreateCommand();
        command.CommandText =
            """
            SELECT "FilePath", "Length", "LastWriteUtcTicks", "ConfigFingerprint", "StampedAtUtc"
            FROM meta.SourceFileStamps WHERE "SourceKey" = ?
            """;
        var parameter = command.CreateParameter();
        parameter.Value = sourceKey;
        command.Parameters.Add(parameter);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        return new SourceFileStamp(
            sourceKey,
            reader.GetString(0),
            reader.GetInt64(1),
            new DateTime(reader.GetInt64(2), DateTimeKind.Utc),
            reader.GetString(3),
            AsUtc(reader.GetDateTime(4)));
    }

    /// <summary>
    /// Records a source's file identity after a successful merge. Called <b>only</b> on success —
    /// stamping a failed or skipped run would let the next cycle skip a file that was never
    /// actually ingested.
    /// </summary>
    public void WriteSourceFileStamp(SourceFileStamp stamp)
    {
        Execute("DELETE FROM meta.SourceFileStamps WHERE \"SourceKey\" = ?", stamp.SourceKey);
        Execute(
            "INSERT INTO meta.SourceFileStamps VALUES (?, ?, ?, ?, ?, ?)",
            stamp.SourceKey, stamp.FilePath, stamp.Length, stamp.LastWriteUtc.Ticks,
            stamp.ConfigFingerprint, stamp.StampedAtUtc);
    }

    /// <summary>
    /// Every stamp, for publishing alongside the set they describe. Read at publish time, when no
    /// merge is in flight (the loop ingests, pumps, then publishes), so what this returns is exactly
    /// consistent with the parquet about to be committed.
    /// </summary>
    public IReadOnlyList<SourceFileStamp> ReadAllSourceFileStamps()
    {
        using var command = Connection.CreateCommand();
        command.CommandText =
            """
            SELECT "SourceKey", "FilePath", "Length", "LastWriteUtcTicks", "ConfigFingerprint", "StampedAtUtc"
            FROM meta.SourceFileStamps ORDER BY "SourceKey"
            """;

        using var reader = command.ExecuteReader();
        var stamps = new List<SourceFileStamp>();
        while (reader.Read())
        {
            stamps.Add(new SourceFileStamp(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt64(2),
                new DateTime(reader.GetInt64(3), DateTimeKind.Utc),
                reader.GetString(4),
                AsUtc(reader.GetDateTime(5))));
        }

        return stamps;
    }

    /// <summary>Drops a source's stamp, forcing a full read on its next cycle.</summary>
    public void ClearSourceFileStamp(string sourceKey) =>
        Execute("DELETE FROM meta.SourceFileStamps WHERE \"SourceKey\" = ?", sourceKey);

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
    /// family's source columns plus <c>_PrimaryKey</c>, <c>_RowHash</c>, and
    /// <c>_ReplicationHash</c> (nullable at staging time — ingestors fill them in-DB with
    /// <see cref="RowHash.Expression"/> after loading, so
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

        // Identity and hashes are nullable AT STAGING TIME so a bad source row lands and
        // fails the merge's validation with a loud Failed:InvalidStagingRows run record —
        // instead of exploding mid-append. The merge validates both before touching data.
        var columnsDdl =
            $"""
            {sourceColumns},
            "{BookkeepingColumns.PrimaryKey}" VARCHAR,
            "{BookkeepingColumns.RowHash}" VARCHAR,
            "{BookkeepingColumns.ReplicationHash}" VARCHAR,
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
    // ReplicationWatermarkTests): the watermark written on success is the _ReplicationModified
    // value CAPTURED WHEN THE ROW WAS LOADED — never the current time, never the row's latest
    // value. A document-affecting edit while its push is in flight therefore stays dirty;
    // source-only edits intentionally do not.

    internal static string DirtyPredicate =>
        $"""
        ("{BookkeepingColumns.LastReplicationDate}" < "{BookkeepingColumns.ReplicationModified}"
         OR "{BookkeepingColumns.LastReplicationDate}" IS NULL)
        AND "{BookkeepingColumns.ReplicationAttempts}" < {MaxReplicationAttempts}
        """;

    /// <summary>Loads a batch of dirty rows, capturing each row's <c>_ReplicationModified</c> at load time.</summary>
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
            SELECT "{BookkeepingColumns.PrimaryKey}", "{BookkeepingColumns.ReplicationModified}",
                   "{BookkeepingColumns.Deleted}", "{BookkeepingColumns.ReplicationStamp}",
                   "{BookkeepingColumns.SourceScope}", "{BookkeepingColumns.ReplicatedAt}",
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
                var value = reader.GetValue(i + 6);
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
                SourceScope: reader.IsDBNull(4) ? null : reader.GetString(4),
                ReplicatedAt: reader.IsDBNull(5)
                    ? null
                    : DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)));
        }

        return rows;
    }

    /// <summary>
    /// Loads distinct replication groups and every source row belonging to those groups.
    /// Group selection and row loading are both set-based: one query selects up to
    /// <paramref name="limit"/> keys (plus a has-more sentinel), then one query fetches all
    /// affected rows. There is never one DuckDB query per changed source row or group.
    /// </summary>
    internal ReplicationGroupPage ReadReplicationGroups(
        SnapshotTableDefinition table,
        CosmosGroupProjection grouping,
        string? afterGroupKey,
        int limit,
        bool dirtyGroupsOnly)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit));
        if (!ReferenceEquals(table, grouping.Table))
            throw new ArgumentException(
                $"Grouped mapping table '{grouping.Table.Name}' does not match pump table '{table.Name}'.",
                nameof(grouping));

        var groupExpression = $"coalesce(CAST(\"{grouping.GroupColumn}\" AS VARCHAR), '')";
        var groupKeys = new List<string>(limit + 1);
        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                $"""
                SELECT {groupExpression} AS "hawta$group"
                FROM {table.QualifiedName}
                WHERE {(dirtyGroupsOnly ? DirtyPredicate : "true")}
                  {(afterGroupKey is null ? "" : $"AND {groupExpression} > ?")}
                GROUP BY {groupExpression}
                ORDER BY "hawta$group"
                LIMIT {limit + 1}
                """;
            AddParameters(command, afterGroupKey is null ? [] : [afterGroupKey]);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                groupKeys.Add(reader.GetString(0));
        }

        var hasMore = groupKeys.Count > limit;
        if (hasMore)
            groupKeys.RemoveAt(groupKeys.Count - 1);
        if (groupKeys.Count == 0)
            return new ReplicationGroupPage([], HasMore: false, LastGroupKey: null);

        var selectedValues = string.Join(", ", groupKeys.Select(_ => "(CAST(? AS VARCHAR))"));
        var groups = groupKeys.ToDictionary(
            key => key,
            key => new List<ReplicationGroupSourceRow>(),
            StringComparer.Ordinal);

        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                $"""
                WITH "hawta$selected"("GroupKey") AS (VALUES {selectedValues})
                SELECT {groupExpression} AS "hawta$group",
                       t."{BookkeepingColumns.PrimaryKey}",
                       t."{BookkeepingColumns.ReplicationModified}",
                       t."{BookkeepingColumns.Deleted}",
                       t."{BookkeepingColumns.ReplicationStamp}",
                       t."{BookkeepingColumns.SourceScope}",
                       t."{BookkeepingColumns.ReplicatedAt}",
                       ({DirtyPredicate}) AS "hawta$dirty",
                       {string.Join(", ", table.Columns.Select(column => $"t.\"{column.Name}\""))}
                FROM {table.QualifiedName} AS t
                JOIN "hawta$selected" AS selected
                  ON selected."GroupKey" = {groupExpression}
                ORDER BY "hawta$group", t."{grouping.OrderColumn}" NULLS LAST,
                         t."{BookkeepingColumns.PrimaryKey}"
                """;
            AddParameters(command, groupKeys.Cast<object?>().ToArray());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var values = new Dictionary<string, object?>(table.Columns.Count);
                for (var index = 0; index < table.Columns.Count; index++)
                {
                    var value = reader.GetValue(index + 8);
                    values[table.Columns[index].Name] = value is DBNull ? null : value;
                }

                var groupKey = reader.GetString(0);
                var row = new DirtyRow(
                    PrimaryKey: reader.GetString(1),
                    CapturedLastModified: DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc),
                    Deleted: reader.GetBoolean(3),
                    ReplicationStamp: reader.IsDBNull(4) ? null : reader.GetString(4),
                    Values: values,
                    SourceScope: reader.IsDBNull(5) ? null : reader.GetString(5),
                    ReplicatedAt: reader.IsDBNull(6)
                        ? null
                        : DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc));
                groups[groupKey].Add(new ReplicationGroupSourceRow(row, Dirty: reader.GetBoolean(7)));
            }
        }

        return new ReplicationGroupPage(
            groupKeys.Select(key => new ReplicationGroup(key, groups[key])).ToList(),
            hasMore,
            groupKeys[^1]);
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
            SELECT "{BookkeepingColumns.PrimaryKey}", "{BookkeepingColumns.ReplicationModified}",
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
    /// Records a successful push: stamps the CAPTURED <c>_ReplicationModified</c> as the watermark and
    /// the replication stamp in the same statement (they can never drift apart), and clears the
    /// failure ledger. If a merge bumped the row mid-flight, captured &lt; current
    /// <c>_ReplicationModified</c> → the row remains dirty and is re-pushed next cycle.
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
              AND "{BookkeepingColumns.ReplicationModified}" = ?
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
              AND target."{BookkeepingColumns.ReplicationModified}" = outcome."CapturedLastModified"
              AND (target."{BookkeepingColumns.LastReplicationDate}" < target."{BookkeepingColumns.ReplicationModified}"
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

/// <summary>A dirty row loaded for replication, with its <c>_ReplicationModified</c> captured at load time.</summary>
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
    string? SourceScope = null,
    DateTime? ReplicatedAt = null);

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

internal sealed record ReplicationGroupSourceRow(DirtyRow Row, bool Dirty);

internal sealed record ReplicationGroup(
    string GroupKey,
    IReadOnlyList<ReplicationGroupSourceRow> Rows)
{
    public IReadOnlyList<DirtyRow> LiveRows => Rows
        .Where(item => !item.Row.Deleted)
        .Select(item => item.Row)
        .ToList();

    public IReadOnlyList<DirtyRow> DirtyRows => Rows
        .Where(item => item.Dirty)
        .Select(item => item.Row)
        .ToList();
}

internal sealed record ReplicationGroupPage(
    IReadOnlyList<ReplicationGroup> Groups,
    bool HasMore,
    string? LastGroupKey);
