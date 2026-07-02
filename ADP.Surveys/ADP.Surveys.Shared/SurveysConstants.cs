namespace ShiftSoftware.ADP.Surveys.Shared;

public static class SurveysConstants
{
    /// <summary>
    /// <c>SurveyInstance.TriggeredBy</c> value stamped on instances created via the
    /// dashboard's "Test run" action. Instances carrying it are test fixtures:
    /// their responses skip the outbox (no fan-out to
    /// <c>ISurveyResponseSubscriber</c>s / BI), and the dashboard allows answering
    /// them in-place. Filter on this value to exclude test data from reporting.
    /// </summary>
    public const string DashboardTestTriggerSource = "dashboard-test";
}
