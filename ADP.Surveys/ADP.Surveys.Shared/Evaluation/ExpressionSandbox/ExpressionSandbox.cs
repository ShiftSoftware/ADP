using System.Text.Json;

namespace ShiftSoftware.ADP.Surveys.Shared.Evaluation.ExpressionSandbox;

/// <summary>
/// Public entry point for the survey expression mini-language. Used by
/// <see cref="Evaluation.LogicEvaluator"/> to evaluate the <c>expression</c> escape
/// hatch in logic rules (Decision #10) and by the builder to surface parse errors
/// at authoring time.
///
/// Grammar, AST and interpreter all live in this folder. A TypeScript mirror will
/// ship with the renderer SDK in Phase 3 — the same spec, two small implementations.
/// </summary>
public static class ExpressionSandbox
{
    /// <summary>
    /// Parse <paramref name="source"/> into an AST. Throws
    /// <see cref="ExpressionSyntaxException"/> on invalid syntax.
    /// </summary>
    public static ExprNode Parse(string source)
    {
        var tokens = new Lexer(source).Tokenize();
        return new Parser(tokens).Parse();
    }

    /// <summary>
    /// Evaluate <paramref name="source"/> against the answer map. Returns the boolean
    /// interpretation of the result (truthiness). Runtime errors during evaluation
    /// fall through as <c>false</c> so a broken expression defaults to "rule did not
    /// match" rather than blocking the survey — per Decision #10.
    /// </summary>
    public static bool EvaluateBoolean(string source, IReadOnlyDictionary<string, JsonElement> answers)
    {
        try
        {
            var ast = Parse(source);
            return Interpreter.ToBool(Interpreter.Evaluate(ast, answers));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Evaluate a pre-parsed AST against the answer map. Useful when a rule is parsed
    /// once at publish time and evaluated many times at runtime. Returns boolean
    /// truthiness; runtime errors fall through as <c>false</c>.
    /// </summary>
    public static bool EvaluateBoolean(ExprNode ast, IReadOnlyDictionary<string, JsonElement> answers)
    {
        try
        {
            return Interpreter.ToBool(Interpreter.Evaluate(ast, answers));
        }
        catch
        {
            return false;
        }
    }
}
