using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.EndpointParity.Harness;

// ============================================================================================
// CAPTURE LAYER (verification.md 2/3). HttpClient, System.Text.Json and string ONLY.
// No ShiftEntityResponse<T>, no DTO type, no IMapper, no repository type may appear in this
// file or anywhere else under Harness/. That rule is what makes the observation code
// version-independent even though it compiles once against each framework version.
// ============================================================================================

/// <summary>
/// ALL normalization rules from verification.md section 4, in one file, each naming the rule
/// it implements.
///
/// <para>
/// <b>This is the file a reviewer reads to decide whether to trust a green run.</b>
/// Normalization is where this harness is won or lost: over-normalize and you erase the
/// regression, under-normalize and the diff is unreadable noise that gets ignored. The bias
/// throughout is deliberately toward UNDER-normalizing - an unexplained diff you have to look
/// at beats a silent pass.
/// </para>
///
/// <para>
/// If a rule is ever loosened, the loosening gets a comment right here saying what signal it
/// gives up. Every rule you loosen is a regression you can no longer see.
/// </para>
/// </summary>
public sealed class Normalizer
{
    private readonly NormalizerOptions options;

    /// <summary>Values flagged by Rule 2's safety net during the last Normalize call.</summary>
    private readonly List<string> suspectedVolatile = new();

    public Normalizer(NormalizerOptions options) => this.options = options;

    public IReadOnlyList<string> SuspectedVolatile => suspectedVolatile;

