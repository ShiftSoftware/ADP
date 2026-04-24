using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs;

/// <summary>
/// Top-level survey schema. A survey always belongs to a version (immutable after publish).
/// Draft surveys may carry <see cref="ScreenTemplateRefDto"/> screens and <see cref="QuestionRefDto"/>
/// entries; resolved/published surveys are fully inline.
/// </summary>
public class SurveyDto
{
    [JsonPropertyName("surveyId")]
    public string SurveyId { get; set; } = "";

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset? PublishedAt { get; set; }

    [JsonPropertyName("title")]
    public LocalizedString Title { get; set; } = new();

    [JsonPropertyName("description")]
    public LocalizedString? Description { get; set; }

    [JsonPropertyName("locales")]
    public List<string> Locales { get; set; } = new();

    [JsonPropertyName("defaultLocale")]
    public string DefaultLocale { get; set; } = "en";

    [JsonPropertyName("branding")]
    public BrandingDto? Branding { get; set; }

    [JsonPropertyName("screens")]
    public List<ScreenDto> Screens { get; set; } = new();

    [JsonPropertyName("logic")]
    public List<LogicRuleDto> Logic { get; set; } = new();
}

/// <summary>
/// Draft-time validator. Runs on every Survey save so the builder can persist
/// work-in-progress without being blocked by publish-gate rules. Structural
/// checks (type shape, screen/question item-level validity, logic shape) stay
/// here; "has-at-least-one-screen / title / locales" checks are in
/// <see cref="SurveyPublishValidator"/>.
/// </summary>
public class SurveyDtoValidator : AbstractValidator<SurveyDto>
{
    public SurveyDtoValidator()
    {
        // SurveyId is server-owned — stamped from the entity ID on load (see
        // GeneralMappingProfile.MapSurvey). Not validated at author-time.
        RuleFor(x => x.Version).GreaterThanOrEqualTo(0);

        // Title / Description / Locales / DefaultLocale are allowed to be empty on
        // drafts. When they are populated they still have to be well-formed.
        When(x => x.Title is not null && x.Title.Count > 0, () =>
            RuleFor(x => x.Title!).SetValidator(new LocalizedStringValidator()));
        When(x => x.Description is not null && x.Description.Count > 0, () =>
            RuleFor(x => x.Description!).SetValidator(new LocalizedStringValidator()));
        RuleFor(x => x).Must(x => x.Locales.Contains(x.DefaultLocale))
            .When(x => !string.IsNullOrEmpty(x.DefaultLocale) && x.Locales.Count > 0)
            .WithMessage("defaultLocale must be one of the declared locales.");

        When(x => x.Branding is not null, () =>
            RuleFor(x => x.Branding!).SetValidator(new BrandingDtoValidator()));

        RuleForEach(x => x.Screens).SetInheritanceValidator(v =>
        {
            v.Add(new InlineScreenDtoValidator());
            v.Add(new ScreenTemplateRefDtoValidator());
        });

        RuleForEach(x => x.Logic).SetValidator(new LogicRuleDtoValidator());
    }
}

/// <summary>
/// Publish-time validator. Adds the strict rules that drafts are exempt from: a
/// survey must declare locales, a default locale, a title, and at least one
/// screen before it can be published. Runs alongside <see cref="SurveyDtoValidator"/>
/// in the publish pipeline.
/// </summary>
public class SurveyPublishValidator : AbstractValidator<SurveyDto>
{
    public SurveyPublishValidator()
    {
        RuleFor(x => x.Title).NotNull().SetValidator(new LocalizedStringValidator());
        RuleFor(x => x.Locales).NotEmpty()
            .WithMessage("Survey must declare at least one locale before publishing.");
        RuleFor(x => x.DefaultLocale).NotEmpty()
            .WithMessage("Survey must declare a default locale before publishing.");
        RuleFor(x => x.Screens).NotEmpty()
            .WithMessage("Survey must have at least one screen before publishing.");
    }
}
