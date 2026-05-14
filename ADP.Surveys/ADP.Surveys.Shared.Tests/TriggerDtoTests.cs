using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Triggers;
using ShiftSoftware.ADP.Surveys.Shared.Enums;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class TriggerDtoTests
{
    private static readonly JsonSerializerOptions Json = SurveySchemaSerializer.Options;

    private static TriggerDto CsiGrFixture() => new()
    {
        Id = "csi-gr-trigger",
        Enabled = true,
        EventKind = "service-visit-closed",
        Filter = new PredicateConditionDto
        {
            QuestionId = "candidate.jobType",
            Op = LogicOperator.Equals,
            Value = JsonDocument.Parse("\"GR\"").RootElement,
        },
        DedupRecipe = new() { "templateId", "recipient.address", "candidate.dealerId", "candidate.wip" },
        Schedule = new TriggerScheduleDto { InitialDelay = "0d", Reminders = new() { "1d" } },
        Channel = "whatsapp:default",
    };

    [Fact]
    public void Trigger_CsiGrFixture_RoundTripsThroughJson()
    {
        var original = CsiGrFixture();
        var json = JsonSerializer.Serialize(original, Json);
        var roundTripped = JsonSerializer.Deserialize<TriggerDto>(json, Json)!;
        var jsonAgain = JsonSerializer.Serialize(roundTripped, Json);
        Assert.Equal(json, jsonAgain);
    }

    [Fact]
    public void Trigger_EmbeddedInSurvey_RoundTrips()
    {
        var survey = new SurveyDto
        {
            SurveyId = "demo",
            Version = 1,
            Title = LocalizedString.From("en", "Demo"),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Triggers = { CsiGrFixture() },
        };
        var json = JsonSerializer.Serialize(survey, Json);
        var roundTripped = JsonSerializer.Deserialize<SurveyDto>(json, Json)!;
        Assert.Single(roundTripped.Triggers);
        Assert.Equal("csi-gr-trigger", roundTripped.Triggers[0].Id);
        Assert.Equal("service-visit-closed", roundTripped.Triggers[0].EventKind);
        Assert.Equal(4, roundTripped.Triggers[0].DedupRecipe.Count);
        Assert.Equal("candidate.wip", roundTripped.Triggers[0].DedupRecipe[3]);
        Assert.Equal("0d", roundTripped.Triggers[0].Schedule.InitialDelay);
        Assert.Equal("1d", Assert.Single(roundTripped.Triggers[0].Schedule.Reminders));
    }

    [Theory]
    [InlineData("0d", 0)]
    [InlineData("1d", 24 * 60 * 60)]
    [InlineData("4h", 4 * 60 * 60)]
    [InlineData("15m", 15 * 60)]
    [InlineData("30s", 30)]
    [InlineData("  2d  ", 2 * 24 * 60 * 60)]
    [InlineData("1D", 24 * 60 * 60)]
    public void Duration_Parses_ValidInputs(string input, long expectedSeconds)
    {
        Assert.True(TriggerDuration.TryParse(input, out var d));
        Assert.Equal(expectedSeconds, (long)d.TotalSeconds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("d")]
    [InlineData("1")]
    [InlineData("1y")]
    [InlineData("1.5d")]
    [InlineData("1 d 2h")]
    [InlineData("-1d")]
    public void Duration_Rejects_InvalidInputs(string? input)
    {
        Assert.False(TriggerDuration.TryParse(input, out _));
    }

    [Fact]
    public void DraftValidator_AllowsEmptyTrigger()
    {
        var empty = new TriggerDto();
        var result = new TriggerDtoValidator().Validate(empty);
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void DraftValidator_RejectsBadDurationInSchedule()
    {
        var t = new TriggerDto { Schedule = new() { InitialDelay = "later" } };
        var result = new TriggerDtoValidator().Validate(t);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("initialDelay"));
    }

    [Fact]
    public void DraftValidator_RejectsBadReminderDuration()
    {
        var t = new TriggerDto { Schedule = new() { Reminders = new() { "5d", "soon" } } };
        var result = new TriggerDtoValidator().Validate(t);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("reminder"));
    }

    [Fact]
    public void PublishValidator_RejectsEmptyId()
    {
        var t = CsiGrFixture();
        t.Id = "";
        var result = new TriggerPublishValidator().Validate(t);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(TriggerDto.Id));
    }

    [Fact]
    public void PublishValidator_RejectsEmptyEventKind()
    {
        var t = CsiGrFixture();
        t.EventKind = "";
        var result = new TriggerPublishValidator().Validate(t);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(TriggerDto.EventKind));
    }

    [Fact]
    public void PublishValidator_RejectsEmptyDedupRecipe()
    {
        var t = CsiGrFixture();
        t.DedupRecipe.Clear();
        var result = new TriggerPublishValidator().Validate(t);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("dedupRecipe"));
    }

    [Fact]
    public void PublishValidator_RejectsEmptyDedupRecipeEntry()
    {
        var t = CsiGrFixture();
        t.DedupRecipe = new() { "templateId", "" };
        var result = new TriggerPublishValidator().Validate(t);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("non-empty"));
    }

    [Fact]
    public void PublishValidator_RejectsEmptyChannel()
    {
        var t = CsiGrFixture();
        t.Channel = "";
        var result = new TriggerPublishValidator().Validate(t);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(TriggerDto.Channel));
    }

    [Fact]
    public void PublishValidator_RejectsEmptyInitialDelay()
    {
        var t = CsiGrFixture();
        t.Schedule.InitialDelay = "";
        var result = new TriggerPublishValidator().Validate(t);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("initialDelay"));
    }

    [Fact]
    public void PublishValidator_AcceptsCsiGrFixture()
    {
        var result = new TriggerPublishValidator().Validate(CsiGrFixture());
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void SurveyDraftValidator_AllowsEmptyTriggers()
    {
        var survey = new SurveyDto { SurveyId = "x" };
        var result = new SurveyDtoValidator().Validate(survey);
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void SurveyPublishValidator_RejectsDuplicateTriggerIds()
    {
        var survey = new SurveyDto
        {
            SurveyId = "demo",
            Version = 1,
            Title = LocalizedString.From("en", "Demo"),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens = { new InlineScreenDto { Id = "s1" } },
            Triggers =
            {
                CsiGrFixture(),
                CsiGrFixture(),
            },
        };
        var result = new SurveyPublishValidator().Validate(survey);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("unique"));
    }

    [Fact]
    public void SurveyPublishValidator_AcceptsSurveyWithOneValidTrigger()
    {
        var survey = new SurveyDto
        {
            SurveyId = "demo",
            Version = 1,
            Title = LocalizedString.From("en", "Demo"),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens = { new InlineScreenDto { Id = "s1" } },
            Triggers = { CsiGrFixture() },
        };
        var result = new SurveyPublishValidator().Validate(survey);
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }
}
