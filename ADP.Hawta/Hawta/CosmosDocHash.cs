using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// Canonical hash over a MAPPED document shape — the recon hash. Both sides of a
/// duck-vs-cosmos comparison hash the same canonical form: properties sorted recursively,
/// NUMBERS normalized by VALUE (Cosmos re-renders number text on read — a written
/// <c>1.500</c> comes back <c>1.5</c>, so raw-text hashing could never match), invariant
/// JSON formatting, SHA-256 hex. (This is deliberately NOT <see cref="RowHash"/>: that is
/// the ingest change-detection hash over the raw source row.)
/// </summary>
public static class CosmosDocHash
{
    public static string Compute(CosmosDocument document)
    {
        var node = JsonSerializer.SerializeToNode(document.Body) as JsonObject
            ?? throw new ArgumentException("Document body must serialize to a JSON object.", nameof(document));
        node["id"] = document.Id;
        return Compute(node);
    }

    /// <summary>Hashes any JSON object in canonical form — use on the Cosmos-read side with the same field list.</summary>
    public static string Compute(JsonObject json)
    {
        var canonical = Canonicalize(json)!.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static JsonNode? Canonicalize(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(obj
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => KeyValuePair.Create(p.Key, Canonicalize(p.Value)))),
        JsonArray array => new JsonArray(array.Select(Canonicalize).ToArray()),
        JsonValue value => CanonicalizeLeaf(value),
        null => null,
        _ => node.DeepClone(),
    };

    private static JsonNode CanonicalizeLeaf(JsonValue value)
    {
        if (value.TryGetValue<JsonElement>(out var element))
        {
            return element.ValueKind == JsonValueKind.Number
                ? NormalizeNumber(element.GetRawText())
                : value.DeepClone();
        }

        // CLR-backed leaf (a value that never went through a JSON parse).
        return value.GetValueKind() == JsonValueKind.Number
            ? NormalizeNumber(value.ToJsonString())
            : value.DeepClone();
    }

    /// <summary>One canonical text per numeric VALUE: 1.500, 1.5, and 15E-1 all hash alike.</summary>
    private static JsonNode NormalizeNumber(string rawText)
    {
        if (decimal.TryParse(rawText, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return JsonNode.Parse(number.ToString("0.############################", CultureInfo.InvariantCulture))!;

        // Out of decimal's range/precision — canonicalize through double's round-trip text.
        var wide = double.Parse(rawText, CultureInfo.InvariantCulture);
        return JsonNode.Parse(wide.ToString("R", CultureInfo.InvariantCulture))!;
    }
}
