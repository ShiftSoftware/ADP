namespace ShiftSoftware.ADP.Hawta;

/// <summary>One file's identity as the change gate sees it: how big, and when it last changed.</summary>
public readonly record struct FileMetadata(long Length, DateTime LastWriteUtc);

/// <summary>
/// Why a probe answered the way it did. <b>Absence and unreachability are different answers</b> —
/// the publish tier learned this the expensive way (decision D15: a partially ported abstraction
/// fails silently in the direction of doing nothing). A missing feed and an offline share demand
/// opposite responses, so they never share a representation here.
/// </summary>
public enum FileProbeStatus
{
    /// <summary>The file exists and <see cref="FileProbeResult.Metadata"/> describes it.</summary>
    Found,

    /// <summary>The location was readable and the file is not there.</summary>
    Absent,

    /// <summary>Nothing could be established — share offline, permission denied, IO error.</summary>
    Unavailable,
}

public readonly record struct FileProbeResult(FileProbeStatus Status, FileMetadata Metadata)
{
    public static readonly FileProbeResult Absent = new(FileProbeStatus.Absent, default);
    public static readonly FileProbeResult Unavailable = new(FileProbeStatus.Unavailable, default);

    public static FileProbeResult Found(long length, DateTime lastWriteUtc) =>
        new(FileProbeStatus.Found, new FileMetadata(length, lastWriteUtc));
}

/// <summary>
/// Reads file metadata for the change gate. <b>The interface is batch-shaped on purpose.</b>
/// Per-file stats and a single directory listing are interchangeable behind it, so the cheap
/// implementation and the scalable one differ in one class rather than at every call site —
/// which is the part that gets expensive to change once sources multiply.
///
/// <para><b>Instances cache and are single-cycle.</b> A probe answers each path once and reuses
/// the answer, so one cycle sees one consistent picture of the share. Reusing a probe across
/// cycles would gate on stale metadata and skip a file that changed — create one per cycle.</para>
///
/// <para>Not thread-safe: the dispatcher is a single sequential worker by contract.</para>
/// </summary>
public abstract class FileMetadataProbe
{
    /// <summary>
    /// Metadata for every requested path, keyed by the <b>caller's own path string</b> so callers
    /// never have to reason about normalization or casing. Every requested path is present in the
    /// result; failures are carried as <see cref="FileProbeStatus"/>, never as a missing key.
    /// </summary>
    public abstract IReadOnlyDictionary<string, FileProbeResult> Read(IReadOnlyCollection<string> paths);

    public FileProbeResult Read(string path) =>
        Read([path]).TryGetValue(path, out var result) ? result : FileProbeResult.Unavailable;

    /// <summary>Comparer matching the host file system's path semantics.</summary>
    internal static StringComparer PathComparer =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
}

/// <summary>
/// The default probe: <b>one directory enumeration per folder</b> rather than one stat per file.
/// <see cref="DirectoryInfo.EnumerateFiles()"/> yields <see cref="FileInfo"/> objects whose
/// <see cref="FileInfo.Length"/> and <see cref="FileInfo.LastWriteTimeUtc"/> are already populated
/// from the enumeration buffer, so a folder of any size costs a handful of round trips instead of
/// two per file. That is invisible at TCA's four feeds and is the difference that matters once a
/// registry carries dozens.
///
/// <para><b>Enumeration failure is never an answer.</b> A folder that will not enumerate falls back
/// to per-file probing, and a per-file failure reports <see cref="FileProbeStatus.Unavailable"/>.
/// The gate only ever skips on <see cref="FileProbeStatus.Found"/>, so no failure mode here can
/// turn an outage into a silent skip — the worst case is the ordinary full read.</para>
///
/// <para><b>SMB caveat, and why it is the safe direction.</b> A directory entry can carry slightly
/// stale length/mtime for a file being written right now, because the entry updates lazily. We then
/// see the file's PREVIOUS identity, match the stamp, and skip — picking the change up next cycle.
/// A one-cycle delay, never a wrong answer.</para>
/// </summary>
public sealed class DirectoryListingFileMetadataProbe : FileMetadataProbe
{
    private readonly Dictionary<string, FileProbeResult> cache;

    public DirectoryListingFileMetadataProbe()
    {
        cache = new Dictionary<string, FileProbeResult>(PathComparer);
    }

    /// <summary>Folders whose enumeration failed this cycle — probed per file instead.</summary>
    public int FoldersDegradedToPerFileProbing { get; private set; }

    public override IReadOnlyDictionary<string, FileProbeResult> Read(IReadOnlyCollection<string> paths)
    {
        var answer = new Dictionary<string, FileProbeResult>(paths.Count, PathComparer);
        var outstanding = new List<string>();

        foreach (var path in paths)
        {
            if (cache.TryGetValue(path, out var cached))
                answer[path] = cached;
            else if (!answer.ContainsKey(path))
                outstanding.Add(path);
        }

        if (outstanding.Count == 0)
            return answer;

        foreach (var folder in outstanding.GroupBy(GetFolder, PathComparer))
        {
            var listing = TryEnumerate(folder.Key);
            if (listing is null)
            {
                FoldersDegradedToPerFileProbing++;
                foreach (var path in folder)
                    Record(answer, path, ProbeOneFile(path));
                continue;
            }

            foreach (var path in folder)
            {
                Record(answer, path, listing.TryGetValue(Path.GetFileName(path), out var found)
                    ? found
                    // The folder listed cleanly and the name is not in it — genuinely absent,
                    // which is a first-class outcome rather than a failure.
                    : FileProbeResult.Absent);
            }
        }

        return answer;
    }

    private void Record(Dictionary<string, FileProbeResult> answer, string path, FileProbeResult result)
    {
        answer[path] = result;
        // Unavailable is deliberately NOT cached: it is a transient condition, and a share that
        // comes back mid-cycle should be usable rather than poisoned for the rest of the cycle.
        if (result.Status is not FileProbeStatus.Unavailable)
            cache[path] = result;
    }

    private static string GetFolder(string path)
    {
        try
        {
            return Path.GetDirectoryName(Path.GetFullPath(path)) ?? path;
        }
        catch
        {
            // A malformed path cannot be grouped; give it its own bucket so it fails alone.
            return path;
        }
    }

    /// <summary>Null means "could not establish anything" — the caller degrades, it does not assume.</summary>
    private static Dictionary<string, FileProbeResult>? TryEnumerate(string folder)
    {
        try
        {
            var directory = new DirectoryInfo(folder);
            var listing = new Dictionary<string, FileProbeResult>(PathComparer);

            // A folder that does not exist is not the same as one that will not enumerate. The
            // first is an answer (every file under it is absent); the second is a failure.
            if (!directory.Exists)
                return listing;

            foreach (var file in directory.EnumerateFiles())
                listing[file.Name] = FileProbeResult.Found(file.Length, file.LastWriteTimeUtc);

            return listing;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static FileProbeResult ProbeOneFile(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists
                ? FileProbeResult.Found(info.Length, info.LastWriteTimeUtc)
                : FileProbeResult.Absent;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return FileProbeResult.Unavailable;
        }
    }
}
