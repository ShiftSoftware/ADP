using ShiftSoftware.ADP.Surveys.Shared;
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
    /// When true, authenticated endpoints enforce TypeAuth actions per entity. When false
    /// (default), endpoints only require authentication — <b>any</b> signed-in user of the
    /// host application can read, edit and delete surveys and read every response.
    /// </summary>
    /// <remarks>
    /// Hiding the module's navigation links does not substitute for this: the endpoints are
    /// reachable directly. The default is <c>false</c> only because switching it on without
    /// granting the actions first locks the authoring team out; it is not a safe
    /// steady state. Startup logs a warning while it is off outside Development.
    /// </remarks>
    public bool EnableSurveysActionTreeAuthorization { get; set; } = false;

    /// <summary>
    /// Lets the host gate the module on <b>its own</b> action tree instead of
    /// <see cref="SurveysActionTree"/>.
    /// </summary>
    /// <remarks>
    /// Without this, switching authorization on means deploying and granting a second
    /// action tree in the host's identity system, purely because the module ships one —
    /// which is enough friction that deployments leave authorization off instead. A host
    /// that already has, say, a <c>CRM.Survey</c> action can point these at it and turn
    /// enforcement on the same day. Anything left null falls back to the module's own tree.
    /// </remarks>
    public SurveyActionOverrides Actions { get; set; } = new();

    /// <summary>
    /// Whether to register <see cref="SurveysActionTree"/> with TypeAuth. Set false when
    /// the host gates entirely on its own actions via <see cref="Actions"/>, so the module's
    /// unused tree doesn't clutter the permissions UI. Default true.
    /// </summary>
    public bool RegisterSurveysActionTree { get; set; } = true;

    /// <summary>
    /// Locale catalog. Nothing on the server reads this.
    /// </summary>
    /// <remarks>
    /// The builder is Blazor and reads <c>SurveysWebOptions.Locales</c>; setting this changes
    /// nothing an author sees. It was documented as the knob for a while despite having no
    /// consumer at all, which cost a debugging session — hence the hard deprecation rather
    /// than another warning in prose.
    /// </remarks>
    [Obsolete("Not read by anything. The builder's locale catalog is SurveysWebOptions.Locales on the Blazor side.")]
    public List<SurveyLocaleOption> Locales { get; set; } = new();

    /// <summary>
    /// Max accepted request body, in bytes, for the anonymous <c>/responses</c> submit
    /// endpoint. Null (default) = the host's own limit applies.
    /// </summary>
    /// <remarks>
    /// Worth setting on any deployment whose surveys use <c>signature</c> or <c>file</c>
    /// questions: both carry payloads inline in the answer map, and the endpoint is
    /// unauthenticated, so the instance id is the only thing between the internet and an
    /// arbitrarily large POST. Enforced in <c>PublicSurveyController.SubmitResponse</c> —
    /// oversized bodies are rejected with 413 before deserialization.
    /// </remarks>
    public long? MaxResponseBodyBytes { get; set; }

    /// <summary>
    /// Requests allowed per <see cref="PublicEndpointRateLimitWindow"/>, per client IP,
    /// across the anonymous schema/submit/status endpoints. Null (default) = no limit
    /// registered by this module.
    /// </summary>
    /// <remarks>
    /// Requires the host to call <c>app.UseRateLimiter()</c> — without that middleware the
    /// policy is inert metadata and nothing is enforced. Set it if the survey app is
    /// internet-facing and nothing in front of it (WAF, APIM, Front Door) already limits.
    /// </remarks>
    public int? PublicEndpointRateLimit { get; set; }

    /// <summary>Window for <see cref="PublicEndpointRateLimit"/>. Default 1 minute.</summary>
    public TimeSpan PublicEndpointRateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How many dispatch attempts an outbox event gets before it is dead-lettered.
    /// Default 5.
    /// </summary>
    public int OutboxMaxAttempts { get; set; } = 5;

    /// <summary>
    /// Base delay before retrying a failed outbox dispatch; doubles each attempt.
    /// Default 1 minute — with the default budget that spans roughly 15 minutes of outage.
    /// </summary>
    public TimeSpan OutboxRetryBackoff { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Ceiling on the exponential backoff. Default 1 hour.</summary>
    public TimeSpan OutboxRetryBackoffCap { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Whether this deployment can actually store uploaded files. Default <c>false</c>,
    /// and publishing a survey containing a <c>file</c> question is rejected while it is.
    /// </summary>
    /// <remarks>
    /// The renderer records a selected file's <c>{name, size, type}</c> and discards the
    /// bytes — presigned-URL upload was deferred and never built. Nothing about that is
    /// visible to an author or a respondent: the survey accepts the file, the response
    /// records that a file was attached, and the file is gone. Blocking publish is the
    /// only honest default until upload exists.
    ///
    /// Set true only if the deployment has arranged its own upload path (for example a
    /// custom question renderer that uploads before submit). Doing so re-enables publish
    /// and puts the data-integrity question in the deployment's hands.
    /// </remarks>
    public bool FileUploadsSupported { get; set; } = false;

    /// <summary>
    /// Template the trigger scheduler substitutes <c>{publicId}</c> into when handing a
    /// survey link to a channel, and that the dashboard composes copy-links from. Default
    /// points at the standalone Vite app for dev; production deployments MUST override it
    /// to their hosted survey app.
    /// </summary>
    /// <remarks>
    /// Leaving the default in place outside Development is treated as a misconfiguration:
    /// it is logged at startup, and the scheduler refuses to send rather than spend a
    /// recipient's message on a link that resolves to the server's own machine. See
    /// <see cref="PublicSurveyUrl"/>.
    /// </remarks>
    public string PublicSurveyUrlTemplate { get; set; } = PublicSurveyUrl.DevDefault;

    /// <summary>
    /// How long after a row's last send (or its creation, if it was never sent) the
    /// scheduler's expiry sweep flips it to <c>Expired</c>. Applies to rows whose
    /// schedule has run out (<c>NextSendAt IS NULL</c>) and that still haven't been
    /// completed. Default 30d.
    /// </summary>
    public TimeSpan ExpiryGracePeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Deployment-wide branding (logo, colors, favicon) applied to every survey this
    /// install serves — the knob that makes each deployment's surveys carry that
    /// deployment's own brand. A survey's own <c>SurveyDto.Branding</c> overrides it
    /// field-by-field at serve time (<see cref="BrandingDto.Merge"/>), so per-survey
    /// brands (e.g. a sub-brand's survey inside a parent-brand deployment) still work.
    /// Serve-time means a rebrand here reaches in-flight instances without republishing
    /// anything. Null (default) = schemas are served byte-for-byte as frozen at publish.
    /// </summary>
    public BrandingDto? DefaultBranding { get; set; }
}
