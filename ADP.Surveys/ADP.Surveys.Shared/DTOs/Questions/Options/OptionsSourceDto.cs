using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;

/// <summary>
/// External options for choice-type questions (singleChoice, multiChoice, dropdown,
/// navigationList): instead of authoring <c>options</c> inline, the question points at
/// a public HTTP endpoint returning a JSON array of items.
///
/// The fetch happens <b>client-side, at render time</b>, by design: the endpoints in
/// scope are the same public CORS-open APIs other websites already consume, a survey
/// may use several parameter variations of one endpoint across its branches (only the
/// branch actually visited gets fetched), and <c>Accept-Language</c> is sent from the
/// renderer's active locale so labels arrive pre-localized. Consequences:
/// <list type="bullet">
/// <item>This block ships verbatim in the public schema — never put secrets in
/// <see cref="Headers"/>. Endpoints must be public and CORS-open.</item>
/// <item>The server never sees the fetched options, so submit-time validation for
/// sourced questions is shape-only (any string id is accepted); the option-membership
/// check runs client-side in the SDK's answer-validator mirror.</item>
/// </list>
///
/// Every request field — <see cref="Url"/>, <see cref="QueryParams"/> values,
/// <see cref="Headers"/> values and <see cref="Body"/> — may carry personalization
/// tokens. <c>{{recipient.*}}</c> / <c>{{candidate.*}}</c> are filled at serve time
/// (<see cref="Personalization.PersonalizationTokens"/>); <c>{{answers.&lt;id&gt;}}</c>
/// is filled by the renderer from the respondent's own answers just before the fetch,
/// which is what lets one question's endpoint depend on an earlier answer. Values are
/// escaped for where they land: percent-encoded in the URL and in a form body,
/// JSON-string-escaped in a JSON body, raw elsewhere.
/// </summary>
public class OptionsSourceDto
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    /// <summary>
    /// Appended to the URL's query string. This is the per-survey variation axis
    /// (e.g. <c>services=body-and-paint</c> vs <c>services=new-vehicle-sale</c>) —
    /// banked questions can override these per reference via
    /// <see cref="QuestionOverridesDto.SourceParams"/>.
    /// </summary>
    [JsonPropertyName("queryParams")]
    public Dictionary<string, string>? QueryParams { get; set; }

    /// <summary>
    /// Extra request headers. <c>Accept-Language</c> is sent automatically from the
    /// renderer's active locale; an explicit entry here wins over the automatic one.
    /// Public values only — this ships to the browser.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Dot-path to the item array inside the response. Empty/null when the response
    /// body itself is the array (the common shape).
    /// </summary>
    [JsonPropertyName("itemsPath")]
    public string? ItemsPath { get; set; }

    /// <summary>Dot-path to each item's answer value. Defaults to <c>ID</c> client-side.</summary>
    [JsonPropertyName("valuePath")]
    public string? ValuePath { get; set; }

    /// <summary>Dot-path to each item's display label. Defaults to <c>Name</c> client-side.</summary>
    [JsonPropertyName("labelPath")]
    public string? LabelPath { get; set; }

    /// <summary>
    /// navigationList only: the single destination screen shared by every fetched
    /// option (per-option routing needs authored options). Required there; ignored
    /// for the other choice types.
    /// </summary>
    [JsonPropertyName("nextScreen")]
    public string? NextScreen { get; set; }

    /// <summary>
    /// HTTP method — <c>GET</c> (the default when null) or <c>POST</c>. POST is what
    /// lets the request carry a <see cref="Body"/>; the endpoint still has to be
    /// public and CORS-open, and a JSON body makes the browser preflight it.
    /// </summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary>
    /// Request body template, sent with POST after token substitution. Write it in
    /// the shape the endpoint expects — typically JSON such as
    /// <c>{"vin": "{{candidate.vin}}", "branch": "{{answers.branch}}"}</c>. Substituted
    /// values are escaped for the <see cref="ContentType"/>, so a quote or newline in an
    /// answer cannot break the document.
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>
    /// Media type of <see cref="Body"/>, sent as <c>Content-Type</c>. Defaults to
    /// <c>application/json</c> when a body is present; an explicit <c>Content-Type</c>
    /// entry in <see cref="Headers"/> wins. Also selects the escaping applied to
    /// substituted tokens inside the body.
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    public const string DefaultBodyContentType = "application/json";

    /// <summary>True when the request is a POST (case-insensitive; null means GET).</summary>
    [JsonIgnore]
    public bool IsPost => string.Equals(Method?.Trim(), "POST", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The media type the body is sent as: the explicit <see cref="ContentType"/>,
    /// else <see cref="DefaultBodyContentType"/> when there is a body, else null.
    /// </summary>
    [JsonIgnore]
    public string? EffectiveContentType =>
        !string.IsNullOrWhiteSpace(ContentType) ? ContentType!.Trim()
        : Body is not null ? DefaultBodyContentType
        : null;

    /// <summary>
    /// The options source of a question, for the four choice types that can carry one.
    /// Null for every other question type.
    /// </summary>
    public static OptionsSourceDto? Of(QuestionDto question) => question switch
    {
        SingleChoiceQuestionDto q => q.OptionsSource,
        MultiChoiceQuestionDto q => q.OptionsSource,
        DropdownQuestionDto q => q.OptionsSource,
        NavigationListQuestionDto q => q.OptionsSource,
        _ => null,
    };
}

public class OptionsSourceDtoValidator : AbstractValidator<OptionsSourceDto>
{
    public OptionsSourceDtoValidator()
    {
        RuleFor(x => x.Url).NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("optionsSource.url must be an absolute http(s) URL.");
        When(x => x.QueryParams is not null, () =>
            RuleForEach(x => x.QueryParams!).ChildRules(pair =>
                pair.RuleFor(p => p.Key).NotEmpty().WithMessage("optionsSource.queryParams keys must be non-empty.")));
        When(x => x.Headers is not null, () =>
            RuleForEach(x => x.Headers!).ChildRules(pair =>
                pair.RuleFor(p => p.Key).NotEmpty().WithMessage("optionsSource.headers keys must be non-empty.")));
        RuleFor(x => x.Method)
            .Must(m => m is null || IsGet(m) || string.Equals(m.Trim(), "POST", StringComparison.OrdinalIgnoreCase))
            .WithMessage("optionsSource.method must be GET or POST.");
        RuleFor(x => x.Body)
            .Must((source, body) => body is null || source.IsPost)
            .WithMessage("optionsSource.body needs method POST — a GET request has no body.");
    }

    private static bool IsGet(string method) =>
        string.IsNullOrWhiteSpace(method) || string.Equals(method.Trim(), "GET", StringComparison.OrdinalIgnoreCase);
}
