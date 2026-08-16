using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Hawta;

public sealed class SnapshotPublishOptions
{
    private static readonly Regex SnapshotNamePattern = new("^[A-Za-z][A-Za-z0-9_-]*$", RegexOptions.Compiled);

    /// <summary>
    /// Location the read tier is published into — a local folder in dev, the blob container in
    /// production (the write DB itself must never live here). The publisher owns this location
    /// exclusively: retention deletes any parquet the kept manifests don't reference, so never
    /// point two snapshots at one directory.
    /// </summary>
    public required string PublishDirectory { get; init; }

    /// <summary>
    /// Where the publish tier reads and writes. Null means a plain local directory at
    /// <see cref="PublishDirectory"/> — the incumbent behaviour, and what every existing caller
    /// gets without changing a line. Set it to a <see cref="BlobPublishStore"/> to publish into a
    /// container.
    ///
    /// <para>When this is set, <see cref="PublishDirectory"/> performs no IO and only names the
    /// estate in messages and run records. Both exist so a deployment can move the tier without a
    /// flag day, and so the local drills keep exercising the same code path production runs.</para>
    /// </summary>
    public PublishStore? Store { get; init; }

    /// <summary>The store this publish will use. Never null.</summary>
    internal PublishStore ResolveStore() => Store ?? new LocalPublishStore(PublishDirectory);

    /// <summary>
    /// Base name of the manifests consumers resolve, e.g. <c>company-data-read</c>
    /// (published as <c>company-data-read-{ts}.json</c>).
    /// </summary>
    public required string SnapshotName { get; init; }

    /// <summary>Tables to publish. A table absent from this list keeps its parquet only as long as an older kept manifest references it.</summary>
    public required IReadOnlyList<SnapshotTableDefinition> Tables { get; init; }

    /// <summary>
    /// Per-table sort for parquet export (parquet has no index — row-group min/max stats plus
    /// ordering are what give consumer lookups real pruning). Keys are table names; values are
    /// column names (source or bookkeeping). Default: <c>_PrimaryKey</c>.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? SortColumns { get; init; }

    /// <summary>Re-export every table even when its signature is unchanged (the key-gated on-demand path; also heals suspected parquet corruption).</summary>
    public bool Force { get; init; }

    /// <summary>
    /// Published sets kept by retention; parquet referenced by any kept manifest survives.
    ///
    /// <para><b>This is a recovery parameter, not a storage-cost knob.</b> Cold start walks
    /// published sets newest-first and skips any that are torn or incomplete, so "keep 3" also
    /// means "survive 2 bad publishes on startup". It is likewise what lets a consumer that is
    /// mid-refresh against the previous manifest still find its files. Trimming it to save
    /// storage silently shortens both windows, and no test would catch that — which is why the
    /// reason is written here rather than assumed.</para>
    /// </summary>
    public int KeepPublishes { get; init; } = 3;

    /// <summary>
    /// A fixed name, refreshed after each commit, always holding a copy of the newest manifest —
    /// the stable entry point external consumers bookmark, since the committed manifests
    /// themselves are versioned (<c>{SnapshotName}-{ts}.json</c>) and their names change every
    /// publish. Set to null to publish versioned manifests only.
    ///
    /// <para>It is deliberately a <b>copy, not the commit</b>. The versioned manifest is what
    /// resolve-newest, retention and cold start all work from; this is refreshed immediately
    /// afterwards. A crash in between leaves a consumer on the previous publish, whose files
    /// retention still protects — stale but whole, never torn.</para>
    /// </summary>
    public string? StableManifestFileName { get; init; } = "latest.json";

    /// <summary>
    /// Fault-injection hook for tests and operator recovery drills only. Invoked after the
    /// staging manifest is fully written, immediately before its atomic rename to the final
    /// published name. Production callers must leave this unset.
    /// </summary>
    public Action? OnBeforeManifestCommit { get; init; }

    /// <summary>
    /// Whether the publisher sweeps retention in-process after committing. Leave true unless a
    /// standalone <see cref="SnapshotRetention"/> task owns cleanup for this directory. Running
    /// both is harmless — a sweep is idempotent — but running neither means orphaned parquet
    /// accumulates forever.
    /// </summary>
    public bool RetentionEnabled { get; init; } = true;

