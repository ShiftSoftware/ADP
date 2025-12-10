
using Microsoft.Extensions.Logging;

namespace ShiftSoftware.ADP.SyncAgent;

public class ILoggerSyncProgressIndicator : ISyncProgressIndicator2
{
    private readonly ILogger logger;

    public IEnumerable<SyncTaskStatus2> CurrentSyncTaskStatuses { get; private set; } = [];

    public SyncTaskStatus2? CurrentSyncTaskStatus { get; private set; }
    public string ID { get; private set; }
    public string? SyncID { get; private set; }

    public ILoggerSyncProgressIndicator(ILogger logger)
    {
        this.logger = logger;
    }

    public ValueTask<ISyncProgressIndicator2> SetSyncTaskStatus(SyncTaskStatus2 syncTaskStatus)
    {
        this.CurrentSyncTaskStatus = syncTaskStatus;
        return new(this);
    }

    public ValueTask CompleteAllRunningTasks()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask FailAllRunningTasks()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask LogErrorAsync(SyncTaskStatus2 syncTask, string message)
    {
        this.logger.LogError(message);
        return ValueTask.CompletedTask;
    }

    public ValueTask LogInformationAsync(SyncTaskStatus2 syncTask, string message)
    {
        this.logger.LogInformation(message);
        return ValueTask.CompletedTask;
    }

    public ValueTask LogWarningAsync(SyncTaskStatus2 syncTask, string message)
    {
        this.logger.LogWarning(message);
        return ValueTask.CompletedTask;
    }
}
