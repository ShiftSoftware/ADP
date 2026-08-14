using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// Re-lays a published read snapshot into the flat, self-describing shape external analytics
/// consumers (Power BI, Fabric shortcuts) can read without DuckDB: one folder per table
/// holding immutable <c>{publishId}.parquet</c> versions, plus a small JSON manifest naming
/// the current version of each table.
///
/// <para><b>This is a projection, never a second source of truth.</b> The publisher already
/// re-exports only tables whose signature changed, so the newest shim's
/// <c>meta.publish_manifest</c> is already "newest parquet per table" — this exporter mirrors
/// exactly that set into a different layout. It reads nothing but the publish directory and
/// takes no <see cref="SnapshotStore"/>, so it is a pure function of the published set and can
/// run in a different process from the agent that produced it.</para>
///
/// <para><b>The manifest is the atomic commit</b>, mirroring the shim: every parquet is copied
/// in (each to a <c>.staging</c> name and renamed) <i>before</i> the manifest is written to its
/// own <c>.staging</c> name and renamed last. A consumer mid-refresh either sees the previous
/// manifest — whose files retention still protects — or the new one, whose files are all
/// already present. It never sees a manifest naming a file that is not there.</para>
///
/// <para><b>Copies are skipped by name.</b> Published parquet names embed the publish stamp and
/// are immutable, so a target file that already exists at the same name and length is the same
/// bytes; an unchanged 99 MB table costs nothing after its first export. Retention keeps
/// <see cref="SnapshotExportOptions.KeepVersionsPerTable"/> versions per table so the previous
/// manifest's files outlive the switch.</para>
///
/// <para><b>Empty tables are exported, not hidden.</b> A family whose ingest is not wired yet
/// still publishes as valid, empty parquet, and a consumer cannot tell that from "no rows
/// today". Every manifest entry therefore carries <c>rowCount</c> and <c>dataAsOf</c>, and
/// <see cref="SnapshotExportResult.TablesWithNoRows"/> names them so a host can log it.</para>
/// </summary>
public static class SnapshotExporter
{
    /// <summary>Bumped only for a breaking change to the manifest contract; new fields are additive.</summary>
    public const int ManifestVersion = 1;

    /// <summary>Every path in the manifest is relative to the manifest's own directory.</summary>
    private const string PathBase = ".";

