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
    [JsonIgnore]
    public override QuestionType QuestionType => QuestionType.NavigationList;

    [JsonIgnore]
    public override bool IsNavigationTerminal => true;

    [JsonPropertyName("options")]
    public List<NavigationListOptionDto> Options { get; set; } = new();

    /// <summary>
    /// External options endpoint — replaces inline <see cref="Options"/>; fetched
    /// client-side at render time. Every fetched option navigates to the source's
    /// single <see cref="OptionsSourceDto.NextScreen"/> (per-option routing needs
    /// authored options).
    /// </summary>
    [JsonPropertyName("optionsSource")]
    public OptionsSourceDto? OptionsSource { get; set; }
}

public class NavigationListQuestionDtoValidator : QuestionDtoBaseValidator<NavigationListQuestionDto>
{
    public NavigationListQuestionDtoValidator()
    {
        RuleFor(x => x.Options).NotEmpty()
            .When(x => x.OptionsSource is null)
            .WithMessage("NavigationList questions must have at least one option (or an optionsSource).");
        When(x => x.OptionsSource is not null, () =>
        {
            RuleFor(x => x.OptionsSource!).SetValidator(new OptionsSourceDtoValidator());
            RuleFor(x => x.OptionsSource!.NextScreen).NotEmpty()
                .WithMessage("A sourced navigationList must declare optionsSource.nextScreen — all fetched options share it as their destination.");
        });
        RuleForEach(x => x.Options).SetValidator(new NavigationListOptionDtoValidator());
        RuleFor(x => x.Options)
            .Must(options => options.Select(o => o.Id).Distinct().Count() == options.Count)
            .When(x => x.Options.Count > 0)
            .WithMessage("Option ids must be unique within the question.");
    }
}
