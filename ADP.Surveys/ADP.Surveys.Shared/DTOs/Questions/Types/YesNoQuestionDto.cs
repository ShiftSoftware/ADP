using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class YesNoQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.YesNo;

    [JsonPropertyName("yesLabel")]
    public LocalizedString? YesLabel { get; set; }

    [JsonPropertyName("noLabel")]
    public LocalizedString? NoLabel { get; set; }
}

public class YesNoQuestionDtoValidator : QuestionDtoBaseValidator<YesNoQuestionDto>
{
    public YesNoQuestionDtoValidator()
    {
        When(x => x.YesLabel is not null, () =>
            RuleFor(x => x.YesLabel!).SetValidator(new LocalizedStringValidator()));
        When(x => x.NoLabel is not null, () =>
            RuleFor(x => x.NoLabel!).SetValidator(new LocalizedStringValidator()));
    }
}
