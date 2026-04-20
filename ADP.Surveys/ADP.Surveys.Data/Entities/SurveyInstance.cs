using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Surveys.Data.Entities;

/// <summary>
/// A one-off survey link handed out to a specific recipient. <see cref="PublicID"/>
/// is the GUID that appears in the URL — knowing it is the only capability required
/// to load and submit the survey (Decision #7: no auth on the public endpoints).
///
/// Pinned to a specific <see cref="SurveyVersion"/> at creation — editing the draft
/// after the instance exists has no effect on what this recipient sees or what's
/// validated at submit time.
/// </summary>
[TemporalShiftEntity]
public class SurveyInstance : ShiftEntity<SurveyInstance>
{
    /// <summary>Public URL anchor. Unique, immutable.</summary>
    public Guid PublicID { get; set; } = Guid.NewGuid();

    public long SurveyID { get; set; }
    public Survey Survey { get; set; } = default!;

    public long SurveyVersionID { get; set; }
    public SurveyVersion SurveyVersion { get; set; } = default!;

    public DateTimeOffset TriggeredAt { get; set; }

    /// <summary>Free-form trigger source — e.g. <c>"ticket:123"</c>, <c>"manual"</c>, <c>"event:service-complete"</c>.</summary>
    public string? TriggeredBy { get; set; }

    public SurveyInstanceStatus Status { get; set; } = SurveyInstanceStatus.Pending;

    /// <summary>Optional opaque reference to the customer in the consumer system.</summary>
    public string? CustomerRef { get; set; }

    /// <summary>
    /// JSON blob of pre-fill / pre-context the trigger captured at creation
    /// (ticket id, appointment details, whatever the consumer wants to correlate).
    /// Follows TIQ's proven pattern.
    /// </summary>
    public string? MetaDataJson { get; set; }

    public virtual ICollection<SurveyResponse> Responses { get; set; } = new HashSet<SurveyResponse>();
}
