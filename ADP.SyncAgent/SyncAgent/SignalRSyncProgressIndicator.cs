namespace ShiftSoftware.ADP.SyncAgent;

public class SignalRSyncProgressIndicator : ISyncProgressIndicator2
{
    private readonly ISyncProgressIndicator syncProgressIndicator;

    public IEnumerable<SyncTaskStatus2> SyncTaskStatuses { get; private set; } = [];

    public SyncTaskStatus2? CurrentSyncTaskStatus { get; private set; }

    public string ID { get; private set; }
    public string? SyncID { get; private set; }
    public long? OperationTimeoutInSeconds { get; private set; }
    public DateTime OperationStart { get; private set; }

    public SignalRSyncProgressIndicator(ISyncProgressIndicator syncProgressIndicator)
    {
        this.syncProgressIndicator = syncProgressIndicator;
        Setup();
    }

    public void Setup(string? id = null, string? syncID = null)
    {
        ID = id ?? Guid.NewGuid().ToString();
        SyncID = syncID;
    }

    public ISyncProgressIndicator2 SetOperationTimeoutInSeconds(long? seconds)
    {
        this.OperationTimeoutInSeconds = seconds;
        return this;
    }

    public ISyncProgressIndicator2 SetOperationStart(DateTime startDate)
    {
        this.OperationStart = startDate;
        return this;
    }

    public ValueTask<ISyncProgressIndicator2> SetSyncTaskStatus(SyncTaskStatus2 syncTaskStatus)
    {
        if (!this.SyncTaskStatuses.Contains(syncTaskStatus))
            this.SyncTaskStatuses = this.SyncTaskStatuses?.Append(syncTaskStatus!) ?? [];

        this.CurrentSyncTaskStatus = syncTaskStatus;

        return new(this);
    }

    public async ValueTask CompleteAllRunningTasks()
    {
        await syncProgressIndicator.CompleteAllRunningTasks();
    }

    public async ValueTask FailAllRunningTasks()
    {
        await syncProgressIndicator.FailAllRunningTasks();
    }

    public async ValueTask LogError(string? message, params object?[] args)
    {
        await syncProgressIndicator.LogErrorAsync(MapTask(CurrentSyncTaskStatus!), message!);
    }

    public async ValueTask LogError(Exception? exception, string? message, params object?[] args)
    {
        await syncProgressIndicator.LogErrorAsync(MapTask(CurrentSyncTaskStatus!), message!);
    }

    public async ValueTask LogInformation(string? message, params object?[] args)
    {
        await syncProgressIndicator.LogInformationAsync(MapTask(CurrentSyncTaskStatus!), message!);
    }

    public async ValueTask LogWarning(string? message, params object?[] args)
    {
        await syncProgressIndicator.LogWarningAsync(MapTask(CurrentSyncTaskStatus!), message!);
    }

    private SyncTaskStatus MapTask(SyncTaskStatus2 task)
    {
        return new SyncTaskStatus
        {
            ID = ID,
            SyncID = SyncID,
            TaskDescription = task.TaskDescription,
            Progress = task.Progress,
            CurrentStep = (int)task.CurrentStep,
            TotalStep = (int)(task.TotalStep ?? 0),
            Elapsed = task.Elapsed,
            RemainingTimeToShutdown = task.RemainingTimeToShutdown,
            Completed = task.Completed,
            Failed = task.Failed,
            Sort = task.Sort,
            OperationStart = OperationStart,
            OperationTimeoutInSeconds = (int)(OperationTimeoutInSeconds ?? 0),
            OperationType = task.OperationType,
        };
    }
}
