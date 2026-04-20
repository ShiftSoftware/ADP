using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class NpsQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.Nps;

    [JsonPropertyName("min")]
    public int Min { get; set; } = 0;

    [JsonPropertyName("max")]
    public int Max { get; set; } = 10;

    [JsonPropertyName("lowLabel")]
    public LocalizedString? LowLabel { get; set; }

    [JsonPropertyName("highLabel")]
    public LocalizedString? HighLabel { get; set; }
}

public class NpsQuestionDtoValidator : QuestionDtoBaseValidator<NpsQuestionDto>
{
    public NpsQuestionDtoValidator()
    {
        RuleFor(x => x.Min).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Max).LessThanOrEqualTo(20);
        RuleFor(x => x).Must(x => x.Min < x.Max)
            .WithMessage("NPS min must be strictly less than max.");
        When(x => x.LowLabel is not null, () =>
            RuleFor(x => x.LowLabel!).SetValidator(new LocalizedStringValidator()));
        When(x => x.HighLabel is not null, () =>
            RuleFor(x => x.HighLabel!).SetValidator(new LocalizedStringValidator()));
    }
}
