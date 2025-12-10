namespace ShiftSoftware.ADP.SyncAgent;

public class SignalRSyncProgressIndicator : ISyncProgressIndicator2
{
    public IEnumerable<SyncTaskStatus2> CurrentSyncTaskStatuses { get; private set; } = [];

    public SyncTaskStatus2? CurrentSyncTaskStatus { get; private set; }

    public string ID { get; private set; }
    public string? SyncID { get; private set; }

    public SignalRSyncProgressIndicator(string? id = null, string? syncID = null)
    {
        ID = id ?? Guid.NewGuid().ToString();
        SyncID = syncID;
    }

    public ValueTask<ISyncProgressIndicator2> SetSyncTaskStatus(SyncTaskStatus2 syncTaskStatus)
    {
        if (CurrentSyncTaskStatus is not null)
            this.CurrentSyncTaskStatuses = this.CurrentSyncTaskStatuses.Append(CurrentSyncTaskStatus);

        this.CurrentSyncTaskStatus = syncTaskStatus;

        return new(this);
    }

    public ValueTask CompleteAllRunningTasks()
    {
        throw new NotImplementedException();
    }

    public ValueTask FailAllRunningTasks()
    {
        throw new NotImplementedException();
    }

    public ValueTask LogError(string? message, params object?[] args)
    {
        throw new NotImplementedException();
    }

    public ValueTask LogError(Exception? exception, string? message, params object?[] args)
    {
        throw new NotImplementedException();
    }

    public ValueTask LogInformation(string? message, params object?[] args)
    {
        throw new NotImplementedException();
    }

    public ValueTask LogWarning(string? message, params object?[] args)
    {
        throw new NotImplementedException();
    }
}
