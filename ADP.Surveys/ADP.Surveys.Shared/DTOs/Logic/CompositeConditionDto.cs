using System.Text.Json.Serialization;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;

/// <summary>
/// Boolean composition of nested conditions. Exactly one of <see cref="All"/> (AND) or
/// <see cref="Any"/> (OR) is populated per instance.
/// </summary>
public class CompositeConditionDto : LogicConditionDto
{
    [JsonPropertyName("all")]
    public List<LogicConditionDto>? All { get; set; }

    [JsonPropertyName("any")]
    public List<LogicConditionDto>? Any { get; set; }
}

public class CompositeConditionDtoValidator : AbstractValidator<CompositeConditionDto>
{
    public CompositeConditionDtoValidator()
    {
        RuleFor(x => x).Must(x => (x.All is not null) ^ (x.Any is not null))
            .WithMessage("Composite condition must set exactly one of 'all' or 'any'.");
        RuleFor(x => x.All!).NotEmpty().When(x => x.All is not null)
            .WithMessage("'all' must contain at least one child condition.");
        RuleFor(x => x.Any!).NotEmpty().When(x => x.Any is not null)
            .WithMessage("'any' must contain at least one child condition.");

        // Walk children via Custom() — instantiating per-type validators at validation time
        // instead of construction time. This breaks the construction-time cycle that would
        // otherwise arise (LogicConditionDtoValidator → CompositeConditionDtoValidator → …).
        RuleFor(x => x).Custom((comp, ctx) =>
        {
            var children = comp.All ?? comp.Any;
            if (children is null) return;
            var prefix = comp.All is not null ? "all" : "any";
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                FluentValidation.IValidator? inner = child switch
                {
                    PredicateConditionDto => new PredicateConditionDtoValidator(),
                    CompositeConditionDto => new CompositeConditionDtoValidator(),
                    ExpressionConditionDto => new ExpressionConditionDtoValidator(),
                    _ => null,
                };
                if (inner is null) continue;
                var childContext = new FluentValidation.ValidationContext<LogicConditionDto>(child);
                var result = inner.Validate(childContext);
                foreach (var failure in result.Errors)
                    ctx.AddFailure($"{prefix}[{i}].{failure.PropertyName}", failure.ErrorMessage);
            }
        });
    }
}
