using System.Globalization;
using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class DateQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.Date;

    [JsonPropertyName("minDate")]
    public string? MinDate { get; set; }

    [JsonPropertyName("maxDate")]
    public string? MaxDate { get; set; }
}

public class DateQuestionDtoValidator : QuestionDtoBaseValidator<DateQuestionDto>
{
    public DateQuestionDtoValidator()
    {
        RuleFor(x => x.MinDate).Must(BeValidIsoDate!)
            .When(x => !string.IsNullOrEmpty(x.MinDate))
            .WithMessage("minDate must be an ISO date (yyyy-MM-dd).");
        RuleFor(x => x.MaxDate).Must(BeValidIsoDate!)
            .When(x => !string.IsNullOrEmpty(x.MaxDate))
            .WithMessage("maxDate must be an ISO date (yyyy-MM-dd).");
        RuleFor(x => x).Must(x =>
            DateOnly.ParseExact(x.MinDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            <= DateOnly.ParseExact(x.MaxDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture))
            .When(x => BeValidIsoDate(x.MinDate) && BeValidIsoDate(x.MaxDate))
            .WithMessage("minDate must be on or before maxDate.");
    }

    internal static bool BeValidIsoDate(string? value) =>
        !string.IsNullOrEmpty(value) &&
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
