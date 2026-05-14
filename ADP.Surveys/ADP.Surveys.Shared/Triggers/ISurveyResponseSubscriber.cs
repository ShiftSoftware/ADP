using System.Text.Json;

namespace ShiftSoftware.ADP.Surveys.Shared.Triggers;

/// <summary>
/// Pluggable post-response fanout. Each subscriber registers under a stable <see cref="Key"/>
/// in DI; the outbox dispatch worker iterates registered subscribers per drained event.
///
/// Replaces the legacy hardcoded Service Bus routing (csi-ticket-answered, ssi-ticket-answered,
/// csi-complaint-tickets, potential-csi-complaint-tickets). Real subscribers wrap whatever
/// downstream transport the consumer uses — Service Bus, webhook, in-process handler, etc.
/// </summary>
public interface ISurveyResponseSubscriber
{
    /// <summary>e.g. <c>"servicebus:csi-tickets"</c>, <c>"webhook:bi-ingest"</c>, <c>"memory:default"</c>.</summary>
    string Key { get; }

    Task<SubscriberDispatchResult> DispatchAsync(SurveyOutboxPayload payload, CancellationToken ct);
}

public class SurveyOutboxPayload
{
    public long SurveyId { get; set; }
    public int SurveyVersion { get; set; }
    public string? SurveyIntegrationId { get; set; }
    public Guid InstancePublicId { get; set; }
    public string? TriggerId { get; set; }
    public string? CustomerRef { get; set; }
    public string? RecipientAddress { get; set; }
    public string? RecipientLocale { get; set; }

    /// <summary>Slice 7 always emits <c>"response-completed"</c>. Future events (instance-expired, instance-opened) extend this.</summary>
    public string EventType { get; set; } = "response-completed";

    public DateTimeOffset CompletedAt { get; set; }
    public string? AgentId { get; set; }

    /// <summary>Submitted answers keyed by question id at submission. Values are raw JSON elements.</summary>
    public Dictionary<string, JsonElement> Answers { get; set; } = new();

    /// <summary>The candidate payload that originally fired the trigger (vehicle invoice, service visit, …). Subscribers correlate via this.</summary>
    public string? CandidateMetadataJson { get; set; }
}

public class SubscriberDispatchResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }

    public static SubscriberDispatchResult Ok() => new() { Success = true };
    public static SubscriberDispatchResult Fail(string error) => new() { Success = false, Error = error };
}
