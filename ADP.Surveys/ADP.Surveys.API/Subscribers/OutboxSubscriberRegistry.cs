using ShiftSoftware.ADP.Surveys.Shared.Triggers;

namespace ShiftSoftware.ADP.Surveys.API.Subscribers;

/// <summary>
/// Lookup table built once at app startup from all registered <see cref="ISurveyResponseSubscriber"/>
/// implementations. The outbox dispatch worker iterates <see cref="All"/> per event.
///
/// Slice 7 fans every outbox event out to every registered subscriber. Per-survey or
/// per-subscriber filtering (e.g., the legacy "if has-a-complaint = Yes" routing) is a
/// future polish — would land as a filter expression on the trigger schema, evaluated
/// against the response answers at dispatch time, reusing <c>LogicEvaluator</c>.
/// </summary>
public class OutboxSubscriberRegistry
{
    private readonly Dictionary<string, ISurveyResponseSubscriber> subscribers;

    public OutboxSubscriberRegistry(IEnumerable<ISurveyResponseSubscriber> subscribers)
    {
        this.subscribers = subscribers.ToDictionary(s => s.Key, s => s, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<ISurveyResponseSubscriber> All => subscribers.Values;
    public IReadOnlyCollection<string> RegisteredKeys => subscribers.Keys;

    public ISurveyResponseSubscriber? Resolve(string? key)
        => string.IsNullOrEmpty(key) ? null : subscribers.GetValueOrDefault(key);
}
