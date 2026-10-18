using System.Globalization;
using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Triggers;

namespace ShiftSoftware.ADP.Surveys.Shared.Triggers;

/// <summary>
/// Evaluates a <see cref="TriggerDeadlineDto"/> against a candidate payload. Shared by ingest
/// (a past-deadline event creates no instance) and the scheduler (a past-deadline instance is not
/// sent), and public so a host that derives its own work from instances can apply the same rule.
/// </summary>
public static class TriggerDeadline
{
    /// <summary>
    /// True when <paramref name="now"/> is beyond the event's date plus the deadline. False when
    /// the trigger has no deadline. An event whose date is missing or unreadable — or a deadline
    /// whose duration does not parse — counts as past it: an event that cannot be placed in time
    /// is exactly the one the deadline exists to stop.
    /// </summary>
    public static bool IsPast(TriggerDeadlineDto? deadline, JsonElement candidatePayload, DateTimeOffset now)
    {
        if (deadline is null) return false;
        if (!TriggerDuration.TryParse(deadline.Within, out var within)) return true;

        return TryReadEventDate(deadline.Field, candidatePayload, out var occurredAt)
            ? now > occurredAt + within
            : true;
    }

    /// <inheritdoc cref="IsPast(TriggerDeadlineDto?, JsonElement, DateTimeOffset)"/>
    public static bool IsPast(TriggerDeadlineDto? deadline, string? candidatePayloadJson, DateTimeOffset now)
    {
        if (deadline is null) return false;
        if (string.IsNullOrWhiteSpace(candidatePayloadJson)) return true;

        try
        {
            using var doc = JsonDocument.Parse(candidatePayloadJson);
            return IsPast(deadline, doc.RootElement, now);
        }
        catch (JsonException)
        {
            return true;
        }
    }

    static bool TryReadEventDate(string? field, JsonElement payload, out DateTimeOffset occurredAt)
    {
        occurredAt = default;
        const string prefix = TriggerDeadlineDtoValidator.CandidatePrefix;
        if (string.IsNullOrEmpty(field)
            || !field.StartsWith(prefix, StringComparison.Ordinal)
            || payload.ValueKind != JsonValueKind.Object
            || !payload.TryGetProperty(field[prefix.Length..], out var value)
            || value.ValueKind != JsonValueKind.String)
            return false;

        return DateTimeOffset.TryParse(
            value.GetString(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out occurredAt);
    }
}
