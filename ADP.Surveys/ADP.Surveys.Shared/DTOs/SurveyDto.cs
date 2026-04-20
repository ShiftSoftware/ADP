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

public class SurveyDtoValidator : AbstractValidator<SurveyDto>
{
    public SurveyDtoValidator()
    {
        RuleFor(x => x.SurveyId).NotEmpty();
        RuleFor(x => x.Version).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Title).NotNull().SetValidator(new LocalizedStringValidator());
        When(x => x.Description is not null, () =>
            RuleFor(x => x.Description!).SetValidator(new LocalizedStringValidator()));

        RuleFor(x => x.Locales).NotEmpty()
            .WithMessage("Survey must declare at least one locale.");
        RuleFor(x => x.DefaultLocale).NotEmpty();
        RuleFor(x => x).Must(x => x.Locales.Contains(x.DefaultLocale))
            .When(x => !string.IsNullOrEmpty(x.DefaultLocale) && x.Locales.Count > 0)
            .WithMessage("defaultLocale must be one of the declared locales.");

        When(x => x.Branding is not null, () =>
            RuleFor(x => x.Branding!).SetValidator(new BrandingDtoValidator()));

        RuleFor(x => x.Screens).NotEmpty()
            .WithMessage("Survey must have at least one screen.");
        RuleForEach(x => x.Screens).SetInheritanceValidator(v =>
        {
            v.Add(new InlineScreenDtoValidator());
            v.Add(new ScreenTemplateRefDtoValidator());
        });

        RuleForEach(x => x.Logic).SetValidator(new LogicRuleDtoValidator());
    }
}
