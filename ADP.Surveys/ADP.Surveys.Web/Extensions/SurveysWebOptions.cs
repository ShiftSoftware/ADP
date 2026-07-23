using ShiftSoftware.ADP.Surveys.Shared.DTOs;

namespace ShiftSoftware.ADP.Surveys.Web.Extensions;

public class SurveysWebOptions
{
    /// <summary>
    /// Locales the survey builder offers when an author picks a survey's languages, and when a
    /// bank question is authored. Order here is the order shown.
    /// </summary>
    /// <remarks>
    /// Deployment configuration, because deployments serve different markets and share no common
    /// language set — one may author in en/ar/ku, another in en/ru. The default preserves the
    /// catalog this module shipped with before the list was configurable, so an existing host
    /// that sets nothing sees no change.
    ///
    /// A locale already present on a survey or question is always offered even if it is absent
    /// here — the picker unions the two. Otherwise opening an existing survey authored in a
    /// since-removed language and touching the control would silently drop that language from
    /// the schema, taking its translations out of the renderer with it.
    /// </remarks>
    public List<SurveyLocaleOption> Locales { get; set; } = new()
    {
        new("en", "English (en)"),
        new("ar", "العربية (ar)"),
        new("ku", "Kurdî (ku)"),
    };

    /// <summary>Label for <paramref name="culture"/> from <see cref="Locales"/>, falling back to
    /// the raw code so a locale carried by existing data still renders sensibly.</summary>
    public string LocaleLabel(string culture) =>
        Locales.FirstOrDefault(x => string.Equals(x.Culture, culture, StringComparison.OrdinalIgnoreCase))?.Label
        ?? culture;

    /// <summary>Culture codes only, in configured order.</summary>
    public IEnumerable<string> LocaleCultures => Locales.Select(x => x.Culture);

    /// <summary>
    /// Optional custom layout for Surveys pages. When null, pages use whatever layout
    /// the consumer's <c>DefaultApp</c> provides.
    /// </summary>
    public Type? Layout { get; set; }

    /// <summary>
    /// When true, Surveys Blazor UI hides actions / nav items the user lacks
    /// <c>SurveysActionTree</c> permission for. Default false — only authentication required.
    /// </summary>
    public bool EnableSurveysActionTreeAuthorization { get; set; } = false;

    /// <summary>
    /// Route prefix the Surveys API controllers are mounted under (relative to the
    /// HttpClient base address). Must match the tail of <c>SurveyApiOptions.RoutePrefix</c>.
    /// E.g. if the API side uses <c>"api/Surveys"</c> and HttpClient.BaseAddress ends in
    /// <c>"/api/"</c>, set this to <c>"Surveys"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>
    /// <see cref="RoutePrefix"/> normalized to end with a trailing slash (or empty when unset).
    /// Pre-pend this to every URL that hits the Surveys API.
    /// </summary>
    public string ResolvedRoutePrefix =>
        string.IsNullOrWhiteSpace(RoutePrefix) ? string.Empty : RoutePrefix.Trim('/') + "/";
}
