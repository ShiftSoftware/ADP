using System.Net.Http.Json;
using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Responses;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Json;
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
    /// Resolves a single banked question into inline form by POSTing a minimal
    /// synthetic draft at the Preview endpoint (same <c>SchemaResolver</c> the
    /// live preview and publish use). Returns the resolved <see cref="QuestionDto"/>,
    /// or null on any failure — callers degrade to free-form editing.
    /// </summary>
    public async Task<QuestionDto?> ResolveBankQuestionAsync(
        string bankRef, List<string>? locales, CancellationToken ct = default)
    {
        var probe = BuildProbeDraft(locales);
        probe.Screens.Add(new InlineScreenDto
        {
            Id = "probe",
            Questions = new List<QuestionEntryDto>
            {
                QuestionEntryDto.FromRef(new QuestionRefDto { BankRef = bankRef }),
            },
        });

        var resolved = await ResolveProbeAsync(probe, ct);
        var screen = resolved?.Screens.OfType<InlineScreenDto>().FirstOrDefault();
        return screen?.Questions.FirstOrDefault()?.Inline;
    }

    /// <summary>
    /// Resolves a screen template into inline form via the same synthetic-draft
    /// probe. Returns the resolved screen (whose questions are all inline), or
    /// null on any failure.
    /// </summary>
    public async Task<InlineScreenDto?> ResolveScreenTemplateAsync(
        string templateRef, List<string>? locales, CancellationToken ct = default)
    {
        var probe = BuildProbeDraft(locales);
        probe.Screens.Add(new ScreenTemplateRefDto { TemplateRef = templateRef });

        var resolved = await ResolveProbeAsync(probe, ct);
        return resolved?.Screens.OfType<InlineScreenDto>().FirstOrDefault();
    }

    static SurveyDto BuildProbeDraft(List<string>? locales) => new()
    {
        Locales = locales is { Count: > 0 } ? locales : new List<string> { "en" },
        DefaultLocale = locales?.FirstOrDefault() ?? "en",
    };

    async Task<SurveyDto?> ResolveProbeAsync(SurveyDto probe, CancellationToken ct)
    {
        try
        {
            var response = await http.PostAsJsonAsync(
                $"{prefix}Preview", probe, SurveySchemaSerializer.Options, ct);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<SurveyDto>(json, SurveySchemaSerializer.Options);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// GET <c>{prefix}SurveyResponses/public-url-template</c> — lets the dashboard
    /// compose recipient links client-side (the OData instance list can't carry
    /// them). Null when unset or on failure; callers hide the copy action.
    /// </summary>
    public async Task<string?> GetPublicUrlTemplateAsync(CancellationToken ct = default)
    {
        try
        {
            var dto = await http.GetFromJsonAsync<PublicUrlTemplateDTO>(
                $"{prefix}SurveyResponses/public-url-template", ct);
            return dto?.Template;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// GET <c>{prefix}SurveyResponses/instance/{publicId}</c> — instance metadata,
    /// recorded responses with answers, and the pinned resolved schema JSON.
    /// </summary>
    public async Task<SurveyInstanceDetailDTO?> GetInstanceDetailAsync(
        Guid publicId, CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<SurveyInstanceDetailDTO>(
                $"{prefix}SurveyResponses/instance/{publicId}", ct);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// POST <c>{prefix}SurveyResponses/{surveyId}/test-instances</c>. Returns the raw
    /// response so callers can surface the API's error copy (e.g. "publish first").
    /// </summary>
    public async Task<HttpResponseMessage> CreateTestInstanceAsync(
        string hashedSurveyId, CancellationToken ct = default)
    {
        return await http.PostAsync($"{prefix}SurveyResponses/{hashedSurveyId}/test-instances", content: null, ct);
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
