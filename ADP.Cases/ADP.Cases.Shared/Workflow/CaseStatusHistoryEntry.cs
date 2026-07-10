namespace ShiftSoftware.ADP.Cases.Shared.Workflow;

/// <summary>
/// A persistence-agnostic record of one case-history event, produced by
/// <see cref="CaseWorkflow{TStatus, TTrigger}"/> for every applied transition and every note.
/// The consumer maps it onto its own append-only, temporal history entity (per skr-dtr D5:
/// history is immutable and visible to all roles; a note without a status change is recorded as a
/// row whose <see cref="FromStatus"/> equals <see cref="ToStatus"/>).
/// </summary>
/// <typeparam name="TStatus">The consumer's case-status enum.</typeparam>
public sealed record CaseStatusHistoryEntry<TStatus>(
    TStatus? FromStatus,
    TStatus ToStatus,
    DateTimeOffset Timestamp,
    string? ActorUserId,
    string? Comment)
    where TStatus : struct, Enum
{
    /// <summary>True when this entry records a note only (no status change).</summary>
    public bool IsNote => FromStatus.HasValue && EqualityComparer<TStatus>.Default.Equals(FromStatus.Value, ToStatus);
}
