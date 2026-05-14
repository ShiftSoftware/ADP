using ShiftSoftware.ADP.Surveys.Shared.Triggers;

namespace ShiftSoftware.ADP.Surveys.API.Channels;

/// <summary>
/// Lookup table built once at app startup from all registered <see cref="ISurveyChannel"/>
/// implementations. The scheduler resolves the channel for a <see cref="Data.Entities.SurveyInstance"/>
/// by the key snapshotted on the instance row at trigger-ingest time.
///
/// Duplicate keys are a config error — last one wins for now; if it ever becomes a real
/// issue we'll throw at startup. Slice 4 doesn't validate this strictly because there's
/// only one channel impl in the box.
/// </summary>
public class SurveyChannelRegistry
{
    private readonly Dictionary<string, ISurveyChannel> channels;

    public SurveyChannelRegistry(IEnumerable<ISurveyChannel> channels)
    {
        this.channels = channels.ToDictionary(c => c.Key, c => c, StringComparer.Ordinal);
    }

    public ISurveyChannel? Resolve(string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return channels.GetValueOrDefault(key);
    }

    public IReadOnlyCollection<string> RegisteredKeys => channels.Keys;
}
