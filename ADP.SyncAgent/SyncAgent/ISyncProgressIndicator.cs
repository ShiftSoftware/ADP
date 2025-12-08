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
    public IEnumerable<SyncTaskStatus> CurrentSyncTaskStatuses { get; }
    public SyncTaskStatus? CurrentSyncTaskStatus { get; }

    ValueTask SetSyncTaskStatus(SyncTaskStatus syncTaskStatus);
    ValueTask LogInformationAsync(SyncTaskStatus syncTask, string message);
    ValueTask LogErrorAsync(SyncTaskStatus syncTask, string message);
    ValueTask LogWarningAsync(SyncTaskStatus syncTask, string message);
    ValueTask FailAllRunningTasks();
    ValueTask CompleteAllRunningTasks();
}