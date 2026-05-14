using ShiftSoftware.ADP.Surveys.Shared.Triggers;

namespace ShiftSoftware.ADP.Surveys.API.Channels;

/// <summary>
/// Always-succeeds in-memory channel — used by the e2e harness so the scheduler-tick
/// path can execute end-to-end without needing a real WhatsApp / SMS / email backend
/// stood up. The instance row's <c>DeliveryLogJson</c> is the inspection surface for
/// what got sent.
///
/// Production deployments should register their real <see cref="ISurveyChannel"/>
/// implementations under whichever keys their authored triggers reference.
/// </summary>
public class InMemorySurveyChannel : ISurveyChannel
{
    public string Key => "memory:default";

    public Task<ChannelSendResult> SendAsync(ChannelSendRequest request, CancellationToken ct)
    {
        return Task.FromResult(ChannelSendResult.Success(providerMessageId: $"mem-{Guid.NewGuid():N}"));
    }
}
