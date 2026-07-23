using ShiftSoftware.ADP.Surveys.Shared.DTOs;

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
    /// Locale catalog for server-side consumers.
    /// </summary>
    /// <remarks>
    /// NOT what drives the builder UI. The builder is Blazor and reads
    /// <c>SurveysWebOptions.Locales</c> — setting this alone changes nothing an author sees.
    /// (It previously claimed otherwise while having no consumer at all, which is a trap worth
    /// naming rather than leaving to be rediscovered.) Configure BOTH sides to the same list.
    /// </remarks>
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

    /// <summary>
    /// Deployment-wide branding (logo, colors, favicon) applied to every survey this
    /// install serves — the "Toyota Iraq looks Toyota Iraq, TCA looks TCA" knob. A
    /// survey's own <c>SurveyDto.Branding</c> overrides it field-by-field at serve
    /// time (<see cref="BrandingDto.Merge"/>), so per-survey brands (e.g. a Lexus
    /// survey inside a Toyota deployment) still work. Serve-time means a rebrand here
    /// reaches in-flight instances without republishing anything. Null (default) =
    /// schemas are served byte-for-byte as frozen at publish.
    /// </summary>
    public BrandingDto? DefaultBranding { get; set; }
}