    /// <summary>What <c>asOfPublishId</c> plus the per-table versions mean: newest published parquet per table.</summary>
    private const string SelectionMode = "latest-per-table";

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    /// <summary>Exported version-file shape; retention never touches files outside it.</summary>
    private static readonly Regex ExportedParquetShape =
        new(@"^[0-9]{17}\.parquet$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static SnapshotExportResult Export(SnapshotExportOptions options)
    {
        options.Validate();

        var shimPath = PublishedSnapshot.ResolveNewest(options.PublishDirectory, options.SnapshotName);
        if (shimPath is null)
            return SnapshotExportResult.NoPublish;

        PublishInfo info;
        IReadOnlyList<PublishedTableManifest> published;
        using (var snapshot = PublishedSnapshot.Open(shimPath))
        {
            info = snapshot.ReadInfo();
            published = snapshot.ReadManifest();
        }

        var selected = Select(published, options);

        Directory.CreateDirectory(options.ExportDirectory);

        // Leftovers can only come from a crashed prior export (callers hold the write gate).
        // Table folders nest one level, so the sweep has to recurse.
        foreach (var stale in Directory.EnumerateFiles(options.ExportDirectory, "*.staging", SearchOption.AllDirectories))
            TryDelete(stale, options.RetentionDelete);

        var entries = new List<SnapshotExportManifestTable>(selected.Count);
        foreach (var table in selected)
            entries.Add(ToManifestEntry(table));

        // Short-circuit only when the standing manifest names this exact publish AND every file
        // it points at is actually on disk — an interrupted export must re-run, not be believed.
        var manifestPath = Path.Combine(options.ExportDirectory, options.ManifestFileName);
        if (IsUpToDate(manifestPath, info.PublishId, entries, options.ExportDirectory))
            return new SnapshotExportResult(SnapshotExportStatus.SkippedUpToDate, info.PublishId,
                [], [.. entries.Select(e => e.Table)], NoRows(entries), 0, 0);

        var written = new List<string>();
        var reused = new List<string>();

        for (var i = 0; i < selected.Count; i++)
        {
            var sourcePath = Path.Combine(options.PublishDirectory, selected[i].ParquetFile);
            var targetPath = Path.Combine(options.ExportDirectory, ToLocalPath(entries[i].ParquetPath));

            if (CopyIfMissing(sourcePath, targetPath))
                written.Add(selected[i].Table);
            else
                reused.Add(selected[i].Table);
        }

        WriteManifest(manifestPath, options, info.PublishId, entries);

        var (filesDeleted, filesSkipped) = ApplyRetention(options, entries);

        return new SnapshotExportResult(SnapshotExportStatus.Exported, info.PublishId,
            written, reused, NoRows(entries), filesDeleted, filesSkipped);
    }

    // ---- Selection ---------------------------------------------------------------------------

    private static IReadOnlyList<PublishedTableManifest> Select(
        IReadOnlyList<PublishedTableManifest> published, SnapshotExportOptions options)
    {
        if (options.Tables is null)
            return published;

        var byName = published.ToDictionary(e => e.Table, StringComparer.OrdinalIgnoreCase);

        // A configured name matching no published table is the silent-misconfig class this
        // exists to prevent: a typo would quietly ship a handoff missing a whole table.
        var unknown = options.Tables.Where(t => !byName.ContainsKey(t)).ToList();
        if (unknown.Count > 0)
        {
            throw new ArgumentException(
                $"Tables names no published table: {string.Join(", ", unknown)}. " +
                $"Published: {string.Join(", ", published.Select(e => e.Table))}.");
        }

        return [.. options.Tables.Select(t => byName[t])];
    }

    private static SnapshotExportManifestTable ToManifestEntry(PublishedTableManifest entry)
    {
        var match = SnapshotPublisher.PublishedParquetShape.Match(entry.ParquetFile);
        if (!match.Success)
        {
            throw new InvalidDataException(
                $"Published manifest entry for '{entry.Table}' names '{entry.ParquetFile}', " +
                "which is not a published parquet name ({Table}-{publishId}.parquet).");
        }

        var publishId = match.Groups["stamp"].Value;

        return new SnapshotExportManifestTable(
            Table: entry.Table,
            // Forward slashes: the manifest is read by consumers that are not necessarily on Windows.
            ParquetPath: $"{entry.Table}/{publishId}.parquet",
            PublishId: publishId,
            RowCount: entry.RowCount,
            DataAsOf: entry.MaxLastModified,
            ExportedAt: entry.ExportedAt);
    }

    private static IReadOnlyList<string> NoRows(IReadOnlyList<SnapshotExportManifestTable> entries) =>
        [.. entries.Where(e => e.RowCount == 0).Select(e => e.Table)];

    // ---- Up-to-date probe --------------------------------------------------------------------

    private static bool IsUpToDate(string manifestPath, string publishId,
        IReadOnlyList<SnapshotExportManifestTable> expected, string exportDirectory)
    {
        SnapshotExportManifest? standing;
        try
        {
            if (!File.Exists(manifestPath))
                return false;
            standing = JsonSerializer.Deserialize<SnapshotExportManifest>(File.ReadAllText(manifestPath), SerializerOptions);
        }
        catch
        {
            return false;   // unreadable manifest degrades to a full re-export, never to a wrong one
        }

        if (standing is null || !string.Equals(standing.AsOfPublishId, publishId, StringComparison.Ordinal))
            return false;

        // The standing manifest must name the same set, and every file must be present.
        var standingPaths = standing.Tables.ToDictionary(t => t.Table, t => t.ParquetPath, StringComparer.OrdinalIgnoreCase);
        if (standingPaths.Count != expected.Count)
            return false;

        foreach (var entry in expected)
        {
            if (!standingPaths.TryGetValue(entry.Table, out var path) ||
                !string.Equals(path, entry.ParquetPath, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(Path.Combine(exportDirectory, ToLocalPath(entry.ParquetPath))))
            {
                return false;
            }
        }

        return true;
    }

    // ---- Writing -----------------------------------------------------------------------------

    /// <summary>Copies unless the target already carries the same immutable name at the same length. Returns whether it copied.</summary>
    private static bool CopyIfMissing(string sourcePath, string targetPath)
    {
        var source = new FileInfo(sourcePath);
        if (!source.Exists)
            throw new FileNotFoundException($"Published parquet '{sourcePath}' is named by the manifest but missing.", sourcePath);

        var target = new FileInfo(targetPath);
        if (target.Exists && target.Length == source.Length)
            return false;

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        var stagingPath = targetPath + ".staging";
        File.Copy(sourcePath, stagingPath, overwrite: true);
        File.Move(stagingPath, targetPath, overwrite: true);
        return true;
    }

    private static void WriteManifest(string manifestPath, SnapshotExportOptions options,
        string publishId, IReadOnlyList<SnapshotExportManifestTable> entries)
    {
        var manifest = new SnapshotExportManifest(
            ManifestVersion: ManifestVersion,
            SnapshotName: options.SnapshotName,
            SelectionMode: SelectionMode,
            AsOfPublishId: publishId,
            PathBase: PathBase,
            ExportedAt: DateTime.UtcNow,
            Tables: entries);

        var stagingPath = manifestPath + ".staging";
        File.WriteAllText(stagingPath, JsonSerializer.Serialize(manifest, SerializerOptions));

        // Deliberately after the staging file is complete: drills injected here observe every
        // parquet in place while consumers still resolve the previous manifest.
        options.OnBeforeManifestCommit?.Invoke();

        // The atomic commit: the new manifest replaces the old one in a single rename.
        File.Move(stagingPath, manifestPath, overwrite: true);
    }

    // ---- Retention ---------------------------------------------------------------------------

    /// <summary>
    /// Keeps the newest N versions per exported table. Only folders this export wrote are swept —
    /// a table that dropped out of the allowlist keeps its files rather than being guessed at.
    /// </summary>
    private static (int Deleted, int Skipped) ApplyRetention(
        SnapshotExportOptions options, IReadOnlyList<SnapshotExportManifestTable> entries)
    {
        var deleted = 0;
        var skipped = 0;

        foreach (var entry in entries)
        {
            var folder = Path.Combine(options.ExportDirectory, entry.Table);
            if (!Directory.Exists(folder))
                continue;

            var versions = Directory.EnumerateFiles(folder, "*.parquet", new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive,
            })
                .Where(path => ExportedParquetShape.IsMatch(Path.GetFileName(path)))
                .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var old in versions.Skip(options.KeepVersionsPerTable))
            {
                // The current manifest's file is the newest, so KeepVersionsPerTable >= 1 already
                // protects it; this guard makes that independent of the ordering assumption.
                if (string.Equals(Path.GetFileName(old), $"{entry.PublishId}.parquet", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (TryDelete(old, options.RetentionDelete)) deleted++;
                else skipped++;
            }
        }

        return (deleted, skipped);
    }

    // ---- Plumbing ----------------------------------------------------------------------------

    private static string ToLocalPath(string manifestPath) =>
        manifestPath.Replace('/', Path.DirectorySeparatorChar);

    private static bool TryDelete(string path, Func<string, bool>? retentionDelete)
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
            return false;   // held open by a consumer; the next export's retention pass retries
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}

public sealed class SnapshotExportOptions
{
    /// <summary>The published read tier to project from — the same directory the publisher owns.</summary>
    public required string PublishDirectory { get; init; }

    /// <summary>
    /// Where the folder-per-table layout is written. Must not be the publish directory: the two
    /// have separate retention rules and would delete each other's files.
    /// </summary>
    public required string ExportDirectory { get; init; }

    /// <summary>Base name of the shims to resolve, matching the publisher's <c>SnapshotName</c>.</summary>
    public required string SnapshotName { get; init; }

    /// <summary>
    /// Tables to export, or null for every table in the published manifest. A name matching no
    /// published table throws rather than silently shipping a short handoff.
    /// </summary>
    public IReadOnlyList<string>? Tables { get; init; }

    /// <summary>
    /// Versions kept per table folder. Must be at least 2 so the previous manifest's files
    /// survive the switch for a consumer that is mid-refresh.
    /// </summary>
    public int KeepVersionsPerTable { get; init; } = 3;

    /// <summary>Manifest file name; the consumer contract's entry point.</summary>
    public string ManifestFileName { get; init; } = "powerbi-manifest.json";

    /// <summary>
    /// Fault-injection hook for tests and operator recovery drills only. Invoked after the
    /// staging manifest is fully written, immediately before its atomic rename. Production
    /// callers must leave this unset.
    /// </summary>
    public Action? OnBeforeManifestCommit { get; init; }

    /// <summary>
    /// Fault-injection hook for retention tests only. When supplied, it performs the requested
    /// delete and returns whether retention should treat it as successful. Production callers
    /// must leave this unset.
    /// </summary>
    public Func<string, bool>? RetentionDelete { get; init; }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ManifestFileName) || ManifestFileName.Contains('/') || ManifestFileName.Contains('\\'))
            throw new ArgumentException($"'{ManifestFileName}' is not a valid manifest file name (a bare file name is required).");

