using ShiftSoftware.ADP.Surveys.Shared.Triggers;

namespace ShiftSoftware.ADP.Surveys.API.Subscribers;

/// <summary>
/// Always-succeeds in-memory subscriber. Used by the e2e harness so the dispatch path
/// can execute end-to-end without a real Service Bus or webhook target. The outbox row's
/// <c>DispatchLogJson</c> is the inspection surface for what was sent.
///
/// Real deployments register their actual subscribers (Service Bus senders, webhook
/// clients, in-process handlers) alongside or in place of this one.
/// </summary>
public class InMemoryOutboxSubscriber : ISurveyResponseSubscriber
{
    public string Key => "memory:default";

    public Task<SubscriberDispatchResult> DispatchAsync(SurveyOutboxPayload payload, CancellationToken ct)
    {
        return Task.FromResult(SubscriberDispatchResult.Ok());
    }
}
