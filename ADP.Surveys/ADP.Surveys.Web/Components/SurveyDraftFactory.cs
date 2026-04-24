using System.Text.Json;
using System.Text.Json.Nodes;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.Json;

namespace ShiftSoftware.ADP.Surveys.Web.Components;

/// <summary>
/// First-materialize defaults for a <see cref="SurveyDto"/> in the builder. Used by
/// both <c>ScreenList.AddScreen</c> (user adds a screen before visiting General) and
/// <c>GeneralTab</c> (user edits locale/title first) so either entry point produces
/// a Draft that passes <c>SurveyDtoValidator</c>. <c>SurveyId</c> is server-stamped on
/// load (see <c>GeneralMappingProfile.MapSurvey</c>), so it stays empty here.
/// </summary>
internal static class SurveyDraftFactory
{
    public static SurveyDto CreateSkeleton() => new()
    {
        Locales = new List<string> { "en" },
        DefaultLocale = "en",
        Title = new LocalizedString(),
    };

    /// <summary>
    /// Serializes a draft for the Raw JSON editor, omitting server-owned fields
    /// (<c>surveyId</c>, <c>version</c>, <c>publishedAt</c>) that the user
    /// shouldn't edit and would otherwise be round-tripped back to the DB.
    /// </summary>
    public static string ToEditorJson(SurveyDto? draft)
    {
        if (draft is null) return "";
        var json = JsonSerializer.Serialize(draft, SurveySchemaSerializer.Pretty);
        if (JsonNode.Parse(json) is not JsonObject obj) return json;
        obj.Remove("surveyId");
        obj.Remove("version");
        obj.Remove("publishedAt");
        return obj.ToJsonString(SurveySchemaSerializer.Pretty);
    }

    /// <summary>
    /// Parses a draft from the Raw JSON editor. Server-owned fields are blanked
    /// — the next load stamps <c>surveyId</c> from the entity ID, and publish
    /// overwrites <c>version</c> / <c>publishedAt</c>.
    /// </summary>
    public static SurveyDto? FromEditorJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        var draft = JsonSerializer.Deserialize<SurveyDto>(json, SurveySchemaSerializer.Options);
        if (draft is not null)
        {
            draft.SurveyId = "";
            draft.Version = 0;
            draft.PublishedAt = null;
        }
        return draft;
    }
}
