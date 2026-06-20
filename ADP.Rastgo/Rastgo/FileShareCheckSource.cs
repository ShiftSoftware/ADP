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
public sealed class FileShareCheckSource(string basePath, string? conflictCopyMarker = null) : ICheckSource
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
