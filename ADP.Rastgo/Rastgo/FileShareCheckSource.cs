namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// Surfaces file-share signals — the most upstream hop we can see (upstream systems → file sync →
/// share). This is how we tell "did data actually get delivered?" apart from "did our parse/load
/// succeed?". <see cref="MeasureSpec.Path"/> is relative to a configured base.
/// <list type="bullet">
/// <item>plain path → that file's last-write time (a timestamp metric).</item>
/// <item>wildcard, scalar → the NEWEST match's mtime (conflict copies excluded).</item>
/// <item>wildcard, <c>breakdown</c> set → one mtime per matching file (auto-covers every matching feed file).</item>
/// <item><c>**/</c> prefix → recurse from the base.</item>
/// <item><c>valueKind: count</c> → number of matching files (e.g. count Azure File Sync conflict copies).</item>
/// </list>
/// <para>
/// <paramref name="conflictCopyMarker"/> is an optional substring identifying file-sync conflict copies
/// (Azure File Sync renames a losing copy to <c>name-MachineName.ext</c>). When set, those are excluded from
/// freshness (so a stale duplicate can't look fresh) but still counted by <c>valueKind: count</c> — that is
/// the conflict-copy detector. Null/blank = no exclusion.
/// </para>
/// </summary>
public sealed class FileShareCheckSource(string basePath, string? conflictCopyMarker = null) : ICheckSource, ICheckSourceCatalog
{
    public string Name => "fileshare";

    public Task<MeasureOutcome> MeasureAsync(MeasureSpec spec, bool grouped, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(spec.Path))
            return Error("fileshare measure requires 'path'.");

