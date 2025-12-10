namespace ShiftSoftware.ADP.SyncAgent;

public class SyncTaskStatus
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public string? SyncID { get; set; }
    public string TaskDescription { get; set; } = default!;
    public double Progress { get; set; }
    public int CurrentStep { get; set; }
    public int TotalStep { get; set; }
    public TimeSpan Elapsed { get; set; }
    public TimeSpan RemainingTimeToShutdown { get; set; }
    public bool Completed { get; set; }
    public bool Failed { get; set; }
    public int Sort { get; set; }

    internal DateTime OperationStart { get; set; }
    internal int OperationTimeoutInSeconds { get; set; }

    public SyncOperationType OperationType { get; set; }

    public SyncTaskStatus UpdateProgress(bool incrementStep = true)
    {
        if (incrementStep)
            CurrentStep++;

        Progress = TotalStep == 0 ? 0 : (double)(CurrentStep) / TotalStep;
        Elapsed = DateTime.UtcNow - OperationStart;
        RemainingTimeToShutdown = OperationStart.AddSeconds(OperationTimeoutInSeconds) - DateTime.UtcNow;

        return this;
    }
}

public class SyncTaskStatus2
{
    public string TaskDescription { get; private set; } = default!;
    public double Progress { get; private set; }
    public long CurrentStep { get; private set; }
    public long? TotalStep { get; private set; }

    public long? BatchSize { get; private set; }

    public long? CurrentRetryCount { get; private set; }
    public long? MaxRetryCount { get; private set; }

    public TimeSpan Elapsed { get; private set; }
    public TimeSpan RemainingTimeToShutdown { get; private set; }
    public bool Completed { get; private set; }
    public bool Failed { get; private set; }
    public int Sort { get; private set; }

    internal DateTime OperationStart { get; private set; }
    internal int OperationTimeoutInSeconds { get; private set; }

    public SyncActionType? ActionType { get; private set; }
    public SyncOperationType OperationType { get; private set; }

    internal SyncTaskStatus2(SyncOperationType operationType, SyncActionType? syncActionType, long currentStep, long? totalSteps, long? batchSize, long? currentRetryCount, long? maxRetryCount)
    {
        OperationType = operationType;
        ActionType = syncActionType;
        CurrentStep = currentStep;
        TotalStep = totalSteps;
        BatchSize = batchSize;
        CurrentRetryCount = currentRetryCount;
        MaxRetryCount = maxRetryCount;
        UpdateProgress(false);
    }

   
    internal SyncTaskStatus2(SyncOperationType operationType, SyncActionType actionType) : this(operationType, actionType, 1, 1, null, null, null)
    {
    }

    internal SyncTaskStatus2(SyncOperationType operationType) : this(operationType, null, 1, 1, null, null, null)
    {
    }

    public SyncTaskStatus2 UpdateProgress(bool incrementStep = true)
    {
        if (incrementStep)
            CurrentStep++;

        Progress = TotalStep.GetValueOrDefault() == 0 ? 0 : (double)(CurrentStep) / TotalStep.GetValueOrDefault();
        Elapsed = DateTime.UtcNow - OperationStart;
        RemainingTimeToShutdown = OperationStart.AddSeconds(OperationTimeoutInSeconds) - DateTime.UtcNow;

        return this;
    }
}