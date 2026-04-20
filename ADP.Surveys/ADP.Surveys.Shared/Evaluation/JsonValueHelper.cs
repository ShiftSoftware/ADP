using System.Text.Json;

namespace ShiftSoftware.ADP.Surveys.Shared.Evaluation;

/// <summary>
/// Converts <see cref="JsonElement"/> values into the interpreter / predicate value
/// domain (<see cref="decimal"/> / <see cref="string"/> / <see cref="bool"/> / <see cref="List{T}"/> / null),
/// and provides equality + comparison with the same semantics both the expression
/// sandbox and the DTO-level <see cref="LogicEvaluator"/> use.
/// </summary>
public static class JsonValueHelper
{
    public static object? Unwrap(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => v.GetDecimal(),
        JsonValueKind.String => v.GetString(),
        JsonValueKind.Array => v.EnumerateArray().Select(Unwrap).ToList<object?>(),
        JsonValueKind.Object => null,
        _ => null,
    };

    public static object? Unwrap(JsonElement? v) => v.HasValue ? Unwrap(v.Value) : null;

    public static bool AreEqual(object? left, object? right)
    {
        if (left is null || right is null) return left is null && right is null;
        if (left is decimal ld && right is decimal rd) return ld == rd;
        if (left is string ls && right is string rs) return ls == rs;
        if (left is bool lb && right is bool rb) return lb == rb;
        if (left is List<object?> la && right is List<object?> ra)
            return la.Count == ra.Count && la.Zip(ra, AreEqual).All(x => x);
        return false;
    }

    /// <summary>
    /// Returns -1/0/1 for number-vs-number and string-vs-string comparisons.
    /// Throws <see cref="InvalidOperationException"/> otherwise — comparison across
    /// mismatched types is ambiguous, and logic-rule authors should make intent explicit.
    /// </summary>
    public static int Compare(object? left, object? right)
    {
        if (left is decimal ld && right is decimal rd) return ld.CompareTo(rd);
        if (left is string ls && right is string rs) return string.CompareOrdinal(ls, rs);
        throw new InvalidOperationException("Comparison operators require two numbers or two strings.");
    }

    public static bool ToBool(object? value) => value switch
    {
        null => false,
        bool b => b,
        decimal d => d != 0m,
        string s => !string.IsNullOrEmpty(s),
        List<object?> list => list.Count > 0,
        _ => true,
    };
}
