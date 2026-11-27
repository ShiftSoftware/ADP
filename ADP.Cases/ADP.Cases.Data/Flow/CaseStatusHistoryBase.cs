using ShiftSoftware.ADP.Cases.Shared.Flow;
using ShiftSoftware.ADP.Cases.Shared.Workflow;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Cases.Data.Flow;

/// <summary>
/// One row of a case's visible history: who did what, when, and with which comment. Rows are only ever appended. A
/// comment without a status change is a row whose <see cref="FromStatus"/> equals <see cref="ToStatus"/>.
/// </summary>
/// <remarks>
/// Each case type has its own table deriving from this, so the foreign key to the case is real and the status column is
/// the case's own enum. SQL temporal tables do not replace it: they say what a row looked like, not who moved it and what
/// they said.
/// </remarks>
/// <typeparam name="TSelf">The concrete history entity.</typeparam>
/// <typeparam name="TStatus">The case type's status enum.</typeparam>
public abstract class CaseStatusHistoryBase<TSelf, TStatus> : ShiftEntity<TSelf>
    where TSelf : ShiftEntity<TSelf>
    where TStatus : struct, Enum
{
    /// <summary>The status before this event. Null on the row that records the case being created.</summary>
    public TStatus? FromStatus { get; set; }

    /// <summary>The status after this event.</summary>
    public TStatus ToStatus { get; set; }

    /// <summary>When the event happened, in UTC.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Who caused the event. Null for anything the system did on its own.</summary>
    public long? ActorUserID { get; set; }

    /// <summary>The name that user went by at the time, copied so the history reads the same later.</summary>
    public string? ActorUserName { get; set; }

    /// <summary>Which side that user was on: one of the <see cref="CaseRoles"/> constants.</summary>
    public string? ActorRole { get; set; }

    /// <summary>The response or note that went with the event.</summary>
    public string? Comment { get; set; }

    /// <summary>True when this row is a comment with no status change.</summary>
    public bool IsNote => FromStatus.HasValue && EqualityComparer<TStatus>.Default.Equals(FromStatus.Value, ToStatus);

    /// <summary>
    /// Copies an entry produced by the case engine onto this row. The engine carries the actor as free text, so the
    /// numeric user id, the name and the side are passed separately.
    /// </summary>
    public void Record(
        CaseStatusHistoryEntry<TStatus> entry, long? actorUserID, string? actorUserName = null, string? actorRole = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        FromStatus = entry.FromStatus;
        ToStatus = entry.ToStatus;
        Timestamp = entry.Timestamp;
        Comment = entry.Comment;
        ActorUserID = actorUserID;
        ActorUserName = actorUserName;
        ActorRole = actorRole;
    }
}