    // Rule 2 safety net: what "looks like a timestamp" means. Deliberately ISO-8601-shaped
    // and anchored, so bare integers, years and version strings do NOT trip it. A loose
    // matcher here would bury the report in false positives and the report would stop being
    // read - which is the failure mode this net exists to prevent.
    private static readonly Regex IsoTimestamp = new(
        @"^\d{4}-\d{2}-\d{2}[Tt ]\d{2}:\d{2}(:\d{2}(\.\d+)?)?(([Zz])|([+-]\d{2}:?\d{2}))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Normalizes a parsed response body and returns the canonical text.
    /// <paramref name="rootPath"/> is the path prefix used for reporting and for matching
    /// the order-insensitive allowlist.
    /// </summary>
    public string Normalize(JsonNode? body, string rootPath = "$")
    {
        suspectedVolatile.Clear();
        var walked = Walk(body, rootPath, insideRevisions: false);
        return Canonical.Write(walked);
    }

    /// <summary>Normalizes without producing text - used when composing a whole transcript.</summary>
    public JsonNode? NormalizeNode(JsonNode? body, string rootPath = "$")
    {
        suspectedVolatile.Clear();
        return Walk(body, rootPath, insideRevisions: false);
    }

    private JsonNode? Walk(JsonNode? node, string path, bool insideRevisions)
    {
        switch (node)
        {
            case JsonObject obj:
            {
                var result = new JsonObject();
                foreach (var kv in obj)
                {
                    var childPath = path + "." + kv.Key;

                    // ---- Rule 3: ProblemDetails noise -------------------------------------
                    // traceId is per-request by construction and carries no behaviour signal.
                    if (kv.Key is "traceId")
                    {
                        result[kv.Key] = "<traceid>";
                        continue;
                    }

                    // Detailed errors are ON in the samples, so an exception body carries a
                    // full stack. A changed exception TYPE is signal; a changed stack offset
                    // or line number is not. Truncating to the first line keeps the former and
                    // drops the latter. GIVES UP: a regression that changes only the depth of
                    // an otherwise identically-typed exception.
                    if (kv.Key is "detail" or "exception" && kv.Value is JsonValue dv &&
                        dv.TryGetValue<string>(out var dtext) && dtext is not null)
                    {
                        result[kv.Key] = FirstLine(dtext);
                        continue;
                    }

                    // ---- Rule 2: timestamps, by NAME ALLOWLIST only -----------------------
                    // There is no injectable clock - DateTimeOffset.UtcNow is read directly
                    // inside ShiftEntity.EFCore and no TimeProvider/IClock seam exists - so
                    // audit stamps genuinely differ run to run and can only be normalized,
                    // never frozen.
                    if (options.TimestampNames.Contains(kv.Key) && IsTimestampValue(kv.Value))
                    {
                        result[kv.Key] = "<ts>";
                        continue;
                    }

                    // ValidFrom/ValidTo normalize ONLY inside a Revisions array. Outside one
                    // they are business dates, and business dates are exactly what a mapper
                    // regression corrupts - both WarrantyClaims profiles perform a hand-written
                    // DateTime -> DateTimeOffset conversion, and if the generator does that
                    // differently (offset, kind, precision) it MUST show.
                    if (insideRevisions && kv.Key is "ValidFrom" or "ValidTo" && IsTimestampValue(kv.Value))
                    {
                        result[kv.Key] = "<ts>";
                        continue;
                    }

                    // ---- Rule 1 fallback, narrowly scoped ----------------------------------
                    // Server-minted guids, and ONLY those the seed did not write. See
                    // NormalizerOptions.ServerGeneratedGuidNames for why this is the sanctioned
                    // "observed drift, cannot be made deterministic" case rather than a widening.
                    if (options.ServerGeneratedGuidNames.Contains(kv.Key) &&
                        kv.Value is JsonValue gv && gv.TryGetValue<string>(out var guidText) &&
                        guidText is not null &&
                        !options.KnownDeterministicValues.Contains(guidText))
                    {
                        result[kv.Key] = "<server-guid>";
                        continue;
                    }

                    // ---- Rule 3: revisions -------------------------------------------------
                    // Normalize the timestamps inside; KEEP the count and the ordering. A
                    // revision-count change means the write path changed.
                    var childInsideRevisions = insideRevisions || kv.Key is "Revisions";

                    result[kv.Key] = Walk(kv.Value, childPath, childInsideRevisions);
                }
                return result;
            }

            case JsonArray arr:
            {
                var items = new List<JsonNode?>();
                for (var i = 0; i < arr.Count; i++)
                    items.Add(Walk(arr[i], path + "[" + i + "]", insideRevisions));

                // ---- Rule 4: ordering --------------------------------------------------
                // Source order is preserved BY DEFAULT. Only paths explicitly listed in the
                // per-group orderInsensitive allowlist are sorted, and adding an entry there
                // is a deliberate act reviewed in the PR. Sorting is by canonical text so it
                // is stable and value-based rather than reference-based.
                if (options.OrderInsensitivePaths.Contains(ToPattern(path)))
                {
                    items = items
                        .OrderBy(n => Canonical.Write(n), StringComparer.Ordinal)
                        .ToList();
                }

                var result = new JsonArray();
                foreach (var it in items) result.Add(it);
                return result;
            }

            case JsonValue val:
            {
                // ---- Rule 1: DO NOT NORMALIZE IDs ---------------------------------------
                // IDs are the payload of trap 2 - a link row's own PK leaking into a child's
                // ID comes back as a well-formed, plausible hash id, and nothing short of
                // comparing it against a known-good value distinguishes it from correct.
                // Replacing IDs with <id> deletes exactly that signal.
                //
                // Determinism instead of normalization: the seed carries explicit long primary
                // keys, the hash-id salt and minimum length are pinned in the parity host
                // config, and every run gets a fresh database (section 7). Same salt + same
                // long => same hash id, so seeded IDs compare LITERALLY and a wrong ID is a
                // diff. IDs the harness CREATES compare literally too - see
                // NormalizerOptions.EnableCreatedIdAliasing for why the alias map is off.
                //
                // There is deliberately no ID branch in this switch. Do not add one.

                // ---- Rule 2 safety net --------------------------------------------------
                // Any OTHER value that parses as a timestamp inside [runStart-5min, now] is
                // FLAGGED in the report and left untouched. Classify it once, then add it to
                // the name allowlist deliberately if it really is volatile.
                if (val.TryGetValue<string>(out var s) && s is not null && IsoTimestamp.IsMatch(s))
                {
                    if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var parsed))
                    {
                        var lower = options.RunStart.AddMinutes(-5);
                        var upper = DateTimeOffset.UtcNow.AddMinutes(1);
                        if (parsed >= lower && parsed <= upper)
                            suspectedVolatile.Add(path + " = " + s);
                    }
                }

                // Rule 5: numbers are re-emitted as their original raw text. DeepClone keeps
                // the underlying JsonElement, so 1.0 stays 1.0 and never becomes 1.
                return val.DeepClone();
            }

            default:
                // Rule 5: null and absent are NOT collapsed. A JSON null present in the body
                // stays a present null here; a property the server omitted stays omitted. The
                // generated mapper may emit a property AutoMapper omitted entirely, or vice
                // versa, and that is a wire-contract change a consumer can see.
                return null;
        }
    }

    /// <summary>
    /// Rule 3: header handling. Only Content-Type and the per-group allowlist survive; ETag
    /// is normalized because it is content-derived but format-volatile.
    /// </summary>
    public SortedDictionary<string, string> NormalizeHeaders(
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        var result = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            var name = header.Key;

            // Volatile by construction - dropped outright rather than normalized, because a
            // <token> for each would be pure noise in every golden.
            if (name.Equals("Date", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Server", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Request-Context", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("traceparent", StringComparison.OrdinalIgnoreCase))
                continue;

            if (name.Equals("ETag", StringComparison.OrdinalIgnoreCase))
            {
                result[name] = "<etag>";
                continue;
            }

            if (!options.HeaderAllowlist.Contains(name))
                continue;

            result[name] = string.Join(", ", header.Value);
        }

        return result;
    }

    /// <summary>
    /// Print-token responses are <c>expires=&lt;stamp&gt;&amp;token=&lt;hash over that stamp&gt;</c> - volatile
    /// by construction, since the token is a signature over an expiry the server computes from the
    /// current time. Neither half can be frozen. The SHAPE is still compared: a print-token
    /// response that stopped carrying both fields, or started carrying others, is still a diff.
    /// </summary>
    public static string NormalizePrintToken(string body) =>
        Regex.Replace(
            Regex.Replace(body, @"expires=[^&\s]+", "expires=<expires>"),
            @"token=[0-9a-fA-F]{16,}", "token=<token>");

    private bool IsTimestampValue(JsonNode? node) =>
        node is JsonValue v && v.TryGetValue<string>(out var s) && s is not null && IsoTimestamp.IsMatch(s);

    private static string FirstLine(string text)
    {
        var idx = text.IndexOfAny(new[] { '\r', '\n' });
        return idx < 0 ? text : text.Substring(0, idx);
    }

    /// <summary>Turns $.a[3].b[0] into $.a[].b[] so allowlist entries are index-free.</summary>
    internal static string ToPattern(string path) =>
        Regex.Replace(path, @"\[\d+\]", "[]");
}
