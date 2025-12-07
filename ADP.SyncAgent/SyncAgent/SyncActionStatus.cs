namespace ShiftSoftware.ADP.SyncAgent;

public class SyncActionStatus
{
    public long CurrentStep { get; private set; }
    public long? TotalSteps { get; private set; }
    public long BatchSize { get; private set; }
    public long? TotalCount { get; private set; }
    public long CurrentRetryCount { get; private set; }
    public long MaxRetryCount { get; private set; }
    public SyncActionType ActionType { get; private set; }

    internal SyncActionStatus(
        long currentStep,
        long? totalSteps,
        long batchSize,
        long? totalCount,
        long maxRetryCount,
        long currentRetryCount,
        SyncActionType actionType)
    {
        CurrentStep = currentStep;
        TotalSteps = totalSteps;
        BatchSize = batchSize;
        TotalCount = totalCount;
        MaxRetryCount = maxRetryCount;
        CurrentRetryCount = currentRetryCount;
        ActionType = actionType;
    }
}
