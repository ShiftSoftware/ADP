namespace ShiftSoftware.ADP.SyncAgent;

public enum RetryAction
{
    RetryAndContinueAfterLastRetry,

    /// <summary>
    /// If the last retry failed, the operation will be stopped and BatchCompleted event will not be executed.
    /// </summary>
    RetryAndStopAfterLastRetry,

    Skip,

    /// <summary>
    /// Stop the operation and BatchCompleted event will not be executed.
    /// </summary>
    Stop
}
