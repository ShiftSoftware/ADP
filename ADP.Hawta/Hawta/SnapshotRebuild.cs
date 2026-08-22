namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// The standard cold start: rebuild a fresh write DB from the newest published parquet set.
/// Bookkeeping columns are published too, so replication state survives — after a rebuild the
/// dirty predicate matches nothing that was already replicated, and the next pump cycle writes
/// zero Cosmos ops. This is also the slot-swap story, which is why it must stay boring.
///
/// <para>Tries published sets newest-first: a manifest whose parquet set is missing or torn
/// (the crash that motivates a DR rebuild can be the same crash that tore a file) is skipped in
/// favor of the next kept compatible one, so DR converges on the newest <em>intact</em> v4
/// published set — which is what <see cref="SnapshotPublishOptions.KeepPublishes"/> is really
/// buying. If every readable manifest is provably pre-v4, it returns an empty seed so the normal
/// source cycle can build the clean v4 estate. A current manifest that is torn, or an unreadable
/// manifest whose version cannot be proved old, still fails loudly rather than silently falling
/// back past data or cursor values consumers may already have observed.</para>
///
/// <para>The caller opens (or recreates) the empty store and holds the write gate. Tables in
/// the published manifest that the caller's definitions don't declare are skipped and
/// reported; declared tables missing from the manifest are created empty (a new family not
/// yet published). Source columns are loaded by intersection with the parquet schema, so a
/// widened definition rebuilds with its new source columns NULL (the same additive drift
/// <see cref="SnapshotStore.EnsureTable"/> allows). Every v4 bookkeeping column and every durable
/// source-version value is required; a missing/invalid contract field rejects the whole seed.</para>
/// </summary>
public static class SnapshotRebuild
{
    /// <summary>
    /// The residency-aware cold start: tables the <paramref name="registry"/> qualifies for
    /// deferral — and whose committed copy is provably not owed to the replication pump — are
    /// NOT loaded; their rows stay in the published parquet and the write DB records a
    /// Deferred state plus everything hydration needs. Everything else loads exactly as
    /// before. This is where the entire cold-start saving comes from: on the measured estate
    /// one quiet catalog table is 87% of the publish set's bytes.
    /// </summary>
    public static SnapshotRebuildResult Execute(
        SnapshotStore store,
        SourceRegistry registry,
        string publishDirectory,
        string snapshotName,
        PublishStore? publishStore = null) =>
        ExecuteCore(store, registry.Tables, publishDirectory, snapshotName, publishStore, registry);

    /// <summary>
    /// Without a registry nothing can qualify for deferral, so every manifest table loads
    /// Resident — the pre-residency behaviour, kept for callers that only have table
    /// definitions.
    /// </summary>
    public static SnapshotRebuildResult Execute(
        SnapshotStore store,
        IReadOnlyList<SnapshotTableDefinition> tables,
        string publishDirectory,
        string snapshotName,
        PublishStore? publishStore = null) =>
        ExecuteCore(store, tables, publishDirectory, snapshotName, publishStore, registry: null);

