using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// Path joining and splitting for the <b>published tier only</b>, where a location may be a
/// local directory, an SMB share, or a remote URI such as <c>az://container/prefix</c>.
///
/// <para><see cref="Path"/>'s members assume a local filesystem and quietly corrupt URIs:
/// <c>Path.Combine("az://c/p", "x.parquet")</c> yields <c>az://c/p\x.parquet</c> on Windows
/// (a backslash inside a URI), and <c>Path.GetFullPath("az://c/p")</c> resolves the whole
/// thing against the current directory and returns a local absolute path. Neither throws —
/// they return a plausible-looking wrong answer, which is why the publish tier routes every
/// path join and split through here instead.</para>
///
/// <para><b>Not for the write DB.</b> That is instance-local disk by design (it must never
/// live on a network filesystem), so its paths stay plain <see cref="Path"/> calls.</para>
/// </summary>
public static class PublishPath
{
    /// <summary>
    /// A URI scheme prefix. The scheme requires at least two characters so a Windows drive
    /// root can never be mistaken for one: <c>az://</c> and <c>s3://</c> match, <c>C://</c>
    /// does not.
    /// </summary>
    private static readonly Regex SchemeShape =
        new(@"^[A-Za-z][A-Za-z0-9+.\-]+://", RegexOptions.Compiled);

    /// <summary>True when the location is a URI (<c>az://</c>, <c>abfss://</c>, <c>s3://</c>, <c>https://</c>) rather than a local or UNC path.</summary>
    public static bool IsRemote(string location) => SchemeShape.IsMatch(location);

    /// <summary>
    /// Joins a directory and a forward-slashed relative path (<c>Widget/20260814…​.parquet</c>),
    /// keeping '/' for remote locations and translating to the platform separator locally.
    /// </summary>
    public static string Combine(string directory, string relativePath) =>
        IsRemote(directory)
            ? $"{directory.TrimEnd('/')}/{relativePath}"
            : Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>The last segment of a location. Remote locations split on '/' only — a backslash is a legal URI character, not a separator.</summary>
    public static string FileName(string location) =>
        IsRemote(location)
            ? location[(location.LastIndexOf('/') + 1)..]
            : Path.GetFileName(location);

    /// <summary>The containing directory of a location, or null when there is none.</summary>
    public static string? DirectoryName(string location)
    {
        if (!IsRemote(location))
            return Path.GetDirectoryName(location);

        // Everything up to and including the scheme's own "//" is off limits, so a container
        // root ("az://container") reports no parent rather than "az:/".
        var schemeEnd = SchemeShape.Match(location).Length;
        var lastSlash = location.LastIndexOf('/');
        return lastSlash < schemeEnd ? null : location[..lastSlash];
    }

    /// <summary>
    /// Fails loudly for a remote location on the operations that still need a real directory —
    /// listing, creating, enumerating. Blob-backed publish locations are not wired up yet.
    ///
    /// <para>This exists because the failure would otherwise be <b>silent and wrong</b>, not
    /// merely absent: <c>Directory.Exists("az://…")</c> returns <c>false</c> rather than
    /// throwing, and every caller reads that as "nothing is published yet". The publisher would
    /// re-export every table against an empty baseline, retention would find nothing to
    /// protect, and cold start would conclude the published set is gone and come up empty.
    /// <c>Path.GetFullPath</c> is the same shape of hazard — it rebases the URI under the
    /// current directory and returns a plausible local path. An explicit throw is the only
    /// honest answer until the blob listing lands.</para>
    /// </summary>
    public static void RequireLocal(string location, string operation)
    {
        if (IsRemote(location))
        {
            throw new NotSupportedException(
                $"{operation} needs a directory listing, which '{location}' cannot provide through System.IO " +
                "(Directory.Exists would silently answer 'false' and be believed). Blob-backed publish " +
                "locations are not wired up yet.");
        }
    }

    /// <summary>
    /// Whether a manifest's stored location is a relative path <b>contained</b> under the
    /// manifest's own directory — forward-slashed, no scheme, not rooted, and with no traversal
    /// segment.
    ///
    /// <para>Manifests are data read back from a shared location, so an entry naming
    /// <c>../../elsewhere.parquet</c> or an absolute path must be rejected rather than resolved:
    /// retention would fail to protect it and a rebuild would read a file the publisher never
    /// wrote. Keeping entries relative is also what makes a published set relocatable across a
    /// local folder, an SMB share, and a blob container without rewriting it.</para>
    ///
    /// <para>Forward slashes only — a manifest is read by consumers that are not necessarily on
    /// Windows, and a backslash is a legal file-name character on Linux rather than a separator,
    /// so accepting one would make the same manifest mean two different things.</para>
    /// </summary>
    public static bool IsRelativeContainedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains('\\') || IsRemote(path))
            return false;

        // Path.IsPathRooted misses a leading '/' on Windows, and misses "C:\…" on Linux; the
        // backslash and scheme checks above cover the rest of that gap.
        if (path.StartsWith('/') || Path.IsPathRooted(path))
            return false;

        var segments = path.Split('/');
        return segments.Length > 0
            && segments.All(segment => segment.Length > 0 && segment != "." && segment != "..");
    }
}
