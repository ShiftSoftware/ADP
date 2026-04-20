using System.Text.Json.Serialization;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;

/// <summary>
/// Option for <c>navigationList</c> questions — an option that is both the answer
/// and the screen transition. Selecting it records <c>{ questionId: optionId }</c>
/// and navigates to <see cref="NextScreen"/>.
/// </summary>
public class NavigationListOptionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("label")]
    public LocalizedString Label { get; set; } = new();

    [JsonPropertyName("nextScreen")]
    public string? NextScreen { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("biColumn")]
    public string? BiColumn { get; set; }
}

public class NavigationListOptionDtoValidator : AbstractValidator<NavigationListOptionDto>
{
    public NavigationListOptionDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Label).NotNull().SetValidator(new LocalizedStringValidator());
        RuleFor(x => x.NextScreen).NotEmpty()
            .WithMessage("NavigationList options must declare a nextScreen.");
    }
}
