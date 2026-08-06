using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Hawta;

public sealed class SnapshotPublishOptions
{
    private static readonly Regex SnapshotNamePattern = new("^[A-Za-z][A-Za-z0-9_-]*$", RegexOptions.Compiled);

    /// <summary>
    /// Directory the read tier is published into — a local folder in dev, the mounted file
    /// share in production (the write DB itself must never live here). The publisher owns
    /// this directory exclusively: retention deletes any parquet the kept shims don't
    /// reference, so never point two snapshots at one directory.
    /// </summary>
    public required string PublishDirectory { get; init; }

    /// <summary>
    /// Base name of the views-shim files consumers resolve, e.g. <c>company-data-read</c>
    /// (published as <c>company-data-read-{ts}.duckdb</c>).
    /// </summary>
    public required string SnapshotName { get; init; }

    /// <summary>Tables to publish. A table absent from this list keeps its parquet only as long as an older kept shim references it.</summary>
    public required IReadOnlyList<SnapshotTableDefinition> Tables { get; init; }

    /// <summary>
    /// Per-table sort for parquet export (parquet has no index — row-group min/max stats plus
    /// ordering are what give consumer lookups real pruning). Keys are table names; values are
    /// column names (source or bookkeeping). Default: <c>_PrimaryKey</c>.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? SortColumns { get; init; }

    /// <summary>Re-export every table even when its signature is unchanged (the key-gated on-demand path; also heals suspected parquet corruption).</summary>
    public bool Force { get; init; }

    /// <summary>Shims kept by retention; parquet referenced by any kept shim survives.</summary>
    public int KeepShims { get; init; } = 3;

    /// <summary>
    /// Fault-injection hook for tests and operator recovery drills only. Invoked after the
    /// staging shim is fully closed and checkpointed, immediately before its atomic rename
    /// to the final published name. Production callers must leave this unset.
    /// </summary>
    public Action? OnBeforeShimCommit { get; init; }

    internal void Validate()
    {
        if (!SnapshotNamePattern.IsMatch(SnapshotName))
            throw new ArgumentException($"'{SnapshotName}' is not a valid snapshot name (letters, digits, '-', '_'; must start with a letter).");
        if (Tables.Count == 0)
            throw new ArgumentException("At least one table is required to publish.");
        if (KeepShims < 1)
            throw new ArgumentException("KeepShims must be at least 1.");

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
    /// <summary>A new shim was committed (with at least one table re-exported, or under <see cref="SnapshotPublishOptions.Force"/>).</summary>
    Published,

    /// <summary>No table's signature changed — nothing was written; the previous shim stands.</summary>
    SkippedNoChanges,
}

/// <param name="PublishId">The publish stamp (also embedded in every file name this run wrote).</param>
/// <param name="ShimFile">Bare filename of the committed shim (for <see cref="SnapshotPublishStatus.SkippedNoChanges"/>: the standing shim).</param>
/// <param name="FilesSkippedByRetention">Files retention could not delete this pass (typically held open by a consumer); retried next publish.</param>
/// <param name="ParquetCleanupSkipped">True when a kept shim's manifest was unreadable, so unreferenced-parquet deletion was skipped entirely (conservative).</param>
public sealed record SnapshotPublishResult(
    string PublishId,
    SnapshotPublishStatus Status,
    string? ShimFile,
    IReadOnlyList<string> TablesExported,
    IReadOnlyList<string> TablesReused,
    int ShimsDeleted,
    int ParquetFilesDeleted,
    int FilesSkippedByRetention,
    bool ParquetCleanupSkipped);
