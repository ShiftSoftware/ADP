using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.Evaluation;

namespace ShiftSoftware.ADP.Surveys.Shared.Evaluation.ExpressionSandbox;

/// <summary>
/// Tree-walking interpreter. No host-environment access — the only I/O is the answer
/// map passed in and the hard-coded built-in functions.
///
/// Value domain is <see cref="object"/>? with four concrete runtime types: <see cref="decimal"/>,
/// <see cref="string"/>, <see cref="bool"/>, or <c>null</c>, plus <see cref="List{T}"/> of
/// <see cref="object"/>? for array literals. <see cref="JsonElement"/> answer values are
/// unwrapped to these types on access.
/// </summary>
internal static class Interpreter
{
    public static object? Evaluate(ExprNode node, IReadOnlyDictionary<string, JsonElement> answers) => node switch
    {
        LiteralNode lit => lit.Value,
        AnswersAccessNode access => ReadAnswer(access.Key, answers),
        UnaryOpNode unary => EvaluateUnary(unary, answers),
        BinaryOpNode bin => EvaluateBinary(bin, answers),
        CallNode call => EvaluateCall(call, answers),
        ArrayNode arr => arr.Items.Select(i => Evaluate(i, answers)).ToList<object?>(),
        _ => throw new InvalidOperationException($"Unknown node type {node.GetType().Name}."),
    };

    private static object? EvaluateUnary(UnaryOpNode node, IReadOnlyDictionary<string, JsonElement> answers)
    {
        var operand = Evaluate(node.Operand, answers);
        return node.Op switch
        {
            "!" => !ToBool(operand),
            _ => throw new InvalidOperationException($"Unknown unary operator '{node.Op}'."),
        };
    }

    private static object? EvaluateBinary(BinaryOpNode node, IReadOnlyDictionary<string, JsonElement> answers)
    {
        // Short-circuit logicals
        if (node.Op == "&&")
        {
            var l = Evaluate(node.Left, answers);
            if (!ToBool(l)) return false;
            return ToBool(Evaluate(node.Right, answers));
        }
        if (node.Op == "||")
        {
            var l = Evaluate(node.Left, answers);
            if (ToBool(l)) return true;
            return ToBool(Evaluate(node.Right, answers));
        }

        var left = Evaluate(node.Left, answers);
        var right = Evaluate(node.Right, answers);

        return node.Op switch
        {
            "==" => AreEqual(left, right),
            "!=" => !AreEqual(left, right),
            "<" => Compare(left, right) < 0,
            ">" => Compare(left, right) > 0,
            "<=" => Compare(left, right) <= 0,
            ">=" => Compare(left, right) >= 0,
            _ => throw new InvalidOperationException($"Unknown binary operator '{node.Op}'."),
        };
    }

    private static object? EvaluateCall(CallNode node, IReadOnlyDictionary<string, JsonElement> answers)
    {
        return node.Name switch
        {
            "has" or "isSet" => CallIsSet(node, answers),
            "isNotSet" => !CallIsSet(node, answers),
            "in" => CallIn(node, answers),
            _ => throw new InvalidOperationException($"Unknown function '{node.Name}'."),
        };
    }

    private static bool CallIsSet(CallNode node, IReadOnlyDictionary<string, JsonElement> answers)
    {
        if (node.Args.Count != 1)
            throw new InvalidOperationException($"{node.Name}() takes one argument.");
        var key = Evaluate(node.Args[0], answers);
        if (key is not string s) return false;
        return answers.TryGetValue(s, out var v) && v.ValueKind != JsonValueKind.Null;
    }

    private static bool CallIn(CallNode node, IReadOnlyDictionary<string, JsonElement> answers)
    {
        if (node.Args.Count != 2)
            throw new InvalidOperationException("in() takes two arguments: in(value, [array]).");
        var value = Evaluate(node.Args[0], answers);
        var collection = Evaluate(node.Args[1], answers);
        if (collection is not List<object?> list) return false;
        return list.Any(item => AreEqual(value, item));
    }

    private static object? ReadAnswer(string key, IReadOnlyDictionary<string, JsonElement> answers) =>
        answers.TryGetValue(key, out var v) ? JsonValueHelper.Unwrap(v) : null;

    internal static bool ToBool(object? value) => JsonValueHelper.ToBool(value);

    internal static bool AreEqual(object? left, object? right) => JsonValueHelper.AreEqual(left, right);

    private static int Compare(object? left, object? right) => JsonValueHelper.Compare(left, right);
}
