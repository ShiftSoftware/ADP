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
    public static SnapshotRebuildResult Execute(
        SnapshotStore store,
        IReadOnlyList<SnapshotTableDefinition> tables,
        string publishDirectory,
        string snapshotName,
        PublishStore? publishStore = null)
    {
        foreach (var table in tables)
            store.EnsureTable(table);

        // The documented precondition, checked rather than assumed. The seed below is a bare
        // INSERT, so a caller that hands over a POPULATED store gets a primary-key violation from
        // deep inside the per-manifest loop — where it is indistinguishable from a torn parquet
        // file, and comes back as "no published set could be loaded, rebuild from sources
        // instead". That message names the wrong culprit and recommends the most expensive path in
        // the system to fix a caller bug the published set is innocent of. One count per table is
        // nothing next to reading the set, and it turns that misdirection into the actual answer.
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
            return new SnapshotRebuildResult(null, [], [], [.. tables.Select(t => t.Name)], []);
        }

        var publishesSkipped = new List<string>();
        Exception? lastFailure = null;
        long observedChangeSequenceHighWatermark = 0;
        var compatibleManifestSeen = false;
        var incompatibleManifestSeen = false;
        var unclassifiedFailureSeen = false;

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

                observedChangeSequenceHighWatermark = Math.Max(
                    observedChangeSequenceHighWatermark,
                    published.ChangeSequenceHighWatermark ?? 0);

                return LoadFromManifest(
                    store, tables, store_, snapshotName, manifestPath, published,
                    observedChangeSequenceHighWatermark, publishesSkipped);
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
                null, [], [], [.. tables.Select(table => table.Name)], publishesSkipped);
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
        IReadOnlyList<string> publishesSkipped)
    {
        var manifestByTable = published.Tables.ToDictionary(e => e.Table, StringComparer.OrdinalIgnoreCase);

        var definedNames = new HashSet<string>(tables.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
        var skipped = published.Tables.Where(e => !definedNames.Contains(e.Table)).Select(e => e.Table).ToList();

        // Validate the whole parquet set up front (footers parse, row counts match) before
        // loading anything — a torn file must fail the publish, not the rebuild.
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

        var loaded = new List<SnapshotRebuildTable>();
        var createdEmpty = new List<string>();
        var startedAt = DateTime.UtcNow;

        store.Execute("BEGIN TRANSACTION");
        try
        {
            foreach (var table in tables)
            {
                if (!sources.TryGetValue(table.Name, out var entry))
                {
                    createdEmpty.Add(table.Name);
                    continue;
                }

                var rows = LoadTable(store, table, entry, publishStore.Root);
                store.RestoreSourceOwnership(table, entry.SourceCatalog);
                loaded.Add(new SnapshotRebuildTable(table.Name, entry.Location.Paths, rows));

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
            PublishPath.FileName(manifestPath), loaded, skipped, createdEmpty, [.. publishesSkipped]);
    }

    private static long LoadTable(
        SnapshotStore store, SnapshotTableDefinition table, PublishedTableManifest entry, string publishDirectory)
    {
        var source = entry.ReadParquetSql(publishDirectory);

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
                $"Published v4 table '{table.Name}' is missing required bookkeeping column(s): " +
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
                $"Published v4 table '{table.Name}' has {invalidVersions} row(s) with invalid durable change metadata.");
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
/// <param name="TablesSkipped">Manifest tables the caller's definitions don't declare (published by an older configuration).</param>
/// <param name="TablesCreatedEmpty">Declared tables absent from the manifest (new families not yet published).</param>
/// <param name="PublishesSkipped">Newer published sets that could not be loaded (missing/torn parquet) before one succeeded.</param>
public sealed record SnapshotRebuildResult(
    string? ManifestFile,
    IReadOnlyList<SnapshotRebuildTable> TablesLoaded,
    IReadOnlyList<string> TablesSkipped,
    IReadOnlyList<string> TablesCreatedEmpty,
    IReadOnlyList<string> PublishesSkipped)
{
    public long TotalRows => TablesLoaded.Sum(t => t.Rows);
}

public sealed record SnapshotRebuildTable(string Table, IReadOnlyList<string> Files, long Rows);
