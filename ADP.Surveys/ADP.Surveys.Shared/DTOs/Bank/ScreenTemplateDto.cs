using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;

/// <summary>
/// Reusable composition of banked questions. Surveys reference a template by
/// <see cref="Id"/> via <see cref="Screens.ScreenTemplateRefDto"/>; at publish the
/// template is expanded inline into the frozen survey version.
/// </summary>
public class ScreenTemplateDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public LocalizedString Title { get; set; } = new();

    [JsonPropertyName("description")]
    public LocalizedString? Description { get; set; }

    /// <summary>
    /// Banked-question references in template order. Templates compose only from the
    /// bank — inline questions inside a template are not supported.
    /// </summary>
    [JsonPropertyName("questions")]
    public List<QuestionRefDto> Questions { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

public class ScreenTemplateDtoValidator : AbstractValidator<ScreenTemplateDto>
{
    public ScreenTemplateDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotNull().SetValidator(new LocalizedStringValidator());
        When(x => x.Description is not null, () =>
            RuleFor(x => x.Description!).SetValidator(new LocalizedStringValidator()));
        RuleFor(x => x.Questions).NotEmpty()
            .WithMessage("Screen templates must contain at least one question reference.");
        RuleForEach(x => x.Questions).SetValidator(new QuestionRefDtoValidator());
    }
}
