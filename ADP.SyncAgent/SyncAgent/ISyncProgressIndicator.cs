namespace ShiftSoftware.ADP.SyncAgent;

public interface ISyncProgressIndicator
{
    Task LogInformationAsync(SyncTaskStatus syncTask, string message);
    Task LogErrorAsync(SyncTaskStatus syncTask, string message);
    Task LogWarningAsync(SyncTaskStatus syncTask, string message);
    Task FailAllRunningTasks();
    Task CompleteAllRunningTasks();
}