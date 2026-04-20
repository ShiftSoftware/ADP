namespace ShiftSoftware.ADP.Surveys.API.Extensions;

public class SurveyApiOptions
{
    /// <summary>
    /// Route prefix prepended to every controller inside <c>ADP.Surveys.API</c>. Matches
    /// the <c>ADP.Menus</c> convention — consumer app typically sets <c>"api/Surveys"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "api";

    /// <summary>
    /// When true, authenticated endpoints enforce <c>SurveysActionTree</c> TypeAuth actions
    /// per entity. When false (default), endpoints only require authentication.
    /// </summary>
    public bool EnableSurveysActionTreeAuthorization { get; set; } = false;

    /// <summary>
    /// Locales the builder's <c>LocalizedField</c> editor offers per Decision #5.
    /// Empty means "no localization UI" — the editor falls back to a single-language
    /// text input. Order here determines the tab / field order in the builder.
    /// </summary>
    public List<SurveyLocaleOption> Locales { get; set; } = new();

    /// <summary>
    /// Optional max response size in bytes for the public <c>/responses</c> endpoint.
    /// Null = framework default. Useful for clamping <c>file</c> question payloads.
    /// </summary>
    public long? MaxResponseBodyBytes { get; set; }
}

public record SurveyLocaleOption(string Culture, string Label);
