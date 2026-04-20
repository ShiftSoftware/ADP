namespace ShiftSoftware.ADP.Surveys.Shared.Evaluation.ExpressionSandbox;

/// <summary>
/// AST node types for the survey expression mini-language. Kept deliberately small
/// so it ports cleanly to the TypeScript SDK later. Grammar (precedence low → high):
/// <code>
/// expr       → or
/// or         → and ('||' and)*
/// and        → equality ('&amp;&amp;' equality)*
/// equality   → comparison (('==' | '===' | '!=' | '!==') comparison)*
/// comparison → unary (('&lt;' | '>' | '&lt;=' | '>=') unary)*
/// unary      → '!' unary | primary
/// primary    → literal | answers-access | call | array | '(' expr ')'
/// </code>
/// </summary>
public abstract record ExprNode;

/// <summary>Literal value: number (decimal), string, bool, or null.</summary>
public sealed record LiteralNode(object? Value) : ExprNode;

/// <summary><c>answers['key']</c> or <c>answers.key</c>.</summary>
public sealed record AnswersAccessNode(string Key) : ExprNode;

/// <summary>Binary operator: one of <c>== === != !== &lt; > &lt;= >= &amp;&amp; ||</c>.</summary>
public sealed record BinaryOpNode(string Op, ExprNode Left, ExprNode Right) : ExprNode;

/// <summary>Unary <c>!</c>.</summary>
public sealed record UnaryOpNode(string Op, ExprNode Operand) : ExprNode;

/// <summary>Call to a built-in function: <c>has</c>, <c>isSet</c>, <c>isNotSet</c>, <c>in</c>.</summary>
public sealed record CallNode(string Name, IReadOnlyList<ExprNode> Args) : ExprNode;

/// <summary>Array literal: <c>[1, 2, 3]</c> — used as the second argument to <c>in(...)</c>.</summary>
public sealed record ArrayNode(IReadOnlyList<ExprNode> Items) : ExprNode;
