namespace ShiftSoftware.ADP.Surveys.Data.Entities;

/// <summary>
/// Lifecycle state of a <see cref="SurveyInstance"/>. Transitions happen server-side —
/// there is no transition from <see cref="Completed"/> or <see cref="Expired"/>.
/// </summary>
public enum SurveyInstanceStatus
{
    Pending = 0,
    Sent = 1,
    Opened = 2,
    Completed = 3,
    Expired = 4,
}
