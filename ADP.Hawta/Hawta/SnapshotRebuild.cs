namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// The standard cold start: rebuild a fresh write DB from the newest published parquet set.
/// Bookkeeping columns are published too, so replication state survives — after a rebuild the
/// dirty predicate matches nothing that was already replicated, and the next pump cycle writes
/// zero Cosmos ops. This is also the slot-swap story, which is why it must stay boring.
///
/// <para>Tries shims newest-first: a shim whose parquet set is missing or torn (the crash
/// that motivates a DR rebuild can be the same crash that tore a file) is skipped in favor of
/// the next kept shim, so DR converges on the newest <em>intact</em> published set. Only when
/// no published shim is loadable does it throw — falling back to sources is the caller's
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
        string snapshotName)
    {
        foreach (var table in tables)
            store.EnsureTable(table);

        var shims = PublishedSnapshot.ListShims(publishDirectory, snapshotName);
        if (shims.Count == 0)
        {
            return new SnapshotRebuildResult(null, [], [], [.. tables.Select(t => t.Name)], []);
        }

        var shimsSkipped = new List<string>();
        Exception? lastFailure = null;

        foreach (var shimPath in shims)
        {
            try
            {
                return LoadFromShim(store, tables, publishDirectory, snapshotName, shimPath, shimsSkipped);
            }
            catch (SnapshotSchemaMismatchException)
            {
                // Version mismatch is systemic, not per-file — an older shim can only be older still.
                throw;
            }
            catch (Exception exception)
            {
                shimsSkipped.Add(Path.GetFileName(shimPath));
                lastFailure = exception;
            }
        }

        throw new InvalidDataException(
            $"No published shim of '{snapshotName}' in '{publishDirectory}' could be loaded " +
            $"(tried: {string.Join(", ", shimsSkipped)}) — rebuild from sources instead.",
            lastFailure);
    }

    private static SnapshotRebuildResult LoadFromShim(
        SnapshotStore store, IReadOnlyList<SnapshotTableDefinition> tables,
        string publishDirectory, string snapshotName, string shimPath, IReadOnlyList<string> shimsSkipped)
    {
        IReadOnlyList<PublishedTableManifest> manifest;
        using (var published = PublishedSnapshot.Open(shimPath))
        {
            var info = published.ReadInfo();
            if (info.SchemaVersion != SnapshotStore.CurrentSchemaVersion)
                throw new SnapshotSchemaMismatchException(SnapshotStore.CurrentSchemaVersion, info.SchemaVersion);
            manifest = published.ReadManifest();
        }
        var manifestByTable = manifest.ToDictionary(e => e.Table, StringComparer.OrdinalIgnoreCase);

        var definedNames = new HashSet<string>(tables.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
        var skipped = manifest.Where(e => !definedNames.Contains(e.Table)).Select(e => e.Table).ToList();

        // Validate the whole parquet set up front (bare names, footers parse, row counts
        // match) before loading anything — a torn file must fail the shim, not the rebuild.
        var parquetPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in tables)
        {
            if (!manifestByTable.TryGetValue(table.Name, out var entry))
                continue;

            if (entry.ParquetFile != Path.GetFileName(entry.ParquetFile))
                throw new InvalidDataException(
                    $"Manifest of '{Path.GetFileName(shimPath)}' references '{entry.ParquetFile}' — not a bare filename.");

            var parquetPath = Path.Combine(publishDirectory, entry.ParquetFile);
            if (!SnapshotPublisher.ParquetIsIntact(store, parquetPath, entry.RowCount))
                throw new InvalidDataException(
                    $"'{entry.ParquetFile}' referenced by '{Path.GetFileName(shimPath)}' is missing or torn.");

            parquetPaths[table.Name] = parquetPath;
        }

        var loaded = new List<SnapshotRebuildTable>();
        var createdEmpty = new List<string>();
        var startedAt = DateTime.UtcNow;

        store.Execute("BEGIN TRANSACTION");
        try
        {
            foreach (var table in tables)
            {
                if (!parquetPaths.TryGetValue(table.Name, out var parquetPath))
                {
                    createdEmpty.Add(table.Name);
                    continue;
                }

                var rows = LoadTable(store, table, parquetPath);
                loaded.Add(new SnapshotRebuildTable(table.Name, Path.GetFileName(parquetPath), rows));

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

            store.Execute("COMMIT");
        }
        catch
        {
            try { store.Execute("ROLLBACK"); } catch { /* original exception wins */ }
            throw;
        }

        return new SnapshotRebuildResult(Path.GetFileName(shimPath), loaded, skipped, createdEmpty, [.. shimsSkipped]);
    }

    private static long LoadTable(SnapshotStore store, SnapshotTableDefinition table, string parquetPath)
    {
        var escapedPath = parquetPath.Replace("'", "''");

        var parquetColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var command = store.Connection.CreateCommand())
        {
            command.CommandText = $"DESCRIBE SELECT * FROM read_parquet('{escapedPath}')";
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
            SELECT {string.Join(", ", columns)} FROM read_parquet('{escapedPath}')
            """);
    }
}

/// <param name="ShimFile">The shim the rebuild loaded, or null when no snapshot is published (the store stays empty — a genuinely fresh start).</param>
/// <param name="TablesSkipped">Manifest tables the caller's definitions don't declare (published by an older configuration).</param>
/// <param name="TablesCreatedEmpty">Declared tables absent from the manifest (new families not yet published).</param>
/// <param name="ShimsSkipped">Newer shims that could not be loaded (missing/torn parquet) before one succeeded.</param>
public sealed record SnapshotRebuildResult(
    string? ShimFile,
    IReadOnlyList<SnapshotRebuildTable> TablesLoaded,
    IReadOnlyList<string> TablesSkipped,
    IReadOnlyList<string> TablesCreatedEmpty,
    IReadOnlyList<string> ShimsSkipped)
{
    public long TotalRows => TablesLoaded.Sum(t => t.Rows);
}

public sealed record SnapshotRebuildTable(string Table, string ParquetFile, long Rows);
