using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.Answers;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class AnswerValidatorTests
{
    [Fact]
    public void Validate_MissingRequiredAnswer_ReportsError()
    {
        var survey = SurveyWith(new TextQuestionDto { Id = "name", Title = LocalizedString.From("en", "Name"), Required = true });
        var errors = AnswerValidator.Validate(survey, new Dictionary<string, JsonElement>());
        Assert.Contains(errors, e => e.QuestionId == "name" && e.Message.Contains("required"));
    }

    [Fact]
    public void Validate_TextTooShort_ReportsError()
    {
        var survey = SurveyWith(new TextQuestionDto { Id = "name", Title = LocalizedString.From("en", "Name"), MinLength = 3 });
        var errors = AnswerValidator.Validate(survey, AnswerMap(("name", "\"ab\"")));
        Assert.Contains(errors, e => e.Message.Contains("minLength"));
    }

    [Fact]
    public void Validate_TextInsideBounds_Passes()
    {
        var survey = SurveyWith(new TextQuestionDto { Id = "name", Title = LocalizedString.From("en", "Name"), MinLength = 1, MaxLength = 10 });
        var errors = AnswerValidator.Validate(survey, AnswerMap(("name", "\"Aza\"")));
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NpsOutOfRange_ReportsError()
    {
        var survey = SurveyWith(new NpsQuestionDto { Id = "nps", Title = LocalizedString.From("en", "NPS") });
        var errors = AnswerValidator.Validate(survey, AnswerMap(("nps", "11")));
        Assert.Contains(errors, e => e.QuestionId == "nps" && e.Message.Contains("outside"));
    }

    [Fact]
    public void Validate_SingleChoiceWithUnknownOption_ReportsError()
    {
        var q = new SingleChoiceQuestionDto
        {
            Id = "brand",
            Title = LocalizedString.From("en", "Brand"),
            Options = { new() { Id = "toyota", Label = LocalizedString.From("en", "Toyota") } }
        };
        var errors = AnswerValidator.Validate(SurveyWith(q), AnswerMap(("brand", "\"honda\"")));
        Assert.Contains(errors, e => e.Message.Contains("'honda'"));
    }

    [Fact]
    public void Validate_MultiChoiceMaxSelectedExceeded_ReportsError()
    {
        var q = new MultiChoiceQuestionDto
        {
            Id = "features",
            Title = LocalizedString.From("en", "Features"),
            MaxSelected = 1,
            Options =
            {
                new() { Id = "a", Label = LocalizedString.From("en", "A") },
                new() { Id = "b", Label = LocalizedString.From("en", "B") },
            }
        };
        var errors = AnswerValidator.Validate(SurveyWith(q), AnswerMap(("features", "[\"a\",\"b\"]")));
        Assert.Contains(errors, e => e.Message.Contains("At most"));
    }

    [Fact]
    public void Validate_NavigationListAnswerNotInOptions_ReportsError()
    {
        var q = new NavigationListQuestionDto
        {
            Id = "has-car",
            Title = LocalizedString.From("en", "?"),
            Options = { new NavigationListOptionDto { Id = "yes", Label = LocalizedString.From("en", "Y"), NextScreen = "s2" } }
        };
        var errors = AnswerValidator.Validate(SurveyWith(q), AnswerMap(("has-car", "\"maybe\"")));
        Assert.Contains(errors, e => e.Message.Contains("'maybe'"));
    }

    [Fact]
    public void Validate_YesNoWithString_ReportsError()
    {
        var q = new YesNoQuestionDto { Id = "agree", Title = LocalizedString.From("en", "Agree?") };
        var errors = AnswerValidator.Validate(SurveyWith(q), AnswerMap(("agree", "\"yes\"")));
        Assert.Contains(errors, e => e.Message.Contains("boolean"));
    }

    [Fact]
    public void Validate_DateOutOfBounds_ReportsError()
    {
        var q = new DateQuestionDto
        {
            Id = "dob",
            Title = LocalizedString.From("en", "DOB"),
            MinDate = "2000-01-01",
            MaxDate = "2020-01-01",
        };
        var errors = AnswerValidator.Validate(SurveyWith(q), AnswerMap(("dob", "\"1999-06-15\"")));
        Assert.Contains(errors, e => e.Message.Contains("before minDate"));
    }

    [Fact]
    public void Validate_AllGood_NoErrors()
    {
        var survey = SurveyWith(
            new TextQuestionDto { Id = "name", Title = LocalizedString.From("en", "Name") },
            new NpsQuestionDto { Id = "nps", Title = LocalizedString.From("en", "NPS") },
            new YesNoQuestionDto { Id = "ok", Title = LocalizedString.From("en", "OK?") });
        var errors = AnswerValidator.Validate(survey, AnswerMap(
            ("name", "\"Aza\""),
            ("nps", "9"),
            ("ok", "true")));
        Assert.Empty(errors);
    }

    // ── Path-aware required enforcement ────────────────────────────────────
    // A branching survey routes the respondent past whole screens; required
    // questions on unvisited branches must not block the submission. Visited
    // screens are replayed from the answers via the computeNext mirror.

    [Fact]
    public void Validate_RequiredOnUnvisitedNavigationBranch_Passes()
    {
        var survey = BranchingSurvey();
        var errors = AnswerValidator.Validate(survey, AnswerMap(
            ("pick", "\"a\""),
            ("a-input", "\"went with branch a\"")));
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_RequiredOnVisitedNavigationBranch_StillEnforced()
    {
        var survey = BranchingSurvey();
        var errors = AnswerValidator.Validate(survey, AnswerMap(("pick", "\"a\"")));
        Assert.Contains(errors, e => e.QuestionId == "a-input" && e.Message.Contains("required"));
        Assert.DoesNotContain(errors, e => e.QuestionId == "b-input");
    }

    [Fact]
    public void Validate_AnswerOnUnvisitedScreen_ShapeStillChecked()
    {
        // Menu loops legitimately leave answers on screens the replay doesn't
        // visit — those values must still be well-formed.
        var survey = BranchingSurvey();
        var errors = AnswerValidator.Validate(survey, AnswerMap(
            ("pick", "\"a\""),
            ("a-input", "\"fine\""),
            ("b-input", "42")));
        Assert.Contains(errors, e => e.QuestionId == "b-input" && e.Message.Contains("string"));
    }

    [Fact]
    public void Validate_RequiredOnLogicSkippedScreen_Passes()
    {
        var survey = LogicSurvey();
        // Promoter: rule doesn't match, s1.nextScreen routes to end — detractor unvisited.
        var errors = AnswerValidator.Validate(survey, AnswerMap(("nps", "9")));
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_RequiredOnLogicRoutedScreen_StillEnforced()
    {
        var survey = LogicSurvey();
        // Detractor: rule routes to the details screen, whose paragraph is required.
        var errors = AnswerValidator.Validate(survey, AnswerMap(("nps", "3")));
        Assert.Contains(errors, e => e.QuestionId == "details" && e.Message.Contains("required"));
    }

    [Fact]
    public void Validate_SequentialFallback_EnforcesFollowingScreens()
    {
        var survey = new SurveyDto
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
                    Questions = { QuestionEntryDto.FromInline(new TextQuestionDto { Id = "first", Title = LocalizedString.From("en", "First"), Required = true }) }
                },
                new InlineScreenDto
                {
                    Id = "s2",
                    Questions = { QuestionEntryDto.FromInline(new TextQuestionDto { Id = "second", Title = LocalizedString.From("en", "Second"), Required = true }) }
                },
            }
        };
        var errors = AnswerValidator.Validate(survey, new Dictionary<string, JsonElement>());
        Assert.Contains(errors, e => e.QuestionId == "first");
        Assert.Contains(errors, e => e.QuestionId == "second");
    }

    [Fact]
    public void Validate_StickyRule_EndScreenTerminatesReplay()
    {
        // Mirrors the SDK's "sticky logic rule cannot drag navigation out of a
        // zero-question end screen" — a global rule matches the final answer map
        // forever, so the replay's terminal tier must run BEFORE the logic tier
        // or the walk would leave the end screen. A complete low-score submission
        // (rule fired, detour answered) validates clean.
        var answers = new Dictionary<string, JsonElement>
        {
            ["nps"] = JsonDocument.Parse("0").RootElement.Clone(),
            ["details"] = JsonDocument.Parse("\"it was slow\"").RootElement.Clone(),
        };
        var errors = AnswerValidator.Validate(LogicSurvey(), answers);
        Assert.Empty(errors);
    }

    /// <summary>pick (navList: a → branch-a, b → branch-b) → required paragraph per branch → end.</summary>
    private static SurveyDto BranchingSurvey() => new()
    {
        SurveyId = "s",
        Title = LocalizedString.From("en", "S"),
        Locales = new() { "en" },
        DefaultLocale = "en",
        Screens =
        {
            new InlineScreenDto
            {
                Id = "pick",
                Questions =
                {
                    QuestionEntryDto.FromInline(new NavigationListQuestionDto
                    {
                        Id = "pick",
                        Title = LocalizedString.From("en", "Pick"),
                        Required = true,
                        Options =
                        {
                            new NavigationListOptionDto { Id = "a", Label = LocalizedString.From("en", "A"), NextScreen = "branch-a" },
                            new NavigationListOptionDto { Id = "b", Label = LocalizedString.From("en", "B"), NextScreen = "branch-b" },
                        }
                    })
                }
            },
            new InlineScreenDto
            {
                Id = "branch-a",
                Questions = { QuestionEntryDto.FromInline(new ParagraphQuestionDto { Id = "a-input", Title = LocalizedString.From("en", "A?"), Required = true }) },
                NextScreen = "end",
            },
            new InlineScreenDto
            {
                Id = "branch-b",
                Questions = { QuestionEntryDto.FromInline(new ParagraphQuestionDto { Id = "b-input", Title = LocalizedString.From("en", "B?"), Required = true }) },
                NextScreen = "end",
            },
            new InlineScreenDto { Id = "end", Title = LocalizedString.From("en", "Done") },
        }
    };

    /// <summary>nps screen (nextScreen=end) with a nps&lt;=6 → details rule; details carries a required paragraph.</summary>
    private static SurveyDto LogicSurvey() => new()
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
                Questions = { QuestionEntryDto.FromInline(new NpsQuestionDto { Id = "nps", Title = LocalizedString.From("en", "NPS"), Required = true }) },
                NextScreen = "end",
            },
            new InlineScreenDto
            {
                Id = "details",
                Questions = { QuestionEntryDto.FromInline(new ParagraphQuestionDto { Id = "details", Title = LocalizedString.From("en", "What went wrong?"), Required = true }) },
                NextScreen = "end",
            },
            new InlineScreenDto { Id = "end", Title = LocalizedString.From("en", "Done") },
        },
        Logic =
        {
            new DTOs.Logic.LogicRuleDto
            {
                If = new DTOs.Logic.PredicateConditionDto
                {
                    QuestionId = "nps",
                    Op = Enums.LogicOperator.LessThanOrEqual,
                    Value = JsonDocument.Parse("6").RootElement.Clone(),
                },
                Then = new DTOs.Logic.LogicActionDto { Goto = "details" }
            }
        }
    };

    [Fact]
    public void Validate_SourcedChoice_AcceptsAnyStringId_ButKeepsShapeCheck()
    {
        // Options are fetched client-side at render time; the server never sees
        // them, so membership is not checkable here — shape still is.
        var q = new DropdownQuestionDto
        {
            Id = "branch",
            Title = LocalizedString.From("en", "Branch"),
            OptionsSource = new OptionsSourceDto { Url = "https://example.test/api/public/company-branch" },
        };
        Assert.Empty(AnswerValidator.Validate(SurveyWith(q), AnswerMap(("branch", "\"MJLGr\""))));
        var errors = AnswerValidator.Validate(SurveyWith(q), AnswerMap(("branch", "42")));
        Assert.Contains(errors, e => e.QuestionId == "branch" && e.Message.Contains("JSON string"));
    }

    [Fact]
    public void Validate_SourcedMultiChoice_SkipsMembership_KeepsSelectionBounds()
    {
        var q = new MultiChoiceQuestionDto
        {
            Id = "cities",
            Title = LocalizedString.From("en", "Cities"),
            MaxSelected = 1,
            OptionsSource = new OptionsSourceDto { Url = "https://example.test/api/public/city" },
        };
        Assert.Empty(AnswerValidator.Validate(SurveyWith(q), AnswerMap(("cities", "[\"L0VEX\"]"))));
        var errors = AnswerValidator.Validate(SurveyWith(q), AnswerMap(("cities", "[\"L0VEX\",\"MWjQ0\"]")));
        Assert.Contains(errors, e => e.Message.Contains("At most"));
        Assert.DoesNotContain(errors, e => e.Message.Contains("not a valid option id"));
    }

    [Fact]
    public void Validate_SourcedNavigationList_ReplaysPathViaSourceNextScreen()
    {
        var survey = new SurveyDto
        {
            SurveyId = "s",
            Title = LocalizedString.From("en", "S"),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens =
            {
                new InlineScreenDto
                {
                    Id = "pick",
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NavigationListQuestionDto
                        {
                            Id = "branch",
                            Title = LocalizedString.From("en", "Branch"),
                            OptionsSource = new OptionsSourceDto
                            {
                                Url = "https://example.test/api/public/company-branch",
                                NextScreen = "details",
                            },
                        })
                    }
                },
                // Sequential decoy: a terminal zero-question screen. Without the
                // sourced dispatch the replay stops here and never enforces "details".
                new InlineScreenDto { Id = "dead-end" },
                new InlineScreenDto
                {
                    Id = "details",
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new TextQuestionDto
                        {
                            Id = "notes",
                            Title = LocalizedString.From("en", "Notes"),
                            Required = true,
                        })
                    }
                },
            }
        };

        var errors = AnswerValidator.Validate(survey, AnswerMap(("branch", "\"any-fetched-id\"")));

        Assert.Contains(errors, e => e.QuestionId == "notes" && e.Message.Contains("required"));
    }

    private static SurveyDto SurveyWith(params QuestionDto[] questions) => new()
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
                Questions = new(questions.Select(QuestionEntryDto.FromInline))
            }
        }
    };

    private static Dictionary<string, JsonElement> AnswerMap(params (string Key, string JsonValue)[] pairs)
    {
        var d = new Dictionary<string, JsonElement>();
        foreach (var (k, v) in pairs)
            d[k] = JsonDocument.Parse(v).RootElement.Clone();
        return d;
    }
}
