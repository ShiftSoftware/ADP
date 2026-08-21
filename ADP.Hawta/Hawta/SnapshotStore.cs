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
    // v5 collapsed meta.SourceOwnership from one row per data row to one row per (table,
    // scope). A v4 published set is still a valid cold-start seed — ownership was never
    // published; it derives from the manifest's source catalog — so the rebuild floor stays 4.
    public const int CurrentSchemaVersion = 5;
    internal const int OldestRebuildableSchemaVersion = 4;

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
                "ManifestsDeleted" INTEGER NOT NULL DEFAULT 0,
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

        // How far each Cosmos-reading source has already ingested. Additive on exactly the same
        // terms as meta.SourceFileStamps above — this CREATE block has no version gate, so an
        // existing write DB gains the table empty on the next open and no schema-version bump
        // (and therefore no forced cold-start rebuild) is needed.
        //
        // The UPSTREAM cursor. Not to be confused with meta.ChangeSequenceState below, which
        // allocates the DOWNSTREAM one: this records what we have already read out of a container,
        // that records what consumers have yet to read out of us.
        Execute(
            """
            CREATE TABLE IF NOT EXISTS meta.SourceCosmosCursors (
                "SourceKey" VARCHAR NOT NULL PRIMARY KEY,
                "Database" VARCHAR NOT NULL,
                "Container" VARCHAR NOT NULL,
                "ContinuationToken" VARCHAR NOT NULL,
                "IngestVersion" VARCHAR,
                "StampedAtUtc" TIMESTAMP NOT NULL
            )
            """);

        // Store-wide allocator for the downstream change cursor. It is a table rather than a
        // DuckDB SEQUENCE because its value must roll back with a failed merge and be reconciled
        // to the maximum sequence restored from a published manifest.
        Execute(
            """
            CREATE TABLE IF NOT EXISTS meta.ChangeSequenceState (
                "Singleton" BOOLEAN NOT NULL PRIMARY KEY,
                "LastValue" BIGINT NOT NULL
            )
            """);
        Execute(
            """
            INSERT INTO meta.ChangeSequenceState
            SELECT true, 0
            WHERE NOT EXISTS (SELECT 1 FROM meta.ChangeSequenceState WHERE "Singleton" = true)
            """);

        // Internal ownership preserves source adoption semantics without leaking source keys onto
        // every public row. One row per (table, scope), never per key: the registry guarantees
        // each table/scope has exactly one owning source, so per-key rows would only repeat this
        // map — measured at 1.5M rows and 134 MiB on a deployment with one large catalog table.
        // "SourceScope" stores chr(0) for a table's single unscoped universe, because DuckDB
        // refuses NULL in any primary-key component; the NULL<->sentinel translation never
        // leaves this store's read/write methods. The scope column is case-SENSITIVE, exactly
        // like the merge's IS NOT DISTINCT FROM — the registry's case-insensitive uniqueness
        // check is the stricter construction-time guard. A rebuild recreates the map from the
        // manifest's table/scope catalog before ingestion resumes.
        Execute(
            """
            CREATE TABLE IF NOT EXISTS meta.SourceOwnership (
                "TableName" VARCHAR NOT NULL,
                "SourceScope" VARCHAR NOT NULL,
                "SourceKey" VARCHAR NOT NULL,
                PRIMARY KEY ("TableName", "SourceScope")
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

    // ---- Cosmos read cursors ------------------------------------------------------------------

    /// <summary>
    /// How far <paramref name="sourceKey"/> has already ingested from its Cosmos container, or
    /// null if it has never merged one. Null is the expensive answer here, not the safe one: it
    /// means the next read starts from the beginning of the container.
    /// </summary>
    public SourceCosmosCursor? ReadSourceCosmosCursor(string sourceKey)
    {
        using var command = Connection.CreateCommand();
        command.CommandText =
            """
            SELECT "Database", "Container", "ContinuationToken", "IngestVersion", "StampedAtUtc"
            FROM meta.SourceCosmosCursors WHERE "SourceKey" = ?
            """;
        var parameter = command.CreateParameter();
        parameter.Value = sourceKey;
        command.Parameters.Add(parameter);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        return new SourceCosmosCursor(
            sourceKey,
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            AsUtc(reader.GetDateTime(4)));
    }

    /// <summary>
    /// Records a Cosmos source's resume position. Called <b>only after</b> the documents it
    /// describes have merged — writing it first would let a crash between the two skip documents
    /// that are in no table, which is the one failure direction this design cannot absorb.
    /// </summary>
    public void WriteSourceCosmosCursor(SourceCosmosCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);

        Execute("DELETE FROM meta.SourceCosmosCursors WHERE \"SourceKey\" = ?", cursor.SourceKey);
        Execute(
            "INSERT INTO meta.SourceCosmosCursors VALUES (?, ?, ?, ?, ?, ?)",
            cursor.SourceKey, cursor.Database, cursor.Container, cursor.ContinuationToken,
            cursor.IngestVersion, cursor.StampedAtUtc);
    }

    /// <summary>
    /// Every cursor, for publishing alongside the set it describes. Read at publish time, when no
    /// merge is in flight, so what this returns is exactly consistent with the parquet about to be
    /// committed.
    /// </summary>
    public IReadOnlyList<SourceCosmosCursor> ReadAllSourceCosmosCursors()
    {
        using var command = Connection.CreateCommand();
        command.CommandText =
            """
            SELECT "SourceKey", "Database", "Container", "ContinuationToken", "IngestVersion", "StampedAtUtc"
            FROM meta.SourceCosmosCursors ORDER BY "SourceKey"
            """;

        using var reader = command.ExecuteReader();
        var cursors = new List<SourceCosmosCursor>();
        while (reader.Read())
        {
            cursors.Add(new SourceCosmosCursor(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                AsUtc(reader.GetDateTime(5))));
        }

        return cursors;
    }

    /// <summary>Drops a source's cursor. Its next read starts from the beginning of the container.</summary>
    public void ClearSourceCosmosCursor(string sourceKey) =>
        Execute("DELETE FROM meta.SourceCosmosCursors WHERE \"SourceKey\" = ?", sourceKey);

    /// <summary>
    /// The newest run record per source, for publishing alongside the set it describes. Read at
    /// publish time, when no merge is in flight.
    ///
    /// <para>A source that has never run has no entry, and that absence is the point: a source
    /// crashing before it can stage anything writes no run record at all, so "never ran" and
    /// "failing every tick" both present as an absent row — which a consumer must be able to tell
    /// from a source that ran and found nothing.</para>
    /// </summary>
    public IReadOnlyList<SourceRunSummary> ReadLatestRunPerSource()
    {
        using var command = Connection.CreateCommand();
        command.CommandText =
            """
            SELECT "Source", "TargetTable", "StartedAt", "FinishedAt", "Status",
                   "RowsStaged", "RowsInserted", "RowsUpdated", "RowsTombstoned"
            FROM meta.SyncRuns
            QUALIFY row_number() OVER (PARTITION BY "Source" ORDER BY "StartedAt" DESC, "RunId" DESC) = 1
            ORDER BY "Source"
            """;

        using var reader = command.ExecuteReader();
        var runs = new List<SourceRunSummary>();
        while (reader.Read())
        {
            runs.Add(new SourceRunSummary(
                reader.GetString(0),
                reader.GetString(1),
                AsUtc(reader.GetDateTime(2)),
                reader.IsDBNull(3) ? null : AsUtc(reader.GetDateTime(3)),
                reader.GetString(4),
                reader.GetInt64(5),
                reader.GetInt64(6),
                reader.GetInt64(7),
                reader.GetInt64(8)));
        }

        return runs;
    }

    /// <summary>
    /// Creates the family's consolidated table (source columns + bookkeeping) if missing.
    /// Additive schema drift is applied automatically: source columns present in the
    /// definition but missing on an existing table are added via <c>ALTER TABLE ADD COLUMN</c>
    /// (as nullable), so shipping a widened definition against a live write DB just works.
    ///
    /// <para>Snapshot tables declare no PRIMARY KEY, deliberately. Key uniqueness is the
    /// merge's contract, not the storage engine's: staging with a duplicate or NULL
    /// <c>_PrimaryKey</c> is refused before any mutation, inserts are anti-joined against
    /// resident keys, a rebuild refuses a seed containing duplicates, and the publish
    /// contract refuses to export a store that carries them. The index a
    /// primary key would add costs real money at scale — measured on a deployment with
    /// ~1.4M rows in one table, it was ~105 MiB of the write database and most of that
    /// table's load time — and the only per-key reads in the engine are the replication
    /// pump's bookkeeping writes, whose scans stay far cheaper than the remote work each
    /// one follows. A table created by an older package keeps its index until the next
    /// schema rebuild; that is harmless, only larger.</para>
    /// </summary>
    public void EnsureTable(SnapshotTableDefinition table)
    {
        var sourceColumns = string.Join(",\n    ", table.Columns.Select(c => $"\"{c.Name}\" {c.DuckDbType}"));

        Execute(
            $"""
            CREATE TABLE IF NOT EXISTS {table.QualifiedName} (
                {sourceColumns},
                {BookkeepingColumns.TableDdl}
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

    // ---- Source ownership ---------------------------------------------------------------------
    // One map row per (table, scope). The SQL below writes chr(0) in place of a NULL scope —
    // DuckDB refuses NULL in a primary-key component — and callers only ever pass and receive
    // the real nullable scope. Scope matching is case-sensitive SQL equality, deliberately in
    // agreement with the merge's IS NOT DISTINCT FROM; any caller comparing the returned KEY in
    // C# must use ordinal comparison for the same reason.

    /// <summary>
    /// The source key owning one (table, scope), or null when no merge or rebuild has ever
    /// recorded an owner for that scope.
    /// </summary>
    internal string? ReadSourceOwner(string tableName, string? sourceScope)
    {
        var owner = ExecuteScalar(
            """
            SELECT "SourceKey" FROM meta.SourceOwnership
            WHERE "TableName" = ? AND "SourceScope" = coalesce(?, chr(0))
            """,
            tableName, sourceScope);
        return owner is null or DBNull ? null : (string)owner;
    }

    /// <summary>Records a scope's owner, replacing any previous owner of the same (table, scope).</summary>
    internal void WriteSourceOwner(string tableName, string? sourceScope, string sourceKey)
    {
        Execute(
            """
            DELETE FROM meta.SourceOwnership
            WHERE "TableName" = ? AND "SourceScope" = coalesce(?, chr(0))
            """,
            tableName, sourceScope);
        Execute(
            """
            INSERT INTO meta.SourceOwnership ("TableName", "SourceScope", "SourceKey")
            VALUES (?, coalesce(?, chr(0)), ?)
            """,
            tableName, sourceScope, sourceKey);
    }

    /// <summary>Recreates a table's ownership map from one validated manifest source catalog.</summary>
    internal void RestoreSourceOwnership(
        SnapshotTableDefinition table,
        IReadOnlyList<PublishedSourceCatalogEntry> sourceCatalog)
    {
        // Refused rather than last-writer-wins: a catalog attributing one scope twice would
        // otherwise silently drop an owner here and only fail later, at the publish contract,
        // where nothing points back at the seed. Default string equality is ordinal and
        // case-sensitive — the same comparison the map's primary key applies.
        var duplicate = sourceCatalog
            .GroupBy(source => source.SourceScope)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidDataException(
                $"Table '{table.Name}' manifest catalog attributes scope '{duplicate.Key ?? "<null>"}' to " +
                $"{duplicate.Count()} sources. Every row scope must have exactly one source owner.");

        Execute("DELETE FROM meta.SourceOwnership WHERE \"TableName\" = ?", table.Name);
        foreach (var source in sourceCatalog)
            WriteSourceOwner(table.Name, source.SourceScope, source.SourceKey);

        // Coverage: every scope present on the restored rows must have an owner in the map just
        // written. The per-key shape verified this by counting rows; scopes are what is stored
        // now, so scopes are what is checked.
        var unowned = Convert.ToInt64(ExecuteScalar(
            $"""
            SELECT count(*)
            FROM (SELECT DISTINCT coalesce("{BookkeepingColumns.SourceScope}", chr(0)) AS "Scope"
                  FROM {table.QualifiedName}) AS scopes
            LEFT JOIN meta.SourceOwnership AS map
              ON map."TableName" = ? AND map."SourceScope" = scopes."Scope"
            WHERE map."SourceKey" IS NULL
            """,
            table.Name));
        if (unowned > 0)
            throw new InvalidDataException(
                $"Table '{table.Name}' restored rows under {unowned} scope(s) its manifest catalog does not " +
                "attribute. Every row scope must have exactly one source owner.");
    }

    /// <summary>Moves the allocator above every row restored from published parquet.</summary>
    internal void ReconcileChangeSequence(
        IReadOnlyList<SnapshotTableDefinition> tables,
        long publishedHighWatermark = 0)
    {
        var maximum = publishedHighWatermark;
        foreach (var table in tables)
        {
            var value = ExecuteScalar(
                $"SELECT max(\"{BookkeepingColumns.ChangeSequence}\") FROM {table.QualifiedName}");
            if (value is not null and not DBNull)
                maximum = Math.Max(maximum, Convert.ToInt64(value));
        }

        var current = Convert.ToInt64(ExecuteScalar(
            "SELECT \"LastValue\" FROM meta.ChangeSequenceState WHERE \"Singleton\" = true"));
        if (maximum > current)
            Execute("UPDATE meta.ChangeSequenceState SET \"LastValue\" = ? WHERE \"Singleton\" = true", maximum);
    }

    internal long ReadChangeSequenceHighWatermark() => Convert.ToInt64(ExecuteScalar(
        "SELECT \"LastValue\" FROM meta.ChangeSequenceState WHERE \"Singleton\" = true"));

    /// <summary>Reserves a contiguous, transactionally durable range and returns its first value.</summary>
    internal long ReserveChangeSequences(long count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "A sequence reservation must be positive.");

        var last = Convert.ToInt64(ExecuteScalar(
            "SELECT \"LastValue\" FROM meta.ChangeSequenceState WHERE \"Singleton\" = true"));
        var nextLast = checked(last + count);
        Execute("UPDATE meta.ChangeSequenceState SET \"LastValue\" = ? WHERE \"Singleton\" = true", nextLast);
        return checked(last + 1);
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
