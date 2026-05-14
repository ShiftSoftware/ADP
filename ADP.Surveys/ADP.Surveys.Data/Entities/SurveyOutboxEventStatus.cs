namespace ShiftSoftware.ADP.Surveys.Data.Entities;

/// <summary>
/// Lifecycle of a <see cref="SurveyOutboxEvent"/>.
/// </summary>
public enum SurveyOutboxEventStatus
{
    /// <summary>Written by the submit path; waiting for the dispatch worker to pick up.</summary>
    Pending = 0,
    /// <summary>All registered subscribers handled the event successfully.</summary>
    Dispatched = 1,
    /// <summary>At least one subscriber threw or returned a failure result. See <c>DispatchLogJson</c> for per-subscriber outcome.</summary>
    Failed = 2,
}
