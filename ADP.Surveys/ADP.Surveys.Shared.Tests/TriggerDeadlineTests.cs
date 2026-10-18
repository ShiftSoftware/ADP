using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Triggers;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using ShiftSoftware.ADP.Surveys.Shared.Triggers;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class TriggerDeadlineTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private static readonly TriggerDeadlineDto SevenDays = new() { Field = "candidate.closedDate", Within = "7d" };

    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);

    [Theory]
    [InlineData("2026-09-26T00:00:00", false)]       // six and a half days old
    [InlineData("2026-09-25T12:00:00", false)]       // exactly on the deadline
    [InlineData("2026-09-25T11:59:59", true)]
    [InlineData("2026-04-08T15:26:45", true)]        // months old, however freshly it arrived
    public void PastOnlyWhenTheEventsOwnDatePlusWithinHasGone(string closedDate, bool expected)
        => Assert.Equal(expected, TriggerDeadline.IsPast(SevenDays, Payload(new { closedDate }), Now));

    [Fact]
    public void AnOffsetIsHonouredAndAMissingOneIsUtc()
    {
        // 18:00 at +05:00 is 13:00 UTC on the 25th: still within 7 days of noon UTC on the 2nd.
        Assert.False(TriggerDeadline.IsPast(SevenDays, Payload(new { closedDate = "2026-09-25T18:00:00+05:00" }), Now));
        // 16:00 at +05:00 is 11:00 UTC: an hour past.
        Assert.True(TriggerDeadline.IsPast(SevenDays, Payload(new { closedDate = "2026-09-25T16:00:00+05:00" }), Now));
    }

    [Fact]
    public void NoDeadlineIsNeverPast()
    {
        Assert.False(TriggerDeadline.IsPast(null, Payload(new { closedDate = "2000-01-01" }), Now));
        Assert.False(TriggerDeadline.IsPast(null, (string?)null, Now));
    }

    [Theory]
    [InlineData("{}")]                               // missing
    [InlineData("{\"closedDate\":null}")]
    [InlineData("{\"closedDate\":20260930}")]        // not a string
    [InlineData("{\"closedDate\":\"soon\"}")]        // unreadable
    [InlineData("not json")]
    [InlineData("")]
    public void AnEventThatCannotBePlacedInTimeIsPast(string payloadJson)
        => Assert.True(TriggerDeadline.IsPast(SevenDays, payloadJson, Now));

    [Fact]
    public void AnUnparseableDurationIsPast()
        => Assert.True(TriggerDeadline.IsPast(
            new TriggerDeadlineDto { Field = "candidate.closedDate", Within = "a week" },
            Payload(new { closedDate = "2026-10-01" }), Now));

    [Fact]
    public void TheStringOverloadMatchesTheElementOne()
    {
        var json = JsonSerializer.Serialize(new { closedDate = "2026-09-20T00:00:00" });
        Assert.True(TriggerDeadline.IsPast(SevenDays, json, Now));
    }

    [Theory]
    [InlineData("candidate.closedDate", "7d", true)]
    [InlineData("closedDate", "7d", false)]          // must be a candidate field
    [InlineData("candidate.", "7d", false)]
    [InlineData("", "7d", false)]
    [InlineData("candidate.closedDate", "0d", false)]
    [InlineData("candidate.closedDate", "", false)]
    [InlineData("candidate.closedDate", "1w", false)]
    public void APublishedDeadlineMustBeComplete(string field, string within, bool valid)
    {
        var trigger = new TriggerDto
        {
            Id = "t", EventKind = "e", Channel = "c", DedupRecipe = { "templateId" },
            Schedule = new TriggerScheduleDto { InitialDelay = "1d" },
            Deadline = new TriggerDeadlineDto { Field = field, Within = within },
        };
        Assert.Equal(valid, new TriggerPublishValidator().Validate(trigger).IsValid);
    }

    [Fact]
    public void ADraftMaySaveAnIncompleteDeadlineButNotABadDuration()
    {
        var draft = new TriggerDto { Deadline = new TriggerDeadlineDto() };
        Assert.True(new TriggerDtoValidator().Validate(draft).IsValid);

        draft.Deadline.Within = "1w";
        Assert.False(new TriggerDtoValidator().Validate(draft).IsValid);
    }

    [Fact]
    public void AbsentDeadlineLeavesTheSerializedSchemaUnchanged()
    {
        var json = JsonSerializer.Serialize(new TriggerDto { Id = "t" }, SurveySchemaSerializer.Options);
        Assert.DoesNotContain("deadline", json);

        var withDeadline = JsonSerializer.Serialize(
            new TriggerDto { Id = "t", Deadline = SevenDays }, SurveySchemaSerializer.Options);
        var back = JsonSerializer.Deserialize<TriggerDto>(withDeadline, SurveySchemaSerializer.Options)!;
        Assert.Equal("candidate.closedDate", back.Deadline!.Field);
        Assert.Equal("7d", back.Deadline.Within);
    }
}
