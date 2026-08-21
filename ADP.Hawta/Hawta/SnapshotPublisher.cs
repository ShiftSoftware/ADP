using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// Publishes the read tier: per-table ZSTD parquet for tables that changed since the last
/// publish, plus a small JSON manifest (<c>{SnapshotName}-{ts}.json</c>) naming the newest
/// parquet per table — consumers keep the resolve-newest contract and upload cost is
/// proportional to changed data, never database size.
///
/// <para><b>The manifest is the atomic commit</b>: parquet lands first under new pinned names
/// (each written to a <c>.staging</c> name and renamed), the manifest is written to a
/// <c>.staging</c> name and renamed <b>last</b> — a consumer can never observe a
/// half-published set. Retention keeps <see cref="SnapshotPublishOptions.KeepPublishes"/>
/// manifests and deletes parquet no on-disk manifest references (a delete-skipped manifest a
/// consumer still holds keeps protecting its parquet until it can actually be removed).</para>
///
/// <para><b>Change detection</b> is a per-table signature — row count plus an
/// order-independent XOR aggregate of a per-row state hash over <c>_PrimaryKey</c>,
/// <c>_RowHash</c>, and every bookkeeping column — compared against the previous manifest.
/// The hash covers replication state, not just content: the published set doubles as the DR
/// seed, so pump progress (which never bumps <c>_LastModified</c>) must re-export too. A
/// per-row hash is deliberate — aggregate shortcuts like <c>MAX(_LastModified)</c> are not
/// injective (a stamp can legitimately land below a future-pinned MAX; sums can alias) and
/// were shown to skip real changes under clock skew. The manifest lives beside the parquet —
/// not in the write DB — so publish state survives a write-DB rebuild and the publish
/// directory is self-describing.</para>
///
/// <para>Callers hold the write gate. The publisher owns its directory exclusively (one
/// directory per published snapshot); if manifests of another snapshot are detected, parquet
/// cleanup is skipped rather than guessed at.</para>
/// </summary>
public static class SnapshotPublisher
{
    /// <summary>Timestamp format embedded in published file names; lexicographic order == chronological.</summary>
    internal const string TimestampFormat = "yyyyMMddHHmmssfff";

    /// <summary>Suffix every artifact is built under before its atomic rename to the final name.</summary>
    internal const string StagingSuffix = ".staging";

    /// <summary>
    /// A table's folder name. One folder per table, holding immutable <c>{publishId}.parquet</c>
    /// versions — the layout consumers already read, and the only one that survives a table
    /// growing past a single file (partitioning today, a Delta directory later).
    /// </summary>
    internal static string FolderFor(SnapshotTableDefinition table) => table.Name;

    /// <summary>
    /// Published version-file shape, <b>within a table's folder</b>. Retention never touches
    /// files outside it, so ad-hoc parquet dropped into the publish tier survives. The named
    /// group lets a caller recover a file's publish stamp without re-deriving the naming rule.
    /// </summary>
    internal static readonly Regex PublishedParquetShape =
        new(@"^(?<stamp>[0-9]{17})\.parquet$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static SnapshotPublishResult Publish(SnapshotStore store, SnapshotPublishOptions options)
    {
        options.Validate();
        var startedAt = DateTime.UtcNow;

        try
        {
            // Everything storage-touching sits inside the try: the destination being offline is
            // the single most likely production failure and must land in PublishRuns.
            var publishStore = options.ResolveStore();

            // Loud, and BEFORE anything reads. Every listing below reports an unreachable store as
            // an empty one, and "empty" here means "nothing has ever been published" — which sends
            // the publisher down the full-re-export path and skips the stamp-monotonicity clamp,
            // permanently breaking resolve-newest ordering. This is the replacement for the
            // RequireLocal stop-sign that used to stand here.
            publishStore.EnsureReady();

            // Leftover .staging files can only come from a crashed prior publish (the write gate
            // serializes publishers); clear them first. The listing is recursive because table
            // folders nest one level.
            foreach (var stale in publishStore.List()
                .Where(entry => entry.RelativePath.EndsWith(StagingSuffix, StringComparison.OrdinalIgnoreCase)))
            {
                publishStore.Delete(stale.Location);
            }

            var result = PublishCore(store, options, publishStore);

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
                        SnapshotPublishStatus.Published, null, [], [], 0, 0, 0, false, []),
                    startedAt, "Failed:Exception", exception.Message);
            }
            catch { /* recording must never mask the original failure */ }

