using System;
using System.Collections.Generic;
using System.Text.Json;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ShiftSoftware.ADP.Lookup.Services.DuckDB;

/// <summary>Catalog JSON is authored with either casing. An unreadable rule must never become an unconditional item.</summary>
internal static class EligibilityConditionJsonCell
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    internal static IEnumerable<EligibilityConditionModel> Read(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (HasDuplicateMembers(document.RootElement)) return Invalid();
            return JsonSerializer.Deserialize<List<EligibilityConditionModel>>(json, Options);
        }
        catch (JsonException)
        {
            return Invalid();
        }
    }

    private static IEnumerable<EligibilityConditionModel> Invalid() =>
        new[] { new EligibilityConditionModel { Field = "unsupported.invalidConditionJson" } };

    private static bool HasDuplicateMembers(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
                if (!names.Add(property.Name) || HasDuplicateMembers(property.Value)) return true;
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray())
                if (HasDuplicateMembers(child)) return true;
        return false;
    }
}
