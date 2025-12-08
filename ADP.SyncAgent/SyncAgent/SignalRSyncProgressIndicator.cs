
using Microsoft.Extensions.Logging;

namespace ShiftSoftware.ADP.SyncAgent;

public class SignalRSyncProgressIndicator : ISyncProgressIndicator2
{
    public IEnumerable<SyncTaskStatus> CurrentSyncTaskStatuses { get; private set; } = [];

    public SyncTaskStatus? CurrentSyncTaskStatus { get; private set; }

    public SignalRSyncProgressIndicator(string? syncID = null)
    {
        this.SetSyncTaskStatus(new SyncTaskStatus
        {
            SyncID = syncID
        });
    }

    public ValueTask SetSyncTaskStatus(SyncTaskStatus syncTaskStatus)
    {
        if (CurrentSyncTaskStatus is not null)
            this.CurrentSyncTaskStatuses = this.CurrentSyncTaskStatuses.Append(CurrentSyncTaskStatus);

        this.CurrentSyncTaskStatus = syncTaskStatus;

        return ValueTask.CompletedTask;
    }

    public ValueTask CompleteAllRunningTasks()
    {
        throw new NotImplementedException();
    }

    public ValueTask FailAllRunningTasks()
    {
        throw new NotImplementedException();
    }

    public ValueTask LogErrorAsync(SyncTaskStatus syncTask, string message)
    {
        throw new NotImplementedException();
    }

    public ValueTask LogInformationAsync(SyncTaskStatus syncTask, string message)
    {
        throw new NotImplementedException();
    }

    public ValueTask LogWarningAsync(SyncTaskStatus syncTask, string message)
    {
        throw new NotImplementedException();
    }
}
