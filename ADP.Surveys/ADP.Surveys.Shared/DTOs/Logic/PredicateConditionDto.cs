using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Enums;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;

/// <summary>
/// A single <c>{ questionId, op, value }</c> predicate. The answer to <c>questionId</c>
/// is compared to <c>value</c> with the operator <c>op</c>.
/// <c>value</c> is kept as a <see cref="JsonElement"/> so it can be any JSON primitive,
/// array (for <c>in</c> / <c>notIn</c>), or null.
/// </summary>
public class PredicateConditionDto : LogicConditionDto
{
    [JsonPropertyName("questionId")]
    public string QuestionId { get; set; } = "";

    [JsonPropertyName("op")]
    public LogicOperator Op { get; set; }

    [JsonPropertyName("value")]
    public JsonElement? Value { get; set; }
}

public class PredicateConditionDtoValidator : AbstractValidator<PredicateConditionDto>
{
    public PredicateConditionDtoValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.Value).NotNull()
            .When(x => x.Op != LogicOperator.IsSet && x.Op != LogicOperator.IsNotSet)
            .WithMessage("value is required for this operator.");
        RuleFor(x => x.Value)
            .Must(v => v!.Value.ValueKind == JsonValueKind.Array)
            .When(x => (x.Op == LogicOperator.In || x.Op == LogicOperator.NotIn) && x.Value.HasValue)
            .WithMessage("in/notIn operators require an array value.");
    }
}
