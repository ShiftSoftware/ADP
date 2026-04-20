using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;

/// <summary>
/// Base class for every question in a resolved survey schema. The <c>"type"</c>
/// JSON discriminator dispatches to the concrete per-type DTO on deserialization.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextQuestionDto), "text")]
[JsonDerivedType(typeof(ParagraphQuestionDto), "paragraph")]
[JsonDerivedType(typeof(NumberQuestionDto), "number")]
[JsonDerivedType(typeof(RatingQuestionDto), "rating")]
[JsonDerivedType(typeof(NpsQuestionDto), "nps")]
[JsonDerivedType(typeof(SingleChoiceQuestionDto), "singleChoice")]
[JsonDerivedType(typeof(MultiChoiceQuestionDto), "multiChoice")]
[JsonDerivedType(typeof(DropdownQuestionDto), "dropdown")]
[JsonDerivedType(typeof(DateQuestionDto), "date")]
[JsonDerivedType(typeof(DateTimeQuestionDto), "dateTime")]
[JsonDerivedType(typeof(FileQuestionDto), "file")]
[JsonDerivedType(typeof(SignatureQuestionDto), "signature")]
[JsonDerivedType(typeof(YesNoQuestionDto), "yesNo")]
[JsonDerivedType(typeof(NavigationListQuestionDto), "navigationList")]
public abstract class QuestionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public LocalizedString Title { get; set; } = new();

    [JsonPropertyName("help")]
    public LocalizedString? Help { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("biColumn")]
    public string? BiColumn { get; set; }

    [JsonIgnore]
    public abstract QuestionType QuestionType { get; }

    /// <summary>
    /// "Navigation-terminal" types replace the Next button with their own transition affordance.
    /// Currently only <see cref="NavigationListQuestionDto"/>.
    /// </summary>
    [JsonIgnore]
    public virtual bool IsNavigationTerminal => false;
}

/// <summary>
/// Generic base for per-type question validators. Each <c>{Type}QuestionDtoValidator</c>
/// derives from <see cref="QuestionDtoBaseValidator{T}"/> to pick up the shared
/// <see cref="QuestionDto.Id"/> / <see cref="QuestionDto.Title"/> / <see cref="QuestionDto.Help"/>
/// rules and then adds its type-specific field checks.
/// </summary>
public abstract class QuestionDtoBaseValidator<T> : AbstractValidator<T> where T : QuestionDto
{
    protected QuestionDtoBaseValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotNull().SetValidator(new LocalizedStringValidator());
        When(x => x.Help is not null, () =>
            RuleFor(x => x.Help!).SetValidator(new LocalizedStringValidator()));
    }
}
