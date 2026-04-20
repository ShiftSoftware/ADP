using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

public class TextQuestionDto : QuestionDto
{
    public override QuestionType QuestionType => QuestionType.Text;

    [JsonPropertyName("minLength")]
    public int? MinLength { get; set; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    [JsonPropertyName("placeholder")]
    public LocalizedString? Placeholder { get; set; }
}

public class TextQuestionDtoValidator : QuestionDtoBaseValidator<TextQuestionDto>
{
    public TextQuestionDtoValidator()
    {
        RuleFor(x => x.MinLength).GreaterThanOrEqualTo(0).When(x => x.MinLength.HasValue);
        RuleFor(x => x.MaxLength).GreaterThanOrEqualTo(0).When(x => x.MaxLength.HasValue);
        RuleFor(x => x).Must(x => x.MinLength!.Value <= x.MaxLength!.Value)
            .When(x => x.MinLength.HasValue && x.MaxLength.HasValue)
            .WithMessage("minLength must be less than or equal to maxLength.");
        RuleFor(x => x.Pattern).Must(BeValidRegex!)
            .When(x => !string.IsNullOrEmpty(x.Pattern))
            .WithMessage("Pattern must be a valid regular expression.");
    }

    private static bool BeValidRegex(string pattern)
    {
        try { _ = new Regex(pattern); return true; }
        catch { return false; }
    }
}