        if (KeepVersionsPerTable < 2)
            throw new ArgumentException("KeepVersionsPerTable must be at least 2 so a consumer reading the previous manifest keeps its files.");

        if (Tables is not null)
        {
            if (Tables.Count == 0)
                throw new ArgumentException("Tables is empty — omit it entirely to export every published table.");

            var duplicates = Tables.GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Count > 0)
                throw new ArgumentException($"Duplicate table(s) in Tables: {string.Join(", ", duplicates)}.");
        }

        // Same directory would put the publisher's retention and the exporter's in one place,
        // each sweeping names the other does not recognise.
        if (PathsEqual(PublishDirectory, ExportDirectory))
            throw new ArgumentException("ExportDirectory must differ from PublishDirectory.");
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
        catch
        {
            return false;   // an unresolvable path fails later, with a better message than this
        }
    }
}

public enum SnapshotExportStatus
{
    /// <summary>The manifest was committed (with at least one parquet copied, or a changed table set).</summary>
    Exported,

    /// <summary>Nothing is published yet — no shim to project from.</summary>
    SkippedNoPublish,

    /// <summary>The standing manifest already names this publish and all its files are present.</summary>
    SkippedUpToDate,
}

/// <param name="AsOfPublishId">Publish id of the shim this export projects, or null when nothing is published.</param>
/// <param name="TablesWritten">Tables whose parquet was copied this run.</param>
/// <param name="TablesReused">Tables whose target file was already present at the same immutable name.</param>
/// <param name="TablesWithNoRows">Exported tables carrying zero rows — an un-wired feed looks identical to an empty one to a consumer.</param>
/// <param name="FilesSkippedByRetention">Files retention could not delete this pass (typically held open); retried next export.</param>
public sealed record SnapshotExportResult(
    SnapshotExportStatus Status,
    string? AsOfPublishId,
    IReadOnlyList<string> TablesWritten,
    IReadOnlyList<string> TablesReused,
    IReadOnlyList<string> TablesWithNoRows,
    int FilesDeleted,
    int FilesSkippedByRetention)
{
    internal static readonly SnapshotExportResult NoPublish =
        new(SnapshotExportStatus.SkippedNoPublish, null, [], [], [], 0, 0);
}

