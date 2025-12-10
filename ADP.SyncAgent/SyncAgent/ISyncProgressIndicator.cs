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

    ValueTask<ISyncProgressIndicator2> SetSyncTaskStatus(SyncTaskStatus2 syncTaskStatus);
    ValueTask LogInformationAsync(SyncTaskStatus2 syncTask, string message);
    ValueTask LogErrorAsync(SyncTaskStatus2 syncTask, string message);
    ValueTask LogWarningAsync(SyncTaskStatus2 syncTask, string message);
    ValueTask FailAllRunningTasks();
    ValueTask CompleteAllRunningTasks();
}