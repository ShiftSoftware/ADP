using System.Net.Http.Headers;
using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;

namespace ShiftSoftware.ADP.Surveys.Web.WebServices;

/// <summary>
/// Resolves external option ids back to human labels for the dashboard's answers view.
///
/// Questions with an <see cref="OptionsSourceDto"/> fetch their options in the respondent's
/// browser at answer time, so the server never sees them and the stored answer is a bare
/// external id. Showing an analyst <c>"4f9c"</c> where the respondent chose a branch name
/// is technically accurate and practically useless, so the dashboard re-fetches the same
/// public endpoint and maps ids to labels.
///
/// Mirrors the SDK's <c>options-source.ts</c>: same query-param composition, same
/// <c>Accept-Language</c>, same dot-path extraction with <c>ID</c> / <c>Name</c> defaults.
/// Failures resolve to an empty map — the caller falls back to showing the raw id, which
/// is exactly the pre-existing behaviour.
/// </summary>
public class SourcedOptionsResolver
{
    /// <summary>
    /// Deliberately NOT the injected <c>HttpClient</c>. That one carries the dashboard's
    /// bearer token on every request, and these URLs point at arbitrary third-party hosts
    /// an author typed into a survey — sending a staff access token to them would be a
    /// credential leak triggered by ordinary authoring. A bare client sends nothing.
    /// </summary>
    private readonly HttpClient http = new();

    // Keyed by url+locale, like the renderer's session cache: revisiting an answer set
    // shouldn't refetch, but switching the locale picker should.
    private readonly Dictionary<string, Dictionary<string, string>> cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Fetches and maps the source's options to an id → label dictionary. Never throws;
    /// an unreachable or malformed endpoint yields an empty map.
    /// </summary>
    public async Task<Dictionary<string, string>> ResolveAsync(
        OptionsSourceDto source, string? locale, CancellationToken ct = default)
    {
        var url = BuildUrl(source);
        var cacheKey = $"{locale}|{url}";
        if (cache.TryGetValue(cacheKey, out var cached)) return cached;

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(locale))
                request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(locale));
            foreach (var (key, value) in source.Headers ?? new())
                request.Headers.TryAddWithoutValidation(key, value);

            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                cache[cacheKey] = map;
                return map;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var items = Dig(doc.RootElement, source.ItemsPath);
            if (items?.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.Value.EnumerateArray())
                {
                    var id = Stringify(Dig(item, source.ValuePath ?? "ID"));
                    var label = Stringify(Dig(item, source.LabelPath ?? "Name"));
                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(label))
                        map[id] = label;
                }
            }
        }
        catch
        {
            // Network, CORS, parse — all the same outcome here: no labels, show raw ids.
        }

        cache[cacheKey] = map;
        return map;
    }

    private static string BuildUrl(OptionsSourceDto source)
    {
        if (source.QueryParams is not { Count: > 0 }) return source.Url;

        var separator = source.Url.Contains('?') ? '&' : '?';
        var query = string.Join('&', source.QueryParams.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? "")}"));
        return $"{source.Url}{separator}{query}";
    }

    /// <summary>
    /// Walks a dotted path. A null or empty path returns the element itself, which is how
    /// a flat top-level array (the common shape) is handled.
    /// </summary>
    private static JsonElement? Dig(JsonElement element, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return element;

        var current = element;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.ValueKind != JsonValueKind.Object) return null;
            if (!current.TryGetProperty(segment, out var next)) return null;
            current = next;
        }
        return current;
    }

    /// <summary>Ids arrive as strings or numbers depending on the endpoint; both are valid keys.</summary>
    private static string? Stringify(JsonElement? element) => element?.ValueKind switch
    {
        JsonValueKind.String => element.Value.GetString(),
        JsonValueKind.Number => element.Value.GetRawText(),
        _ => null,
    };
}
