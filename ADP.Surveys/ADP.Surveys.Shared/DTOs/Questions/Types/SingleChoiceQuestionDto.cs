using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class SingleChoiceQuestionDto : QuestionDto
{
    [JsonIgnore]
    public override QuestionType QuestionType => QuestionType.SingleChoice;

    [JsonPropertyName("options")]
    public List<OptionDto> Options { get; set; } = new();

    /// <summary>External options endpoint — replaces inline <see cref="Options"/>; fetched client-side at render time.</summary>
    [JsonPropertyName("optionsSource")]
    public OptionsSourceDto? OptionsSource { get; set; }
}

public class SingleChoiceQuestionDtoValidator : QuestionDtoBaseValidator<SingleChoiceQuestionDto>
{
    public SingleChoiceQuestionDtoValidator()
    {
        RuleFor(x => x.Options).NotEmpty()
            .When(x => x.OptionsSource is null)
            .WithMessage("SingleChoice questions must have at least one option (or an optionsSource).");
        RuleForEach(x => x.Options).SetValidator(new OptionDtoValidator());
        RuleFor(x => x.Options)
            .Must(options => options.Select(o => o.Id).Distinct().Count() == options.Count)
            .When(x => x.Options.Count > 0)
            .WithMessage("Option ids must be unique within the question.");
        When(x => x.OptionsSource is not null, () =>
            RuleFor(x => x.OptionsSource!).SetValidator(new OptionsSourceDtoValidator()));
    }
}
