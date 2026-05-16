using System.Net.Http.Json;
using ShiftSoftware.ADP.Surveys.Web.Extensions;

namespace ShiftSoftware.ADP.Surveys.Web.WebServices;

/// <summary>
/// Thin HTTP wrapper for Surveys-specific operations the stock <c>ShiftEntityForm</c> /
/// <c>ShiftList</c> components can't express on their own — currently just Publish.
/// Vanilla CRUD goes through the framework components directly (they call the
/// authenticated controllers).
/// </summary>
public class SurveyService
{
    private readonly HttpClient http;
    private readonly string prefix;

    public SurveyService(HttpClient http, SurveysWebOptions options)
    {
        this.http = http;
        this.prefix = options.ResolvedRoutePrefix;
    }

    /// <summary>
    /// POST <c>{prefix}Publish/{id}</c>. Returns the JSON body the API emitted (success
    /// payload with version + hash, or structured error payload with Errors[]).
    /// </summary>
    public async Task<HttpResponseMessage> PublishAsync(string hashedId, CancellationToken ct = default)
    {
        var url = $"{prefix}Publish/{hashedId}";
        return await http.PostAsync(url, content: null, ct);
    }

    /// <summary>
    /// GET <c>{prefix}Triggers/channels</c>. Returns the registry keys the host has
    /// wired up. Empty (not null) on failure so the dropdown still renders.
    /// </summary>
    public async Task<List<string>> GetRegisteredChannelsAsync(CancellationToken ct = default)
    {
        var url = $"{prefix}Triggers/channels";
        try
        {
            var keys = await http.GetFromJsonAsync<List<string>>(url, ct);
            return keys ?? new();
        }
        catch
        {
            return new();
        }
    }
}
