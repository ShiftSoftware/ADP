using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class DateTimeQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.DateTime;

    [JsonPropertyName("minDateTime")]
    public string? MinDateTime { get; set; }

    [JsonPropertyName("maxDateTime")]
    public string? MaxDateTime { get; set; }
}

public class DateTimeQuestionDtoValidator : QuestionDtoBaseValidator<DateTimeQuestionDto>
{
    public DateTimeQuestionDtoValidator()
    {
        RuleFor(x => x.MinDateTime).Must(BeValidIsoDateTime!)
            .When(x => !string.IsNullOrEmpty(x.MinDateTime))
            .WithMessage("minDateTime must be a valid ISO 8601 datetime.");
        RuleFor(x => x.MaxDateTime).Must(BeValidIsoDateTime!)
            .When(x => !string.IsNullOrEmpty(x.MaxDateTime))
            .WithMessage("maxDateTime must be a valid ISO 8601 datetime.");
        RuleFor(x => x).Must(x =>
            DateTimeOffset.Parse(x.MinDateTime!) <= DateTimeOffset.Parse(x.MaxDateTime!))
            .When(x => BeValidIsoDateTime(x.MinDateTime) && BeValidIsoDateTime(x.MaxDateTime))
            .WithMessage("minDateTime must be on or before maxDateTime.");
    }

    private static bool BeValidIsoDateTime(string? value) =>
        !string.IsNullOrEmpty(value) && DateTimeOffset.TryParse(value, out _);
}
