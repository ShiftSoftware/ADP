using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.Evaluation.ExpressionSandbox;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class ExpressionSandboxTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("!true", false)]
    [InlineData("!!true", true)]
    [InlineData("1 == 1", true)]
    [InlineData("1 != 1", false)]
    [InlineData("2 > 1", true)]
    [InlineData("2 >= 2", true)]
    [InlineData("1 < 2 && 2 < 3", true)]
    [InlineData("1 < 2 && 3 < 2", false)]
    [InlineData("1 > 2 || 1 < 2", true)]
    [InlineData("'a' == 'a'", true)]
    [InlineData("'a' == \"a\"", true)]
    [InlineData("'a' != 'b'", true)]
    [InlineData("(1 == 1) && true", true)]
    public void LiteralsAndOperators_EvaluateCorrectly(string source, bool expected)
    {
        var result = ExpressionSandbox.EvaluateBoolean(source, new Dictionary<string, JsonElement>());
        Assert.Equal(expected, result);
    }

    [Fact]
    public void AnswersAccess_DotNotation()
    {
        var answers = AnswerMap(("nps", "9"));
        Assert.True(ExpressionSandbox.EvaluateBoolean("answers.nps >= 9", answers));
        Assert.False(ExpressionSandbox.EvaluateBoolean("answers.nps < 9", answers));
    }

    [Fact]
    public void AnswersAccess_BracketNotation_WorksForKeysWithDashes()
    {
        var answers = AnswerMap(("has-car", "\"yes\""));
        Assert.True(ExpressionSandbox.EvaluateBoolean("answers['has-car'] == 'yes'", answers));
    }

    [Fact]
    public void MissingAnswer_IsNull_NotTruthy()
    {
        var answers = new Dictionary<string, JsonElement>();
        Assert.False(ExpressionSandbox.EvaluateBoolean("answers.missing == 'x'", answers));
        Assert.True(ExpressionSandbox.EvaluateBoolean("answers.missing == null", answers));
    }

    [Fact]
    public void BuiltInHas_And_IsSet()
    {
        var answers = AnswerMap(("nps", "9"));
        Assert.True(ExpressionSandbox.EvaluateBoolean("has('nps')", answers));
        Assert.True(ExpressionSandbox.EvaluateBoolean("isSet('nps')", answers));
        Assert.False(ExpressionSandbox.EvaluateBoolean("has('gone')", answers));
        Assert.True(ExpressionSandbox.EvaluateBoolean("isNotSet('gone')", answers));
    }

    [Fact]
    public void BuiltInIn_AcceptsArrayLiteral()
    {
        var answers = AnswerMap(("brand", "\"toyota\""));
        Assert.True(ExpressionSandbox.EvaluateBoolean("in(answers.brand, ['toyota', 'honda'])", answers));
        Assert.False(ExpressionSandbox.EvaluateBoolean("in(answers.brand, ['bmw', 'audi'])", answers));
    }

    [Fact]
    public void ShortCircuit_OrStopsAtFirstTruthy()
    {
        // Second operand compares string to number — would throw if evaluated. OR short-circuits first.
        Assert.True(ExpressionSandbox.EvaluateBoolean("true || 'a' > 5", new Dictionary<string, JsonElement>()));
    }

    [Fact]
    public void ShortCircuit_AndStopsAtFirstFalsy()
    {
        Assert.False(ExpressionSandbox.EvaluateBoolean("false && 'a' > 5", new Dictionary<string, JsonElement>()));
    }

    [Fact]
    public void RuntimeError_FallsThroughAsFalse()
    {
        // Comparing string to number throws; per Decision #10 we treat that as the rule not matching.
        var result = ExpressionSandbox.EvaluateBoolean("'abc' > 5", new Dictionary<string, JsonElement>());
        Assert.False(result);
    }

    [Fact]
    public void ParseError_Throws()
    {
        Assert.Throws<ExpressionSyntaxException>(() => ExpressionSandbox.Parse("answers.nps >="));
        Assert.Throws<ExpressionSyntaxException>(() => ExpressionSandbox.Parse("answers = 5"));
        Assert.Throws<ExpressionSyntaxException>(() => ExpressionSandbox.Parse("'unterminated"));
    }

    [Fact]
    public void PreParsedAst_CanBeEvaluatedMultipleTimes()
    {
        var ast = ExpressionSandbox.Parse("answers.nps >= 9");
        Assert.True(ExpressionSandbox.EvaluateBoolean(ast, AnswerMap(("nps", "9"))));
        Assert.False(ExpressionSandbox.EvaluateBoolean(ast, AnswerMap(("nps", "5"))));
    }

    [Fact]
    public void SchemaExample_FromSharedSchemaDoc_Works()
    {
        // Taken verbatim from shared-schema.md "expression escape hatch" section.
        var src = "answers['nps'] >= 9 && answers['has-car'] == 'yes'";
        var answers = AnswerMap(("nps", "10"), ("has-car", "\"yes\""));
        Assert.True(ExpressionSandbox.EvaluateBoolean(src, answers));
        Assert.False(ExpressionSandbox.EvaluateBoolean(src, AnswerMap(("nps", "10"), ("has-car", "\"no\""))));
        Assert.False(ExpressionSandbox.EvaluateBoolean(src, AnswerMap(("nps", "8"), ("has-car", "\"yes\""))));
    }

    private static Dictionary<string, JsonElement> AnswerMap(params (string Key, string JsonValue)[] pairs)
    {
        var d = new Dictionary<string, JsonElement>();
        foreach (var (k, v) in pairs)
            d[k] = JsonDocument.Parse(v).RootElement.Clone();
        return d;
    }
}
