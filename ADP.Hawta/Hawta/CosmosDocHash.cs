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
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalText(json)))).ToLowerInvariant();
    }

    /// <summary>
    /// The canonical text this hash is taken over: properties sorted recursively, numbers
    /// normalized by value, no indentation.
    ///
    /// <para>Exposed because a Cosmos-read ingestor that STORES a nested fragment as a JSON string
    /// column has the same problem for a different reason. <see cref="RowHash"/> hashes the byte
    /// literal of the stored VARCHAR, and Cosmos re-renders numbers on read (a written
    /// <c>1.500</c> comes back <c>1.5</c>), so storing the service's raw text would change
    /// <c>_RowHash</c> on drift in property order, whitespace, escaping or number rendering — and
    /// every re-read of an unchanged container would republish every row instead of being the
    /// promised no-op.</para>
    /// </summary>
    public static string CanonicalText(JsonNode? json) =>
        Canonicalize(json)?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? "null";

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
