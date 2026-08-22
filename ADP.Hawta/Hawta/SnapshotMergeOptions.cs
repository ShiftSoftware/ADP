namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// What a source record identity means. The registry publishes the matching structured descriptor
/// once per table/scope; merge options retain the kind as an operational consistency signal.
/// </summary>
public enum SourceRecordIdentityKind
{
    /// <summary>A database-owned primary key (for example an application table's numeric ID).</summary>
    DatabaseKey,

    /// <summary>
    /// A deterministic key derived from business columns. A correction to a key component is
    /// represented as tombstone + insert; Hawta cannot prove it is the same upstream record.
    /// </summary>
    LogicalKey,

    /// <summary>
    /// A logical group plus an encounter-order occurrence ordinal. This is explicit no-lineage
    /// mode for repeated rows without a stable key: reordering within the group can re-identify
    /// rows and is intentionally represented as ordinary version churn.
    /// </summary>
    OccurrenceOrdinal,
}

public sealed class SnapshotMergeOptions
{
    private SourceRecordIdentityKind recordIdentityKind = SourceRecordIdentityKind.LogicalKey;

    /// <summary>Source name recorded on the <c>meta.SyncRuns</c> row (e.g. the registry source key).</summary>
    public required string Source { get; init; }

    /// <summary>
    /// Declares the semantics of the staged <c>_PrimaryKey</c>. It must match the registry's
    /// <see cref="SnapshotSource.RecordIdentity"/> descriptor.
    /// </summary>
    public SourceRecordIdentityKind RecordIdentityKind
    {
        get => recordIdentityKind;
        init
        {
            recordIdentityKind = value;
            RecordIdentityKindWasExplicitlySet = true;
        }
    }

    // A caller compiled before this property existed necessarily leaves it unset. File ingestion
    // can then infer OccurrenceOrdinal from its already-explicit OccurrenceRowIdentity without a
    // patch-level behaviour break, while new callers that explicitly mislabel it still fail loud.
    internal bool RecordIdentityKindWasExplicitlySet { get; private set; }

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

    /// <summary>
    /// Suppresses the per-<see cref="SnapshotMerge.Execute"/> run record, for an ingestor that
    /// merges ONE source run in several transactions and writes a single aggregate record itself.
    ///
    /// <para><b>`meta.SyncRuns` means "one row per source run" everywhere it is read</b> — the
    /// publisher takes the newest row per source into the manifest's `sourceRuns`, and the health
    /// checks age off it. The Cosmos change-feed drain merges every <c>MergeBatchSize</c> rows so
    /// the cursor only ever advances past data that landed, which made a 49,532-document bootstrap
    /// write 25 records; the newest held the last batch's 1,532, and that is the number that
    /// reached the manifest. Internal, because a source that needs this is implementing the
    /// aggregate itself.</para>
    /// </summary>
    internal bool SuppressRunRecord { get; init; }

    /// <summary>A copy whose per-merge run record is suppressed; see <see cref="SuppressRunRecord"/>.</summary>
    internal SnapshotMergeOptions WithSuppressedRunRecord() => new()
    {
        Source = Source,
        RecordIdentityKind = RecordIdentityKind,
        SourceScope = SourceScope,
        DeletesEnabled = DeletesEnabled,
        MaxDeletedPercent = MaxDeletedPercent,
        MinDeletedRowsAbsolute = MinDeletedRowsAbsolute,
        ForceDeletes = ForceDeletes,
        MaxAdoptedPercent = MaxAdoptedPercent,
        MinAdoptedRowsAbsolute = MinAdoptedRowsAbsolute,
        ForceAdoptions = ForceAdoptions,
        RunId = RunId,
        SuppressRunRecord = true,
    };

    internal SnapshotMergeOptions WithRecordIdentityKind(SourceRecordIdentityKind identityKind) => new()
    {
        Source = Source,
        RecordIdentityKind = identityKind,
        SourceScope = SourceScope,
        DeletesEnabled = DeletesEnabled,
        MaxDeletedPercent = MaxDeletedPercent,
        MinDeletedRowsAbsolute = MinDeletedRowsAbsolute,
        ForceDeletes = ForceDeletes,
        MaxAdoptedPercent = MaxAdoptedPercent,
        MinAdoptedRowsAbsolute = MinAdoptedRowsAbsolute,
        ForceAdoptions = ForceAdoptions,
        RunId = RunId,
        SuppressRunRecord = SuppressRunRecord,
    };
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
    /// <summary>Staging rows with a NULL identity or required content/replication hash — the ingestor's contract was not met.</summary>
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
    /// <summary>
    /// The source change gate found the file byte-for-byte identical in length and last-write time
    /// to its last SUCCESSFUL merge, under an unchanged ingest configuration and inside the
    /// re-ingest window — so the file was never opened. Recorded as
    /// <c>Skipped:SourceUnchanged</c>.
    ///
    /// <para>This is a <b>run record, not silence</b>: a source that stops being read has to stay
    /// visible in <c>meta.SyncRuns</c>, or a gate holding on stale metadata would look exactly like
    /// a healthy idle feed.</para>
    /// </summary>
    SkippedSourceUnchanged,
    /// <summary>
    /// The gate said the file changed, but the staged content is identical to what the table's
    /// Deferred published copy already holds for this scope — a timestamp refresh or a
    /// byte-different re-delivery of the same rows. The file WAS read and hashed (that part is
    /// local and already paid); what was skipped is hydrating the whole published copy back
    /// just to merge zero changes. Recorded as <c>Skipped:ContentUnchanged</c>, the gate stamp
    /// is refreshed so the next tick skips at the metadata level again, and the table stays
    /// Deferred.
    /// </summary>
    SkippedContentUnchanged,
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
