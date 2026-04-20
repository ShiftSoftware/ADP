using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;
using ShiftSoftware.ADP.Surveys.Shared.Enums;
using ShiftSoftware.ADP.Surveys.Shared.Evaluation.ExpressionSandbox;

namespace ShiftSoftware.ADP.Surveys.Shared.Evaluation;

/// <summary>
/// Evaluates cross-screen logic rules against an accumulated answer map. Per the
/// schema contract, rules are ordered; the first one whose condition matches wins.
/// If none match, the caller falls back to <c>screen.NextScreen</c> (or sequential
/// order, or end of survey).
///
/// Per Decision #10: evaluator accepts both structured conditions (predicates +
/// <c>all</c>/<c>any</c> composites) and the string-expression escape hatch
/// (<see cref="ExpressionConditionDto"/>), delegating the latter to <see cref="ExpressionSandbox.ExpressionSandbox"/>.
/// Any runtime evaluation failure is treated as <c>false</c> so a broken rule
/// falls through to the default navigation rather than blocking the survey.
/// </summary>
public static class LogicEvaluator
{
    /// <summary>
    /// Walks <see cref="SurveyDto.Logic"/> in order; returns the first matching rule's
    /// goto target, or <c>null</c> when nothing matches.
    /// </summary>
    public static string? EvaluateNext(SurveyDto survey, IReadOnlyDictionary<string, JsonElement> answers)
    {
        foreach (var rule in survey.Logic)
        {
            if (EvaluateCondition(rule.If, answers))
                return rule.Then?.Goto;
        }
        return null;
    }

    /// <summary>
    /// Evaluate a single condition. Exposed so the builder's rule-preview panel
    /// can test rules one at a time.
    /// </summary>
    public static bool EvaluateCondition(LogicConditionDto condition, IReadOnlyDictionary<string, JsonElement> answers)
    {
        try
        {
            return condition switch
            {
                PredicateConditionDto p => EvaluatePredicate(p, answers),
                CompositeConditionDto c => EvaluateComposite(c, answers),
                ExpressionConditionDto e => ExpressionSandbox.ExpressionSandbox.EvaluateBoolean(e.Expression, answers),
                _ => false,
            };
        }
        catch
        {
            // Defensive: a broken rule must not block navigation. Caller falls through.
            return false;
        }
    }

    private static bool EvaluateComposite(CompositeConditionDto composite, IReadOnlyDictionary<string, JsonElement> answers)
    {
        if (composite.All is { Count: > 0 } all)
            return all.All(child => EvaluateCondition(child, answers));
        if (composite.Any is { Count: > 0 } any)
            return any.Any(child => EvaluateCondition(child, answers));
        return false;
    }

    private static bool EvaluatePredicate(PredicateConditionDto predicate, IReadOnlyDictionary<string, JsonElement> answers)
    {
        var answerPresent = answers.TryGetValue(predicate.QuestionId, out var answerRaw)
            && answerRaw.ValueKind != JsonValueKind.Null;

        switch (predicate.Op)
        {
            case LogicOperator.IsSet: return answerPresent;
            case LogicOperator.IsNotSet: return !answerPresent;
        }

        // The remaining operators all need a value; treat "no value provided" as non-matching.
        if (!predicate.Value.HasValue) return false;

        var answerValue = answerPresent ? JsonValueHelper.Unwrap(answerRaw) : null;
        var predicateValue = JsonValueHelper.Unwrap(predicate.Value.Value);

        return predicate.Op switch
        {
            LogicOperator.Equals => JsonValueHelper.AreEqual(answerValue, predicateValue),
            LogicOperator.NotEquals => !JsonValueHelper.AreEqual(answerValue, predicateValue),
            LogicOperator.GreaterThan => JsonValueHelper.Compare(answerValue, predicateValue) > 0,
            LogicOperator.GreaterThanOrEqual => JsonValueHelper.Compare(answerValue, predicateValue) >= 0,
            LogicOperator.LessThan => JsonValueHelper.Compare(answerValue, predicateValue) < 0,
            LogicOperator.LessThanOrEqual => JsonValueHelper.Compare(answerValue, predicateValue) <= 0,
            LogicOperator.In => InList(predicateValue, answerValue),
            LogicOperator.NotIn => !InList(predicateValue, answerValue),
            _ => false,
        };
    }

    private static bool InList(object? list, object? value)
    {
        if (list is not List<object?> items) return false;
        return items.Any(item => JsonValueHelper.AreEqual(value, item));
    }
}
