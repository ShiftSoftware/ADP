using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Personalization;
using ShiftSoftware.ADP.Surveys.Shared.Triggers;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class TestDeliveryTests
{
    private static SurveyDto Survey() => new()
    {
        Variables =
        {
            new SurveyVariableDto { Name = "candidate.vin", Description = "Booking ticket", Example = LocalizedString.From("en", "JT123") },
            new SurveyVariableDto { Name = "recipient.address" },
        },
        Screens =
        {
            new InlineScreenDto
            {
                Id = "welcome",
                Title = LocalizedString.From("en", "Hello {{candidate.customerName}} {{recipient.locale}} {{answers.q}} {{candidate.vin}}"),
            },
        },
    };

    [Fact]
    public void AsksForDeclaredVariablesThenCopyTokensThenHostFields()
    {
        var fields = TestDelivery.CollectFields(Survey(), ["candidate.franchise", "candidate.vin"]);

        Assert.Equal(["candidate.vin", "candidate.customerName", "candidate.franchise"], fields.Select(x => x.Name));
    }

    [Fact]
    public void AVariableKeepsItsExampleAndDescription()
    {
        var vin = TestDelivery.CollectFields(Survey()).Single(x => x.Name == "candidate.vin");

        Assert.Equal("JT123", vin.Example["en"]);
        Assert.Equal("Booking ticket", vin.Description);
    }

    [Fact]
    public void ThePayloadIsWhatAnEventWouldCarry()
    {
        var json = TestDelivery.BuildCandidatePayload(new Dictionary<string, string>
        {
            ["candidate.customerName"] = " Aza ",
            ["candidate.vin"] = "   ",
            ["recipient.address"] = "+964",
        });

        Assert.Equal("""{"customerName":"Aza"}""", json);

        var context = PersonalizationTokens.BuildContext(null, null, null, json);
        Assert.Equal("Aza", context["candidate.customerName"]);
    }

    [Theory]
    [InlineData("dashboard-test", "whatsapp:infobip", true)]
    [InlineData("dashboard-test", null, false)]
    [InlineData("dashboard-test", "", false)]
    [InlineData("event:vehicle-purchased", "whatsapp:infobip", false)]
    public void OnlyATestWithAChannelCountsAsDelivered(string triggeredBy, string? channel, bool expected)
        => Assert.Equal(expected, SurveysConstants.IsDeliveredTest(triggeredBy, channel));

    [Fact]
    public void TestSubscribersOptIn()
    {
        Assert.False(((ISurveyResponseSubscriber)new Silent()).ReceivesTestResponses);
    }

    private class Silent : ISurveyResponseSubscriber
    {
        public string Key => "silent";

        public Task<SubscriberDispatchResult> DispatchAsync(SurveyOutboxPayload payload, CancellationToken ct) =>
            Task.FromResult(SubscriberDispatchResult.Ok());
    }
}