    /// <summary>
    /// Age floor handed to <see cref="SnapshotRetention"/>. Zero — the default <b>here</b> — is
    /// correct for the in-process sweep: it runs immediately after this publish's own manifest
    /// commit, so the set just written is protected by being <i>referenced</i>, and it holds the
    /// write gate meanwhile, so no other publish can be mid-flight.
    ///
    /// <para>A <b>standalone</b> sweeper is the opposite case and must set a real floor: it can
    /// land in the middle of a publish, where parquet is on disk that the not-yet-committed
    /// manifest does not reference yet, and would delete the set being written.</para>
    /// </summary>
    public TimeSpan RetentionMinimumAge { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Fault-injection hook for retention tests only. When supplied, it performs the requested
    /// delete and returns whether retention should treat it as successful. Production callers
    /// must leave this unset.
    /// </summary>
    public Func<string, bool>? RetentionDelete { get; init; }

    internal void Validate()
    {
        if (!SnapshotNamePattern.IsMatch(SnapshotName))
            throw new ArgumentException($"'{SnapshotName}' is not a valid snapshot name (letters, digits, '-', '_'; must start with a letter).");
        if (Tables.Count == 0)
            throw new ArgumentException("At least one table is required to publish.");
        if (StableManifestFileName is not null
            && (string.IsNullOrWhiteSpace(StableManifestFileName)
                || StableManifestFileName.Contains('/')
                || StableManifestFileName.Contains('\\')))
        {
            throw new ArgumentException(
                $"'{StableManifestFileName}' is not a valid stable manifest name (a bare file name is required).");
        }

        if (KeepPublishes < 2)
            throw new ArgumentException(
                "KeepPublishes must be at least 2 so a consumer reading the previous manifest keeps its files, " +
                "and so cold start has a second set to fall back to when the newest is torn.");

        var duplicates = Tables.GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            throw new ArgumentException($"Duplicate table(s) in Tables: {string.Join(", ", duplicates)}.");

        // A key matching no declared table is a dead configuration entry — the silent-misconfig
        // class this validation exists to prevent (a typo'd table would export unsorted forever).
        if (SortColumns is not null)
        {
            foreach (var key in SortColumns.Keys)
            {
                if (!Tables.Any(t => t.Name.Equals(key, StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException($"SortColumns key '{key}' does not match any published table.");
            }
        }

        foreach (var table in Tables)
        {
            var valid = new HashSet<string>(
                table.Columns.Select(c => c.Name).Concat(BookkeepingColumns.All),
                StringComparer.OrdinalIgnoreCase);

            foreach (var column in SortColumnsFor(table))
            {
                if (!valid.Contains(column))
                    throw new ArgumentException($"Sort column '{column}' does not exist on table '{table.Name}'.");
            }
        }
    }

    internal IReadOnlyList<string> SortColumnsFor(SnapshotTableDefinition table)
    {
        // Case-insensitive independent of the caller's dictionary comparer — table names are
        // compared OrdinalIgnoreCase everywhere else in the slice.
        if (SortColumns is not null)
        {
            foreach (var (key, columns) in SortColumns)
            {
                if (key.Equals(table.Name, StringComparison.OrdinalIgnoreCase) && columns.Count > 0)
                    return columns;
            }
        }
        return [BookkeepingColumns.PrimaryKey];
    }
}

public enum SnapshotPublishStatus
{
    /// <summary>A new manifest was committed (with at least one table re-exported, or under <see cref="SnapshotPublishOptions.Force"/>).</summary>
    Published,

    /// <summary>No table's signature changed — nothing was written; the previous manifest stands.</summary>
    SkippedNoChanges,
}

/// <param name="PublishId">The publish stamp (also embedded in every file name this run wrote).</param>
/// <param name="ManifestFile">Bare filename of the committed manifest (for <see cref="SnapshotPublishStatus.SkippedNoChanges"/>: the standing manifest).</param>
/// <param name="FilesSkippedByRetention">Files retention could not delete this pass (typically held open by a consumer); retried next publish.</param>
/// <param name="ParquetCleanupSkipped">True when a kept manifest was unreadable, so unreferenced-parquet deletion was skipped entirely (conservative).</param>
/// <param name="TablesWithNoRows">
/// Published tables carrying zero rows. A family whose ingest is not wired yet still publishes
/// as valid, well-formed, EMPTY parquet, which a consumer cannot tell from "no rows today" —
/// so the condition is named here (and <c>rowCount</c> is in the manifest) rather than left to
/// be discovered in a report as a silent zero.
/// </param>
public sealed record SnapshotPublishResult(
    string PublishId,
    SnapshotPublishStatus Status,
    string? ManifestFile,
    IReadOnlyList<string> TablesExported,
    IReadOnlyList<string> TablesReused,
    int ManifestsDeleted,
    int ParquetFilesDeleted,
    int FilesSkippedByRetention,
    bool ParquetCleanupSkipped,
    IReadOnlyList<string> TablesWithNoRows);
