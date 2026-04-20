using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

/// <summary>
/// Single-choice question presented as a forward-action list. Selecting an option is
/// both the answer (option id) and the screen transition — no separate Next button.
/// Per <see cref="NavigationListOptionDto.NextScreen"/> on each option.
/// </summary>
public class NavigationListQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.NavigationList;

    public override bool IsNavigationTerminal => true;

    [JsonPropertyName("options")]
    public List<NavigationListOptionDto> Options { get; set; } = new();
}

public class NavigationListQuestionDtoValidator : QuestionDtoBaseValidator<NavigationListQuestionDto>
{
    public NavigationListQuestionDtoValidator()
    {
        RuleFor(x => x.Options).NotEmpty()
            .WithMessage("NavigationList questions must have at least one option.");
        RuleForEach(x => x.Options).SetValidator(new NavigationListOptionDtoValidator());
        RuleFor(x => x.Options)
            .Must(options => options.Select(o => o.Id).Distinct().Count() == options.Count)
            .When(x => x.Options.Count > 0)
            .WithMessage("Option ids must be unique within the question.");
    }
}
