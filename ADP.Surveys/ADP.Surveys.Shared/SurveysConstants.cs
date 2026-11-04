namespace ShiftSoftware.ADP.Surveys.Shared;

public static class SurveysConstants
{
    /// <summary>
    /// <c>SurveyInstance.TriggeredBy</c> value stamped on instances created via the
    /// dashboard's "Test run" action. Instances carrying it are test fixtures: the
    /// dashboard allows answering them in-place, and reporting filters on this value
    /// to leave them out. One answered in the dashboard skips the outbox; one delivered
    /// to a recipient (it has a channel) reaches the subscribers that opted in to test
    /// responses through <c>ISurveyResponseSubscriber.ReceivesTestResponses</c>.
    /// </summary>
    public const string DashboardTestTriggerSource = "dashboard-test";

    /// <summary>
    /// True for a test instance sent over a real channel. It behaves like a real instance
    /// from there on, because that is what it is testing: it personalizes from the values
    /// the operator entered and its response goes through the outbox, marked as a test.
    /// </summary>
    public static bool IsDeliveredTest(string? triggeredBy, string? channel) =>
        triggeredBy == DashboardTestTriggerSource && !string.IsNullOrEmpty(channel);
}
