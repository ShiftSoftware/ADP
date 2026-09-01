using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace ShiftSoftware.ADP.EndpointParity.Harness;

// ============================================================================================
// CAPTURE LAYER (verification.md 2/3). HttpClient, System.Text.Json and string ONLY.
// ============================================================================================

/// <summary>
/// Drives a case list against a booted host, normalizes what comes back, and either writes the
/// goldens (capture) or diffs against them (verify).
///
/// <para>
/// <b>This class carries the global HTML assertion.</b> Both the Menus and the Surveys samples
/// map a fallback file (<c>MapFallbackToFile("index.html")</c>), so a deleted or renamed route
/// returns <b>200 + HTML rather than 404</b> and would pass silently as a perfectly ordinary
/// success. The rule is global from the start, not a Menus special case retro-fitted at Step 01
/// - Surveys carries the same hazard right beside a PublicSurveyController that answers
/// NotFound().
/// </para>
/// </summary>
public sealed class ParityRunner
{
    private readonly HttpClient client;
    private readonly Normalizer normalizer;
    private readonly string baselineDir;
    private readonly ParityMode mode;
    private readonly ParityGrant grant;

    public ParityRunner(HttpClient client, Normalizer normalizer, string baselineDir, ParityMode mode, ParityGrant grant)
    {
        this.client = client;
        this.normalizer = normalizer;
        this.baselineDir = baselineDir;
        this.mode = mode;
        this.grant = grant;
    }

    /// <summary>Failures that are hard errors regardless of the baseline, collected across the run.</summary>
    public List<string> HardFailures { get; } = new();

    public List<Transcript> Transcripts { get; } = new();

