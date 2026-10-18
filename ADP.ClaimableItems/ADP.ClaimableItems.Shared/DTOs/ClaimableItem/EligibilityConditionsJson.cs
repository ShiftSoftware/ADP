using System.Text.Json;
using System.Text.Json.Serialization;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;

/// <summary>Reads authored conditions and rejects any leaf or group outside the shared grammar shapes.</summary>
public static class EligibilityConditionsJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false,
    };

    public static List<EligibilityConditionModel>? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        var conditions = JsonSerializer.Deserialize<List<EligibilityConditionModel>>(json, Options);
        // Top-level leaves and anyOf alternatives pass the same shape rules, so a misspelled field or an
        // unreadable value is rejected here instead of silently hiding the item at lookup.
        if (conditions?.Any(c => c is null || (c.AnyOf is not null
                ? !EligibilityConditionGroup.IsValid(c)
                : !EligibilityConditionGroup.IsSupportedLeaf(c))) == true)
            throw new JsonException("Use supported leaf conditions or a non-empty anyOf of them. Groups cannot nest or mix leaf fields; set whenUnmet on the group.");

        // The lookup evaluator remains the runtime authority and still fails closed on anything it cannot read.
        return conditions;
    }

    public static bool IsValid(string? json)
    {
        try { Parse(json); return true; }
        catch (JsonException) { return false; }
    }
}
