using System.Text.Json.Serialization;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;

/// <summary>
/// Decision #10 escape hatch — a string expression evaluated sandboxed against the
/// full answer map. Kept for flows that resist a visual rule editor.
/// </summary>
public class ExpressionConditionDto : LogicConditionDto
{
    [JsonPropertyName("expression")]
    public string Expression { get; set; } = "";
}

public class ExpressionConditionDtoValidator : AbstractValidator<ExpressionConditionDto>
{
    public ExpressionConditionDtoValidator()
    {
        RuleFor(x => x.Expression).NotEmpty();
        // Note: expression *syntax* is validated by the sandbox evaluator, not here —
        // FluentValidation only checks it's non-empty. The evaluator's parse step
        // surfaces syntax errors with richer messages at publish / run time.
    }
}
