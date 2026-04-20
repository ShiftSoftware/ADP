using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Json;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;

/// <summary>
/// Base class for the three condition shapes inside a <see cref="LogicRuleDto.If"/>:
/// <see cref="PredicateConditionDto"/>, <see cref="CompositeConditionDto"/>, or
/// <see cref="ExpressionConditionDto"/>. Discriminated by property presence —
/// see <see cref="LogicConditionDtoConverter"/>.
/// </summary>
[JsonConverter(typeof(LogicConditionDtoConverter))]
public abstract class LogicConditionDto
{
}

public class LogicConditionDtoValidator : AbstractValidator<LogicConditionDto>
{
    public LogicConditionDtoValidator()
    {
        RuleFor(x => x).SetInheritanceValidator(v =>
        {
            v.Add(new PredicateConditionDtoValidator());
            v.Add(new CompositeConditionDtoValidator());
            v.Add(new ExpressionConditionDtoValidator());
        });
    }
}
