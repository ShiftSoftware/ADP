namespace ShiftSoftware.ADP.SyncAgent;

public enum RetryActionType
{
    RetryAndContinueAfterLastRetry,
    RetryAndStopAfterLastRetry,
    Skip,
    Stop
}