/// <summary>
/// The consumer contract, written as JSON beside the table folders. Field names are stable:
/// new fields may be added, existing ones do not change meaning without a
/// <see cref="SnapshotExporter.ManifestVersion"/> bump.
/// </summary>
public sealed record SnapshotExportManifest(
    [property: JsonPropertyName("manifestVersion")] int ManifestVersion,
    [property: JsonPropertyName("snapshotName")] string SnapshotName,
    [property: JsonPropertyName("selectionMode")] string SelectionMode,
    [property: JsonPropertyName("asOfPublishId")] string AsOfPublishId,
    [property: JsonPropertyName("pathBase")] string PathBase,
    [property: JsonPropertyName("exportedAt")] DateTime ExportedAt,
    [property: JsonPropertyName("tables")] IReadOnlyList<SnapshotExportManifestTable> Tables);

/// <param name="ParquetPath">Forward-slashed path relative to the manifest's directory.</param>
/// <param name="PublishId">The publish this table's data was exported at — older than <c>asOfPublishId</c> when the table did not change.</param>
/// <param name="RowCount">Rows in the parquet. Zero is meaningful: the family may not be wired yet.</param>
/// <param name="DataAsOf">Newest source modification carried by the table, or null when the source has no date column.</param>
public sealed record SnapshotExportManifestTable(
    [property: JsonPropertyName("table")] string Table,
    [property: JsonPropertyName("parquetPath")] string ParquetPath,
    [property: JsonPropertyName("publishId")] string PublishId,
    [property: JsonPropertyName("rowCount")] long RowCount,
    [property: JsonPropertyName("dataAsOf")] DateTime? DataAsOf,
    [property: JsonPropertyName("exportedAt")] DateTime ExportedAt);
