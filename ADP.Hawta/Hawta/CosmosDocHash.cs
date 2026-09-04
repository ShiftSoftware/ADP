using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// Canonical hash over a MAPPED document shape — the recon hash. Both sides of a
/// duck-vs-cosmos comparison hash the same canonical form: properties sorted recursively,
/// NUMBERS normalized by VALUE (Cosmos re-renders number text on read — a written
/// <c>1.500</c> comes back <c>1.5</c>, so raw-text hashing could never match), invariant
/// JSON formatting, SHA-256 hex. (This is deliberately NOT <see cref="RowHash"/>: that is
/// the ingest change-detection hash over the raw source row.)
///
/// <para><b>Offset date-times hash by INSTANT, at microsecond precision.</b> A writer that
/// serializes a <c>DateTimeOffset</c> keeps the offset it was given (<c>…T09:03:44.9551234+05:00</c>);
/// a snapshot that stores the same value as a UTC <c>TIMESTAMP</c> renders it
/// <c>…T04:03:44.955123Z</c> — DuckDB holds microseconds, and DuckDB.NET truncates the 100 ns
/// ticks on every write path (measured 2026-09-04). Same moment, two texts. The canonical form
/// is the UTC instant truncated to six fractional digits, so two documents that agree on the
/// instant hash alike and two that differ by a microsecond still differ.</para>
///
/// <para><b>Date-times without an offset keep their text but lose the seventh digit too.</b>
/// A <c>datetime2(7)</c> column serialized by the app (<c>…14.1078611</c>) and the same value
/// through the snapshot (<c>…14.107861</c>) are one value with two renderings for the same
/// storage reason; the canonical form is the text as written with the fraction truncated to six
/// digits. No offset is added and none is inferred: <c>…T13:06:08</c> and <c>…T13:06:08Z</c> stay
/// different, because consumers parse them differently. Bare dates and free text are compared
/// as written.</para>
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
            return element.ValueKind switch
            {
                JsonValueKind.Number => NormalizeNumber(element.GetRawText()),
                JsonValueKind.String => NormalizeString(element.GetString()!),
                _ => value.DeepClone(),
            };
        }

        // CLR-backed leaf (a value that never went through a JSON parse): its JSON text is the
        // one rendering both sides can agree on, whatever CLR type is behind it.
        var text = value.ToJsonString();
        if (value.GetValueKind() == JsonValueKind.Number)
            return NormalizeNumber(text);
        if (text.Length > 0 && text[0] == '"')
            return NormalizeString(JsonSerializer.Deserialize<string>(text)!);
        return value.DeepClone();
    }

    /// <summary>
    /// ISO-8601 date-time text as .NET renders it: seconds, an optional fraction of up to seven
    /// digits, and an optional offset (or <c>Z</c>). Anything else — a bare date, free text — is
    /// not a date-time for the purposes of this hash and is compared as written.
    /// </summary>
    private static readonly Regex DateTimeText = new(
        @"^(?<stamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(?:\.(?<fraction>\d{1,7}))?(?<offset>Z|[+-]\d{2}:\d{2})?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static JsonNode NormalizeString(string text)
    {
        var match = DateTimeText.Match(text);
        if (!match.Success)
            return JsonValue.Create(text)!;

        if (!match.Groups["offset"].Success)
        {
            // No instant to agree on: the text is the value, at the precision the snapshot can hold.
            var fraction = match.Groups["fraction"].Value.PadRight(6, '0')[..6];
            return JsonValue.Create($"{match.Groups["stamp"].Value}.{fraction}")!;
        }

        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var instant))
            return JsonValue.Create(text)!;

        var utcTicks = instant.UtcTicks;
        var microsecondTicks = utcTicks - utcTicks % 10;    // truncate, as DuckDB.NET does on write
        return JsonValue.Create(
            new DateTime(microsecondTicks, DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture))!;
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
