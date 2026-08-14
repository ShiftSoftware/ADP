using System.Globalization;
using System.Text.RegularExpressions;
using DuckDB.NET.Data;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// Publishes the read tier: per-table ZSTD parquet for tables that changed since the last
/// publish, plus a tiny views-shim DuckDB file (<c>{SnapshotName}-{ts}.duckdb</c>, KBs) whose
/// <c>data.*</c> views compose the newest parquet per table — consumers keep the
/// resolve-newest contract and upload cost is proportional to changed data, never database
/// size.
///
/// <para><b>The shim is the atomic commit</b>: parquet lands first under new pinned names
/// (each written to a <c>.staging</c> name and renamed), the shim is built at a
/// <c>.staging</c> name and renamed <b>last</b> — a consumer can never observe a
/// half-published set. Retention keeps <see cref="SnapshotPublishOptions.KeepShims"/> shims
/// and deletes parquet no on-disk shim references (a delete-skipped shim a consumer still
/// holds open keeps protecting its parquet until it can actually be removed).</para>
///
/// <para><b>Change detection</b> is a per-table signature — row count plus an
/// order-independent XOR aggregate of a per-row state hash over <c>_PrimaryKey</c>,
/// <c>_RowHash</c>, and every bookkeeping column — compared against the manifest embedded in
/// the newest shim (<c>meta.publish_manifest</c>). The hash covers replication state, not
/// just content: the published set doubles as the DR seed, so pump progress (which never
/// bumps <c>_LastModified</c>) must re-export too. A per-row hash is deliberate — aggregate
/// shortcuts like <c>MAX(_LastModified)</c> are not injective (a stamp can legitimately land
/// below a future-pinned MAX; sums can alias) and were shown to skip real changes under
/// clock skew. The manifest lives in the shim — not the write DB — so publish state survives
/// a write-DB rebuild and the publish directory is self-describing.</para>
///
/// <para>Callers hold the write gate. The publisher owns its directory exclusively (one
/// directory per published snapshot); if shims of another snapshot are detected, parquet
/// cleanup is skipped rather than guessed at.</para>
/// </summary>
public static class SnapshotPublisher
{
    /// <summary>Timestamp format embedded in published file names; lexicographic order == chronological.</summary>
    internal const string TimestampFormat = "yyyyMMddHHmmssfff";

