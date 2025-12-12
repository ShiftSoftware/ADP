namespace ShiftSoftware.ADP.SyncAgent;

public interface ISyncEngineLogger
{
    public IEnumerable<SyncEngineLoggerStatus> SyncTaskStatuses { get; }
    public SyncEngineLoggerStatus? CurrentSyncTaskStatus { get; }
    public string ID { get; }
    public string? SyncID { get;}
    public long? OperationTimeoutInSeconds { get; }
    public DateTime OperationStart { get;}

    ISyncEngineLogger SetOperationTimeoutInSeconds(long? seconds);
    ISyncEngineLogger SetOperationStart(DateTime startDate);
    ValueTask<ISyncEngineLogger> SetSyncTaskStatus(SyncEngineLoggerStatus syncTaskStatus);
    ValueTask LogInformation(string? message, params object?[] args);
    ValueTask LogError(string? message, params object?[] args);
    ValueTask LogError(Exception? exception, string? message, params object?[] args);
    ValueTask LogWarning(string? message, params object?[] args);
    ValueTask FailAllRunningTasks();
    ValueTask CompleteAllRunningTasks();
}