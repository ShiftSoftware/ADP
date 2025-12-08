
using Microsoft.Extensions.Logging;

namespace ShiftSoftware.ADP.SyncAgent;

public class ILoggerSyncProgressIndicator : ISyncProgressIndicator2
{
    private readonly ILogger logger;

    public IEnumerable<SyncTaskStatus> CurrentSyncTaskStatuses { get; private set; } = [];

    public SyncTaskStatus? CurrentSyncTaskStatus { get; private set; }

    public ILoggerSyncProgressIndicator(ILogger logger)
    {
        this.logger = logger;
    }

    public ValueTask SetSyncTaskStatus(SyncTaskStatus syncTaskStatus)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask CompleteAllRunningTasks()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask FailAllRunningTasks()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask LogErrorAsync(SyncTaskStatus syncTask, string message)
    {
        this.logger.LogError(message);
        return ValueTask.CompletedTask;
    }

    public ValueTask LogInformationAsync(SyncTaskStatus syncTask, string message)
    {
        this.logger.LogInformation(message);
        return ValueTask.CompletedTask;
    }

    public ValueTask LogWarningAsync(SyncTaskStatus syncTask, string message)
    {
        this.logger.LogWarning(message);
        return ValueTask.CompletedTask;
    }
}