            throw;
        }
    }

    private static SnapshotPublishResult PublishCore(
        SnapshotStore store, SnapshotPublishOptions options, PublishStore publishStore)
    {
        // Baseline = the previous manifest. Unreadable manifest (older layout, corrupt file)
        // degrades to a full re-export — never to a wrong publish.
        var previousPath = PublishedSnapshot.ResolveNewest(publishStore, options.SnapshotName);
        var baseline = new Dictionary<string, PublishedTableManifest>(StringComparer.OrdinalIgnoreCase);
        if (previousPath is not null)
        {
            PublishedSnapshot? previous = null;
            try
            {
                previous = PublishedSnapshot.Read(publishStore, previousPath);
            }
            catch (SnapshotSequenceContractException)
            {
                throw;
            }
            catch
            {
                baseline.Clear();
            }

            if (previous is not null)
            {
                if (previous.SchemaVersion > SnapshotStore.CurrentSchemaVersion)
                    throw new InvalidOperationException(
                        $"Refusing to publish schema v{SnapshotStore.CurrentSchemaVersion} over newer " +
                        $"snapshot schema v{previous.SchemaVersion} in '{previousPath}'. Upgrade this publisher.");

                // An older-to-v4 transition must re-export EVERY table, including an empty one
                // whose row-count/hash signature is still 0/0. Reusing pre-v4 parquet would put
                // the rejected row contract behind a v4 catalog manifest.
                if (previous.SchemaVersion == SnapshotStore.CurrentSchemaVersion)
                {
                    foreach (var entry in previous.Tables)
                        baseline[entry.Table] = entry;
                }
            }
        }

        // The publish stamp names every file of this run; strictly above the previous manifest's
        // stamp so resolve-newest (ordinal name sort) can never tie or go backward.
        var stamp = DateTime.UtcNow;
        if (previousPath is not null)
        {
            var previousStamp = ParseStamp(PublishPath.FileName(previousPath), options.SnapshotName);
            if (previousStamp is not null && stamp <= previousStamp.Value)
                stamp = previousStamp.Value.AddMilliseconds(1);
        }
        var publishId = stamp.ToString(TimestampFormat, CultureInfo.InvariantCulture);

        var sourceCatalogs = options.Tables.ToDictionary(
            table => table.Name,
            table => BuildSourceCatalog(options, table),
            StringComparer.OrdinalIgnoreCase);
        foreach (var table in options.Tables)
            EnsureSourceVersionContract(store, table, sourceCatalogs[table.Name]);
        EnsureGlobalSequenceContract(store, options.Tables);

        var exported = new List<string>();
        var reused = new List<string>();
        var manifest = new List<PublishedTableManifest>();
        var catalogChanged = previousPath is not null && baseline.Count != options.Tables.Count;

        foreach (var table in options.Tables)
        {
            var sourceCatalog = sourceCatalogs[table.Name];
            var signature = ReadSignature(store, table);
            var baselineEntry = baseline.GetValueOrDefault(table.Name);

            if (baselineEntry is not null && !CatalogsEqual(baselineEntry.SourceCatalog, sourceCatalog))
                catalogChanged = true;

            // Reuse needs more than signature equality: the baseline's files must still be
            // readable at the expected row count (a torn file from a crash would otherwise be
            // re-referenced by every future manifest). Bare-name validation already happened
            // in PublishedSnapshot.Read.
            var upToDate =
                !options.Force
                && baselineEntry is not null
                && baselineEntry.RowCount == signature.RowCount
                && baselineEntry.StateHash == signature.StateHash
                && ParquetIsIntact(store, baselineEntry.Resolve(publishStore.Root), baselineEntry.RowCount);

            if (upToDate)
            {
                reused.Add(table.Name);
                manifest.Add(baselineEntry! with { SourceCatalog = sourceCatalog });
                continue;
            }

            // Forward-slashed and relative: the manifest is read by consumers that are not
            // necessarily on Windows, and the set has to stay relocatable.
            var parquetFile = $"{FolderFor(table)}/{publishId}.parquet";
            ExportParquet(store, table, options, publishStore, publishStore.Resolve(parquetFile));
            exported.Add(table.Name);
            manifest.Add(new PublishedTableManifest(
                Table: table.Name,
                Location: PublishedTableLocation.Parquet(parquetFile),
                PublishId: publishId,
                RowCount: signature.RowCount,
                StateHash: signature.StateHash,
                DataAsOf: signature.MaxLastModified,
                ExportedAt: stamp)
            {
                SourceCatalog = sourceCatalog,
            });
        }

        var noRows = manifest.Where(e => e.RowCount == 0).Select(e => e.Table).Order().ToList();

        if (exported.Count == 0 && previousPath is not null && !catalogChanged)
        {
            // Self-heal only: a pointer lost to a crash (or never written, on an estate published
            // before it existed) comes back on the next cycle. Rewriting it unconditionally would
            // churn its timestamp every publish cadence for no reader benefit.
            if (options.StableManifestFileName is not null
                && !publishStore.Exists(publishStore.Resolve(options.StableManifestFileName)))
            {
                RefreshStablePointer(options, publishStore, previousPath);
            }

            return new SnapshotPublishResult(publishId, SnapshotPublishStatus.SkippedNoChanges,
                PublishPath.FileName(previousPath), exported, reused, 0, 0, 0, false, noRows);
        }

        // Alphabetical, so the manifest's table order is stable across configuration changes.
        manifest.Sort((left, right) => string.Compare(left.Table, right.Table, StringComparison.OrdinalIgnoreCase));

        var manifestFile = $"{options.SnapshotName}-{publishId}{PublishedSnapshot.Extension}";
        var manifestPath = publishStore.Resolve(manifestFile);
        WriteManifest(store, manifestPath, options, publishStore, publishId, stamp, manifest, options.OnBeforeManifestCommit);

        RefreshStablePointer(options, publishStore, manifestPath);

        var retention = options.RetentionEnabled
            ? SnapshotRetention.Sweep(new SnapshotRetentionOptions
            {
                PublishDirectory = options.PublishDirectory,
                Store = publishStore,
                SnapshotName = options.SnapshotName,
                KeepPublishes = options.KeepPublishes,
                // The set just committed is protected by being referenced, not by age, so the
                // in-process sweep does not need a floor. A standalone sweeper does — see
                // SnapshotRetention's remarks.
                MinimumAge = options.RetentionMinimumAge,
                Delete = options.RetentionDelete,
            })
            : new SnapshotRetentionResult(0, 0, 0, 0, CleanupSkipped: false);

        return new SnapshotPublishResult(publishId, SnapshotPublishStatus.Published, manifestFile,
            exported, reused, retention.ManifestsDeleted, retention.ParquetFilesDeleted,
            retention.Skipped, retention.CleanupSkipped, noRows);
    }

    // ---- Change signature --------------------------------------------------------------------

    private static IReadOnlyList<PublishedSourceCatalogEntry> BuildSourceCatalog(
        SnapshotPublishOptions options,
        SnapshotTableDefinition table) =>
        [.. options.Sources
            .Where(source => source.Table.Name.Equals(table.Name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(source => source.SourceScope is null ? 0 : 1)
            .ThenBy(source => source.SourceScope, StringComparer.OrdinalIgnoreCase)
            .ThenBy(source => source.Key, StringComparer.OrdinalIgnoreCase)
            .Select(source => new PublishedSourceCatalogEntry(
                source.Key, source.SourceScope, source.RecordIdentity))];

    private static bool CatalogsEqual(
        IReadOnlyList<PublishedSourceCatalogEntry>? left,
        IReadOnlyList<PublishedSourceCatalogEntry> right) =>
        JsonSerializer.Serialize(left ?? [], PublishedSnapshot.SerializerOptions)
            .Equals(JsonSerializer.Serialize(right, PublishedSnapshot.SerializerOptions), StringComparison.Ordinal);

    private static void EnsureSourceVersionContract(
        SnapshotStore store,
        SnapshotTableDefinition table,
        IReadOnlyList<PublishedSourceCatalogEntry> sourceCatalog)
    {
        var invalid = Convert.ToInt64(store.ExecuteScalar(
            $"""
            SELECT count(*)
            FROM {table.QualifiedName}
            WHERE "{BookkeepingColumns.ChangeSequence}" IS NULL
               OR "{BookkeepingColumns.ChangeSequence}" <= 0
               OR "{BookkeepingColumns.ChangeRecordedAt}" IS NULL
            """));

        if (invalid > 0)
            throw new InvalidOperationException(
                $"Table '{table.Name}' has {invalid} row(s) without complete durable change-sequence metadata.");

        long attributed = 0;
        foreach (var source in sourceCatalog)
        {
            var sourceRows = Convert.ToInt64(store.ExecuteScalar(
                $"SELECT count(*) FROM {table.QualifiedName} " +
                $"WHERE \"{BookkeepingColumns.SourceScope}\" IS NOT DISTINCT FROM ?",
                source.SourceScope));
            attributed += sourceRows;

            // Ownership is a per-scope map, not a per-row join. The key comparison is ordinal
            // (case-sensitive) so it can never disagree with the map's primary key or the
            // IS NOT DISTINCT FROM above. A missing map entry is acceptable only while the
            // scope holds no rows at all — a declared source that has never merged.
            var owner = store.ReadSourceOwner(table.Name, source.SourceScope);
            var ownershipMismatch = owner is null
                ? sourceRows > 0
                : !string.Equals(owner, source.SourceKey, StringComparison.Ordinal);
            if (ownershipMismatch)
                throw new InvalidOperationException(
                    $"Table '{table.Name}' scope '{source.SourceScope ?? "<null>"}' is " +
                    $"not internally owned by catalog source '{source.SourceKey}'. Reingest that source before publishing.");
        }

        var rowCount = Convert.ToInt64(store.ExecuteScalar($"SELECT count(*) FROM {table.QualifiedName}"));
        if (attributed != rowCount)
            throw new InvalidOperationException(
                $"Table '{table.Name}' has {rowCount - attributed} row(s) whose _SourceScope is not owned by exactly one source catalog entry.");

        // No orphaned-ownership check remains: a map row cannot orphan the way per-key rows
        // could, because ownership now derives from a column every row carries.
    }

    private static void EnsureGlobalSequenceContract(
        SnapshotStore store,
        IReadOnlyList<SnapshotTableDefinition> tables)
    {
        var sequenceRows = string.Join(
            " UNION ALL ",
            tables.Select(table =>
                $"SELECT \"{BookkeepingColumns.ChangeSequence}\" AS sequence FROM {table.QualifiedName}"));

        using var command = store.Connection.CreateCommand();
        command.CommandText =
            $"SELECT count(*), count(DISTINCT sequence), max(sequence) FROM ({sequenceRows}) AS versions";
        using var reader = command.ExecuteReader();
        reader.Read();

        var rows = reader.GetInt64(0);
        var distinctSequences = reader.GetInt64(1);
        var maximum = reader.IsDBNull(2) ? 0 : reader.GetInt64(2);
        if (rows != distinctSequences)
        {
            throw new InvalidOperationException(
                $"Published tables contain {rows - distinctSequences} duplicate durable change-sequence value(s); " +
                "_ChangeSequence must be store-wide unique.");
        }

        var highWatermark = store.ReadChangeSequenceHighWatermark();
        if (maximum > highWatermark)
        {
            throw new InvalidOperationException(
                $"Published tables contain change sequence {maximum}, above allocator high watermark {highWatermark}.");
        }
    }

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

    /// <summary>Footer-only probe: every file parses as parquet and together they carry the expected row count.</summary>
    internal static bool ParquetIsIntact(SnapshotStore store, IReadOnlyList<string> parquetPaths, long expectedRows)
    {
        if (parquetPaths.Count == 0)
            return false;

        try
        {
            var rows = 0L;
            foreach (var path in parquetPaths)
            {
                rows += Convert.ToInt64(store.ExecuteScalar(
                    $"SELECT num_rows FROM parquet_file_metadata('{EscapePath(path)}')"));
            }
            return rows == expectedRows;
        }
        catch
        {
            return false;
        }
    }

    // ---- Export ------------------------------------------------------------------------------

    private static void ExportParquet(SnapshotStore store, SnapshotTableDefinition table,
        SnapshotPublishOptions options, PublishStore publishStore, string parquetPath)
    {
        var sortColumns = options.SortColumnsFor(table);
        var orderBy = string.Join(", ", sortColumns.Select(c => $"\"{c}\""));
        var columns = string.Join(", ",
            table.Columns.Select(c => $"\"{c.Name}\"").Concat(BookkeepingColumns.All.Select(c => $"\"{c}\"")));

        // One folder per table; created on first publish of that table. A no-op where the
        // namespace is flat, which is every blob container.
        publishStore.EnsureFolderFor(parquetPath);

        // On a filesystem a reader can observe a half-written file, so the bulk write lands on a
        // staging name and is renamed. On blob it does not: measured, a parquet under construction
        // is visible at ZERO bytes and then jumps to full size, so the content is never torn and a
        // staging name would only add a second name that retention does not recognise -- which is
        // how orphans become permanent.
        var destination = publishStore.BulkWriteNeedsStaging ? parquetPath + StagingSuffix : parquetPath;

        store.Execute(
            $"""
            COPY (SELECT {columns} FROM {table.QualifiedName} ORDER BY {orderBy})
            TO '{EscapePath(destination)}' (FORMAT parquet, COMPRESSION zstd)
            """);

        if (publishStore.BulkWriteNeedsStaging)
            publishStore.PromoteStaged(destination, parquetPath);
    }

    // ---- The manifest ------------------------------------------------------------------------

    private static void WriteManifest(SnapshotStore store, string manifestPath, SnapshotPublishOptions options,
        PublishStore publishStore, string publishId, DateTime publishedAt,
        IReadOnlyList<PublishedTableManifest> tables, Action? onBeforeManifestCommit)
    {
        // Carried INSIDE the manifest so a cold start can restore what the gate already knows
        // instead of re-reading every feed. Correct only because they commit together: these
        // stamps describe exactly the state the parquet above describes, and one conditional PUT
        // names both. Read here, at publish time, when no merge is in flight.
        var sourceStamps = store.ReadAllSourceFileStamps()
            .Select(PublishedSourceStamp.From)
            .ToList();

        // Cosmos read cursors ride the same road, for the same reason and with more at stake: a
        // cursor lost on rebuild does not cost one re-read of one file, it costs a full re-read of
        // a container that only grows.
        var sourceCursors = store.ReadAllSourceCosmosCursors()
            .Select(PublishedSourceCursor.From)
            .ToList();

        // And the run records, for the same reason the stamps ride here: the fact lives on the
        // agent's instance-local disk, where nothing outside the agent can read it. This section
        // is the one a health framework is meant to read.
        var sourceRuns = store.ReadLatestRunPerSource()
            .Select(PublishedSourceRun.From)
            .ToList();

        var manifest = new PublishedSnapshot(
            ManifestVersion: PublishedSnapshot.CurrentManifestVersion,
            SnapshotName: options.SnapshotName,
            PublishId: publishId,
            PublishedAt: publishedAt,
            SchemaVersion: SnapshotStore.CurrentSchemaVersion,
            PackageVersion: typeof(SnapshotPublisher).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            PathBase: PublishedSnapshot.RelativePathBase,
            SelectionMode: PublishedSnapshot.LatestPerTable,
            Tables: tables,
            SourceStamps: sourceStamps.Count == 0 ? null : sourceStamps)
        {
            ChangeSequenceHighWatermark = store.ReadChangeSequenceHighWatermark(),
            SourceCursors = sourceCursors.Count == 0 ? null : sourceCursors,
            SourceRuns = sourceRuns.Count == 0 ? null : sourceRuns,
        };

        var payload = JsonSerializer.Serialize(manifest, PublishedSnapshot.SerializerOptions);

        // Prepared but not visible: on a filesystem this materialises a complete, readable manifest
        // under a .staging name, so at the moment of commit the whole set genuinely exists and only
        // the name is missing. On blob nothing is written until the commit itself.
        var pending = publishStore.PrepareCommit(manifestPath, payload);

        // Deliberately BETWEEN the two halves: a drill injected here observes a finished snapshot
        // whose parquet are all in place, while consumers still resolve the prior commit. That
        // ordering IS the atomicity, and it is exactly what the publish-kill drill asserts.
        onBeforeManifestCommit?.Invoke();

        // THE ATOMIC COMMIT. The fully-built manifest appears under its final name in exactly one
        // operation -- a rename on a filesystem, a conditional PUT (If-None-Match: *) on blob, both
        // of which REFUSE an existing name rather than replacing it. Measured on a real container:
        // the second conditional PUT is rejected 409 BlobAlreadyExists with the first content
        // intact. Falling back to an unconditional write here would silently destroy the guarantee
        // that a consumer never observes a half-published set.
        if (!publishStore.Commit(pending))
        {
            throw new IOException(
                $"'{manifestPath}' already exists, so this publish did not commit. The publish stamp is " +
                "derived to sit strictly above the previous manifest's, so a collision means another " +
                "writer holds this estate -- refusing to overwrite rather than racing it.");
        }
    }

    /// <summary>
    /// Refreshes the fixed-name copy of the newest manifest — the stable entry point consumers
    /// bookmark. Written through a staging name like everything else, so a reader never sees a
    /// half-written pointer.
    ///
    /// <para>Contained rather than fatal: the versioned manifest is already committed and IS the
    /// publish. A failure here leaves consumers on the previous complete set (whose files
    /// retention still protects) and the next publish retries — losing the publish over a
    /// convenience copy would be the wrong trade.</para>
    /// </summary>
    private static void RefreshStablePointer(
        SnapshotPublishOptions options, PublishStore publishStore, string manifestPath)
    {
        if (options.StableManifestFileName is null)
            return;

        try
        {
            // Unconditional overwrite, unlike the commit above: this pointer is MEANT to move, and
            // it is a copy of an already-committed manifest rather than the commit itself.
            publishStore.WriteAllText(
                publishStore.Resolve(options.StableManifestFileName),
                publishStore.ReadAllText(manifestPath));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                                              or Azure.RequestFailedException)
        {
            // Next publish retries. The committed versioned manifest stands either way.
        }
    }

    // ---- Plumbing ----------------------------------------------------------------------------

    internal static DateTime? ParseStamp(string fileName, string snapshotName)
    {
        var match = PublishedSnapshot.ManifestPattern(snapshotName).Match(fileName);
        if (!match.Success)
            return null;
        return DateTime.TryParseExact(match.Groups[1].Value, TimestampFormat,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var stamp)
            ? DateTime.SpecifyKind(stamp, DateTimeKind.Utc)
            : null;
    }

    private static string EscapePath(string path) => path.Replace("'", "''");

    /// <summary>Best-effort removal of the publisher's own staging residue. Retention's delete lives in <see cref="SnapshotRetention"/>.</summary>
    private static bool TryDelete(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (IOException)
        {
            return false; // in use; the next publish's staging sweep retries
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
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
            // The COLUMN is still "ShimsDeleted": meta.PublishRuns is CREATE TABLE IF NOT EXISTS,
            // so renaming it would need a schema-version bump, and a bump forces every live write
            // DB to rebuild — from a published set that, at cutover, has no manifest yet. Not
            // worth it for a name. Rename with the next bump that is happening anyway.
            result.ManifestsDeleted, result.ParquetFilesDeleted, status, error);
    }
}
