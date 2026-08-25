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

        // The gate stamps the baseline carries, kept for the change decision below. An
        // unreadable or pre-v4 baseline leaves this empty, which reads as "changed" — the same
        // conservative direction the table baseline takes.
        IReadOnlyList<PublishedSourceStamp> baselineStamps = [];
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
                    baselineStamps = previous.SourceStamps ?? [];
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

        // Residency read once per table, up front: the deferred branch below has to run
        // BEFORE the contract scan and BEFORE any signature read, because both are questions
        // about resident rows and a Deferred table has none — its committed copy is the
        // answer, and reading (0, empty-hash) instead would send it down the export path,
        // which for a deferred table means empty parquet over the copy of record.
        var residency = options.Tables.ToDictionary(
            table => table.Name,
            table => store.ReadResidency(table.Name),
            StringComparer.OrdinalIgnoreCase);

        foreach (var table in options.Tables)
        {
            if (residency[table.Name] == SnapshotResidency.Resident)
                EnsureSourceVersionContract(store, table, sourceCatalogs[table.Name]);
        }
        EnsureGlobalSequenceContract(store, options.Tables);

        // Every path the baseline could ask about, resolved once. Filling is lazy — see
        // ParquetFooterProbe — so a publish that reuses nothing never pays for this.
        var footerProbe = new ParquetFooterProbe(
            store,
            baseline.Values
                .SelectMany(entry => entry.Resolve(publishStore.Root))
                .Distinct(StringComparer.Ordinal)
                .ToArray());

        var exported = new List<string>();
        var reused = new List<string>();
        var manifest = new List<PublishedTableManifest>();
        var catalogChanged = previousPath is not null && baseline.Count != options.Tables.Count;

        foreach (var table in options.Tables)
        {
            var sourceCatalog = sourceCatalogs[table.Name];
            var baselineEntry = baseline.GetValueOrDefault(table.Name);

            // The catalog a carried entry keeps: identity fields refreshed from the live
            // configuration (the incumbent reuse behaviour — a renamed source key must reach
            // the manifest), content identities grafted from the baseline (they describe rows
            // this cycle did not touch, and only an export may recompute them).
            var carriedCatalog = baselineEntry is null
                ? sourceCatalog
                : CarryContentHashes(sourceCatalog, baselineEntry.SourceCatalog);

            if (baselineEntry is not null && !CatalogsEqual(baselineEntry.SourceCatalog, carriedCatalog))
                catalogChanged = true;

            // ---- Deferred branch — before any signature read, keyed on recorded state ----
            // The committed copy IS this table's current state, so its baseline entry is
            // carried forward verbatim: location, row count, state hash, replication-pending
            // count. No signature read, no contract scan, no export. Riding the same manifest
            // mechanics as the reuse path is what makes the entry survive rewrites triggered
            // by other tables. Force does not override this branch: forcing a re-export needs
            // resident rows, and hydrating is the caller's decision, never the publisher's.
            if (residency[table.Name] == SnapshotResidency.Deferred)
            {
                if (baselineEntry is null)
                {
                    // A deferred table with nothing to carry cannot be published at all —
                    // exporting would write empty parquet over the copy of record. This state
                    // is a contradiction (deferral requires a committed entry), so fail the
                    // publish loudly; a restart re-evaluates residency at cold start.
                    throw new InvalidOperationException(
                        $"Table '{table.Name}' is Deferred but the previous manifest carries no entry to " +
                        "carry forward. Refusing to publish: exporting a deferred table would overwrite " +
                        "the only copy of its rows with an empty file. Restart the agent so cold start " +
                        "re-evaluates residency.");
                }

                // The reused path re-verifies intactness every publish, and a deferred entry
                // needs that MORE: the old self-heal (re-export from resident rows) does not
                // exist here, so a rotted copy of record must be discovered while older
                // manifests still hold a fallback — not when hydration needs it. Measured at
                // a footer read + row count (0.12 MiB / 2 requests on the large catalog
                // table) — cheap at any cadence.
                if (!footerProbe.IsIntact(baselineEntry.Resolve(publishStore.Root), baselineEntry.RowCount))
                {
                    throw new InvalidDataException(
                        $"Table '{table.Name}' is Deferred and its published copy " +
                        $"('{string.Join(", ", baselineEntry.Location.Paths)}') is missing or torn. " +
                        "Refusing to publish rather than re-referencing a bad copy of record. " +
                        "Restart the agent: cold start falls back to older manifests, then to sources.");
                }

                reused.Add(table.Name);
                manifest.Add(baselineEntry with { SourceCatalog = carriedCatalog });
                continue;
            }

            var signature = ReadSignature(store, table);

            // Reuse needs more than signature equality: the baseline's files must still be
            // readable at the expected row count (a torn file from a crash would otherwise be
            // re-referenced by every future manifest). Bare-name validation already happened
            // in PublishedSnapshot.Read.
            var upToDate =
                !options.Force
                && baselineEntry is not null
                && baselineEntry.RowCount == signature.RowCount
                && baselineEntry.StateHash == signature.StateHash
                && footerProbe.IsIntact(baselineEntry.Resolve(publishStore.Root), baselineEntry.RowCount);

            if (upToDate)
            {
                // An unchanged signature covers every exported column of every row, so the
                // baseline's replication-pending count and content identities still hold and
                // ride along unrecomputed.
                reused.Add(table.Name);
                manifest.Add(baselineEntry! with { SourceCatalog = carriedCatalog });
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
                SourceCatalog = ComputeContentHashes(store, table, options, sourceCatalog),
                ReplicationPending = ReadReplicationPendingRaw(store, table, options),
            });
        }

        var noRows = manifest.Where(e => e.RowCount == 0).Select(e => e.Table).Order().ToList();

        // The gate's memory, read once: compared against the baseline immediately below, and
        // carried into the new manifest if one is written. One read, so the set that decides
        // and the set that is committed cannot differ.
        var sourceStamps = store.ReadAllSourceFileStamps().Select(PublishedSourceStamp.From).ToList();

        // Stamps are a third reason to rewrite, alongside an export and a catalog change.
        //
        // A stamp says "this exact file is already in the data you are looking at", and cold
        // start restores the stamps from the manifest it seeds from. So when the live stamps
        // and the baseline's disagree, the baseline is carrying a memory that no longer matches
        // this build — and every cold start from it fires the gate on those sources, re-reads
        // every one of their feeds, and hydrates every table that was deferred against them.
        //
        // Left to signatures alone that never healed: the re-read finds identical content, so
        // no table's signature moves, so nothing is exported, so the manifest is not rewritten
        // and keeps the stale stamps. The next cold start does it again, and the next. The
        // condition is self-perpetuating precisely because it costs nothing in ROWS. Rewriting
        // here ends it in one cycle, and cheaply: every table is reused, so this writes one
        // manifest and moves no data.
        //
        // The commonest cause is an ordinary deploy — the fingerprint carries Hawta's own
        // version, so a release invalidates every stamp on purpose. That is a re-read the new
        // build is owed exactly once, not once per restart until the data happens to change.
        //
        // Cursors deliberately do NOT get this treatment: a change-feed continuation token can
        // advance on a tick that produced no rows, so including them would rewrite the manifest
        // at cadence forever. A stale cursor also costs a bounded partial re-read, where stale
        // stamps cost every feed and every deferred copy.
        var stampsChanged = !GateMemoryEqual(baselineStamps, sourceStamps);

        if (exported.Count == 0 && previousPath is not null && !catalogChanged && !stampsChanged)
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
        WriteManifest(store, manifestPath, options, publishStore, publishId, stamp, manifest, sourceStamps,
            options.OnBeforeManifestCommit);

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

    /// <summary>
    /// Grafts the baseline's per-scope content identities onto a freshly built catalog.
    /// Matched by scope, not by key: the scope is the durable identity of the rows a hash
    /// describes (a source key renamed in configuration keeps the same rows), matched with the
    /// same comparer the rest of the publisher uses for scopes.
    /// </summary>
    private static IReadOnlyList<PublishedSourceCatalogEntry> CarryContentHashes(
        IReadOnlyList<PublishedSourceCatalogEntry> fresh,
        IReadOnlyList<PublishedSourceCatalogEntry>? baseline) =>
        [.. fresh.Select(entry => entry with
        {
            ContentHash = (baseline ?? []).FirstOrDefault(previous =>
                NullableOrdinalIgnoreCaseComparer.Instance.Equals(previous.SourceScope, entry.SourceScope))
                ?.ContentHash,
        })];

    /// <summary>
    /// Computes each scope's content identity from its LIVE resident rows, for scopes owned by
    /// a file source. Live rows only: the identity is compared against a staged file at ingest
    /// time, and a file never delivers the rows it stopped delivering (the tombstones). Only
    /// file-source scopes because only the file gate's false fires consume it today — a SQL or
    /// Cosmos source merges every tick and its table can never defer; extend when another
    /// source kind learns to.
    /// </summary>
    private static IReadOnlyList<PublishedSourceCatalogEntry> ComputeContentHashes(
        SnapshotStore store,
        SnapshotTableDefinition table,
        SnapshotPublishOptions options,
        IReadOnlyList<PublishedSourceCatalogEntry> sourceCatalog)
    {
        var fileSourceScopes = options.Sources
            .Where(source => source.Table.Name.Equals(table.Name, StringComparison.OrdinalIgnoreCase)
                             && source.FileIngestion is not null)
            .Select(source => source.SourceScope)
            .ToHashSet(NullableOrdinalIgnoreCaseComparer.Instance);

        return [.. sourceCatalog.Select(entry => entry with
        {
            ContentHash = fileSourceScopes.Contains(entry.SourceScope)
                ? Convert.ToString(store.ExecuteScalar(
                    $"""
                    SELECT {SnapshotContentHash.AggregateSql}
                    FROM {table.QualifiedName}
                    WHERE "{BookkeepingColumns.Deleted}" = false
                      AND "{BookkeepingColumns.SourceScope}" IS NOT DISTINCT FROM ?
                    """,
                    entry.SourceScope))
                : null,
        })];
    }

    /// <summary>
    /// The raw replication-pending count for the manifest entry, or null for a table with no
    /// Cosmos family. Deliberately NOT <see cref="SnapshotStore.DirtyPredicate"/>: that ends
    /// with an attempts clause, so a dead-lettered row leaves it while still unreplicated —
    /// and a dead-lettered row must keep blocking the cold-start skip (it loads, stays
    /// visible, and waits for a reset or a content change). Recorded raw regardless of whether
    /// replication is currently enabled; the cold-start decision applies the enabled filter
    /// with the configuration in force THEN, which is what makes flipping replication on later
    /// just work: the flip restarts the process, and that cold start sees the full backlog.
    /// </summary>
    private static long? ReadReplicationPendingRaw(
        SnapshotStore store, SnapshotTableDefinition table, SnapshotPublishOptions options)
    {
        var hasCosmosFamily = options.Sources.Any(source =>
            source.Table.Name.Equals(table.Name, StringComparison.OrdinalIgnoreCase)
            && source.Families is { Count: > 0 });
        if (!hasCosmosFamily)
            return null;

        return Convert.ToInt64(store.ExecuteScalar(
            $"""
            SELECT count(*) FROM {table.QualifiedName}
            WHERE "{BookkeepingColumns.LastReplicationDate}" IS NULL
               OR "{BookkeepingColumns.LastReplicationDate}" < "{BookkeepingColumns.ReplicationModified}"
            """));
    }

    private static void EnsureSourceVersionContract(
        SnapshotStore store,
        SnapshotTableDefinition table,
        IReadOnlyList<PublishedSourceCatalogEntry> sourceCatalog)
    {
        long invalid, duplicateKeys;
        using (var command = store.Connection.CreateCommand())
        {
            // One scan, two verdicts. The duplicate count rides the metadata query because
            // snapshot tables carry no PRIMARY KEY index: nothing structural keeps a corrupt
            // store from holding doubled keys, and this contract is the last gate before such
            // a store is exported to every consumer of the published set.
            command.CommandText =
                $"""
                SELECT
                    count(*) FILTER (WHERE "{BookkeepingColumns.ChangeSequence}" IS NULL
                        OR "{BookkeepingColumns.ChangeSequence}" <= 0
                        OR "{BookkeepingColumns.ChangeRecordedAt}" IS NULL),
                    count(*) - count(DISTINCT "{BookkeepingColumns.PrimaryKey}")
                FROM {table.QualifiedName}
                """;
            using var reader = command.ExecuteReader();
            reader.Read();
            invalid = reader.GetInt64(0);
            duplicateKeys = reader.GetInt64(1);
        }

        if (invalid > 0)
            throw new InvalidOperationException(
                $"Table '{table.Name}' has {invalid} row(s) without complete durable change-sequence metadata.");

        if (duplicateKeys > 0)
            throw new InvalidOperationException(
                $"Table '{table.Name}' has {duplicateKeys} duplicated primary key value(s); " +
                "the write store is corrupt — rebuild it before publishing.");

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

    /// <summary>
    /// Do two stamp sets carry the same answer for the change gate? Compares exactly the fields
    /// <see cref="SourceChangeGate"/> compares — path, length, last-write ticks, fingerprint —
    /// keyed by source, order-independent.
    ///
    /// <para><see cref="PublishedSourceStamp.StampedAtUtc"/> is deliberately excluded. It is a
    /// clock, not an identity: it moves on every read, and the only gate rule that consults it
    /// is the re-ingest bound, which measures AGE. A stamp growing older is not a change anyone
    /// can publish their way out of — republishing preserves the original value by design — so
    /// including it would rewrite the manifest on a schedule and heal nothing.</para>
    /// </summary>
    private static bool GateMemoryEqual(
        IReadOnlyList<PublishedSourceStamp> baseline, IReadOnlyList<PublishedSourceStamp> current)
    {
        if (baseline.Count != current.Count)
            return false;

        static IEnumerable<string> Identities(IReadOnlyList<PublishedSourceStamp> stamps) =>
            stamps
                .Select(stamp => string.Join(
                    '',
                    stamp.SourceKey, stamp.FilePath, stamp.Length, stamp.LastWriteUtcTicks, stamp.ConfigFingerprint))
                .Order(StringComparer.Ordinal);

        return Identities(baseline).SequenceEqual(Identities(current), StringComparer.Ordinal);
    }

    /// <summary>
    /// One batched footer probe for a whole publish, instead of one statement per table.
    ///
    /// <para><b>Why this pays, measured.</b> A <c>parquet_file_metadata</c> probe against blob costs
    /// ~590 ms <i>fixed per file, regardless of size</i> — a 5 KB parquet and a 101 MB one price the
    /// same, because the cost is a statement round trip and not a read. Probing 34 published tables
    /// one statement at a time cost <b>26.8 s</b>; the identical set in ONE statement cost
    /// <b>9.3 s</b>, returning identical row totals. That is the whole win: round trips, not reads.
    /// (A glob over the prefix was faster still at ~1.0 s, but it cannot be scoped to a specific
    /// baseline's paths, which is what correctness here requires.)</para>
    ///
    /// <para><b>Lazily filled, deliberately.</b> A publish where every table's signature moved never
    /// reaches a reuse probe at all, and must not pay for one — so nothing is fetched until the
    /// first probe actually asks. The cost is then paid once for the whole publish.</para>
    ///
    /// <para><b>It can only ever be an optimisation.</b> A batched statement throws in its entirety
    /// if a single path is missing — and a missing path is precisely the torn-copy case this probe
    /// exists to detect, so treating a failed batch as "torn" would refuse healthy publishes. Every
    /// failure path therefore falls back to the original per-path probe: a batch that throws, a
    /// path absent from the map, an empty candidate set. The verdict this returns is always the
    /// verdict <see cref="ParquetIsIntact"/> would have returned.</para>
    /// </summary>
    internal sealed class ParquetFooterProbe(SnapshotStore store, IReadOnlyList<string> candidatePaths)
    {
        private Dictionary<string, long>? rowsByPath;
        private bool batchUnavailable;

        internal bool IsIntact(IReadOnlyList<string> parquetPaths, long expectedRows)
        {
            if (parquetPaths.Count == 0)
                return false;

            if (!batchUnavailable)
            {
                rowsByPath ??= TryFill();

                if (rowsByPath is not null)
                {
                    var total = 0L;
                    var covered = true;

                    foreach (var path in parquetPaths)
                    {
                        if (!rowsByPath.TryGetValue(path, out var rows))
                        {
                            covered = false;
                            break;
                        }

                        total += rows;
                    }

                    if (covered)
                        return total == expectedRows;
                }
            }

            return ParquetIsIntact(store, parquetPaths, expectedRows);
        }

        private Dictionary<string, long>? TryFill()
        {
            if (candidatePaths.Count == 0)
            {
                batchUnavailable = true;
                return null;
            }

            try
            {
                // file_name comes back byte-identical to the path passed in (verified against the
                // blob tier), so the map can be keyed on it directly with no normalisation.
                var list = string.Join(", ", candidatePaths.Select(path => $"'{EscapePath(path)}'"));
                using var command = store.Connection.CreateCommand();
                command.CommandText = $"SELECT file_name, num_rows FROM parquet_file_metadata([{list}])";

                var map = new Dictionary<string, long>(StringComparer.Ordinal);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    map[reader.GetString(0)] = reader.GetInt64(1);

                return map;
            }
            catch
            {
                // One unreadable path fails the whole statement. That is not a verdict about any
                // particular table, so it must not become one — give up on batching for the rest of
                // this publish and let every caller take the per-path route.
                batchUnavailable = true;
                return null;
            }
        }
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

    /// <summary>
    /// The last stand of rule "a non-resident table can never be exported". The deferred
    /// branch in the publish loop is what normally keeps a Deferred table away from here; this
    /// guard is the backstop that survives any future reordering of that loop, because the
    /// failure it prevents is the worst one this design has: exporting a Deferred table writes
    /// well-formed EMPTY parquet over the only copy of its rows, and retention then rotates
    /// the truth out a few publishes later. Reaching this throw means a publisher bug, never
    /// an operational condition — there is no remediation except fixing the code.
    /// </summary>
    internal static void EnsureResidentForExport(SnapshotStore store, SnapshotTableDefinition table)
    {
        if (store.ReadResidency(table.Name) == SnapshotResidency.Deferred)
        {
            throw new InvalidOperationException(
                $"Table '{table.Name}' is Deferred — its rows live only in the published copy — and it " +
                "reached parquet export. Exporting would overwrite the copy of record with an empty " +
                "file. The publisher's deferred branch should have carried its manifest entry instead; " +
                "this is a publisher bug.");
        }
    }

    private static void ExportParquet(SnapshotStore store, SnapshotTableDefinition table,
        SnapshotPublishOptions options, PublishStore publishStore, string parquetPath)
    {
        EnsureResidentForExport(store, table);

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

    /// <param name="sourceStamps">
    /// Carried INSIDE the manifest so a cold start can restore what the gate already knows
    /// instead of re-reading every feed. Correct only because they commit together: these stamps
    /// describe exactly the state the parquet above describes, and one conditional PUT names
    /// both. Read by the caller, which also compares them against the baseline to decide whether
    /// this manifest needs writing at all — one read, so deciding and committing cannot diverge.
    /// </param>
    private static void WriteManifest(SnapshotStore store, string manifestPath, SnapshotPublishOptions options,
        PublishStore publishStore, string publishId, DateTime publishedAt,
        IReadOnlyList<PublishedTableManifest> tables, IReadOnlyList<PublishedSourceStamp> sourceStamps,
        Action? onBeforeManifestCommit)
    {
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
             "TablesExported", "TablesReused", "ManifestsDeleted", "ParquetFilesDeleted", "Status", "Error")
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            result.PublishId, options.SnapshotName, startedAt, DateTime.UtcNow,
            result.TablesExported.Count, result.TablesReused.Count,
            result.ManifestsDeleted, result.ParquetFilesDeleted, status, error);
    }
}
