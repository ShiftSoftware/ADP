using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DuckDB.NET.Data;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// A published read snapshot, described by its JSON manifest — the single artifact a consumer
/// resolves. Manifests are named <c>{SnapshotName}-{ts}.json</c>, so ordinal name order is
/// chronological and "newest published set" is a directory listing, not a query.
///
/// <para><b>Why JSON and not a database file.</b> This replaced a KB-sized DuckDB "views-shim"
/// that carried no data — only <c>CREATE VIEW … read_parquet(…)</c> plus a manifest table.
/// The published tier's destination is Azure Blob, and opening a DuckDB <i>database file</i>
/// over <c>az://</c> is undocumented (<c>ATTACH</c> over remote endpoints is documented for
/// HTTP/S3 only), while the parquet it pointed at reads over <c>az://</c> fine. The shim was
/// the only blob-hostile artifact in the design; a manifest a consumer parses instead of
/// attaches removes that constraint, and every row still comes from <c>read_parquet</c>
/// exactly as before.</para>
///
/// <para><b>Paths are bare file names</b> resolved against the manifest's own directory
/// (<c>pathBase</c> is <c>"."</c>), which is what keeps a published set relocatable across a
/// local folder, an SMB share, and a blob container without rewriting it.</para>
/// </summary>
public sealed record PublishedSnapshot(
    [property: JsonPropertyName("manifestVersion")] int ManifestVersion,
    [property: JsonPropertyName("snapshotName")] string SnapshotName,
    [property: JsonPropertyName("publishId")] string PublishId,
    [property: JsonPropertyName("publishedAt")] DateTime PublishedAt,
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("packageVersion")] string PackageVersion,
    [property: JsonPropertyName("pathBase")] string PathBase,
    [property: JsonPropertyName("selectionMode")] string SelectionMode,
    [property: JsonPropertyName("tables")] IReadOnlyList<PublishedTableManifest> Tables)
{
    /// <summary>
    /// Bumped only for a breaking change to the manifest contract; new fields are additive.
    /// v2 replaced the singular <c>parquetPath</c> string with the plural, self-describing
    /// <see cref="PublishedTableLocation"/> — see its remarks for why that break was spent early.
    /// </summary>
    public const int CurrentManifestVersion = 2;

    /// <summary>Every path in the manifest is relative to the manifest's own directory.</summary>
    internal const string RelativePathBase = ".";

    /// <summary>What <c>publishId</c> plus the per-table publish ids mean: newest published file per table.</summary>
    internal const string LatestPerTable = "latest-per-table";

    /// <summary>Manifest file extension. Deliberately not <c>.duckdb</c> — see the type's remarks.</summary>
    internal const string Extension = ".json";

    internal static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    /// <summary>
    /// Manifest name shape. IgnoreCase mirrors the filesystem's semantics (Windows local disk
    /// and the SMB share are both case-insensitive): a manifest whose on-disk casing differs
    /// from the configured name must never become invisible to baseline/retention while its
    /// parquet stays sweepable.
    /// </summary>
    internal static Regex ManifestPattern(string snapshotName) =>
        new($"^{Regex.Escape(snapshotName)}-([0-9]{{17}})\\{Extension}$", RegexOptions.IgnoreCase);

    /// <summary>Full path of the newest manifest in the directory, or null when none is published yet.</summary>
    public static string? ResolveNewest(string publishDirectory, string snapshotName) =>
        ResolveNewest(new LocalPublishStore(publishDirectory), snapshotName);

    /// <summary>Full location of the newest manifest in the store, or null when none is published yet.</summary>
    public static string? ResolveNewest(PublishStore store, string snapshotName) =>
        ListManifests(store, snapshotName).FirstOrDefault();

    /// <summary>All manifest paths in the directory, newest first (ordinal name order — the stamp format makes that chronological).</summary>
    internal static IReadOnlyList<string> ListManifests(string publishDirectory, string snapshotName) =>
        ListManifests(new LocalPublishStore(publishDirectory), snapshotName);

    /// <summary>
    /// All manifest locations in the store, newest first — <b>ordinal name order</b>, never
    /// last-modified. The 17-digit stamp makes the name chronological, and the publisher forces each
    /// stamp strictly above the previous manifest's, so newest can neither tie nor go backwards.
    /// Sorting by storage metadata instead would break that on blob, where mtimes are server-assigned.
    /// </summary>
    internal static IReadOnlyList<string> ListManifests(PublishStore store, string snapshotName)
    {
        var pattern = ManifestPattern(snapshotName);

        // TOP LEVEL ONLY. Manifests live at the root; parquet lives one folder down per table. The
        // local implementation got this free from Directory.EnumerateFiles defaulting to
        // TopDirectoryOnly, but blob listing is flat-and-recursive with no such option — an
        // unfiltered port would let a nested .json masquerade as a manifest.
        return store.List()
            .Where(entry => !entry.RelativePath.Contains('/'))
            .Where(entry => pattern.IsMatch(PublishPath.FileName(entry.Location)))
            .Select(entry => entry.Location)
            .OrderByDescending(PublishPath.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Reads and validates one manifest. Throws <see cref="InvalidDataException"/> on anything malformed.</summary>
    public static PublishedSnapshot Read(string manifestPath) =>
        Read(new LocalPublishStore(PublishPath.DirectoryName(manifestPath) ?? "."), manifestPath);

    /// <summary>Reads and validates one manifest out of a store.</summary>
    public static PublishedSnapshot Read(PublishStore store, string manifestPath)
    {
        PublishedSnapshot? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<PublishedSnapshot>(store.ReadAllText(manifestPath), SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"'{manifestPath}' is not readable as a Hawta publish manifest.", exception);
        }

        if (manifest is null)
            throw new InvalidDataException($"'{manifestPath}' deserialized to null — not a Hawta publish manifest.");

        manifest.Validate(manifestPath);
        return manifest;
    }

    /// <summary>Reads the newest manifest in the directory, or null when nothing is published yet.</summary>
    public static PublishedSnapshot? ReadNewest(string publishDirectory, string snapshotName) =>
        ReadNewest(new LocalPublishStore(publishDirectory), snapshotName);

    /// <summary>Reads the newest manifest in the store, or null when nothing is published yet.</summary>
    public static PublishedSnapshot? ReadNewest(PublishStore store, string snapshotName)
    {
        var newest = ResolveNewest(store, snapshotName);
        return newest is null ? null : Read(store, newest);
    }

    /// <summary>
    /// Structural checks a reader must not skip. A manifest is data read back from a shared
    /// location, so "it deserialized" is not the same as "it is safe to act on": an entry
    /// naming an absolute path or a traversal would let retention fail to protect a file and
    /// let a rebuild read one the publisher never wrote.
    /// </summary>
    private void Validate(string manifestPath)
    {
        // Well-formed JSON that is not a manifest ("{}", or another tool's document) leaves the
        // positional record's members at their defaults rather than throwing, so absence has to
        // be checked before anything is dereferenced. Getting this wrong turns a public read
        // into a NullReferenceException, and the publisher's deliberately-bare baseline catch
        // would swallow it as "no baseline" and silently re-export everything.
        if (Tables is null || SnapshotName is null || PublishId is null)
        {
            throw new InvalidDataException(
                $"'{manifestPath}' parsed as JSON but is missing required manifest fields — not a Hawta publish manifest.");
        }

        if (ManifestVersion > CurrentManifestVersion)
        {
            throw new InvalidDataException(
                $"'{manifestPath}' is manifest v{ManifestVersion}; this build understands v{CurrentManifestVersion}. " +
                "A newer publisher wrote this set — upgrade rather than reading it with older rules.");
        }

        foreach (var entry in Tables)
        {
            if (entry.Location.Paths.Count == 0)
                throw new InvalidDataException($"'{manifestPath}' entry for '{entry.Table}' names no files.");

            foreach (var path in entry.Location.Paths)
            {
                if (!PublishPath.IsRelativeContainedPath(path))
                {
                    throw new InvalidDataException(
                        $"'{manifestPath}' entry for '{entry.Table}' references '{path}' — not a relative path " +
                        "contained under the manifest's own directory.");
                }
            }
        }
    }

    /// <summary>
    /// Materializes this published set as a <b>local, throwaway</b> DuckDB database holding one
    /// <c>data.*</c> view per table, each reading the manifest's parquet. It carries no data.
    ///
    /// <para>This is the convenience shim the publish tier no longer writes, regenerated on
    /// demand — for tools that can only be handed a connection string (Rastgo's check source,
    /// plain <c>duckdb.exe</c>) rather than an open connection. The reason the published tier
    /// stopped writing one is that a DuckDB <i>database file</i> cannot be opened over
    /// <c>az://</c>; a file on local disk has no such problem, so regenerating one here costs
    /// nothing and keeps existing <c>data.*</c> SQL working unchanged.</para>
    ///
    /// <para><b>Write it to local scratch space and delete it after.</b> It must never be
    /// written into the publish directory, where retention does not know it and a stale copy
    /// would outlive the parquet it names.</para>
    /// </summary>
    public void WriteViewDatabase(string publishDirectory, string databasePath)
    {
        PublishPath.RequireLocal(databasePath, "Writing a view database");

        if (File.Exists(databasePath)) File.Delete(databasePath);
        if (File.Exists(databasePath + ".wal")) File.Delete(databasePath + ".wal");

        using var connection = new DuckDBConnection($"Data Source={databasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "CREATE SCHEMA IF NOT EXISTS data";
        command.ExecuteNonQuery();

        foreach (var entry in Tables)
        {
            command.CommandText =
                $"""CREATE OR REPLACE VIEW data."{entry.Table.Replace("\"", "\"\"")}" AS SELECT * FROM {entry.ReadParquetSql(publishDirectory)}""";
            command.ExecuteNonQuery();
        }

        // CHECKPOINT so the file is complete without a WAL sidecar the reader would have to replay.
        command.CommandText = "CHECKPOINT";
        command.ExecuteNonQuery();
    }

    /// <summary>The manifest's own directory, which every path in it resolves against.</summary>
    public static string DirectoryOf(string manifestPath) =>
        PublishPath.DirectoryName(manifestPath)
        ?? throw new ArgumentException($"'{manifestPath}' has no containing directory.", nameof(manifestPath));
}

/// <summary>
/// One table's entry in a manifest: where its data lives, plus the change signature
/// (<paramref name="RowCount"/> + <paramref name="StateHash"/>, the per-row-state XOR
/// aggregate) it was exported at. <paramref name="DataAsOf"/> is observability — the newest
/// source modification the table carries.
/// </summary>
/// <param name="PublishId">The publish this table's data was written at — older than the manifest's own when the table did not change.</param>
/// <param name="RowCount">Rows across every file in <paramref name="Location"/>. Zero is meaningful: the family may not be wired up yet.</param>
public sealed record PublishedTableManifest(
    [property: JsonPropertyName("table")] string Table,
    [property: JsonPropertyName("location")] PublishedTableLocation Location,
    [property: JsonPropertyName("publishId")] string PublishId,
    [property: JsonPropertyName("rowCount")] long RowCount,
    [property: JsonPropertyName("stateHash")] string StateHash,
    [property: JsonPropertyName("dataAsOf")] DateTime? DataAsOf,
    [property: JsonPropertyName("exportedAt")] DateTime ExportedAt)
{
    /// <summary>The entry's files resolved against the manifest's directory, in manifest order.</summary>
    public IReadOnlyList<string> Resolve(string publishDirectory) =>
        [.. Location.Paths.Select(path => PublishPath.Combine(publishDirectory, path))];

    /// <summary>
    /// A DuckDB table function reading this entry's files — the consumer contract in one call.
    /// Always a list, so a table that grows to several files needs no caller change.
    /// </summary>
    public string ReadParquetSql(string publishDirectory)
    {
        if (!string.Equals(Location.Kind, PublishedTableLocation.ParquetKind, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Table '{Table}' is published as '{Location.Kind}'; this build reads " +
                $"'{PublishedTableLocation.ParquetKind}' only.");
        }

        var files = string.Join(", ", Resolve(publishDirectory).Select(path => $"'{path.Replace("'", "''")}'"));
        return $"read_parquet([{files}])";
    }
}

/// <summary>
/// Where one table's data lives, as a self-describing set rather than a single file name.
///
/// <para>This is deliberately plural and object-shaped even though the publisher writes exactly
/// one parquet per table today. A singular <c>parquetPath</c> string breaks on the two things
/// already on the roadmap — a table partitioned across several files, and Delta, where a table
/// is a <i>directory</i> with a <c>_delta_log</c> rather than a file — and adding a
/// <c>files[]</c> field alongside a <c>parquetPath</c> later would leave two fields with
/// ambiguous precedence. The break was spent while the consumer contract was still early.</para>
/// </summary>
/// <param name="Kind">Storage format. <c>parquet</c> today; the field exists so <c>delta</c> can be added without a v3.</param>
/// <param name="Paths">Bare file names relative to the manifest's directory. Readers must handle more than one; the publisher currently writes one.</param>
public sealed record PublishedTableLocation(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("paths")] IReadOnlyList<string> Paths)
{
    public const string ParquetKind = "parquet";

    public static PublishedTableLocation Parquet(string file) => new(ParquetKind, [file]);
}
