namespace ShiftSoftware.ADP.SyncAgent;

public interface ISyncProgressIndicator
{
    Task LogInformationAsync(SyncTaskStatus syncTask, string message);
    Task LogErrorAsync(SyncTaskStatus syncTask, string message);
    Task LogWarningAsync(SyncTaskStatus syncTask, string message);
    Task FailAllRunningTasks();
    Task CompleteAllRunningTasks();
}

public interface ISyncProgressIndicator2
{
    public IEnumerable<SyncTaskStatus2> CurrentSyncTaskStatuses { get; }
    public SyncTaskStatus2? CurrentSyncTaskStatus { get; }
    public string ID { get; }
    public string? SyncID { get;}
    public long? OperationTimeoutInSeconds { get; }
    public DateTime OperationStart { get;}

    ISyncProgressIndicator2 SetOperationTimeoutInSeconds(long? seconds);
    ISyncProgressIndicator2 SetOperationStart(DateTime startDate);
    ValueTask<ISyncProgressIndicator2> SetSyncTaskStatus(SyncTaskStatus2 syncTaskStatus);
    ValueTask LogInformation(string? message, params object?[] args);
    ValueTask LogError(string? message, params object?[] args);
    ValueTask LogError(Exception? exception, string? message, params object?[] args);
    ValueTask LogWarning(string? message, params object?[] args);
    ValueTask FailAllRunningTasks();
    ValueTask CompleteAllRunningTasks();
}