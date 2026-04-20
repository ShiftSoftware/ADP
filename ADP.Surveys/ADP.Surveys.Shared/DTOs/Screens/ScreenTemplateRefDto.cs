using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;

/// <summary>
/// Reference form of a screen inside a draft survey: points at a <c>ScreenTemplate</c>
/// by id, with optional overrides. Resolved inline at publish time.
/// </summary>
public class ScreenTemplateRefDto : ScreenDto
{
    [JsonPropertyName("templateRef")]
    public string TemplateRef { get; set; } = "";

    [JsonPropertyName("overrides")]
    public ScreenTemplateOverridesDto? Overrides { get; set; }
}

public class ScreenTemplateOverridesDto
{
    [JsonPropertyName("title")]
    public LocalizedString? Title { get; set; }

    [JsonPropertyName("description")]
    public LocalizedString? Description { get; set; }

    /// <summary>
    /// Per-banked-question overrides inside this template usage. Keyed by banked question id.
    /// </summary>
    [JsonPropertyName("questions")]
    public Dictionary<string, QuestionOverridesDto>? Questions { get; set; }

    [JsonPropertyName("nextScreen")]
    public string? NextScreen { get; set; }
}

public class ScreenTemplateRefDtoValidator : AbstractValidator<ScreenTemplateRefDto>
{
    public ScreenTemplateRefDtoValidator()
    {
        RuleFor(x => x.TemplateRef).NotEmpty();
        When(x => x.Overrides is not null, () =>
            RuleFor(x => x.Overrides!).SetValidator(new ScreenTemplateOverridesDtoValidator()));
    }
}

public class ScreenTemplateOverridesDtoValidator : AbstractValidator<ScreenTemplateOverridesDto>
{
    public ScreenTemplateOverridesDtoValidator()
    {
        When(x => x.Title is not null, () =>
            RuleFor(x => x.Title!).SetValidator(new LocalizedStringValidator()));
        When(x => x.Description is not null, () =>
            RuleFor(x => x.Description!).SetValidator(new LocalizedStringValidator()));
        When(x => x.Questions is not null, () =>
        {
            RuleForEach(x => x.Questions!).ChildRules(pair =>
            {
                pair.RuleFor(p => p.Key).NotEmpty();
                pair.RuleFor(p => p.Value).NotNull().SetValidator(new QuestionOverridesDtoValidator());
            });
        });
    }
}
