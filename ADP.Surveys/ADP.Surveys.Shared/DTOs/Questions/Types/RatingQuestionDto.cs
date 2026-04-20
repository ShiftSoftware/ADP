using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class RatingQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.Rating;

    [JsonPropertyName("max")]
    public int Max { get; set; } = 5;

    [JsonPropertyName("allowHalf")]
    public bool AllowHalf { get; set; }
}

public class RatingQuestionDtoValidator : QuestionDtoBaseValidator<RatingQuestionDto>
{
    public RatingQuestionDtoValidator()
    {
        RuleFor(x => x.Max).InclusiveBetween(1, 20)
            .WithMessage("Rating max must be between 1 and 20.");
    }
}
