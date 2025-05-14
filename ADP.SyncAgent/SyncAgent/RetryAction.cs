namespace ShiftSoftware.ADP.SyncAgent;

public enum RetryAction
{
    RetryAndContinueAfterLastRetry,
    RetryAndStopAfterLastRetry,
    Skip,
    Stop
}
