
using Microsoft.Extensions.Logging;

namespace ShiftSoftware.ADP.SyncAgent;

public class SyncEngineILogger : ISyncEngineLogger
{
    private readonly ILogger logger;

    public IEnumerable<SyncEngineLoggerStatus> SyncTaskStatuses { get; private set; } = [];

    public SyncEngineLoggerStatus? CurrentSyncTaskStatus { get; private set; }
    public string ID { get; private set; }
    public string? SyncID { get; private set; }
    public long? OperationTimeoutInSeconds { get; private set; }
    public DateTime OperationStart { get; private set; }

    public SyncEngineILogger(ILogger logger)
    {
        this.logger = logger;
    }

    public ISyncEngineLogger SetOperationTimeoutInSeconds(long? seconds)
    {
        this.OperationTimeoutInSeconds = seconds;
        return this;
    }

    public ISyncEngineLogger SetOperationStart(DateTime startDate)
    {
        this.OperationStart = startDate;
        return this;
    }

    public ValueTask<ISyncEngineLogger> SetSyncTaskStatus(SyncEngineLoggerStatus syncTaskStatus)
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

    public ValueTask LogError(string? message, params object?[] args)
    {
        this.logger.LogError(message, args);
        return ValueTask.CompletedTask;
    }

    public ValueTask LogError(Exception? exception, string? message, params object?[] args)
    {
        this.logger.LogError(exception, message, args);
        return ValueTask.CompletedTask;
    }

    public ValueTask LogInformation(string? message, params object?[] args)
    {
        this.logger.LogInformation(message, args);
        return ValueTask.CompletedTask;
    }

    public ValueTask LogWarning(string? message, params object?[] args)
    {
        this.logger.LogWarning(message, args);
        return ValueTask.CompletedTask;
    }
}
