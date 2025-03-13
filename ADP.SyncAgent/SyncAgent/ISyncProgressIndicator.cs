namespace ShiftSoftware.ADP.SyncAgent;

public interface ISyncProgressIndicator
{
    Task LogInformationAsync(SyncTask syncTask, string message);
    Task LogErrorAsync(SyncTask syncTask, string message);
    Task LogWarningAsync(SyncTask syncTask, string message);
    Task FailAllRunningTasks();
    Task CompleteAllRunningTasks();
}