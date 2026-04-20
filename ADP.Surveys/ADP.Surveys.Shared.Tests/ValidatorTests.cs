using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Enums;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class ValidatorTests
{
    [Fact]
    public void LocalizedString_Empty_Fails()
    {
        var result = new LocalizedStringValidator().Validate(new LocalizedString());
        Assert.False(result.IsValid);
    }

    [Fact]
    public void LocalizedString_WithValue_Passes()
    {
        var result = new LocalizedStringValidator().Validate(LocalizedString.From("en", "Hello"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void NpsQuestion_MinGreaterThanMax_Fails()
    {
        var q = new NpsQuestionDto
        {
            Id = "nps",
            Title = LocalizedString.From("en", "x"),
            Min = 10,
            Max = 0,
        };
        var result = new NpsQuestionDtoValidator().Validate(q);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("strictly less than"));
    }

    [Fact]
    public void NavigationList_OptionWithoutNextScreen_Fails()
    {
        var q = new NavigationListQuestionDto
        {
            Id = "has-car",
            Title = LocalizedString.From("en", "x"),
            Options =
            {
                new NavigationListOptionDto { Id = "yes", Label = LocalizedString.From("en", "Yes") /* missing NextScreen */ }
            }
        };
        var result = new NavigationListQuestionDtoValidator().Validate(q);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("nextScreen"));
    }

    [Fact]
    public void SingleChoice_DuplicateOptionIds_Fails()
    {
        var q = new SingleChoiceQuestionDto
        {
            Id = "x",
            Title = LocalizedString.From("en", "x"),
            Options =
            {
                new OptionDto { Id = "a", Label = LocalizedString.From("en", "A") },
                new OptionDto { Id = "a", Label = LocalizedString.From("en", "A again") },
            }
        };
        var result = new SingleChoiceQuestionDtoValidator().Validate(q);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("unique"));
    }

    [Fact]
    public void Composite_WithBothAllAndAny_Fails()
    {
        var condition = new CompositeConditionDto
        {
            All = new() { new ExpressionConditionDto { Expression = "true" } },
            Any = new() { new ExpressionConditionDto { Expression = "false" } },
        };
        var result = new CompositeConditionDtoValidator().Validate(condition);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("exactly one"));
    }

    [Fact]
    public void Predicate_InOperatorWithNonArrayValue_Fails()
    {
        var condition = new PredicateConditionDto
        {
            QuestionId = "brand",
            Op = LogicOperator.In,
            Value = JsonDocument.Parse("\"toyota\"").RootElement,
        };
        var result = new PredicateConditionDtoValidator().Validate(condition);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("array"));
    }

    [Fact]
    public void Survey_DefaultLocaleNotInLocales_Fails()
    {
        var survey = BuildValidSurvey();
        survey.DefaultLocale = "fr"; // not in Locales
        var result = new SurveyDtoValidator().Validate(survey);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("defaultLocale"));
    }

    [Fact]
    public void Survey_MinimalValid_Passes()
    {
        var survey = BuildValidSurvey();
        var result = new SurveyDtoValidator().Validate(survey);
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
    }

    [Fact]
    public void QuestionEntry_BothInlineAndRef_Fails()
    {
        var entry = new QuestionEntryDto
        {
            Inline = new TextQuestionDto { Id = "x", Title = LocalizedString.From("en", "x") },
            Ref = new QuestionRefDto { BankRef = "phone" },
        };
        var result = new QuestionEntryDtoValidator().Validate(entry);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void BankQuestion_IdMismatchBetweenOuterAndInner_Fails()
    {
        var bank = new BankQuestionDto
        {
            Id = "phone",
            Question = new TextQuestionDto { Id = "phone-number", Title = LocalizedString.From("en", "Phone") },
        };
        var result = new BankQuestionDtoValidator().Validate(bank);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("must match"));
    }

    private static SurveyDto BuildValidSurvey() => new()
    {
        SurveyId = "survey-1",
        Version = 1,
        Title = LocalizedString.From("en", "Sample"),
        Locales = new() { "en" },
        DefaultLocale = "en",
        Screens =
        {
            new InlineScreenDto
            {
                Id = "s1",
                Title = LocalizedString.From("en", "Screen 1"),
                Questions =
                {
                    QuestionEntryDto.FromInline(new TextQuestionDto
                    {
                        Id = "name",
                        Title = LocalizedString.From("en", "Name"),
                    })
                }
            }
        }
    };
}