    /// <summary>Any shim-shaped file, regardless of snapshot name — used to detect foreign snapshots sharing the directory.</summary>
    private static readonly Regex AnyShimShape = new(@"^.+-[0-9]{17}\.duckdb$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Published parquet name shape; retention never touches files outside it (ad-hoc parquet in
    /// the directory survives). The named groups let <see cref="SnapshotExporter"/> recover a
    /// file's table and publish stamp without re-deriving the naming rule.
    /// </summary>
    internal static readonly Regex PublishedParquetShape =
        new(@"^(?<table>[A-Za-z][A-Za-z0-9_]*)-(?<stamp>[0-9]{17})\.parquet$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static SnapshotPublishResult Publish(SnapshotStore store, SnapshotPublishOptions options)
    {
        options.Validate();
        var startedAt = DateTime.UtcNow;

        try
        {
            // Everything filesystem-touching sits inside the try: the share being offline is
            // the single most likely production failure and must land in PublishRuns.
            Directory.CreateDirectory(options.PublishDirectory);

            // Leftover .staging files (and their DuckDB .wal sidecars) can only come from a
            // crashed prior publish (the write gate serializes publishers); clear them first.
            foreach (var pattern in new[] { "*.staging", "*.staging.wal" })
                foreach (var stale in Directory.EnumerateFiles(options.PublishDirectory, pattern))
                    TryDelete(stale);

            var result = PublishCore(store, options);

            if (result.Status == SnapshotPublishStatus.Published)
                InsertRunRecord(store, options, result, startedAt, "Published", error: null);

            return result;
        }
        catch (Exception exception)
        {
            // The run record IS the alarm surface — a crashed publish must be visible, not absent.
            try
            {
                InsertRunRecord(store, options,
                    new SnapshotPublishResult(startedAt.ToString(TimestampFormat, CultureInfo.InvariantCulture),
                        SnapshotPublishStatus.Published, null, [], [], 0, 0, 0, false),
                    startedAt, "Failed:Exception", exception.Message);
            }
            catch { /* recording must never mask the original failure */ }

            throw;
        }
    }

    private static SnapshotPublishResult PublishCore(SnapshotStore store, SnapshotPublishOptions options)
    {
        // Baseline = the manifest of the newest shim. Unreadable manifest (older layout,
        // corrupt file) degrades to a full re-export — never to a wrong publish.
        var previousShim = PublishedSnapshot.ResolveNewest(options.PublishDirectory, options.SnapshotName);
        var baseline = new Dictionary<string, PublishedTableManifest>(StringComparer.OrdinalIgnoreCase);
        if (previousShim is not null)
        {
            try
            {
                using var previous = PublishedSnapshot.Open(previousShim);
                foreach (var entry in previous.ReadManifest())
                    baseline[entry.Table] = entry;
            }
            catch
            {
                baseline.Clear();
            }
        }

        // The publish stamp names every file of this run; strictly above the previous shim's
        // stamp so resolve-newest (ordinal name sort) can never tie or go backward.
        var stamp = DateTime.UtcNow;
        if (previousShim is not null)
        {
            var previousStamp = ParseStamp(Path.GetFileName(previousShim), options.SnapshotName);
            if (previousStamp is not null && stamp <= previousStamp.Value)
                stamp = previousStamp.Value.AddMilliseconds(1);
        }
        var publishId = stamp.ToString(TimestampFormat, CultureInfo.InvariantCulture);

        var exported = new List<string>();
        var reused = new List<string>();
        var manifest = new List<PublishedTableManifest>();

        foreach (var table in options.Tables)
        {
            var signature = ReadSignature(store, table);
            var baselineEntry = baseline.GetValueOrDefault(table.Name);

            // Reuse needs more than name equality: the baseline parquet must still be a
            // bare in-directory filename AND readable with the expected row count (a torn
            // file from a crash would otherwise be re-referenced by every future shim).
            var upToDate =
                !options.Force
                && baselineEntry is not null
                && baselineEntry.RowCount == signature.RowCount
                && baselineEntry.StateHash == signature.StateHash
                && baselineEntry.ParquetFile == Path.GetFileName(baselineEntry.ParquetFile)
                && ParquetIsIntact(store, Path.Combine(options.PublishDirectory, baselineEntry.ParquetFile), baselineEntry.RowCount);

            if (upToDate)
            {
                reused.Add(table.Name);
                manifest.Add(baselineEntry!);
                continue;
            }

            var parquetFile = $"{table.Name}-{publishId}.parquet";
            ExportParquet(store, table, options, Path.Combine(options.PublishDirectory, parquetFile));
            exported.Add(table.Name);
            manifest.Add(new PublishedTableManifest(
                table.Name, parquetFile,
                signature.RowCount, signature.StateHash, signature.MaxLastModified, ExportedAt: stamp));
        }

        if (exported.Count == 0 && previousShim is not null)
        {
            return new SnapshotPublishResult(publishId, SnapshotPublishStatus.SkippedNoChanges,
                Path.GetFileName(previousShim), exported, reused, 0, 0, 0, false);
        }

        var shimFile = $"{options.SnapshotName}-{publishId}.duckdb";
        WriteShim(Path.Combine(options.PublishDirectory, shimFile), options.SnapshotName, publishId, stamp, manifest,
            options.OnBeforeShimCommit);

        var (shimsDeleted, parquetDeleted, skipped, cleanupSkipped) = ApplyRetention(options);

        return new SnapshotPublishResult(publishId, SnapshotPublishStatus.Published, shimFile,
            exported, reused, shimsDeleted, parquetDeleted, skipped, cleanupSkipped);
    }

    // ---- Change signature --------------------------------------------------------------------

    private sealed record TableSignature(long RowCount, string StateHash, DateTime? MaxLastModified);

    /// <summary>
    /// The per-row state expression the XOR aggregate runs over: key, content hash, and every
    /// bookkeeping column, NULL-safe and field-separated. Any change to any row's exported
    /// state — content, tombstone, watermark, stamp, failure ledger — changes the aggregate.
    /// Keyed by <c>_PrimaryKey</c>, so equal states on different rows can never cancel.
    /// </summary>
    private static string StateHashExpression()
    {
        var parts = BookkeepingColumns.All
            .Select(c => $"coalesce(CAST(\"{c}\" AS VARCHAR), chr(0))");
        return $"coalesce(CAST(bit_xor(hash(concat_ws(chr(31), {string.Join(", ", parts)}))) AS VARCHAR), '0')";
    }

    private static TableSignature ReadSignature(SnapshotStore store, SnapshotTableDefinition table)
    {
        using var command = store.Connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT count(*), {StateHashExpression()}, max("{BookkeepingColumns.LastModified}")
            FROM {table.QualifiedName}
            """;

        using var reader = command.ExecuteReader();
        reader.Read();
        return new TableSignature(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : SnapshotStore.AsUtc(reader.GetDateTime(2)));
    }

    /// <summary>Footer-only probe: the file parses as parquet and carries the expected row count.</summary>
    internal static bool ParquetIsIntact(SnapshotStore store, string parquetPath, long expectedRows)
    {
        try
        {
            var rows = store.ExecuteScalar(
                $"SELECT num_rows FROM parquet_file_metadata('{EscapePath(parquetPath)}')");
            return Convert.ToInt64(rows) == expectedRows;
        }
        catch
        {
            return false;
        }
    }

    // ---- Export ------------------------------------------------------------------------------

    private static void ExportParquet(SnapshotStore store, SnapshotTableDefinition table, SnapshotPublishOptions options, string parquetPath)
    {
        var sortColumns = options.SortColumnsFor(table);
        var orderBy = string.Join(", ", sortColumns.Select(c => $"\"{c}\""));
        var columns = string.Join(", ",
            table.Columns.Select(c => $"\"{c.Name}\"").Concat(BookkeepingColumns.All.Select(c => $"\"{c}\"")));

        var stagingPath = parquetPath + ".staging";
        store.Execute(
            $"""
            COPY (SELECT {columns} FROM {table.QualifiedName} ORDER BY {orderBy})
            TO '{EscapePath(stagingPath)}' (FORMAT parquet, COMPRESSION zstd)
            """);
        // overwrite: a colliding final name can only be an unreferenced orphan from a crashed
        // run (referenced names always carry stamps older than this publish's).
        File.Move(stagingPath, parquetPath, overwrite: true);
    }

    // ---- The shim ----------------------------------------------------------------------------

    private static void WriteShim(string shimPath, string snapshotName, string publishId, DateTime publishedAt,
        IReadOnlyList<PublishedTableManifest> manifest, Action? onBeforeShimCommit)
    {
        var stagingPath = shimPath + ".staging";

        // A stale staging DB or its WAL sidecar here would be replayed into the new shim.
        if (File.Exists(stagingPath)) File.Delete(stagingPath);
        if (File.Exists(stagingPath + ".wal")) File.Delete(stagingPath + ".wal");

        using (var connection = new DuckDBConnection("Data Source=:memory:"))
        {
            connection.Open();

            // The shim carries only view definitions and the manifest, so it is created with
            // DuckDB's minimum block size: at the 256 KB default the empty-file floor is
            // ~780 KB — bigger than small tables' parquet, which reads as "the shim holds
            // data" when it must never. ATTACH is the only way to pass BLOCK_SIZE.
            Execute(connection, $"ATTACH '{EscapePath(stagingPath)}' AS shim (BLOCK_SIZE 16384)");

            // CREATE VIEW binds read_parquet immediately, so the bare relative filenames in the
            // view SQL must resolve NOW — the setting is session-scoped and is not baked into
            // the stored view text (consumers set their own via PublishedSnapshot.Open).
            var directory = Path.GetDirectoryName(Path.GetFullPath(shimPath))!;
            Execute(connection, $"SET file_search_path = '{EscapePath(directory)}'");

            Execute(connection, "CREATE SCHEMA shim.data");
            Execute(connection, "CREATE SCHEMA shim.meta");

            Execute(connection,
                """
                CREATE TABLE shim.meta.publish_info (
                    "PublishId" VARCHAR NOT NULL,
                    "SnapshotName" VARCHAR NOT NULL,
                    "PublishedAt" TIMESTAMP NOT NULL,
                    "SchemaVersion" INTEGER NOT NULL,
                    "PackageVersion" VARCHAR NOT NULL
                )
                """);
            Execute(connection,
                "INSERT INTO shim.meta.publish_info VALUES (?, ?, ?, ?, ?)",
                publishId, snapshotName, publishedAt, SnapshotStore.CurrentSchemaVersion,
                typeof(SnapshotPublisher).Assembly.GetName().Version?.ToString() ?? "0.0.0");

            Execute(connection,
                """
                CREATE TABLE shim.meta.publish_manifest (
                    "Table" VARCHAR NOT NULL,
                    "ParquetFile" VARCHAR NOT NULL,
                    "RowCount" BIGINT NOT NULL,
                    "StateHash" VARCHAR NOT NULL,
                    "MaxLastModified" TIMESTAMP,
                    "ExportedAt" TIMESTAMP NOT NULL
                )
                """);

            foreach (var entry in manifest)
            {
                Execute(connection,
                    "INSERT INTO shim.meta.publish_manifest VALUES (?, ?, ?, ?, ?, ?)",
                    entry.Table, entry.ParquetFile, entry.RowCount, entry.StateHash,
                    entry.MaxLastModified, entry.ExportedAt);

                // Bare relative filename: the set stays relocatable (local dir, SMB, abfss later).
                // PublishedSnapshot.Open sets file_search_path to the shim's directory; plain
                // duckdb.exe consumers open the shim from inside the directory (CWD resolution).
                Execute(connection,
                    $"""CREATE VIEW shim.data."{entry.Table}" AS SELECT * FROM read_parquet('{EscapePath(entry.ParquetFile)}')""");
            }

            Execute(connection, "DETACH shim");   // checkpoints and removes the WAL
        }

        if (File.Exists(stagingPath + ".wal")) File.Delete(stagingPath + ".wal");

        // Deliberately after the connection is closed and its WAL removed: drills injected
        // here observe a complete staging shim while consumers still resolve the prior commit.
        onBeforeShimCommit?.Invoke();

        // The atomic commit: the fully-built shim appears under its final name in one rename.
        File.Move(stagingPath, shimPath, overwrite: false);
    }

    // ---- Retention ---------------------------------------------------------------------------

    private static (int ShimsDeleted, int ParquetDeleted, int Skipped, bool CleanupSkipped) ApplyRetention(SnapshotPublishOptions options)
    {
        var shims = PublishedSnapshot.ListShims(options.PublishDirectory, options.SnapshotName);

        var shimsDeleted = 0;
        var skipped = 0;
        foreach (var old in shims.Skip(options.KeepShims))
        {
            if (TryDelete(old, options.RetentionDelete)) shimsDeleted++;
            else skipped++;
        }

        // Parquet cleanup assumes exclusive directory ownership. A shim-shaped file under
        // another snapshot name means a second publisher shares this directory — never guess
        // at its references; skip the sweep and surface it.
        var ownPattern = PublishedSnapshot.ShimPattern(options.SnapshotName);
        var foreignShimPresent = Directory.EnumerateFiles(options.PublishDirectory, "*.duckdb")
            .Select(Path.GetFileName)
            .Any(name => name is not null && AnyShimShape.IsMatch(name) && !ownPattern.IsMatch(name));
        if (foreignShimPresent)
            return (shimsDeleted, 0, skipped, CleanupSkipped: true);

        // The referenced set comes from EVERY shim still on disk — kept ones AND those whose
        // delete was just skipped because a consumer holds them open. A held-open shim must
        // keep its parquet alive (its reader is mid-session); the set shrinks by itself once
        // the consumer closes and the shim can actually be deleted. Only delete when every
        // manifest is readable and sane; guessing risks breaking a consumer mid-query.
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var shim in PublishedSnapshot.ListShims(options.PublishDirectory, options.SnapshotName))
        {
            try
            {
                using var snapshot = PublishedSnapshot.Open(shim);
                foreach (var entry in snapshot.ReadManifest())
                {
                    if (entry.ParquetFile != Path.GetFileName(entry.ParquetFile))
                        return (shimsDeleted, 0, skipped, CleanupSkipped: true);
                    referenced.Add(entry.ParquetFile);
                }
            }
            catch
            {
                return (shimsDeleted, 0, skipped, CleanupSkipped: true);
            }
        }

        var parquetDeleted = 0;
        foreach (var parquet in Directory.EnumerateFiles(options.PublishDirectory, "*.parquet"))
        {
            var name = Path.GetFileName(parquet);
            if (!PublishedParquetShape.IsMatch(name) || referenced.Contains(name))
                continue;
            if (TryDelete(parquet, options.RetentionDelete)) parquetDeleted++;
            else skipped++;
        }

        return (shimsDeleted, parquetDeleted, skipped, CleanupSkipped: false);
    }

    // ---- Plumbing ----------------------------------------------------------------------------

    internal static DateTime? ParseStamp(string fileName, string snapshotName)
    {
        var match = PublishedSnapshot.ShimPattern(snapshotName).Match(fileName);
        if (!match.Success)
            return null;
        return DateTime.TryParseExact(match.Groups[1].Value, TimestampFormat,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var stamp)
            ? DateTime.SpecifyKind(stamp, DateTimeKind.Utc)
            : null;
    }

    private static string EscapePath(string path) => path.Replace("'", "''");

    private static bool TryDelete(string path, Func<string, bool>? retentionDelete = null)
    {
        try
        {
            if (retentionDelete is not null)
                return retentionDelete(path);
            File.Delete(path);
            return true;
        }
        catch (IOException)
        {
            return false; // in use by a consumer; the next publish's retention pass retries
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void Execute(DuckDBConnection connection, string sql, params object?[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var value in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
        command.ExecuteNonQuery();
    }

    private static void InsertRunRecord(SnapshotStore store, SnapshotPublishOptions options,
        SnapshotPublishResult result, DateTime startedAt, string status, string? error)
    {
        store.Execute(
            """
            INSERT INTO meta.PublishRuns
            ("PublishId", "SnapshotName", "StartedAt", "FinishedAt",
             "TablesExported", "TablesReused", "ShimsDeleted", "ParquetFilesDeleted", "Status", "Error")
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            result.PublishId, options.SnapshotName, startedAt, DateTime.UtcNow,
            result.TablesExported.Count, result.TablesReused.Count,
            result.ShimsDeleted, result.ParquetFilesDeleted, status, error);
    }
}
