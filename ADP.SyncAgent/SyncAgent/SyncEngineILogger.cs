
using Microsoft.Extensions.Logging;

namespace ShiftSoftware.ADP.SyncAgent;

/// <summary>
/// Bridges the sync engine's own narration into a standard <see cref="ILogger"/>: every status the
/// engine reports through <see cref="SetSyncTaskStatus"/> is written as a log line (batch completions,
/// action boundaries and run outcomes at Information; retries at Warning; failures at Error; the
/// micro-steps inside a batch at Debug), and the engine's explicit <c>Log*</c> calls — including the
/// exceptions it hands over on batch and run failures — pass straight through.
/// </summary>
public class SyncEngineILogger : ISyncEngineLogger
{
    private readonly ILogger logger;
    private readonly string? name;

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

    /// <param name="name">
    /// Optional context rendered into every line — the job or table this engine run is syncing, so
    /// hosts running many engines can tell the narrations apart.
    /// </param>
    public SyncEngineILogger(ILogger logger, string? name) : this(logger)
    {
        this.name = name;
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

        var level = syncTaskStatus.OperationType switch
        {
            SyncOperationType.Failed => LogLevel.Error,
            SyncOperationType.BatchRetry => LogLevel.Warning,
            SyncOperationType.Preparing
                or SyncOperationType.ActionStarted
                or SyncOperationType.BatchCompleted
                or SyncOperationType.ActionCompleted
                or SyncOperationType.Succeeded
                or SyncOperationType.Finished => LogLevel.Information,
            _ => LogLevel.Debug,
        };

        // Run-level statuses (Preparing, Succeeded, Failed, Finished) carry no action or batch — keep
        // their lines clean rather than rendering placeholder nulls.
        if (syncTaskStatus.ActionType is null)
            this.logger.Log(level,
                "Sync engine{Name}: {Operation}.",
                RenderedName, syncTaskStatus.OperationType);
        else if (syncTaskStatus.CurrentRetryCount > 0)
            this.logger.Log(level,
                "Sync engine{Name}: {Operation} (action: {Action}, batch {Step}/{TotalSteps}, retry {Retry}/{MaxRetries}).",
                RenderedName, syncTaskStatus.OperationType, syncTaskStatus.ActionType,
                syncTaskStatus.CurrentStep, syncTaskStatus.TotalStep?.ToString() ?? "?",
                syncTaskStatus.CurrentRetryCount, syncTaskStatus.MaxRetryCount);
        else
            this.logger.Log(level,
                "Sync engine{Name}: {Operation} (action: {Action}, batch {Step}/{TotalSteps}).",
                RenderedName, syncTaskStatus.OperationType, syncTaskStatus.ActionType,
                syncTaskStatus.CurrentStep, syncTaskStatus.TotalStep?.ToString() ?? "?");

        return new(this);
    }

    private string RenderedName => this.name is null ? "" : $" [{this.name}]";

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
