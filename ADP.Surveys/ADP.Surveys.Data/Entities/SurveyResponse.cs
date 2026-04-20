using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Surveys.Data.Entities;

/// <summary>
/// Header row per submitted response. The actual answers land in <see cref="SurveyAnswer"/>
/// — one row per answered question. Normalized per Decision #3 so BI can query by
/// banked question id (Decision #11's <c>BankEntryID</c> on each answer).
///
/// A single <see cref="SurveyInstance"/> may have multiple responses in theory (agent
/// drafts + customer re-submits, retry after partial submit) but the common case is one.
/// </summary>
public class SurveyResponse : ShiftEntity<SurveyResponse>
{
    public long SurveyInstanceID { get; set; }
    public SurveyInstance SurveyInstance { get; set; } = default!;

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Set when the response was submitted via the agent-assist iframe. Analytics only.</summary>
    public string? AgentId { get; set; }

    public SurveyInstanceStatus Status { get; set; } = SurveyInstanceStatus.Opened;

    public virtual ICollection<SurveyAnswer> Answers { get; set; } = new HashSet<SurveyAnswer>();
}
