namespace ShiftSoftware.ADP.SyncAgent;

public class SyncEngineLoggerStatus
{
    public string TaskDescription { get; private set; } = default!;
    public double Progress { get; private set; }
    public long CurrentStep { get; private set; }
    public long? TotalStep { get; private set; }

    public long? TotalCount { get; private set; }

    public long? BatchSize { get; private set; }

    public long? CurrentRetryCount { get; private set; }
    public long? MaxRetryCount { get; private set; }

    public TimeSpan Elapsed { get; private set; }
    public TimeSpan RemainingTimeToShutdown { get; private set; }
    public bool Completed { get; private set; }
    public bool Failed { get; private set; }
    public int Sort { get; private set; }

    public SyncActionType? ActionType { get; private set; }
    public SyncOperationType OperationType { get; private set; }

    internal SyncEngineLoggerStatus(SyncOperationType operationType, SyncActionType? syncActionType, long currentStep, long? totalSteps, long? batchSize, long? currentRetryCount, long? maxRetryCount, long? totalCount)
    {
        OperationType = operationType;
        ActionType = syncActionType;
        CurrentStep = currentStep;
        TotalStep = totalSteps;
        BatchSize = batchSize;
        CurrentRetryCount = currentRetryCount;
        MaxRetryCount = maxRetryCount;
        TotalCount = totalCount;
    }

   
    internal SyncEngineLoggerStatus(SyncOperationType operationType, SyncActionType actionType) : this(operationType, actionType, 0, 1, null, null, null, null)
    {
    }

    internal SyncEngineLoggerStatus(SyncOperationType operationType) : this(operationType, null, 0, 1, null, null, null, null)
    {
    }

    public SyncEngineLoggerStatus UpdateProgress(DateTime operationStart, long? operationTimeoutInSeconds, bool incrementStep = true)
    {
        if (incrementStep)
            CurrentStep++;

        Progress = TotalStep.GetValueOrDefault() == 0 ? 0 : (double)(CurrentStep) / TotalStep.GetValueOrDefault();
        Elapsed = DateTime.UtcNow - operationStart;
        RemainingTimeToShutdown = operationStart.AddSeconds(operationTimeoutInSeconds.GetValueOrDefault()) - DateTime.UtcNow;

        return this;
    }
}