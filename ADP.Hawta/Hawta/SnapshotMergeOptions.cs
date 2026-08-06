namespace ShiftSoftware.ADP.Hawta;

public sealed class SnapshotMergeOptions
{
    /// <summary>Source name recorded on the <c>meta.SyncRuns</c> row (e.g. the registry source key).</summary>
    public required string Source { get; init; }

    /// <summary>
    /// The scope this run's staging represents (e.g. one file of a multi-file family). The
    /// tombstone anti-join is bounded to rows carrying this scope, so one file's absence can
    /// never tombstone another file's rows. Null = the family has a single universe.
    /// </summary>
    public string? SourceScope { get; init; }

    /// <summary>
    /// True for full-universe sources only: rows in scope absent from staging are tombstoned
    /// (<c>_Deleted = true</c>). Leave false for partial/incremental staging (e.g. Change
    /// Tracking net-changes), where absence means "unchanged", not "gone".
    /// </summary>
    public bool DeletesEnabled { get; init; }

    /// <summary>Mass-delete guardrail: abort when would-be tombstones exceed this fraction of live rows…</summary>
    public double MaxDeletedPercent { get; init; } = 0.20;

    /// <summary>…AND exceed this absolute count. Both must trip for an abort.</summary>
    public int MinDeletedRowsAbsolute { get; init; } = 50;

    /// <summary>Key-gated override for intentional purges: skips the mass-delete guardrail for this run.</summary>
    public bool ForceDeletes { get; init; }

    /// <summary>
    /// Mass-adoption guardrail (the delete guardrail's mirror): abort when the staged rows
    /// ADOPTING keys live under a different <c>_SourceScope</c> exceed this fraction of the
    /// staging…
    /// </summary>
    public double MaxAdoptedPercent { get; init; } = 0.20;

    /// <summary>…AND exceed this absolute count. Both must trip for an abort.</summary>
    public int MinAdoptedRowsAbsolute { get; init; } = 50;

    /// <summary>Override for an INTENTIONAL scope migration: skips the mass-adoption guardrail for this run.</summary>
    public bool ForceAdoptions { get; init; }

    /// <summary>
    /// Run id recorded on <c>meta.SyncRuns</c>. Null (the default) generates a fresh GUID per
    /// <see cref="SnapshotMerge.Execute"/> call, so an options instance is safely reusable.
    /// </summary>
    public string? RunId { get; init; }
}

public enum SnapshotMergeStatus
{
    Succeeded,
    AbortedMassDelete,
    /// <summary>
    /// The mass-adoption guardrail tripped: most of this staging's keys are live under a
    /// DIFFERENT <c>_SourceScope</c> — the mis-pasted-connection-string signature. Recorded
    /// as <c>Aborted:MassAdoption</c>; <see cref="SnapshotMergeOptions.ForceAdoptions"/> is
    /// the intentional-migration path.
    /// </summary>
    AbortedMassAdoption,
    FailedDuplicateStagingKeys,
    /// <summary>Staging rows with NULL _PrimaryKey or NULL _RowHash — the ingestor's contract was not met.</summary>
    FailedInvalidStagingRows,
    /// <summary>The merge threw mid-run; everything rolled back and a Failed:Exception run record was written.</summary>
    Failed,
    /// <summary>
    /// The source file was absent (renamed, unmounted, mid-upload), so nothing was staged or
    /// merged — recorded on <c>meta.SyncRuns</c> as <c>Skipped:SourceAbsent</c>. Absence is
    /// never treated as an empty universe: no tombstones, no guardrail, no change.
    /// </summary>
    SkippedSourceAbsent,
    /// <summary>
    /// The source file exists but produced zero rows (0-byte or header-only — the normal
    /// mid-upload window on an SMB share) while the merge would have been delete-enabled.
    /// Recorded as <c>Skipped:SourceEmpty</c>; nothing merged. A full-universe feed that
    /// reads empty is presumed torn, never a purge — <see cref="SnapshotMergeOptions.ForceDeletes"/>
    /// is the intentional-wipe path.
    /// </summary>
    SkippedSourceEmpty,
}

/// <param name="PendingDeletes">On <see cref="SnapshotMergeStatus.AbortedMassDelete"/>: how many tombstones the run would have written.</param>
/// <param name="RowsRescoped">Live rows this run adopted from a DIFFERENT <c>_SourceScope</c>.
/// A one-time burst is a legitimate scope migration; a persistent non-zero count means two
/// sources both claim the same keys (cross-scope churn) — alarm, don't ignore.</param>
public sealed record SnapshotMergeResult(
    string RunId,
    SnapshotMergeStatus Status,
    long RowsStaged,
    long RowsInserted,
    long RowsUpdated,
    long RowsTombstoned,
    long PendingDeletes = 0,
    long RowsRescoped = 0)
{
    public bool Succeeded => Status == SnapshotMergeStatus.Succeeded;
}
