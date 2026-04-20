using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Integrity;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class SurveyIntegrityValidatorTests
{
    [Fact]
    public void Validate_GoodSurvey_NoErrors()
    {
        var survey = BuildTwoScreenSurvey();
        var errors = SurveyIntegrityValidator.Validate(survey);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_DuplicateScreenId_ReportsError()
    {
        var survey = BuildTwoScreenSurvey();
        ((InlineScreenDto)survey.Screens[1]).Id = "s1"; // collides with screens[0]
        var errors = SurveyIntegrityValidator.Validate(survey);
        Assert.Contains(errors, e => e.Message.Contains("Duplicate screen id"));
    }

    [Fact]
    public void Validate_UnknownNextScreen_ReportsError()
    {
        var survey = BuildTwoScreenSurvey();
        ((InlineScreenDto)survey.Screens[0]).NextScreen = "does-not-exist";
        var errors = SurveyIntegrityValidator.Validate(survey);
        Assert.Contains(errors, e => e.Path.EndsWith(".nextScreen") && e.Message.Contains("does-not-exist"));
    }

    [Fact]
    public void Validate_NavigationListOptionPointsAtMissingScreen_ReportsError()
    {
        var survey = BuildTwoScreenSurvey();
        var nav = new NavigationListQuestionDto
        {
            Id = "has-car",
            Title = LocalizedString.From("en", "?"),
            Options =
            {
                new NavigationListOptionDto { Id = "yes", Label = LocalizedString.From("en", "Y"), NextScreen = "missing-screen" },
            }
        };
        ((InlineScreenDto)survey.Screens[0]).Questions.Add(QuestionEntryDto.FromInline(nav));
        var errors = SurveyIntegrityValidator.Validate(survey);
        Assert.Contains(errors, e => e.Path.Contains("options[0].nextScreen"));
    }

    [Fact]
    public void Validate_LogicGotoMissing_ReportsError()
    {
        var survey = BuildTwoScreenSurvey();
        survey.Logic.Add(new LogicRuleDto
        {
            If = new ExpressionConditionDto { Expression = "true" },
            Then = new LogicActionDto { Goto = "no-such-screen" }
        });
        var errors = SurveyIntegrityValidator.Validate(survey);
        Assert.Contains(errors, e => e.Path.Contains("logic[0].then.goto"));
    }

    [Fact]
    public void Validate_DuplicateQuestionIdInSameScreen_ReportsError()
    {
        var survey = BuildTwoScreenSurvey();
        var screen = (InlineScreenDto)survey.Screens[0];
        screen.Questions.Add(QuestionEntryDto.FromInline(new TextQuestionDto
        {
            Id = screen.Questions[0].Inline!.Id, // collision
            Title = LocalizedString.From("en", "dup")
        }));
        var errors = SurveyIntegrityValidator.Validate(survey);
        Assert.Contains(errors, e => e.Message.Contains("Duplicate question id"));
    }

    [Fact]
    public void Validate_UnresolvedRefSurvivingIntoIntegrityPass_ReportsError()
    {
        var survey = BuildTwoScreenSurvey();
        survey.Screens.Add(new ScreenTemplateRefDto { TemplateRef = "not-resolved" });
        var errors = SurveyIntegrityValidator.Validate(survey);
        Assert.Contains(errors, e => e.Path.EndsWith(".templateRef"));
    }

    private static SurveyDto BuildTwoScreenSurvey() => new()
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
                Title = LocalizedString.From("en", "One"),
                NextScreen = "s2",
                Questions = { QuestionEntryDto.FromInline(new TextQuestionDto { Id = "q1", Title = LocalizedString.From("en", "Q1") }) }
            },
            new InlineScreenDto
            {
                Id = "s2",
                Title = LocalizedString.From("en", "Two"),
                Questions = { QuestionEntryDto.FromInline(new TextQuestionDto { Id = "q2", Title = LocalizedString.From("en", "Q2") }) }
            }
        }
    };
}
