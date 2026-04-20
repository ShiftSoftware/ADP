using System.Text.Json.Serialization;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;

/// <summary>
/// One cross-screen branching rule. Evaluated in order against the accumulated
/// answer map on Next; first matching rule's <see cref="Then"/> wins.
/// </summary>
public class LogicRuleDto
{
    [JsonPropertyName("if")]
    public LogicConditionDto If { get; set; } = default!;

    [JsonPropertyName("then")]
    public LogicActionDto Then { get; set; } = new();
}

public class LogicRuleDtoValidator : AbstractValidator<LogicRuleDto>
{
    public LogicRuleDtoValidator()
    {
        RuleFor(x => x.If).NotNull().SetValidator(new LogicConditionDtoValidator());
        RuleFor(x => x.Then).NotNull().SetValidator(new LogicActionDtoValidator());
    }
}