        try
        {
            var wantCount = string.Equals(spec.ValueKind, "count", StringComparison.OrdinalIgnoreCase);

            var path = spec.Path;
            var recursive = path.StartsWith("**/", StringComparison.Ordinal) || path.StartsWith("**\\", StringComparison.Ordinal);
            if (recursive)
                path = path[3..];

            var combined = Path.IsPathRooted(path) ? path : Path.Combine(basePath, path);
            var hasWildcard = combined.Contains('*') || combined.Contains('?');

            if (!hasWildcard)
            {
                if (!File.Exists(combined))
                    return Error($"File not found: {combined}");
                return wantCount
                    ? Scalar(new MeasureCell(1, null))
                    : Scalar(new MeasureCell(null, MTime(combined)));
            }

            var dir = Path.GetDirectoryName(combined) ?? basePath;
            var pattern = Path.GetFileName(combined);
            if (!Directory.Exists(dir))
                return Error($"Directory not found: {dir}");

            var matches = Directory.GetFiles(dir, pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            // Count includes everything (that's the point of the conflict-copy detector).
            if (wantCount)
                return Scalar(new MeasureCell(matches.Length, null));

            // Freshness ignores file-sync conflict copies (stale duplicates) when a marker is configured.
            var canonical = string.IsNullOrEmpty(conflictCopyMarker)
                ? matches
                : matches.Where(f => !Path.GetFileName(f).Contains(conflictCopyMarker, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (canonical.Length == 0)
                return Error($"No files match (excluding conflict copies): {combined}");

            if (grouped)
            {
                var cells = canonical.ToDictionary(
                    f => Path.GetFileNameWithoutExtension(f),
                    f => new MeasureCell(null, MTime(f)));
                return Task.FromResult(new MeasureOutcome { Cells = cells, AsOfUtc = DateTimeOffset.UtcNow });
            }

            return Scalar(new MeasureCell(null, canonical.Max(MTime)));
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    public Task<SourceCatalogSnapshot> DiscoverAsync(SourceCatalogRequest request, CancellationToken ct)
    {
        var excludedTopLevelFolders = request.ExcludedTopLevelFolders
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Where(IsTopLevelFolderName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var excluded = excludedTopLevelFolders.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
            return Task.FromResult(new SourceCatalogSnapshot(
                Name, "File share", [], DateTimeOffset.UtcNow,
                Error: "File metadata is unavailable because the configured root does not exist.")
            {
                ExcludedTopLevelFolders = excludedTopLevelFolders,
            });

        var root = Path.GetFullPath(basePath);
        var datasets = new List<CatalogDataset>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<(string Directory, int Depth)>();
        pending.Enqueue((root, 0));
        var truncated = false;

        // Pack references are the catalog's highest-value entries. Reserve the front of the bounded
        // result for them so a large unrelated directory cannot consume MaxFiles before, for example,
        // a referenced Stock/*.csv pattern is reached by the general breadth-first crawl.
        foreach (var path in request.ReferencedMeasures
            .Select(measure => measure.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (datasets.Count >= request.MaxFiles) { truncated = true; break; }
            foreach (var file in ReferencedFiles(path, root, request.MaxFileDepth, request.MaxFiles - datasets.Count))
            {
                AddFile(file, root, referenced: true);
                if (datasets.Count >= request.MaxFiles) { truncated = true; break; }
            }
        }

        while (pending.Count > 0 && datasets.Count < request.MaxFiles)
        {
            ct.ThrowIfCancellationRequested();
            var (directory, depth) = pending.Dequeue();
            try
            {
                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    AddFile(file, root, referenced: false);
                    if (datasets.Count >= request.MaxFiles) { truncated = true; break; }
                }

                if (depth >= request.MaxFileDepth) continue;
                foreach (var child in Directory.EnumerateDirectories(directory))
                {
                    var info = new DirectoryInfo(child);
                    if (depth == 0 && excluded.Contains(info.Name)) continue;
                    if ((info.Attributes & FileAttributes.ReparsePoint) == 0)
                        pending.Enqueue((child, depth + 1));
                }
            }
            catch (UnauthorizedAccessException)
            {
                // A partial catalog is still useful; inaccessible branches remain absent.
            }
        }

        return Task.FromResult(new SourceCatalogSnapshot(
            Name,
            "File share",
            datasets.OrderBy(d => d.Namespace).ThenBy(d => d.Name).ToList(),
            DateTimeOffset.UtcNow,
            truncated,
            $"File names, sizes, and modification times only. Pack-referenced paths are prioritized, then broader enumeration is capped at {request.MaxFiles} files and depth {request.MaxFileDepth}; file contents are never read.")
        {
            ExcludedTopLevelFolders = excludedTopLevelFolders,
        });

        void AddFile(string path, string catalogRoot, bool referenced)
        {
            var full = Path.GetFullPath(path);
            if (!seen.Add(full)) return;
            var info = new FileInfo(full);
            string display;
            try
            {
                display = Path.GetRelativePath(catalogRoot, full);
                if (display.StartsWith("..", StringComparison.Ordinal)) display = info.Name;
            }
            catch
            {
                display = info.Name;
            }
            var directory = Path.GetDirectoryName(display)?.Replace('\\', '/');
            datasets.Add(new(
                Path.GetFileName(display),
                string.IsNullOrWhiteSpace(info.Extension) ? "file" : info.Extension.TrimStart('.').ToLowerInvariant(),
                referenced && string.IsNullOrWhiteSpace(directory) ? "referenced" : directory,
                [],
                $"{info.Length:N0} bytes · modified {info.LastWriteTimeUtc:yyyy-MM-dd HH:mm} UTC")
            {
                Referenced = referenced,
                SizeBytes = info.Length,
                ModifiedAtUtc = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
            });
        }
    }

    private static bool IsTopLevelFolderName(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value is not "." and not ".." &&
        value.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) < 0;

    private static IEnumerable<string> ReferencedFiles(string configuredPath, string root, int maxDepth, int take)
    {
        var path = configuredPath;
        var recursive = path.StartsWith("**/", StringComparison.Ordinal) || path.StartsWith("**\\", StringComparison.Ordinal);
        if (recursive) path = path[3..];
        var combined = Path.IsPathRooted(path) ? path : Path.Combine(root, path);
        var hasWildcard = combined.Contains('*') || combined.Contains('?');
        if (!hasWildcard) return File.Exists(combined) ? [combined] : [];

        var directory = Path.GetDirectoryName(combined) ?? root;
        if (!Directory.Exists(directory)) return [];
        var pattern = Path.GetFileName(combined);
        if (!recursive) return Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly).Take(take);
        return EnumeratePatternBounded(directory, pattern, maxDepth, take);
    }

    private static IEnumerable<string> EnumeratePatternBounded(string root, string pattern, int maxDepth, int take)
    {
        var pending = new Queue<(string Directory, int Depth)>();
        pending.Enqueue((root, 0));
        var yielded = 0;
        while (pending.Count > 0 && yielded < take)
        {
            var (directory, depth) = pending.Dequeue();
            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly); }
            catch (UnauthorizedAccessException) { continue; }
            foreach (var file in files)
            {
                yield return file;
                if (++yielded >= take) yield break;
            }

            if (depth >= maxDepth) continue;
            IEnumerable<string> children;
            try { children = Directory.EnumerateDirectories(directory); }
            catch (UnauthorizedAccessException) { continue; }
            foreach (var child in children)
            {
                var info = new DirectoryInfo(child);
                if ((info.Attributes & FileAttributes.ReparsePoint) == 0)
                    pending.Enqueue((child, depth + 1));
            }
        }
    }

    private static DateTimeOffset MTime(string file) => new(File.GetLastWriteTimeUtc(file), TimeSpan.Zero);

    private static Task<MeasureOutcome> Scalar(MeasureCell cell)
        => Task.FromResult(new MeasureOutcome
        {
            Cells = new Dictionary<string, MeasureCell> { [MeasureOutcome.ScalarKey] = cell },
            AsOfUtc = DateTimeOffset.UtcNow,
        });

    private static Task<MeasureOutcome> Error(string message)
        => Task.FromResult(new MeasureOutcome { Error = message });
}
