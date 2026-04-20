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
