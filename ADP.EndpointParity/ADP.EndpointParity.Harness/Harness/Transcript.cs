using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShiftSoftware.ADP.EndpointParity.Harness;

// CAPTURE LAYER (verification.md §2/§3): HttpClient, System.Text.Json and string only.
// Nothing in this file may name a framework, DTO or repository type.

/// <summary>
/// One captured request/response pair, normalized and ready to be written as a golden.
///
/// The REQUEST is stored alongside the response deliberately (verification.md §5): if
/// RequestFactory emits different bytes across two runs — a DTO changed, reflection order
/// shifted — the request diff fires first and tells you the comparison itself is invalid,
/// instead of a response diff you would misread as a behaviour regression.
/// </summary>
public sealed class Transcript
{
    public required string CaseName { get; init; }

    /// <summary>Case kind: LIST, DETAIL, REVISIONS, ASOF, PRINT, PRINTTOKEN, CREATE, READBACK, UPDATE, REMOVE, GONE.</summary>
    public required string Kind { get; init; }

    public required string Method { get; init; }
    public required string Url { get; init; }

    /// <summary>Which principal issued it — FullAccess or Restricted (verification.md §8.7).</summary>
    public required string Grant { get; init; }

    public string? RequestBody { get; init; }

    public required int Status { get; init; }
    public required string? ContentType { get; init; }

    /// <summary>Only the per-group header allowlist survives here (Rule 3).</summary>
    public required SortedDictionary<string, string> Headers { get; init; }

    /// <summary>Normalized response body. JSON is canonicalized; text stays text.</summary>
    public string? Body { get; init; }

    /// <summary>
    /// The response body EXACTLY as it came off the wire, before normalization. Never written to
    /// a golden - it exists only so the UPDATE round-trip can re-send the row the server just
    /// rendered. The normalized copy is unusable for that: Rule 2 rewrites CreateDate and
    /// LastSaveDate to &lt;ts&gt;, which then fails to parse as a DateTimeOffset and 400s the PUT.
    /// </summary>
    public string? RawBody { get; init; }

    /// <summary>
    /// Set when the body could not be compared byte-for-byte and a reduced record was taken
    /// instead (Rule 7 — binaries). A PARTIAL case is NOT a covered case and `summary` counts
    /// it separately so it can never pass silently as covered.
    /// </summary>
    public string? Partial { get; init; }

    /// <summary>
    /// Values that parsed as a timestamp inside [runStart-5min, now] but are NOT on Rule 2's
    /// name allowlist. Reported, never normalized — classify once, then add deliberately.
    /// </summary>
    public List<string> SuspectedVolatile { get; init; } = new();

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        // Rule 5: canonical JSON. Escaping is left at the .NET default so the goldens are
        // stable text; indentation is 2 spaces (set below via JsonWriterOptions on write).
    };

    /// <summary>
    /// Serializes the transcript as the golden file's text. Keys are emitted in a fixed order
    /// (not alphabetical over the wrapper) so a golden diff reads top-down: what was asked,
    /// then what came back.
    /// </summary>
    public string ToGolden()
    {
        var o = new JsonObject
        {
            ["case"] = CaseName,
            ["kind"] = Kind,
            ["grant"] = Grant,
            ["request"] = new JsonObject
            {
                ["method"] = Method,
                ["url"] = Url,
                ["body"] = RequestBody is null ? null : ParseOrString(RequestBody),
            },
            ["response"] = new JsonObject
            {
                ["status"] = Status,
                ["contentType"] = ContentType,
                ["headers"] = ToNode(Headers),
                ["body"] = Body is null ? null : ParseOrString(Body),
            },
        };

        if (Partial is not null)
            o["partial"] = Partial;

        if (SuspectedVolatile.Count > 0)
        {
            var arr = new JsonArray();
            foreach (var s in SuspectedVolatile.OrderBy(x => x, StringComparer.Ordinal))
                arr.Add(s);
            o["suspectedVolatile"] = arr;
        }

        return Canonical.Write(o);
    }

    private static JsonNode? ParseOrString(string s)
    {
        try { return JsonNode.Parse(s); }
        catch (JsonException) { return JsonValue.Create(s); }
    }

    private static JsonNode ToNode(SortedDictionary<string, string> d)
    {
        var o = new JsonObject();
        foreach (var kv in d) o[kv.Key] = kv.Value;
        return o;
    }
}
