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
    /// <summary>
    /// At least one subscriber threw or returned a failure result. See <c>DispatchLogJson</c>
    /// for per-subscriber outcome. <b>Retryable</b> — the dispatcher picks these up again once
    /// <c>NextAttemptAt</c> elapses, until the attempt budget runs out.
    /// </summary>
    Failed = 2,
    /// <summary>
    /// Attempt budget exhausted. Terminal: the dispatcher will not touch it again, so this is
    /// the status an operations alert should watch. Requeue by setting Status back to Pending
    /// once the underlying subscriber problem is fixed.
    /// </summary>
    DeadLettered = 3,
}
