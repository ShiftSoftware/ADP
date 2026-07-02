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
    }
}
