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
    [property: JsonPropertyName("tables")] IReadOnlyList<PublishedTableManifest> Tables,
    [property: JsonPropertyName("sourceStamps")] IReadOnlyList<PublishedSourceStamp>? SourceStamps = null)
{
    // Body property preserves the pre-v3 positional constructor's CLR signature for consumers
    // compiled against an earlier patch release while remaining an additive JSON field.
    [JsonPropertyName("changeSequenceHighWatermark")]
    public long? ChangeSequenceHighWatermark { get; init; }

    /// <summary>
    /// Bumped only for a breaking change to the manifest contract; new fields are additive.
    /// v2 replaced the singular <c>parquetPath</c> string with the plural, self-describing
    /// <see cref="PublishedTableLocation"/>. v3 makes the per-table source catalog mandatory
    /// for schema-v4 publishes.
    /// </summary>
    public const int CurrentManifestVersion = 3;

    /// <summary>Every path in the manifest is relative to the manifest's own directory.</summary>
    internal const string RelativePathBase = ".";

    /// <summary>What <c>publishId</c> plus the per-table publish ids mean: newest published file per table.</summary>
    internal const string LatestPerTable = "latest-per-table";

    /// <summary>Manifest file extension. Deliberately not <c>.duckdb</c> — see the type's remarks.</summary>
    internal const string Extension = ".json";

    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) },
    };

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

    /// <summary>
    /// Reads only the schema discriminator. Cold-start migration uses this before full v4
    /// validation so a structurally obsolete pre-v4 manifest can be proven old and ignored,
    /// while unreadable JSON or a missing discriminator remains an unclassified hard failure.
    /// </summary>
    internal static int ReadSchemaVersion(PublishStore store, string manifestPath)
    {
        try
        {
            using var document = JsonDocument.Parse(store.ReadAllText(manifestPath));
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("schemaVersion", out var schema)
                || !schema.TryGetInt32(out var schemaVersion)
                || schemaVersion <= 0)
            {
                throw new InvalidDataException(
                    $"'{manifestPath}' has no integer schemaVersion discriminator.");
            }

            return schemaVersion;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"'{manifestPath}' is not readable enough to determine its Hawta schema version.",
                exception);
        }
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

        if (SchemaVersion >= 4 && ManifestVersion < 3)
            throw new InvalidDataException(
                $"'{manifestPath}' is schema v{SchemaVersion} but manifest v{ManifestVersion} cannot carry the required source catalog.");

        if (SchemaVersion >= 3 && ChangeSequenceHighWatermark is null)
            throw new SnapshotSequenceContractException(
                $"'{manifestPath}' is schema v{SchemaVersion} but carries no change-sequence high watermark.");

        if (ChangeSequenceHighWatermark < 0)
            throw new SnapshotSequenceContractException(
                $"'{manifestPath}' carries a negative change-sequence high watermark.");

        if (Tables.Any(entry => entry is null))
            throw new InvalidDataException($"'{manifestPath}' contains a null table entry.");

        var duplicateTables = Tables
            .GroupBy(entry => entry.Table, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateTables is not null)
            throw new InvalidDataException($"'{manifestPath}' names table '{duplicateTables.Key}' more than once.");

        var sourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Tables)
        {
            if (string.IsNullOrWhiteSpace(entry.Table) || entry.Location?.Paths is null)
                throw new InvalidDataException(
                    $"'{manifestPath}' contains a table entry without a table name or location.");
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

            if (SchemaVersion >= 4 && (entry.SourceCatalog is null || entry.SourceCatalog.Count == 0))
                throw new InvalidDataException(
                    $"'{manifestPath}' schema-v{SchemaVersion} entry for '{entry.Table}' has no source catalog.");

            var scopes = new HashSet<string?>(NullableOrdinalIgnoreCaseComparer.Instance);
            foreach (var source in entry.SourceCatalog ?? [])
            {
                if (source is null)
                    throw new InvalidDataException(
                        $"'{manifestPath}' entry for '{entry.Table}' contains a null source catalog entry.");
                if (string.IsNullOrWhiteSpace(source.SourceKey))
                    throw new InvalidDataException(
                        $"'{manifestPath}' entry for '{entry.Table}' has a blank source key.");
                if (source.SourceScope is not null && string.IsNullOrWhiteSpace(source.SourceScope))
                    throw new InvalidDataException(
                        $"'{manifestPath}' source '{source.SourceKey}' for '{entry.Table}' has a blank source scope; null is the only unscoped representation.");
                if (!sourceKeys.Add(source.SourceKey))
                    throw new InvalidDataException(
                        $"'{manifestPath}' source key '{source.SourceKey}' is declared more than once.");
                if (!scopes.Add(source.SourceScope))
                    throw new InvalidDataException(
                        $"'{manifestPath}' entry for '{entry.Table}' declares scope " +
                        $"'{source.SourceScope ?? "<null>"}' more than once.");

                if (source.RecordIdentity is null)
                    throw new InvalidDataException(
                        $"'{manifestPath}' source '{source.SourceKey}' for table '{entry.Table}' has no record-identity contract.");
                source.RecordIdentity.Validate(manifestPath, entry.Table, source.SourceKey);
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
/// A committed manifest cannot safely participate in fallback or publishing because its global
/// sequence floor is absent or invalid. Unlike an unreadable/torn data artifact, falling back
/// past this error could reuse a cursor value already observed by a consumer.
/// </summary>
internal sealed class SnapshotSequenceContractException(string message) : Exception(message);

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
    /// <summary>
    /// Exactly one source declaration for every scope feeding this table. The catalog explains
    /// the canonical <c>_PrimaryKey</c> without repeating source identity metadata on every row.
    /// </summary>
    [JsonPropertyName("sourceCatalog")]
    public IReadOnlyList<PublishedSourceCatalogEntry> SourceCatalog { get; init; } = [];

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

/// <summary>One source owner in a published table's source catalog.</summary>
public sealed record PublishedSourceCatalogEntry(
    [property: JsonPropertyName("sourceKey")] string SourceKey,
    [property: JsonPropertyName("sourceScope")] string? SourceScope,
    [property: JsonPropertyName("recordIdentity")] SourceRecordIdentityDescriptor RecordIdentity);

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
/// <param name="Kind">Storage format. <c>parquet</c> today; the field exists so <c>delta</c> can be added without replacing this location shape.</param>
/// <param name="Paths">Bare file names relative to the manifest's directory. Readers must handle more than one; the publisher currently writes one.</param>
public sealed record PublishedTableLocation(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("paths")] IReadOnlyList<string> Paths)
{
    public const string ParquetKind = "parquet";

    public static PublishedTableLocation Parquet(string file) => new(ParquetKind, [file]);
}

/// <summary>
/// One file source's change-gate stamp, carried so a fresh instance does not have to re-read every
/// feed to rebuild what it already knows. <b>Agent-internal — consumers must ignore this section.</b>
///
/// <para><b>Why it lives in the manifest rather than beside it.</b> The stamp asserts "this exact
/// file was already merged into the data you are looking at". That claim is only true relative to a
/// particular published state, so it has to be committed by the same act that commits the state — a
/// stamp written between a merge and the next publish describes rows that are in no published set
/// yet, and restoring it against the previous manifest would let the gate skip a file whose rows are
/// not there. Riding inside the manifest makes the two impossible to disagree: they are named by one
/// conditional PUT. A sibling artifact would need a second commit, which is exactly the atomicity the
/// manifest-last design exists to avoid.</para>
///
/// <para>A source that merged after the last publish simply has no entry here, so it re-reads on the
/// next cycle — correct, because its work is not in the restored data either.</para>
///
/// <para><see cref="LastWriteUtcTicks"/> is exact ticks, matching <c>meta.SourceFileStamps</c>:
/// NTFS resolves file times to 100 ns and this value is compared for equality, so any narrowing
/// makes an unchanged file read as changed.</para>
/// </summary>
public sealed record PublishedSourceStamp(
    [property: JsonPropertyName("sourceKey")] string SourceKey,
    [property: JsonPropertyName("filePath")] string FilePath,
    [property: JsonPropertyName("length")] long Length,
    [property: JsonPropertyName("lastWriteUtcTicks")] long LastWriteUtcTicks,
    [property: JsonPropertyName("configFingerprint")] string ConfigFingerprint,
    [property: JsonPropertyName("stampedAtUtc")] DateTime StampedAtUtc)
{
    public static PublishedSourceStamp From(SourceFileStamp stamp) => new(
        stamp.SourceKey, stamp.FilePath, stamp.Length, stamp.LastWriteUtc.Ticks,
        stamp.ConfigFingerprint, stamp.StampedAtUtc);

    /// <remarks>
    /// <see cref="SourceFileStamp.StampedAtUtc"/> keeps its ORIGINAL value across a restore. It is
    /// the age a per-source re-ingest bound measures, and resetting it here would silently extend
    /// every configured bound across a restart — precisely when feeds are most likely to have been
    /// swapped underneath us.
    /// </remarks>
    public SourceFileStamp ToStamp() => new(
        SourceKey, FilePath, Length, new DateTime(LastWriteUtcTicks, DateTimeKind.Utc),
        ConfigFingerprint, StampedAtUtc);
}
