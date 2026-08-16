namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// The standard cold start: rebuild a fresh write DB from the newest published parquet set.
/// Bookkeeping columns are published too, so replication state survives — after a rebuild the
/// dirty predicate matches nothing that was already replicated, and the next pump cycle writes
/// zero Cosmos ops. This is also the slot-swap story, which is why it must stay boring.
///
/// <para>Tries published sets newest-first: a manifest whose parquet set is missing or torn
/// (the crash that motivates a DR rebuild can be the same crash that tore a file) is skipped in
/// favor of the next kept one, so DR converges on the newest <em>intact</em> published set —
/// which is what <see cref="SnapshotPublishOptions.KeepPublishes"/> is really buying. Only when
/// no published set is loadable does it throw — falling back to sources is the caller's
/// decision, never a silent degrade.</para>
///
/// <para>The caller opens (or recreates) the empty store and holds the write gate. Tables in
/// the published manifest that the caller's definitions don't declare are skipped and
/// reported; declared tables missing from the manifest are created empty (a new family not
/// yet published). Columns are loaded by intersection with the parquet schema, so a widened
/// definition rebuilds from an older parquet with the new columns NULL (the same additive
/// drift <see cref="SnapshotStore.EnsureTable"/> allows).</para>
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

        foreach (var manifestPath in manifests)
        {
            try
            {
                return LoadFromManifest(store, tables, store_, snapshotName, manifestPath, publishesSkipped);
            }
            catch (SnapshotSchemaMismatchException)
            {
                // Version mismatch is systemic, not per-file — an older publish can only be older still.
                throw;
            }
            catch (Exception exception)
            {
                publishesSkipped.Add(PublishPath.FileName(manifestPath));
                lastFailure = exception;
            }
        }

        throw new InvalidDataException(
            $"No published set of '{snapshotName}' in '{publishDirectory}' could be loaded " +
            $"(tried: {string.Join(", ", publishesSkipped)}) — rebuild from sources instead.",
            lastFailure);
    }

    private static SnapshotRebuildResult LoadFromManifest(
        SnapshotStore store, IReadOnlyList<SnapshotTableDefinition> tables,
        PublishStore publishStore, string snapshotName, string manifestPath, IReadOnlyList<string> publishesSkipped)
    {
        // Read validates structure (bare file names, non-empty locations) and throws otherwise,
        // which is what demotes a tampered or truncated manifest to "try the next publish".
        var published = PublishedSnapshot.Read(publishStore, manifestPath);
        if (published.SchemaVersion != SnapshotStore.CurrentSchemaVersion)
            throw new SnapshotSchemaMismatchException(SnapshotStore.CurrentSchemaVersion, published.SchemaVersion);

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
