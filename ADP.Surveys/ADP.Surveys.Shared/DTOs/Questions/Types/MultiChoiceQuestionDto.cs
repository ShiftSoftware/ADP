using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class MultiChoiceQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.MultiChoice;

    [JsonPropertyName("options")]
    public List<OptionDto> Options { get; set; } = new();

    [JsonPropertyName("minSelected")]
    public int? MinSelected { get; set; }

    [JsonPropertyName("maxSelected")]
    public int? MaxSelected { get; set; }
}

public class MultiChoiceQuestionDtoValidator : QuestionDtoBaseValidator<MultiChoiceQuestionDto>
{
    public MultiChoiceQuestionDtoValidator()
    {
        RuleFor(x => x.Options).NotEmpty()
            .WithMessage("MultiChoice questions must have at least one option.");
        RuleForEach(x => x.Options).SetValidator(new OptionDtoValidator());
        RuleFor(x => x.Options)
            .Must(options => options.Select(o => o.Id).Distinct().Count() == options.Count)
            .When(x => x.Options.Count > 0)
            .WithMessage("Option ids must be unique within the question.");
        RuleFor(x => x.MinSelected).GreaterThanOrEqualTo(0).When(x => x.MinSelected.HasValue);
        RuleFor(x => x.MaxSelected).GreaterThanOrEqualTo(1).When(x => x.MaxSelected.HasValue);
        RuleFor(x => x).Must(x => x.MinSelected!.Value <= x.MaxSelected!.Value)
            .When(x => x.MinSelected.HasValue && x.MaxSelected.HasValue)
            .WithMessage("minSelected must be less than or equal to maxSelected.");
        RuleFor(x => x).Must(x => x.MaxSelected!.Value <= x.Options.Count)
            .When(x => x.MaxSelected.HasValue && x.Options.Count > 0)
            .WithMessage("maxSelected cannot exceed the number of options.");
    }
}
