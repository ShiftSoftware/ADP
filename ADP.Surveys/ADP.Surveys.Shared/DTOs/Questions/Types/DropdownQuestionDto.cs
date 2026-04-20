using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class DropdownQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.Dropdown;

    [JsonPropertyName("options")]
    public List<OptionDto> Options { get; set; } = new();

    [JsonPropertyName("placeholder")]
    public LocalizedString? Placeholder { get; set; }
}

public class DropdownQuestionDtoValidator : QuestionDtoBaseValidator<DropdownQuestionDto>
{
    public DropdownQuestionDtoValidator()
    {
        RuleFor(x => x.Options).NotEmpty()
            .WithMessage("Dropdown questions must have at least one option.");
        RuleForEach(x => x.Options).SetValidator(new OptionDtoValidator());
        RuleFor(x => x.Options)
            .Must(options => options.Select(o => o.Id).Distinct().Count() == options.Count)
            .When(x => x.Options.Count > 0)
            .WithMessage("Option ids must be unique within the question.");
        When(x => x.Placeholder is not null, () =>
            RuleFor(x => x.Placeholder!).SetValidator(new LocalizedStringValidator()));
    }
}