    public Dictionary<string, IReadOnlyList<TranscriptDifference>> Differences { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Write-lifecycle cases that could not run because the restricted principal was refused the
    /// CREATE they depend on. Reported, never silently dropped.
    /// </summary>
    public List<string> SkippedUnderRestrictedGrant { get; } = new();

    /// <summary>Route templates actually exercised, for the catalogue-coverage gate.</summary>
    public HashSet<string> ExercisedRoutes { get; } = new(StringComparer.Ordinal);

    public async Task RunAsync(IEnumerable<ParityCase> cases, CancellationToken ct = default)
    {
        // Ids minted by CREATE cases, keyed by the entity segment, so the READBACK / UPDATE /
        // REMOVE / GONE cases that follow can address the row that was just created.
        var createdIds = new Dictionary<string, string>(StringComparer.Ordinal);

        // The most recent server-rendered entity body per entity. UPDATE needs it because the
        // framework applies OPTIMISTIC CONCURRENCY on LastSaveDate: a PUT whose body omits it
        // sends DateTime.MinValue and is rejected 409 "modified by another process" - every time,
        // for every entity. Left unfixed, UPDATE 2xx sits at 0/n and the update mapper is covered
        // by nothing while CREATE looks perfectly healthy.
        var lastEntityBody = new Dictionary<string, JsonNode>(StringComparer.Ordinal);

        foreach (var c in cases)
        {
            var url = c.Url;

            // Marked BEFORE any skip: the coverage gate asks "does a case exist for this route",
            // not "did it execute". A case skipped because the restricted principal was refused
            // its CREATE still covers the route; counting it as uncovered would report a coverage
            // hole that is really a privilege boundary.
            if (c.RouteKey is not null)
                ExercisedRoutes.Add(c.RouteKey);

            if (c.NeedsCreatedId)
            {
                var key = EntityKeyOf(c);
                if (!createdIds.TryGetValue(key, out var newId))
                {
                    // Under FULL ACCESS this is a hard failure: the write path covered nothing,
                    // which is the single most likely way trap 3-write goes undetected everywhere.
                    //
                    // Under a RESTRICTED grant it is the expected outcome and the point of the
                    // pass - a read-only principal is refused the CREATE, so there is no row to
                    // read back, update or delete. The refusal itself is captured as its own
                    // transcript; treating its consequences as failures would make the restricted
                    // baseline impossible to capture, and a gate that cannot pass gets weakened.
                    if (grant == ParityGrant.FullAccess)
                        HardFailures.Add(c.Name + ": depends on a created id for '" + key +
                                         "' but the CREATE case produced none (write path not exercised).");
                    else
                        SkippedUnderRestrictedGrant.Add(c.Name);
                    continue;
                }
                url = url.Replace("{newId}", newId);
            }

            // Merge the hand-authored UPDATE body OVER the row as the server last rendered it.
            // That carries ID and LastSaveDate (concurrency token) while keeping every sentinel
            // the hand-authored body sets - which is the point: an ignored member must stay
            // ignored on PUT too, and PUT is the likelier place for a convention mapper to start
            // writing one.
            var effective = c;
            if (c.Kind == "UPDATE" && c.Body is not null &&
                lastEntityBody.TryGetValue(EntityKeyOf(c), out var current) &&
                current is JsonObject currentObject &&
                Canonical.TryParse(c.Body) is JsonObject overlay)
            {
                var merged = (JsonObject)currentObject.DeepClone();
                foreach (var property in overlay)
                {
                    if (property.Key.StartsWith('_')) continue;   // harness metadata

                    // Replace case-insensitively. The server renders PascalCase and the
                    // hand-authored bodies are camelCase, so a naive set produces BOTH keys in
                    // one object - duplicated, confusing, and dependent on the binder's
                    // case handling to come out right.
                    var existing = merged.FirstOrDefault(kv =>
                        string.Equals(kv.Key, property.Key, StringComparison.OrdinalIgnoreCase)).Key;
                    if (existing is not null) merged.Remove(existing);

                    merged[existing ?? property.Key] = property.Value?.DeepClone();
                }
                // Written WITHOUT canonical key sorting, deliberately. Canonical.Write sorts keys
                // alphabetically for readable goldens (Rule 5) - but System.Text.Json requires a
                // polymorphic type's "type" discriminator to be the FIRST property, and sorting
                // moves it. That produced a 500 ("must specify a type discriminator") on the one
                // entity whose DTO is polymorphic. Sorting is a presentation rule for goldens; it
                // must never touch a body being sent to the server.
                effective = c with { Body = merged.ToJsonString() };
            }

            var transcript = await IssueAsync(effective, url, ct);
            Transcripts.Add(transcript);

            if (transcript.Status is >= 200 and < 300 &&
                (c.Kind == "CREATE" || c.Kind == "READBACK"))
            {
                var entity = ExtractEntity(transcript.RawBody);
                if (entity is not null)
                {
                    lastEntityBody[EntityKeyOf(c)] = entity;
                    var id = IdOf(entity);
                    if (id is not null && c.Kind == "CREATE") createdIds[EntityKeyOf(c)] = id;
                }
            }

            CompareOrWrite(c, transcript);
        }
    }

    private async Task<Transcript> IssueAsync(ParityCase c, string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(new HttpMethod(c.Method), url);

        // Rule 6: culture is pinned on every request, then varied in a second pass.
        request.Headers.AcceptLanguage.ParseAdd(c.Culture);

        if (c.Body is not null)
            request.Content = new StringContent(c.Body, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, ct);

        var contentType = response.Content.Headers.ContentType?.ToString();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);

        // ---- GLOBAL ASSERTION: no response body in any group may be text/html -------------
        // A fallback-mapped host answers a route that no longer exists with 200 + index.html.
        // Without this the disappearance of an endpoint is invisible: the status is a success
        // and the body is a well-formed document. This fires for EVERY group, Surveys included.
        if (contentType is not null &&
            contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            HardFailures.Add(c.Name + ": response Content-Type is text/html (" + (int)response.StatusCode +
                             " " + c.Method + " " + url + "). A fallback file answered this request, which " +
                             "means the route does not exist. This is never a legitimate parity response.");
        }

        var headers = normalizer.NormalizeHeaders(
            response.Headers.Concat(response.Content.Headers));

        string? body;
        string? rawBody = null;
        string? partial = null;
        var volatiles = new List<string>();

        if (IsBinary(contentType))
        {
            // ---- Rule 7: binaries are a DECLARED GAP, not a covered case -----------------
            // .xlsx is a zip with embedded timestamps; a PDF carries /CreationDate and an /ID
            // pair. Neither is byte-reproducible. Record content-type plus a size band and mark
            // the case PARTIAL so it can never pass silently as covered. summary counts PARTIAL
            // separately for exactly that reason.
            body = null;
            partial = "binary body; content-type=" + contentType + "; size-band=" + SizeBand(bytes.Length) +
                      "; sha256(full)=" + Sha256Hex(bytes).Substring(0, 16) + " (recorded, NOT compared)";
        }
        else
        {
            var text = Encoding.UTF8.GetString(bytes);
            rawBody = text;
            var parsed = Canonical.TryParse(text);

            if (parsed is not null)
            {
                var normalized = normalizer.NormalizeNode(parsed, "$.response.body");
                volatiles.AddRange(normalizer.SuspectedVolatile);
                body = Canonical.Write(normalized);
            }
            else
            {
                // text/csv and text/plain are deterministic text: diff them AS TEXT. They are
                // covered, not PARTIAL - they are not a Rule 7 case at all. The one exception is
                // the print-token body, which is a signature over a current-time expiry and so
                // cannot be deterministic; its SHAPE is still compared.
                body = text.Replace("\r\n", "\n");
                if (c.Kind == "PRINTTOKEN") body = Normalizer.NormalizePrintToken(body);
            }
        }

        return new Transcript
        {
            CaseName = c.Name,
            Kind = c.Kind,
            Method = c.Method,
            Url = url,
            Grant = grant.ToString(),
            // The request body is normalized INTO THE GOLDEN with the same Rule 2 name allowlist
            // as the response. It is not normalized on the way OUT - the server received the real
            // bytes. An UPDATE body carries the server-rendered row, CreateDate and all, so
            // without this two identical runs differ on the request side alone.
            RequestBody = c.Body is null ? null : NormalizeRequestBody(c.Body),
            Status = (int)response.StatusCode,
            ContentType = contentType,
            Headers = headers,
            Body = body,
            RawBody = rawBody,
            Partial = partial,
            SuspectedVolatile = volatiles,
        };
    }