    private static SnapshotRebuildResult ExecuteCore(
        SnapshotStore store,
        IReadOnlyList<SnapshotTableDefinition> tables,
        string publishDirectory,
        string snapshotName,
        PublishStore? publishStore,
        SourceRegistry? registry)
    {
        foreach (var table in tables)
            store.EnsureTable(table);

        // The documented precondition, checked rather than assumed. The seed below is a bare
        // INSERT, and snapshot tables carry no PRIMARY KEY index — so a caller that hands over
        // a POPULATED store would not fail here at all: every seeded row would land beside the
        // rows already resident, and the doubled keys would only surface at the next publish as
        // a global sequence-contract failure that blames the store's data. This check is what
        // stands between that caller bug and a silently doubled table, and one count per table
        // is nothing next to reading the set.
        foreach (var table in tables)
        {
            var existing = Convert.ToInt64(store.ExecuteScalar($"SELECT count(*) FROM {table.QualifiedName}"));
            if (existing > 0)
            {
                throw new InvalidOperationException(
                    $"Rebuild seeds an EMPTY store, and '{table.QualifiedName}' already holds {existing:N0} row(s). " +
                    "The caller decides cold start by asking whether the write database existed BEFORE it was " +
                    "opened, and only rebuilds then; a warm store needs no seed. The published set is not at fault.");
            }
        }

        // Loud before anything reads: an unreachable store lists as an EMPTY one, and empty here
        // means "the published set is gone" -- which sends cold start to the from-source fallback,
        // the single most expensive path in the system. Never take that branch on a network blip.
        var store_ = publishStore ?? new LocalPublishStore(publishDirectory);
        store_.EnsureReady();

        var manifests = PublishedSnapshot.ListManifests(store_, snapshotName);
        if (manifests.Count == 0)
        {
            return new SnapshotRebuildResult(null, [], [], [], [.. tables.Select(t => t.Name)], []);
        }

        var publishesSkipped = new List<string>();
        Exception? lastFailure = null;
        long observedChangeSequenceHighWatermark = 0;
        var compatibleManifestSeen = false;
        var incompatibleManifestSeen = false;
        var unclassifiedFailureSeen = false;

        // The newest manifest BY NAME, kept even when the estate ends up seeding from an older
        // one. It is the only manifest a table may stay deferred against, because it is the one
        // the publisher will resolve as its baseline — see ResolveDeferralBaseline. Stays null
        // when it cannot be read (then nobody can know what it said, and nothing may defer).
        PublishedSnapshot? newestByName = null;
        string? newestByNamePath = null;

        foreach (var manifestPath in manifests)
        {
            try
            {
                // A committed newer manifest may later lose or tear one of its parquet files after
                // consumers have already observed its sequence values. Even when rebuild falls
                // back to an older intact data set, those values must never be reused.
                var schemaVersion = PublishedSnapshot.ReadSchemaVersion(store_, manifestPath);
                if (schemaVersion > SnapshotStore.CurrentSchemaVersion)
                    throw new SnapshotSchemaMismatchException(
                        SnapshotStore.CurrentSchemaVersion, schemaVersion);

                if (schemaVersion < SnapshotStore.OldestRebuildableSchemaVersion)
                {
                    // v2/v3 carried either no durable sequence contract or the rejected
                    // row-level source metadata. They are not seeds for v4. When they are the
                    // only publishes, cold start deliberately stays empty so the normal source
                    // cycle can build a clean v4 estate instead of deadlocking on every restart.
                    incompatibleManifestSeen = true;
                    publishesSkipped.Add(PublishPath.FileName(manifestPath));
                    lastFailure = new SnapshotSchemaMismatchException(
                        SnapshotStore.CurrentSchemaVersion, schemaVersion);
                    continue;
                }

                compatibleManifestSeen = true;
                var published = PublishedSnapshot.Read(store_, manifestPath);
                if (manifestPath == manifests[0])
                {
                    newestByName = published;
                    newestByNamePath = manifestPath;
                }

                observedChangeSequenceHighWatermark = Math.Max(
                    observedChangeSequenceHighWatermark,
                    published.ChangeSequenceHighWatermark ?? 0);

                return LoadFromManifest(
                    store, tables, store_, snapshotName, manifestPath, published,
                    observedChangeSequenceHighWatermark, publishesSkipped, registry,
                    newestByName, newestByNamePath);
            }
            catch (SnapshotSchemaMismatchException)
            {
                // Version mismatch is systemic, not per-file — an older publish can only be older still.
                throw;
            }
            catch (SnapshotSequenceContractException)
            {
                // A newer manifest without a trustworthy global floor cannot be skipped: its
                // parquet may carry sequence values consumers already observed.
                throw;
            }
            catch (Exception exception)
            {
                publishesSkipped.Add(PublishPath.FileName(manifestPath));
                lastFailure = exception;
                unclassifiedFailureSeen = true;
            }
        }

        if (!compatibleManifestSeen && incompatibleManifestSeen && !unclassifiedFailureSeen)
        {
            return new SnapshotRebuildResult(
                null, [], [], [], [.. tables.Select(table => table.Name)], publishesSkipped);
        }

        throw new InvalidDataException(
            $"No published set of '{snapshotName}' in '{publishDirectory}' could be loaded " +
            $"(tried: {string.Join(", ", publishesSkipped)}) — rebuild from sources instead.",
            lastFailure);
    }

