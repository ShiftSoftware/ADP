using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Surveys.Data.Entities;

/// <summary>
/// At-least-once delivery outbox for post-response side-effects. Written by
/// <c>PublicSurveyController.SubmitResponse</c> in the same transaction as the
/// response — that's what gives the at-least-once guarantee. Drained by
/// <c>OutboxDispatchService.PollOnceAsync</c> on each scheduler tick.
///
/// Replaces the legacy hardcoded Service Bus fanout (csi-ticket-answered /
/// ssi-ticket-answered / csi-complaint-tickets / potential-csi-complaint-tickets).
/// Per-survey or per-subscriber filtering can be layered on later; slice 7 ships
/// the bare bones: every Pending event is fanned out to every registered
/// <c>ISurveyResponseSubscriber</c>.
/// </summary>
public class SurveyOutboxEvent : ShiftEntity<SurveyOutboxEvent>
{
    public long SurveyResponseID { get; set; }
    public SurveyResponse SurveyResponse { get; set; } = default!;

    public long SurveyInstanceID { get; set; }
    public SurveyInstance SurveyInstance { get; set; } = default!;

    /// <summary>Discriminator for future event types (response-completed, instance-expired, …). Slice 7 only emits <c>"response-completed"</c>.</summary>
    public string EventType { get; set; } = "";

    /// <summary>Serialized <c>SurveyOutboxPayload</c> the subscriber receives. Self-contained — subscribers don't need to re-query the DB.</summary>
    public string PayloadJson { get; set; } = "";

    public SurveyOutboxEventStatus Status { get; set; } = SurveyOutboxEventStatus.Pending;

    /// <summary>Number of dispatch attempts. 0 until the first tick picks it up.</summary>
    public int Attempts { get; set; }

    public DateTimeOffset? DispatchedAt { get; set; }

    /// <summary>JSON array of per-subscriber outcomes: <c>[{key, success, error?, dispatchedAt}, ...]</c>.</summary>
    public string? DispatchLogJson { get; set; }

    public string? LastError { get; set; }
}
