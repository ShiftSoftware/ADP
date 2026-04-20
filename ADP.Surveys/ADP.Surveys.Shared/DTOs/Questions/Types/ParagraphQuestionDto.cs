using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class ParagraphQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.Paragraph;

    [JsonPropertyName("minLength")]
    public int? MinLength { get; set; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }

    [JsonPropertyName("placeholder")]
    public LocalizedString? Placeholder { get; set; }
}

public class ParagraphQuestionDtoValidator : QuestionDtoBaseValidator<ParagraphQuestionDto>
{
    public ParagraphQuestionDtoValidator()
    {
        RuleFor(x => x.MinLength).GreaterThanOrEqualTo(0).When(x => x.MinLength.HasValue);
        RuleFor(x => x.MaxLength).GreaterThanOrEqualTo(0).When(x => x.MaxLength.HasValue);
        RuleFor(x => x).Must(x => x.MinLength!.Value <= x.MaxLength!.Value)
            .When(x => x.MinLength.HasValue && x.MaxLength.HasValue)
            .WithMessage("minLength must be less than or equal to maxLength.");
    }
}