    private static SnapshotRebuildResult LoadFromManifest(
        SnapshotStore store, IReadOnlyList<SnapshotTableDefinition> tables,
        PublishStore publishStore, string snapshotName, string manifestPath,
        PublishedSnapshot published, long changeSequenceFloor,
        IReadOnlyList<string> publishesSkipped, SourceRegistry? registry,
        PublishedSnapshot? newestByName, string? newestByNamePath)
    {
        var seedIsNewest = ReferenceEquals(published, newestByName);
        var manifestByTable = published.Tables.ToDictionary(e => e.Table, StringComparer.OrdinalIgnoreCase);

        var definedNames = new HashSet<string>(tables.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
        var skipped = published.Tables.Where(e => !definedNames.Contains(e.Table)).Select(e => e.Table).ToList();

        // Validate the whole parquet set up front (footers parse, row counts match) before
        // loading anything — a torn file must fail the publish, not the rebuild. On the normal
        // path (the seed IS the newest manifest) deferred candidates get exactly the same
        // probe: a table whose rows are about to stay in the published copy needs its copy
        // proven intact MORE than one being loaded does. A failed probe here sends the whole
        // manifest to the newest-first fallback — the existing DR posture, unchanged; on such
        // a fallback seed, ResolveDeferralBaseline probes the newest entry per table instead.
        var sources = new Dictionary<string, PublishedTableManifest>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in tables)
        {
            if (!manifestByTable.TryGetValue(table.Name, out var entry))
                continue;

            if (!SnapshotPublisher.ParquetIsIntact(store, entry.Resolve(publishStore.Root), entry.RowCount))
            {
                throw new InvalidDataException(
                    $"'{string.Join(", ", entry.Location.Paths)}' referenced by " +
                    $"'{PublishPath.FileName(manifestPath)}' is missing or torn.");
            }

            sources[table.Name] = entry;
        }

        // Resolve — and probe — each table's deferral baseline BEFORE the transaction opens.
        // The probe reads publish-tier parquet footers, and a torn file aborts an open DuckDB
        // transaction even though the probe itself absorbs the error; it is remote I/O that
        // has no business inside the seed's atomic load anyway.
        var deferralBaselines = new Dictionary<string, PublishedTableManifest>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in tables)
        {
            var baseline = ResolveDeferralBaseline(
                store, registry, table, newestByName, seedIsNewest, publishStore.Root);
            if (baseline is not null)
                deferralBaselines[table.Name] = baseline;
        }

        var loaded = new List<SnapshotRebuildTable>();
        var deferred = new List<SnapshotRebuildTable>();
        var createdEmpty = new List<string>();
        var deferredFromNewestSourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var startedAt = DateTime.UtcNow;

