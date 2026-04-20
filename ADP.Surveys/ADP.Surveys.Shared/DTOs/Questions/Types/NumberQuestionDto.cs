using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class NumberQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.Number;

    [JsonPropertyName("min")]
    public decimal? Min { get; set; }

    [JsonPropertyName("max")]
    public decimal? Max { get; set; }

    [JsonPropertyName("step")]
    public decimal? Step { get; set; }

    [JsonPropertyName("unit")]
    public LocalizedString? Unit { get; set; }
}

public class NumberQuestionDtoValidator : QuestionDtoBaseValidator<NumberQuestionDto>
{
    public NumberQuestionDtoValidator()
    {
        RuleFor(x => x).Must(x => x.Min!.Value <= x.Max!.Value)
            .When(x => x.Min.HasValue && x.Max.HasValue)
            .WithMessage("min must be less than or equal to max.");
        RuleFor(x => x.Step).GreaterThan(0).When(x => x.Step.HasValue);
        When(x => x.Unit is not null, () =>
            RuleFor(x => x.Unit!).SetValidator(new LocalizedStringValidator()));
    }
}
