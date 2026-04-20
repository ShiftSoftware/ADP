using System.Text.Json.Serialization;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;

/// <summary>
/// Option for choice-type questions (singleChoice, multiChoice, dropdown).
/// </summary>
public class OptionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("label")]
    public LocalizedString Label { get; set; } = new();

    [JsonPropertyName("biColumn")]
    public string? BiColumn { get; set; }
}

public class OptionDtoValidator : AbstractValidator<OptionDto>
{
    public OptionDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Label).NotNull().SetValidator(new LocalizedStringValidator());
    }
}