        store.Execute("BEGIN TRANSACTION");
        try
        {
            foreach (var table in tables)
            {
                sources.TryGetValue(table.Name, out var seedEntry);

                if (deferralBaselines.TryGetValue(table.Name, out var baseline))
                {
                    // The skip itself. The rows stay in the published parquet; what this
                    // start records instead is (a) the Deferred state with the baseline
                    // entry's resolved file list and row count — everything a later
                    // hydration needs — and (b) the per-scope content identities the
                    // ingest-time guard compares re-delivered files against. All of it
                    // comes from the NEWEST manifest's entry (the seed's own on the normal
                    // path), so what hydration will read and what the publisher will carry
                    // are the same files by construction. The ownership map is restored
                    // from that entry's catalog exactly as for a loaded table (it derives
                    // from the catalog; no rows are needed), and the source stamps restore
                    // below — which is what keeps the gate quiet so the deferral holds.
                    store.MarkTableDeferred(
                        table.Name,
                        PublishPath.FileName(newestByNamePath!),
                        baseline.Resolve(publishStore.Root),
                        baseline.RowCount,
                        [.. baseline.SourceCatalog
                            .Where(source => source.ContentHash is not null)
                            .Select(source => (source.SourceScope, source.ContentHash!))],
                        baseline.ReplicationPending);
                    store.RestoreSourceOwnership(table, baseline.SourceCatalog);
                    deferred.Add(new SnapshotRebuildTable(table.Name, baseline.Location.Paths, baseline.RowCount));

                    if (!seedIsNewest)
                        foreach (var source in baseline.SourceCatalog)
                            deferredFromNewestSourceKeys.Add(source.SourceKey);

                    // No synthetic rebuild run record: those say "this start loaded N rows",
                    // and this start deliberately loaded none. Per-source freshness rows
                    // reappear within one cadence tick, exactly as they do for loaded
                    // tables (rebuild never restores per-source SyncRuns rows either way).
                    continue;
                }

                if (seedEntry is null)
                {
                    // Nothing to load (the seed carries no entry for this table) and, per
                    // the check above, nothing it may defer against either — so the only
                    // copy this table can have is resident rows, recorded as an explicit
                    // state, not left to inference. This is a normal condition (a new
                    // family before its first publish, the cutover's first hour), not an
                    // error; the table becomes deferred-eligible at the first cold start
                    // after its first publish.
                    createdEmpty.Add(table.Name);
                    store.MarkTableResident(table.Name);
                    continue;
                }

                var rows = LoadTable(store, table, seedEntry, publishStore.Root);
                store.RestoreSourceOwnership(table, seedEntry.SourceCatalog);
                store.MarkTableResident(table.Name);
                loaded.Add(new SnapshotRebuildTable(table.Name, seedEntry.Location.Paths, rows));

                store.Execute(
                    """
                    INSERT INTO meta.SyncRuns
                    ("RunId", "Source", "TargetTable", "StartedAt", "FinishedAt",
                     "RowsStaged", "RowsInserted", "RowsUpdated", "RowsTombstoned", "Status", "Error")
                    VALUES (?, ?, ?, ?, ?, ?, ?, 0, 0, 'Succeeded', NULL)
                    """,
                    Guid.NewGuid().ToString("N"), $"rebuild:{snapshotName}", table.Name,
                    startedAt, DateTime.UtcNow, rows, rows);
            }

            // Restore the change gate's memory alongside the data it describes, so a fresh instance
            // does not re-read every feed to relearn what this manifest already records. Safe only
            // because the two were committed together: these stamps describe exactly the state just
            // loaded. A source that merged AFTER this publish has no entry here and re-reads — which
            // is right, because its rows are not in what we just loaded either.
            //
            // StampedAtUtc keeps its ORIGINAL value. It is the age a per-source re-ingest bound
            // measures, and refreshing it here would silently extend every configured bound across a
            // restart — exactly when feeds are most likely to have been swapped underneath us.
            foreach (var stamp in published.SourceStamps ?? [])
                store.WriteSourceFileStamp(stamp.ToStamp());

            // A table deferred against the newest manifest while the estate seeded from an
            // older one takes its gate stamps from the NEWEST manifest too. A stamp asserts
            // "this exact file is already in the data you are looking at", and for such a
            // table the data is the newest committed copy, not the seed — the seed's older
            // stamp would fire the gate on the first tick and cost a full file read just for
            // the content guard to conclude nothing changed. Only these tables' own sources
            // are overridden: everyone else's rows really did come from the seed, and the
            // seed's stamps are the ones that describe them.
            if (deferredFromNewestSourceKeys.Count > 0)
            {
                foreach (var stamp in newestByName!.SourceStamps ?? [])
                {
                    if (deferredFromNewestSourceKeys.Contains(stamp.SourceKey))
                        store.WriteSourceFileStamp(stamp.ToStamp());
                }
            }

            // Same act, same transaction, for the upstream Cosmos cursors — and here it is not an
            // optimisation. Without it a DR rebuild silently becomes a full container re-read: the
            // write DB is reseeded from parquet, not from sources, so a cursor that lived only in
            // the write DB is gone, and the next tick starts from the beginning of the container.
            // Harmless at merge level (the hash diff absorbs it) but a full read nobody asked for,
            // and one that gets more expensive every day the container grows.
            foreach (var cursor in published.SourceCursors ?? [])
                store.WriteSourceCosmosCursor(cursor.ToCursor());

            // Only compatible v4 manifests contribute to this floor. A newer torn v4 may have
            // exposed values beyond the older intact set we loaded, so the next reservation must
            // remain strictly above the maximum observed compatible high watermark.
            store.ReconcileChangeSequence(tables, changeSequenceFloor);

            store.Execute("COMMIT");
        }
        catch
        {
            try { store.Execute("ROLLBACK"); } catch { /* original exception wins */ }
            throw;
        }

