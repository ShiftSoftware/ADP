using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShiftSoftware.ADP.EndpointParity.Harness;

// CAPTURE LAYER: HttpClient, System.Text.Json and string only.

/// <summary>
/// Canonical JSON writer — verification.md Rule 5.
///
/// "Canonical" here means exactly four things, and each of them is chosen to PRESERVE a
/// distinction the upgrade could change, not to smooth one away:
///
///   * Object keys sorted ordinally, 2-space indent, LF endings, UTF-8. This is cosmetic —
///     it exists so `git diff` on a golden is readable by a human reviewer, which is the
///     control the whole harness rests on.
///   * `null` and absent are NOT collapsed. The generated mapper may emit a property
///     AutoMapper omitted entirely, or vice versa. That is a wire-contract change a
///     consumer can see, so it must be a diff.
///   * Numbers are re-emitted as their ORIGINAL RAW TEXT, never re-formatted. `1.0` vs
///     `1.00` vs `1` on a decimal? money field is a real serialization change worth seeing
///     on financial DTOs. System.Text.Json's JsonElement preserves the raw token text and
///     writes it verbatim, so the only thing this class must avoid is round-tripping a
///     number through double/decimal.
///   * Array order is PRESERVED. Sort order is semantic in several places; sorting
///     collections to "stabilize" them would erase an ordering regression (Rule 4).
///     Order-insensitive collections are handled in Normalizer, from an explicit per-group
///     allowlist in parity.psd1, never here and never by default.
/// </summary>
public static class Canonical
{
    private static readonly JsonWriterOptions Options = new()
    {
        Indented = true,
        IndentSize = 2,
        // The goldens are read by humans in `git diff`; relaxed escaping keeps non-ASCII
        // (Kurdish/Arabic survey text, brand names) legible rather than as \uXXXX runs.
        // This is a rendering choice and loses no distinction: the decode is lossless.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Writes a node as canonical text. Keys sorted, numbers verbatim, LF endings.</summary>
    public static string Write(JsonNode? node)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, Options))
        {
            WriteNode(writer, Sort(node));
        }

        var text = Encoding.UTF8.GetString(stream.ToArray());

        // LF endings regardless of platform. Written on Windows, potentially diffed on Linux CI.
        return text.Replace("\r\n", "\n");
    }

    /// <summary>
    /// Rebuilds the tree with object keys in ordinal order. Arrays keep their source order.
    /// Returns a detached copy so the caller's node is not mutated.
    /// </summary>
    public static JsonNode? Sort(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
            {
                var result = new JsonObject();
                foreach (var kv in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                    result[kv.Key] = Sort(kv.Value);
                return result;
            }
            case JsonArray arr:
            {
                var result = new JsonArray();
                foreach (var item in arr)
                    result.Add(Sort(item));
                return result;
            }
            case JsonValue val:
                // DeepClone keeps the underlying JsonElement — and therefore the raw
                // number text — intact. Do not "simplify" this to a typed re-create.
                return val.DeepClone();
            default:
                return null;
        }
    }

    private static void WriteNode(Utf8JsonWriter writer, JsonNode? node)
    {
        if (node is null) { writer.WriteNullValue(); return; }
        node.WriteTo(writer, default(JsonSerializerOptions)!);
    }

    /// <summary>Parses text as JSON, or returns null when it is not JSON at all.</summary>
    public static JsonNode? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try { return JsonNode.Parse(text!, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true }); }
        catch (JsonException) { return null; }
    }
}
