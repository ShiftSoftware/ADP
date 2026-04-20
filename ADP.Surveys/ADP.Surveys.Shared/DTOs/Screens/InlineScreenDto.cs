using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;

public class InlineScreenDto : ScreenDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public LocalizedString? Title { get; set; }

    [JsonPropertyName("description")]
    public LocalizedString? Description { get; set; }

    [JsonPropertyName("questions")]
    public List<QuestionEntryDto> Questions { get; set; } = new();

    /// <summary>
    /// Default next screen when no logic rule matches. Null means "end of survey"
    /// or sequential fallback per renderer convention.
    /// </summary>
    [JsonPropertyName("nextScreen")]
    public string? NextScreen { get; set; }
}

public class InlineScreenDtoValidator : AbstractValidator<InlineScreenDto>
{
    public InlineScreenDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        When(x => x.Title is not null, () =>
            RuleFor(x => x.Title!).SetValidator(new LocalizedStringValidator()));
        When(x => x.Description is not null, () =>
            RuleFor(x => x.Description!).SetValidator(new LocalizedStringValidator()));
        RuleForEach(x => x.Questions).SetValidator(new QuestionEntryDtoValidator());
    }
}
