using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Personalization;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class PersonalizationTokenTests
{
    private static Dictionary<string, string> SampleContext() => PersonalizationTokens.BuildContext(
        customerRef: "CUST-42",
        recipientAddress: "+9647701112233",
        recipientLocale: "ar",
        candidateMetadataJson: """
            {
                "customerName": "Aza",
                "vehicleModel": "Land Cruiser",
                "dealerId": 7,
                "vip": true,
                "nested": { "ignored": "yes" },
                "list": [1, 2],
                "nothing": null
            }
            """);

    [Fact]
    public void BuildContext_MapsRecipientAndTopLevelCandidateFields()
    {
        var ctx = SampleContext();

        Assert.Equal("CUST-42", ctx["recipient.customerRef"]);
        Assert.Equal("+9647701112233", ctx["recipient.address"]);
        Assert.Equal("ar", ctx["recipient.locale"]);
        Assert.Equal("Aza", ctx["candidate.customerName"]);
        Assert.Equal("Land Cruiser", ctx["candidate.vehicleModel"]);
        Assert.Equal("7", ctx["candidate.dealerId"]);
        Assert.Equal("true", ctx["candidate.vip"]);
        Assert.False(ctx.ContainsKey("candidate.nested"));
        Assert.False(ctx.ContainsKey("candidate.list"));
        Assert.False(ctx.ContainsKey("candidate.nothing"));
    }

    [Fact]
    public void BuildContext_MalformedCandidateJson_YieldsRecipientOnly()
    {
        var ctx = PersonalizationTokens.BuildContext("c", "a", "en", "{not json");
        Assert.Equal(3, ctx.Count);
        Assert.All(ctx.Keys, k => Assert.StartsWith("recipient.", k));
    }

    [Fact]
    public void SubstituteString_ReplacesKnownTokens_LeavesUnknownVerbatim()
    {
        var ctx = SampleContext();

        Assert.Equal(
            "Hello Aza, how is your Land Cruiser?",
            PersonalizationTokens.SubstituteString("Hello {{candidate.customerName}}, how is your {{candidate.vehicleModel}}?", ctx));

        Assert.Equal(
            "Hello {{candidate.missing}}!",
            PersonalizationTokens.SubstituteString("Hello {{candidate.missing}}!", ctx));
    }

    [Fact]
    public void SubstituteString_ToleratesWhitespaceInsideBraces()
    {
        var ctx = SampleContext();
        Assert.Equal("Aza", PersonalizationTokens.SubstituteString("{{ candidate.customerName }}", ctx));
    }

    [Fact]
    public void Substitute_WalksTitlesHelpAndOptionLabels()
    {
        var survey = new SurveyDto
        {
            Title = new LocalizedString { ["en"] = "Survey for {{candidate.customerName}}" },
            Locales = new List<string> { "en", "ar" },
            DefaultLocale = "en",
            Screens = new List<ScreenDto>
            {
                new InlineScreenDto
                {
                    Id = "s1",
                    Title = new LocalizedString
                    {
                        ["en"] = "Hi {{candidate.customerName}}",
                        ["ar"] = "مرحبا {{candidate.customerName}}",
                    },
                    Questions = new List<QuestionEntryDto>
                    {
                        QuestionEntryDto.FromInline(new SingleChoiceQuestionDto
                        {
                            Id = "q1",
                            Title = new LocalizedString { ["en"] = "About your {{candidate.vehicleModel}}?" },
                            Help = new LocalizedString { ["en"] = "Ref {{recipient.customerRef}}" },
                            Options = new List<OptionDto>
                            {
                                new() { Id = "a", Label = new LocalizedString { ["en"] = "My {{candidate.vehicleModel}} is great" } },
                            },
                        }),
                        QuestionEntryDto.FromInline(new NavigationListQuestionDto
                        {
                            Id = "q2",
                            Title = new LocalizedString { ["en"] = "Pick" },
                            Options = new List<NavigationListOptionDto>
                            {
                                new() { Id = "n1", Label = new LocalizedString { ["en"] = "Sell my {{candidate.vehicleModel}}" } },
                            },
                        }),
                    },
                },
            },
        };

        PersonalizationTokens.Substitute(survey, SampleContext());

        Assert.Equal("Survey for Aza", survey.Title["en"]);
        var screen = Assert.IsType<InlineScreenDto>(survey.Screens[0]);
        Assert.Equal("Hi Aza", screen.Title!["en"]);
        Assert.Equal("مرحبا Aza", screen.Title!["ar"]);
        var q1 = Assert.IsType<SingleChoiceQuestionDto>(screen.Questions[0].Inline);
        Assert.Equal("About your Land Cruiser?", q1.Title["en"]);
        Assert.Equal("Ref CUST-42", q1.Help!["en"]);
        Assert.Equal("My Land Cruiser is great", q1.Options[0].Label["en"]);
        var q2 = Assert.IsType<NavigationListQuestionDto>(screen.Questions[1].Inline);
        Assert.Equal("Sell my Land Cruiser", q2.Options[0].Label["en"]);
    }

    [Fact]
    public void Substitute_DoesNotTouchIdsOrExpressions()
    {
        var survey = new SurveyDto
        {
            Title = new LocalizedString { ["en"] = "t" },
            Screens = new List<ScreenDto>
            {
                new InlineScreenDto
                {
                    Id = "{{candidate.customerName}}",
                    Questions = new List<QuestionEntryDto>(),
                },
            },
            Logic = new List<LogicRuleDto>
            {
                new()
                {
                    If = new ExpressionConditionDto { Expression = "answers['{{candidate.customerName}}'] == 1" },
                    Then = new LogicActionDto { Goto = "x" },
                },
            },
        };

        PersonalizationTokens.Substitute(survey, SampleContext());

        var screen = Assert.IsType<InlineScreenDto>(survey.Screens[0]);
        Assert.Equal("{{candidate.customerName}}", screen.Id);
        var expr = Assert.IsType<ExpressionConditionDto>(survey.Logic[0].If);
        Assert.Equal("answers['{{candidate.customerName}}'] == 1", expr.Expression);
    }

    [Fact]
    public void Substitute_EmptyContext_IsNoOp()
    {
        var survey = new SurveyDto
        {
            Title = new LocalizedString { ["en"] = "Hello {{candidate.customerName}}" },
        };

        PersonalizationTokens.Substitute(survey, new Dictionary<string, string>());

        Assert.Equal("Hello {{candidate.customerName}}", survey.Title["en"]);
    }

    [Fact]
    public void MightContainTokens_FastPathDetection()
    {
        Assert.True(PersonalizationTokens.MightContainTokens("a {{x}} b"));
        Assert.False(PersonalizationTokens.MightContainTokens("plain text { single }"));
        Assert.False(PersonalizationTokens.MightContainTokens(null));
    }
}
