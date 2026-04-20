namespace ShiftSoftware.ADP.Surveys.Web.Extensions;

public class SurveysWebOptions
{
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
