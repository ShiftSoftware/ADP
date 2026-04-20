using System.Text.Json.Serialization;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;

/// <summary>
/// Reference form of a question inside a draft survey: points at a bank entry by id,
/// with optional presentation overrides. Resolved inline at publish time.
/// </summary>
public class QuestionRefDto
{
    [JsonPropertyName("bankRef")]
    public string BankRef { get; set; } = "";

    [JsonPropertyName("overrides")]
    public QuestionOverridesDto? Overrides { get; set; }
}

public class QuestionRefDtoValidator : AbstractValidator<QuestionRefDto>
{
    public QuestionRefDtoValidator()
    {
        RuleFor(x => x.BankRef).NotEmpty();
        When(x => x.Overrides is not null, () =>
            RuleFor(x => x.Overrides!).SetValidator(new QuestionOverridesDtoValidator()));
    }
}