    private string NormalizeRequestBody(string body)
    {
        var parsed = Canonical.TryParse(body);
        if (parsed is null) return body;
        var normalized = normalizer.NormalizeNode(parsed, "$.request.body");
        return Canonical.Write(normalized);
    }

    private void CompareOrWrite(ParityCase c, Transcript transcript)
    {
        Directory.CreateDirectory(baselineDir);
        var path = Path.Combine(baselineDir, c.Name + ".json");
        var golden = transcript.ToGolden();

        if (mode == ParityMode.Capture)
        {
            File.WriteAllText(path, golden, new UTF8Encoding(false));
            return;
        }

        if (!File.Exists(path))
        {
            HardFailures.Add(c.Name + ": no baseline at " + path +
                             ". A case that exists now but had no golden is an uncaptured route, not a pass.");
            return;
        }

        var baseline = File.ReadAllText(path);
        var diffs = TranscriptDiffer.Diff(baseline, golden);
        if (diffs.Count > 0)
            Differences[c.Name] = diffs;
    }

    /// <summary>
    /// Pulls the created row's id out of a CREATE response so the round-trip can address it.
    /// Reads only JSON - it must not name ShiftEntityResponse&lt;T&gt;, so it looks for the
    /// conventional envelope shape by key rather than by type.
    /// </summary>
    private static string? ExtractCreatedId(string? body)
    {
        var entity = ExtractEntity(body);
        return entity is null ? null : IdOf(entity);
    }

    /// <summary>Unwraps the framework's single-entity envelope.</summary>
    private static JsonNode? ExtractEntity(string? body)
    {
        var node = Canonical.TryParse(body);
        if (node is null) return null;

        // The framework wraps single-entity payloads as { "Entity": { ... }, "Message": ... }.
        // Verified against a live response rather than assumed - an earlier guess at "data" made
        // every READBACK case fail with "the CREATE produced no id" while CREATE was reporting
        // 3/3 2xx, which is exactly the kind of contradiction worth chasing rather than tolerating.
        var payload = node;
        if (node is JsonObject o)
        {
            if (o.TryGetPropertyValue("Entity", out var entity) && entity is not null) payload = entity;
            else if (o.TryGetPropertyValue("data", out var d) && d is not null) payload = d;
        }

        return payload;
    }

    private static string? IdOf(JsonNode entity) =>
        entity is JsonObject o && o.TryGetPropertyValue("ID", out var idNode) &&
        idNode is JsonValue idv && idv.TryGetValue<string>(out var id) ? id : null;

    private static string EntityKeyOf(ParityCase c) =>
        c.Name.Split('.', 2)[0];

    private static bool IsBinary(string? contentType)
    {
        if (contentType is null) return false;
        if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) return false;
        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase)) return false;
        if (contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    /// <summary>
    /// Coarse size band for a binary body. Coarse on purpose: an exact length would be a
    /// spurious diff on every rebuild, while a band still catches "the export became empty"
    /// or "the export doubled".
    /// </summary>
    private static string SizeBand(int length)
    {
        if (length == 0) return "0";
        var lower = 1;
        while (lower * 2 <= length) lower *= 2;
        return "[" + lower + "," + (lower * 2) + ")";
    }

    private static string Sha256Hex(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
