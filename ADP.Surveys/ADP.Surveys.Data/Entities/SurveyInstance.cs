using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;

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
public class SurveyInstance : ShiftEntity<SurveyInstance>, IEntityHasUniqueHash<SurveyInstance>
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

    /// <summary>FK by string to <c>SurveyDto.triggers[].id</c> on the pinned version. Null for manually-created instances.</summary>
    public string? TriggerId { get; set; }

    /// <summary>DI key snapshotted at creation, e.g. <c>"whatsapp:default"</c>. Survives later trigger-config edits.</summary>
    public string? Channel { get; set; }

    /// <summary>Channel-specific recipient address (phone, email, etc.). Part of the dedup recipe.</summary>
    public string? RecipientAddress { get; set; }

    /// <summary>ISO locale picked at creation, used for channel template selection at send time.</summary>
    public string? RecipientLocale { get; set; }

    /// <summary>UTC timestamp of the next scheduled send (initial or reminder). Null = no further sends.</summary>
    public DateTimeOffset? NextSendAt { get; set; }

    /// <summary>UTC timestamp of the most recent successful send.</summary>
    public DateTimeOffset? LastSentAt { get; set; }

    /// <summary>Reminder sends still queued. 0 = next send is the last.</summary>
    public int RemindersRemaining { get; set; }

    /// <summary>JSON array of delivery attempts: <c>[{attempt, sentAt, status, error?}, ...]</c>. Diagnostic only.</summary>
    public string? DeliveryLogJson { get; set; }

    public virtual ICollection<SurveyResponse> Responses { get; set; } = new HashSet<SurveyResponse>();

    /// <summary>
    /// The unique-hash column lives as a framework-managed shadow property added by
    /// <c>ModelBuilderExtensions</c> for any <see cref="IEntityHasUniqueHash{T}"/>-implementing
    /// entity. Hash compute lives in the trigger ingest path — both the recipe and the
    /// candidate event payload are needed and live outside this row, so we can't usefully
    /// compute it from instance state alone. Returning null opts out of the framework's
    /// per-row auto-hash; the ingest service stamps the shadow property explicitly.
    /// </summary>
    public string? CalculateUniqueHash() => null;
}