        return new SnapshotRebuildResult(
            PublishPath.FileName(manifestPath), loaded, deferred, skipped, createdEmpty, [.. publishesSkipped]);
    }

    /// <summary>
    /// The manifest entry this cold start may leave a table's rows in — always the newest
    /// manifest BY NAME's own entry — or null when the table must load resident. Four
    /// conditions, all computed: the newest manifest must be readable and a CURRENT-schema
    /// publish, the registry must qualify the table's shape (every source a gate-wired file
    /// source, no pin), the settled rule — the copy is provably not owed to the pump (a
    /// dead-lettered row counts as owed on purpose: the manifest's pending count is
    /// attempts-blind, so such a row blocks the skip, loads, and stays visible to a reset; a
    /// null pending count can prove nothing and loads) — and the entry's own parquet must
    /// probe intact.
    ///
    /// <para>Why only the newest by name, even when the estate seeds from an older manifest:
    /// the publisher resolves its carry-forward baseline from the newest manifest BY NAME and
    /// never consults the residency record. A table deferred against whichever manifest
    /// happened to load diverges from that baseline the moment the newest manifest is corrupt
    /// or a sibling's parquet is torn — then every publish throws on the missing or torn
    /// baseline entry, restarting rebuilds the identical divergence instead of healing it,
    /// and retention (which protects manifest-referenced files only) can meanwhile sweep the
    /// recorded copy out from under a future hydration. So: when the newest manifest can
    /// still supply this table's entry intact, defer against THAT entry — record and baseline
    /// then agree by construction, at zero load. When it cannot — the manifest is unreadable
    /// (nobody can know what it said) or this table's own parquet in it is torn (the copy of
    /// record is down to one good older copy) — the table loads resident and the next publish
    /// re-exports: minting a fresh copy is the pre-residency self-heal, restored on exactly
    /// the faults that used to heal.</para>
    ///
    /// <para>The schema condition is load-bearing, not tidiness — the same wedge family: the
    /// publisher deliberately refuses to reuse entries from an older-schema baseline — a
    /// schema transition must re-export every table — and re-exporting needs resident rows. A
    /// table deferred from an older-schema set would leave the transition publish with
    /// nothing to carry and nothing to export, failing every cycle until someone forced the
    /// rows resident. So an older-schema rebuild loads everything, the transition publish
    /// re-exports it, and the NEXT restart defers from the fresh current-schema set.</para>
    /// </summary>
    private static PublishedTableManifest? ResolveDeferralBaseline(
        SnapshotStore store,
        SourceRegistry? registry,
        SnapshotTableDefinition table,
        PublishedSnapshot? newestByName,
        bool seedIsNewest,
        string publishRoot)
    {
        if (newestByName is null || newestByName.SchemaVersion != SnapshotStore.CurrentSchemaVersion)
            return null;

        if (registry is null || !registry.IsTableDeferredCapable(table.Name))
            return null;

        var entry = newestByName.Tables.FirstOrDefault(candidate =>
            candidate.Table.Equals(table.Name, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return null;

        if (registry.TableHasReplicationEnabledCosmosFamily(table.Name)
            && entry.ReplicationPending != 0)
        {
            return null;
        }

        // The seed's own files were all probed up front (a torn one sent the whole manifest
        // to fallback), so on the normal path the entry is already proven. On a fallback seed
        // nobody has probed the newest entry yet — and it is the one file the deferral is
        // about to stake the table's only unloaded copy on, so a failed probe here quietly
        // resolves to resident rather than failing the seed that is otherwise fine.
        if (!seedIsNewest
            && !SnapshotPublisher.ParquetIsIntact(store, entry.Resolve(publishRoot), entry.RowCount))
        {
            return null;
        }

        return entry;
    }

    private static long LoadTable(
        SnapshotStore store, SnapshotTableDefinition table, PublishedTableManifest entry, string publishDirectory) =>
        LoadTableInto(store, table, entry.ReadParquetSql(publishDirectory), $"Published v4 table '{table.Name}'");

    /// <summary>
    /// The one loader for bringing published rows back into the write DB — cold start and
    /// ingest-time hydration both come through here, so a torn, wrong-schema, or key-corrupt
    /// file is refused by the same checks on both paths. <paramref name="sourceDescription"/>
    /// names the copy in error messages ("Published v4 table 'X'", "Deferred copy of 'X'").
    /// </summary>
    internal static long LoadTableInto(
        SnapshotStore store, SnapshotTableDefinition table, string readParquetSql, string sourceDescription)
    {
        var source = readParquetSql;

        var parquetColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var command = store.Connection.CreateCommand())
        {
            command.CommandText = $"DESCRIBE SELECT * FROM {source}";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                parquetColumns.Add(reader.GetString(0));
        }

        var missingBookkeeping = BookkeepingColumns.All
            .Where(column => !parquetColumns.Contains(column))
            .ToList();
        if (missingBookkeeping.Count > 0)
        {
            throw new InvalidDataException(
                $"{sourceDescription} is missing required bookkeeping column(s): " +
                string.Join(", ", missingBookkeeping));
        }

        var invalidVersions = Convert.ToInt64(store.ExecuteScalar(
            $"SELECT count(*) FROM {source} " +
            $"WHERE \"{BookkeepingColumns.ChangeSequence}\" IS NULL " +
            $"OR \"{BookkeepingColumns.ChangeSequence}\" <= 0 " +
            $"OR \"{BookkeepingColumns.ChangeRecordedAt}\" IS NULL"));
        if (invalidVersions > 0)
        {
            throw new InvalidDataException(
                $"{sourceDescription} has {invalidVersions} row(s) with invalid durable change metadata.");
        }

        // Snapshot tables carry no PRIMARY KEY index, so nothing structural refuses a seed whose
        // rows duplicate a key — and a duplicate restored here would be trusted by every later
        // merge (only staging is checked; the publish contract would refuse the store, but only
        // after the corrupt seed is already resident). Refuse the seed at the door instead.
        // count(DISTINCT) ignores NULLs, so this also catches a NULL key before the insert does.
        var duplicateKeys = Convert.ToInt64(store.ExecuteScalar(
            $"SELECT count(*) - count(DISTINCT \"{BookkeepingColumns.PrimaryKey}\") FROM {source}"));
        if (duplicateKeys > 0)
        {
            throw new InvalidDataException(
                $"{sourceDescription} has {duplicateKeys} duplicated or NULL primary key value(s).");
        }

        var columns = table.Columns.Select(c => c.Name).Concat(BookkeepingColumns.All)
            .Where(parquetColumns.Contains)
            .Select(c => $"\"{c}\"")
            .ToList();

        return store.Execute(
            $"""
            INSERT INTO {table.QualifiedName} ({string.Join(", ", columns)})
            SELECT {string.Join(", ", columns)} FROM {source}
            """);
    }
}

