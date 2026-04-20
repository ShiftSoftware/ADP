using System.Text.Json.Serialization;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;

/// <summary>
/// Action taken when a <see cref="LogicRuleDto"/>'s condition matches.
/// For now only <c>goto</c>. Future actions (skip, end, setVariable) land here as siblings.
/// </summary>
public class LogicActionDto
{
    [JsonPropertyName("goto")]
    public string? Goto { get; set; }
}

public class LogicActionDtoValidator : AbstractValidator<LogicActionDto>
{
    public LogicActionDtoValidator()
    {
        RuleFor(x => x.Goto).NotEmpty()
            .WithMessage("Logic action must declare a goto target.");
    }
}
