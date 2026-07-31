using ShiftSoftware.ADP.Surveys.Shared.ActionTrees;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.TypeAuth.Core.Actions;

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
    /// When true (default), the Surveys pages gate their add / edit / delete / submit controls
    /// on <see cref="Actions"/>: a user without write access on the Survey action gets a
    /// read-only list and form. When false, no action is handed to the underlying components
    /// and every control renders for any authenticated user.
    /// </summary>
    /// <remarks>
    /// This is presentation, not protection — the endpoints are reachable directly whatever
    /// this says, so <c>SurveyApiOptions.EnableSurveysActionTreeAuthorization</c> is the flag
    /// that decides who can actually read and change data. Keep the two in agreement, and see
    /// <see cref="Actions"/> for pointing the pages at a host's own action tree.
    ///
    /// Defaults to <c>true</c>, unlike its API counterpart, because the pages passed their
    /// action unconditionally before this flag was read at all: <c>true</c> is what every
    /// existing consumer already runs, and defaulting to <c>false</c> would silently un-gate
    /// their UI on upgrade. The API defaults to <c>false</c> for the opposite reason — there,
    /// switching on without granting the actions first locks the authoring team out of the
    /// data, which is a worse first experience than an over-permissive default.
    /// </remarks>
    public bool EnableSurveysActionTreeAuthorization { get; set; } = true;

    /// <summary>
    /// Lets the host gate the pages on <b>its own</b> action tree instead of
    /// <see cref="SurveysActionTree"/>. Anything left null falls back to the module's own
    /// action. Mirrors <c>SurveyApiOptions.Actions</c>; set both sides to the same actions.
    /// </summary>
    /// <remarks>
    /// Without this, a host that already has, say, a <c>CRM.Survey</c> action would have to
    /// deploy and grant a second tree purely because the module ships one — which is enough
    /// friction that deployments turn authorization off instead.
    /// </remarks>
    public SurveyEntityActionOverrides Actions { get; set; } = new();

    /// <summary>
    /// Whether <c>AddSurveysBlazorServices</c> registers <see cref="SurveysActionTree"/> with
    /// TypeAuth. Set false when the host gates entirely on its own actions via
    /// <see cref="Actions"/>, so the module's unused tree doesn't clutter the permissions UI.
    /// Default true.
    /// </summary>
    /// <remarks>
    /// TypeAuth is fail-closed for actions it wasn't given: turning this off while any gate
    /// still resolves to <see cref="SurveysActionTree"/> renders that surface read-only for
    /// everyone rather than leaving it ungated.
    /// </remarks>
    public bool RegisterSurveysActionTree { get; set; } = true;

    /// <summary>
    /// Action the Survey list and form gate on, or null when
    /// <see cref="EnableSurveysActionTreeAuthorization"/> is off — ShiftBlazor reads a null
    /// <c>TypeAuthAction</c> as "no gate" and renders every control.
    /// </summary>
    public ReadWriteDeleteAction? SurveysAction =>
        EnableSurveysActionTreeAuthorization ? Actions.ResolvedSurveys : null;

    /// <summary>Bank Question counterpart of <see cref="SurveysAction"/>.</summary>
    public ReadWriteDeleteAction? BankQuestionsAction =>
        EnableSurveysActionTreeAuthorization ? Actions.ResolvedBankQuestions : null;

    /// <summary>Screen Template counterpart of <see cref="SurveysAction"/>.</summary>
    public ReadWriteDeleteAction? ScreenTemplatesAction =>
        EnableSurveysActionTreeAuthorization ? Actions.ResolvedScreenTemplates : null;

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