/// <param name="ManifestFile">The manifest the rebuild loaded, or null when no snapshot is published (the store stays empty — a genuinely fresh start).</param>
/// <param name="TablesDeferred">
/// Tables whose rows deliberately stayed in the published copy (lazy residency): definitions
/// ensured, ownership and stamps restored, rows NOT loaded; <c>Rows</c> is what the copy
/// holds, not what was read. A separate list on purpose — operators read
/// <paramref name="TablesCreatedEmpty"/> as "new family, nothing published yet", which is a
/// different condition entirely.
/// </param>
/// <param name="TablesSkipped">Manifest tables the caller's definitions don't declare (published by an older configuration).</param>
/// <param name="TablesCreatedEmpty">Declared tables absent from the manifest (new families not yet published).</param>
/// <param name="PublishesSkipped">Newer published sets that could not be loaded (missing/torn parquet) before one succeeded.</param>
public sealed record SnapshotRebuildResult(
    string? ManifestFile,
    IReadOnlyList<SnapshotRebuildTable> TablesLoaded,
    IReadOnlyList<SnapshotRebuildTable> TablesDeferred,
    IReadOnlyList<string> TablesSkipped,
    IReadOnlyList<string> TablesCreatedEmpty,
    IReadOnlyList<string> PublishesSkipped)
{
    public long TotalRows => TablesLoaded.Sum(t => t.Rows);
}

public sealed record SnapshotRebuildTable(string Table, IReadOnlyList<string> Files, long Rows);
