# Logging & Monitoring

The ADP Sync Agent provides a pluggable logging system that enables real-time progress tracking, structured logging, and integration with UI-based progress indicators. Multiple loggers can be registered simultaneously — for example, one for console/file logging and another for a real-time dashboard.

## ISyncEngineLogger

The core logging interface. Implement this to create custom loggers that integrate with your monitoring infrastructure.

```csharp
public interface ISyncEngineLogger
{
    IEnumerable<SyncEngineLoggerStatus> SyncTaskStatuses { get; }
    SyncEngineLoggerStatus? CurrentSyncTaskStatus { get; }
    string ID { get; }
    string? SyncID { get; }
    long? OperationTimeoutInSeconds { get; }
    DateTime OperationStart { get; }

    ISyncEngineLogger SetOperationTimeoutInSeconds(long? seconds);
    ISyncEngineLogger SetOperationStart(DateTime startDate);
    ValueTask<ISyncEngineLogger> SetSyncTaskStatus(SyncEngineLoggerStatus syncTaskStatus);
    ValueTask LogInformation(string? message, params object?[] args);
    ValueTask LogError(string? message, params object?[] args);
    ValueTask LogError(Exception? exception, string? message, params object?[] args);
    ValueTask LogWarning(string? message, params object?[] args);
    ValueTask FailAllRunningTasks();
    ValueTask CompleteAllRunningTasks();
}
```

### Registering Loggers

Register one or more loggers on the Sync Engine before calling `RunAsync`:

```csharp
var engine = new SyncEngine<SourceModel, DestModel>();

engine
    .Configure(batchSize: 500)
    .RegisterLogger(new SyncEngineILogger(logger))
    .RegisterLogger(new SyncProgressIndicatorLogger(dashboardIndicator));

await engine.RunAsync();
```

## Built-in Logger Implementations

### SyncEngineILogger

Bridges the Sync Agent logging system with the standard `Microsoft.Extensions.Logging.ILogger` interface. Use this when you want pipeline events to flow through your existing logging infrastructure (Serilog, Application Insights, console, etc.).

```csharp
var logger = loggerFactory.CreateLogger("SyncAgent");
engine.RegisterLogger(new SyncEngineILogger(logger));
```

Log messages from adapters and lifecycle stages are forwarded as `LogInformation`, `LogWarning`, or `LogError` calls to the underlying `ILogger`.

### SyncProgressIndicatorLogger

Bridges the Sync Agent logging system with the `ISyncProgressIndicator` interface — designed for UI-based progress visualization. This logger maps the internal `SyncEngineLoggerStatus` to `SyncTaskStatus` objects that can be consumed by dashboards or progress bars.

```csharp
engine.RegisterLogger(new SyncProgressIndicatorLogger(myDashboardIndicator));
```

## Progress Tracking

The Sync Engine automatically updates all registered loggers at each lifecycle stage. The `SyncEngineLoggerStatus` object provides rich progress information:

| Property | Type | Description |
|---|---|---|
| `OperationType` | `SyncOperationType` | Current lifecycle stage (Preparing, BatchStarted, Mapping, etc.). |
| `ActionType` | `SyncActionType?` | Current action type (Add, Update, Delete). |
| `CurrentStep` | `long` | Current batch index (1-based after increment). |
| `TotalStep` | `long?` | Total number of batches (if known). |
| `BatchSize` | `long?` | Configured batch size. |
| `TotalCount` | `long?` | Total item count (if known). |
| `CurrentRetryCount` | `long?` | Current retry attempt for this batch. |
| `MaxRetryCount` | `long?` | Maximum retries allowed. |
| `Progress` | `double` | Completion percentage (0.0 to 1.0). |
| `Elapsed` | `TimeSpan` | Time elapsed since pipeline start. |
| `RemainingTimeToShutdown` | `TimeSpan` | Time remaining before the operation timeout. |

### Progress Example

With a batch size of 100 and 1,000 total items, the logger will receive 10 progress updates for each action type — enabling a progress bar like:

```
[████████░░░░░░░░░░░░] 40% — Batch 4/10 — Add — Elapsed: 00:01:23 — Remaining: 00:03:37
```

## Logging from Adapters

Within any lifecycle stage, adapters can log messages through the `SyncProgressIndicators` collection available on every `SyncFunctionInput`:

```csharp
engine.SetupPreparing(async input =>
{
    await input.SyncProgressIndicators.LogInformation("Loading CSV file for comparison...");
    
    // ... do work ...
    
    await input.SyncProgressIndicators.LogInformation("Found 342 added lines and 17 deleted lines.");
    
    return SyncPreparingResponseAction.Succeeded;
});
```

The `IEnumerableExtensions` provide convenient extension methods for broadcasting log messages to all registered loggers at once:

- `LogInformation(message, args)`
- `LogWarning(message, args)`
- `LogError(message, args)`
- `LogError(exception, message, args)`

## ISyncProgressIndicator

The `ISyncProgressIndicator` interface is a simplified UI-oriented contract for displaying sync progress to end users:

```csharp
public interface ISyncProgressIndicator
{
    Task LogInformationAsync(SyncTaskStatus syncTask, string message);
    Task LogErrorAsync(SyncTaskStatus syncTask, string message);
    Task LogWarningAsync(SyncTaskStatus syncTask, string message);
    Task FailAllRunningTasks();
    Task CompleteAllRunningTasks();
}
```

Implement this interface to feed progress data into your application's UI — whether it's a Blazor dashboard, a SignalR-powered web page, or a WPF application.

The `SyncProgressIndicatorLogger` class bridges the engine's `ISyncEngineLogger` interface to your `ISyncProgressIndicator` implementation.

## Legacy ILogger Extension

For simpler scenarios, the `AddLogger` extension method wraps an existing `ILogger` to provide automatic lifecycle logging without implementing `ISyncEngineLogger`:

```csharp
engine.AddLogger(logger);
```

!!! warning "Deprecated"
    The `AddLogger(ILogger)` extension is marked as `[Obsolete]`. Use `RegisterLogger` with an `ISyncEngineLogger` implementation for better async support and extensibility.

This extension automatically logs:

- Preparing start/result
- Action start
- Source total item count
- Batch start with step and total
- Source batch item retrieval
- Mapping
- Storage results (succeeded/failed/skipped counts)
- Batch completion
- Action completion
- Overall success/failure
