using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Enums;
using ShiftSoftware.ADP.Surveys.Shared.Evaluation;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class LogicEvaluatorTests
{
    [Fact]
    public void Predicate_EqualsOperator_MatchesAnswerValue()
    {
        var predicate = new PredicateConditionDto
        {
            QuestionId = "brand",
            Op = LogicOperator.Equals,
            Value = Json("\"toyota\""),
        };
        Assert.True(LogicEvaluator.EvaluateCondition(predicate, AnswerMap(("brand", "\"toyota\""))));
        Assert.False(LogicEvaluator.EvaluateCondition(predicate, AnswerMap(("brand", "\"honda\""))));
    }

    [Fact]
    public void Predicate_GreaterThanOrEqual_OnNumber()
    {
        var predicate = new PredicateConditionDto
        {
            QuestionId = "nps",
            Op = LogicOperator.GreaterThanOrEqual,
            Value = Json("9"),
        };
        Assert.True(LogicEvaluator.EvaluateCondition(predicate, AnswerMap(("nps", "10"))));
        Assert.True(LogicEvaluator.EvaluateCondition(predicate, AnswerMap(("nps", "9"))));
        Assert.False(LogicEvaluator.EvaluateCondition(predicate, AnswerMap(("nps", "8"))));
    }

    [Fact]
    public void Predicate_IsSet_And_IsNotSet()
    {
        var isSet = new PredicateConditionDto { QuestionId = "x", Op = LogicOperator.IsSet };
        var isNotSet = new PredicateConditionDto { QuestionId = "x", Op = LogicOperator.IsNotSet };

        Assert.True(LogicEvaluator.EvaluateCondition(isSet, AnswerMap(("x", "1"))));
        Assert.False(LogicEvaluator.EvaluateCondition(isSet, new Dictionary<string, JsonElement>()));

        Assert.False(LogicEvaluator.EvaluateCondition(isNotSet, AnswerMap(("x", "1"))));
        Assert.True(LogicEvaluator.EvaluateCondition(isNotSet, new Dictionary<string, JsonElement>()));
    }

    [Fact]
    public void Predicate_In_ChecksAgainstArrayValue()
    {
        var predicate = new PredicateConditionDto
        {
            QuestionId = "brand",
            Op = LogicOperator.In,
            Value = Json("[\"toyota\", \"honda\"]"),
        };
        Assert.True(LogicEvaluator.EvaluateCondition(predicate, AnswerMap(("brand", "\"toyota\""))));
        Assert.False(LogicEvaluator.EvaluateCondition(predicate, AnswerMap(("brand", "\"bmw\""))));
    }

    [Fact]
    public void Composite_All_RequiresAllChildrenTrue()
    {
        var composite = new CompositeConditionDto
        {
            All = new()
            {
                new PredicateConditionDto { QuestionId = "nps", Op = LogicOperator.GreaterThanOrEqual, Value = Json("9") },
                new PredicateConditionDto { QuestionId = "brand", Op = LogicOperator.Equals, Value = Json("\"toyota\"") },
            }
        };
        Assert.True(LogicEvaluator.EvaluateCondition(composite, AnswerMap(("nps", "10"), ("brand", "\"toyota\""))));
        Assert.False(LogicEvaluator.EvaluateCondition(composite, AnswerMap(("nps", "5"), ("brand", "\"toyota\""))));
    }

    [Fact]
    public void Composite_Any_RequiresOneChildTrue()
    {
        var composite = new CompositeConditionDto
        {
            Any = new()
            {
                new PredicateConditionDto { QuestionId = "nps", Op = LogicOperator.LessThanOrEqual, Value = Json("6") },
                new PredicateConditionDto { QuestionId = "brand", Op = LogicOperator.Equals, Value = Json("\"toyota\"") },
            }
        };
        Assert.True(LogicEvaluator.EvaluateCondition(composite, AnswerMap(("nps", "3"), ("brand", "\"honda\""))));
        Assert.False(LogicEvaluator.EvaluateCondition(composite, AnswerMap(("nps", "8"), ("brand", "\"honda\""))));
    }

    [Fact]
    public void ExpressionCondition_UsesSandbox()
    {
        var condition = new ExpressionConditionDto
        {
            Expression = "answers['nps'] >= 9 && answers['has-car'] == 'yes'"
        };
        Assert.True(LogicEvaluator.EvaluateCondition(condition, AnswerMap(("nps", "10"), ("has-car", "\"yes\""))));
        Assert.False(LogicEvaluator.EvaluateCondition(condition, AnswerMap(("nps", "10"), ("has-car", "\"no\""))));
    }

    [Fact]
    public void EvaluateNext_FirstMatchingRuleWins()
    {
        var survey = BuildSurveyWithLogic(
            // rule 0 requires nps >= 9 AND brand == toyota → screen-toyota-promoter
            new LogicRuleDto
            {
                If = new CompositeConditionDto
                {
                    All = new()
                    {
                        new PredicateConditionDto { QuestionId = "nps", Op = LogicOperator.GreaterThanOrEqual, Value = Json("9") },
                        new PredicateConditionDto { QuestionId = "brand", Op = LogicOperator.Equals, Value = Json("\"toyota\"") },
                    }
                },
                Then = new LogicActionDto { Goto = "screen-toyota-promoter" }
            },
            // rule 1 falls back to "any promoter" → screen-promoter
            new LogicRuleDto
            {
                If = new PredicateConditionDto { QuestionId = "nps", Op = LogicOperator.GreaterThanOrEqual, Value = Json("9") },
                Then = new LogicActionDto { Goto = "screen-promoter" }
            });

        // A toyota promoter should hit the first rule.
        Assert.Equal("screen-toyota-promoter",
            LogicEvaluator.EvaluateNext(survey, AnswerMap(("nps", "10"), ("brand", "\"toyota\""))));

        // A honda promoter should hit the second.
        Assert.Equal("screen-promoter",
            LogicEvaluator.EvaluateNext(survey, AnswerMap(("nps", "10"), ("brand", "\"honda\""))));

        // A detractor should hit nothing → null → caller falls back to screen.NextScreen.
        Assert.Null(LogicEvaluator.EvaluateNext(survey, AnswerMap(("nps", "3"), ("brand", "\"toyota\""))));
    }

    [Fact]
    public void EvaluateNext_CrossScreenAnswer_CanDriveLateRule()
    {
        // Verifies "rules can reference any prior answer, not only the current screen."
        var survey = BuildSurveyWithLogic(new LogicRuleDto
        {
            If = new PredicateConditionDto { QuestionId = "screen-1-answer", Op = LogicOperator.Equals, Value = Json("\"magic\"") },
            Then = new LogicActionDto { Goto = "screen-99" }
        });

        Assert.Equal("screen-99",
            LogicEvaluator.EvaluateNext(survey, AnswerMap(("screen-1-answer", "\"magic\""))));
    }

    [Fact]
    public void BrokenExpressionInRule_FallsThroughAsFalse()
    {
        // A broken rule must not block navigation — EvaluateNext should return null, not throw.
        var survey = BuildSurveyWithLogic(new LogicRuleDto
        {
            If = new ExpressionConditionDto { Expression = "this is not valid syntax" },
            Then = new LogicActionDto { Goto = "never" }
        });
        Assert.Null(LogicEvaluator.EvaluateNext(survey, new Dictionary<string, JsonElement>()));
    }

    private static SurveyDto BuildSurveyWithLogic(params LogicRuleDto[] rules) => new()
    {
        SurveyId = "s",
        Title = LocalizedString.From("en", "S"),
        Locales = new() { "en" },
        DefaultLocale = "en",
        Screens =
        {
            new InlineScreenDto
            {
                Id = "s1",
                Questions = { QuestionEntryDto.FromInline(new TextQuestionDto { Id = "q", Title = LocalizedString.From("en", "Q") }) }
            }
        },
        Logic = new(rules),
    };

    private static JsonElement Json(string src) => JsonDocument.Parse(src).RootElement.Clone();

    private static Dictionary<string, JsonElement> AnswerMap(params (string Key, string JsonValue)[] pairs)
    {
        var d = new Dictionary<string, JsonElement>();
        foreach (var (k, v) in pairs)
            d[k] = Json(v);
        return d;
    }
}
