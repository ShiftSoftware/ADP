using ShiftSoftware.ADP.Cases.Shared.Flow;

namespace ShiftSoftware.ADP.Cases.Shared.DTOs;

/// <summary>One line of a case's history as it is shown. Read-only.</summary>
/// <typeparam name="TStatus">The case type's status enum.</typeparam>
public class CaseStatusHistoryDTO<TStatus> where TStatus : struct, Enum
{
    /// <summary>The status before this line, or null on the line that records the case being raised.</summary>
    public TStatus? FromStatus { get; set; }

    /// <summary>The status after this line.</summary>
    public TStatus ToStatus { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Who did it, under the name they went by at the time.</summary>
    public string? ActorUserName { get; set; }

    /// <summary>Which side they were on: one of the <see cref="CaseRoles"/> constants.</summary>
    public string? ActorRole { get; set; }

    public string? Comment { get; set; }

    /// <summary>True when this line is a comment with no status change.</summary>
    public bool IsNote { get; set; }
}
