using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Enums;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class SchemaRoundTripTests
{
    private static readonly JsonSerializerOptions Json = SurveySchemaSerializer.Options;

    [Fact]
    public void ResolvedSurvey_WithInlineAndNavigationList_RoundTripsExactly()
    {
        var survey = new SurveyDto
        {
            SurveyId = "demo-nps",
            Version = 1,
            Title = LocalizedString.From("en", "NPS Survey"),
            Locales = new() { "en", "ar" },
            DefaultLocale = "en",
            Screens =
            {
                new InlineScreenDto
                {
                    Id = "screen-has-car",
                    Title = LocalizedString.From("en", "Do you own a car?"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NavigationListQuestionDto
                        {
                            Id = "has-car",
                            Title = LocalizedString.From("en", "Do you own a car?"),
                            Required = true,
                            Options =
                            {
                                new NavigationListOptionDto { Id = "yes", Label = LocalizedString.From("en", "Yes"), NextScreen = "screen-brand" },
                                new NavigationListOptionDto { Id = "no", Label = LocalizedString.From("en", "No"), NextScreen = "screen-thanks" },
                            }
                        })
                    }
                },
                new InlineScreenDto
                {
                    Id = "screen-brand",
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new SingleChoiceQuestionDto
                        {
                            Id = "brand",
                            Title = LocalizedString.From("en", "Which brand?"),
                            Options =
                            {
                                new OptionDto { Id = "toyota", Label = LocalizedString.From("en", "Toyota") },
                                new OptionDto { Id = "other", Label = LocalizedString.From("en", "Other") },
                            }
                        })
                    },
                    NextScreen = "screen-nps"
                },
                new InlineScreenDto
                {
                    Id = "screen-nps",
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NpsQuestionDto
                        {
                            Id = "nps",
                            Title = LocalizedString.From("en", "How likely are you to recommend?"),
                            BiColumn = "nps_score",
                        })
                    }
                }
            },
            Logic =
            {
                new LogicRuleDto
                {
                    If = new CompositeConditionDto
                    {
                        All = new()
                        {
                            new PredicateConditionDto { QuestionId = "nps", Op = LogicOperator.GreaterThanOrEqual, Value = JsonDocument.Parse("9").RootElement },
                            new PredicateConditionDto { QuestionId = "brand", Op = LogicOperator.Equals, Value = JsonDocument.Parse("\"toyota\"").RootElement },
                        }
                    },
                    Then = new LogicActionDto { Goto = "screen-toyota-promoter" }
                },
                new LogicRuleDto
                {
                    If = new ExpressionConditionDto { Expression = "answers['nps'] <= 6" },
                    Then = new LogicActionDto { Goto = "screen-detractor" }
                },
            }
        };

        var json = JsonSerializer.Serialize(survey, Json);
        var roundTripped = JsonSerializer.Deserialize<SurveyDto>(json, Json)!;
        var jsonAgain = JsonSerializer.Serialize(roundTripped, Json);

        Assert.Equal(json, jsonAgain);
    }

    [Fact]
    public void DraftSurvey_WithTemplateRefAndBankRef_DeserializesIntoRefSubtypes()
    {
        const string draftJson = """
        {
          "surveyId": "demo-draft",
          "version": 0,
          "title": { "en": "Draft" },
          "locales": ["en"],
          "defaultLocale": "en",
          "screens": [
            { "templateRef": "customer-info", "overrides": { "title": { "en": "Tell us about you" } } },
            {
              "id": "screen-extra",
              "questions": [
                { "bankRef": "phone", "overrides": { "required": true } },
                { "id": "comment", "type": "paragraph", "title": { "en": "Comments" } }
              ]
            }
          ],
          "logic": []
        }
        """;

        var survey = JsonSerializer.Deserialize<SurveyDto>(draftJson, Json)!;

        Assert.Collection(survey.Screens,
            first => Assert.IsType<ScreenTemplateRefDto>(first),
            second =>
            {
                var inline = Assert.IsType<InlineScreenDto>(second);
                Assert.Collection(inline.Questions,
                    firstQ =>
                    {
                        Assert.True(firstQ.IsRef);
                        Assert.Equal("phone", firstQ.Ref!.BankRef);
                        Assert.True(firstQ.Ref.Overrides!.Required);
                    },
                    secondQ =>
                    {
                        Assert.False(secondQ.IsRef);
                        Assert.IsType<ParagraphQuestionDto>(secondQ.Inline);
                    });
            });
    }

    [Fact]
    public void LogicOperator_SerializesToSymbolFromSchema()
    {
        var predicate = new PredicateConditionDto { QuestionId = "x", Op = LogicOperator.GreaterThanOrEqual, Value = JsonDocument.Parse("5").RootElement };
        var json = JsonSerializer.Serialize(predicate, Json);

        Assert.Contains("\"op\":\">=\"", json);
    }

    [Fact]
    public void QuestionType_SerializesToDiscriminatorFromCatalog()
    {
        var q = new NavigationListQuestionDto
        {
            Id = "x",
            Title = LocalizedString.From("en", "x"),
        };
        var json = JsonSerializer.Serialize<QuestionDto>(q, Json);

        Assert.Contains("\"type\":\"navigationList\"", json);
    }
}
