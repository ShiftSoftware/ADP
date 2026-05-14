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

    /// <summary>
    /// Template the trigger scheduler substitutes <c>{publicId}</c> into when handing a
    /// survey link to a channel. Default points at the standalone Vite app for dev;
    /// production deployments override to their hosted renderer URL.
    /// </summary>
    public string PublicSurveyUrlTemplate { get; set; } = "http://localhost:5190/s/{publicId}";

    /// <summary>
    /// How long after a row's last send (or its creation, if it was never sent) the
    /// scheduler's expiry sweep flips it to <c>Expired</c>. Applies to rows whose
    /// schedule has run out (<c>NextSendAt IS NULL</c>) and that still haven't been
    /// completed. Default 30d.
    /// </summary>
    public TimeSpan ExpiryGracePeriod { get; set; } = TimeSpan.FromDays(30);
}

public record SurveyLocaleOption(string Culture, string Label);
