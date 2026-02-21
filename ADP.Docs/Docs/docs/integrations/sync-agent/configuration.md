# Configuration Reference

This page provides a comprehensive reference for all configuration types available in the ADP Sync Agent.

## Sync Engine Configuration

The Sync Engine is configured via the `Configure` method on `ISyncEngine<TSource, TDestination>`.

### Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `batchSize` | `long?` | `null` | Number of items per batch. If `null`, all items are processed in a single batch (when total count is known). |
| `maxRetryCount` | `long` | `0` | Maximum number of retries per batch before applying the default retry action. |
| `operationTimeoutInSeconds` | `long` | `300` | Total time allowed for the pipeline run. A `CancellationToken` is created with this timeout. |
| `defaultRetryAction` | `RetryAction` | `RetryAndStopAfterLastRetry` | Default behavior when a batch fails and no custom `BatchRetry` handler is configured. |
| `actionExecutionAndOrder` | `IEnumerable<SyncActionType>` | `[Delete, Update, Add]` | Which actions to run and in what order. |

### Example

```csharp
engine.Configure(
    batchSize: 1000,
    maxRetryCount: 3,
    operationTimeoutInSeconds: 600,
    defaultRetryAction: RetryAction.RetryAndContinueAfterLastRetry);
```

### Custom Action Order

```csharp
engine.Configure(
    actionExecutionAndOrder: [SyncActionType.Add, SyncActionType.Update],
    batchSize: 500,
    maxRetryCount: 2);
```

---

## Enums

### SyncActionType

Defines the type of data operation being performed.

| Value | Description |
|---|---|
| `Add` | Insert new records into the destination. |
| `Update` | Update existing records in the destination. |
| `Delete` | Remove records from the destination. |
| `Upsert` | Insert or update — used by adapters that don't distinguish between add and update (e.g., Cosmos DB). |

### RetryAction

Controls what happens when a batch fails.

| Value | Description |
|---|---|
| `RetryAndStopAfterLastRetry` | Retry up to `MaxRetryCount`, then stop the entire action. `BatchCompleted` is **not** called. **(Default)** |
| `RetryAndContinueAfterLastRetry` | Retry up to `MaxRetryCount`, then skip the failed batch and continue to the next. `BatchCompleted` **is** called. |
| `Skip` | Skip the failed batch immediately and continue to the next batch. |
| `Stop` | Stop the entire action immediately. `BatchCompleted` is **not** called. |

### SyncPreparingResponseAction

Returned by the `Preparing` stage to control pipeline flow.

| Value | Description |
|---|---|
| `Succeeded` | Preparation was successful — continue to the action loop. |
| `Failed` | Preparation failed — mark the pipeline as failed. |
| `Skipped` | Nothing to process — mark the pipeline as successful without running any actions. |

### SyncStoreDataResultType

Describes the outcome of a `StoreBatchData` operation.

| Value | Description |
|---|---|
| `Succeeded` | All items were written successfully. |
| `Failed` | All items failed to write. |
| `Partial` | Some items succeeded and some failed. |
| `Skipped` | All items were skipped (none succeeded or failed). |

### SyncOperationType

Identifies which lifecycle stage is currently executing. Used primarily by the logging system.

| Value |
|---|
| `Preparing` |
| `ActionStarted` |
| `SourceTotalItemCount` |
| `BatchStarted` |
| `GetSourceBatchItems` |
| `Mapping` |
| `StoreBatchData` |
| `BatchRetry` |
| `BatchCompleted` |
| `ActionCompleted` |
| `Failed` |
| `Succeeded` |
| `Finished` |

---

## Pipeline Stage Input Types

Each stage in the Sync Engine receives a `SyncFunctionInput<T>` that provides context about the current operation.

### SyncFunctionInput

The base input for all stages.

| Property | Type | Description |
|---|---|---|
| `CancellationToken` | `CancellationToken` | Token that is cancelled when the operation timeout expires. |
| `SyncProgressIndicators` | `IEnumerable<ISyncEngineLogger>` | Registered loggers for progress tracking and logging. |

### SyncFunctionInput&lt;T&gt;

Extends `SyncFunctionInput` with stage-specific data via the `Input` property.

---

### SyncActionStatus

Provides batch progress information. Available in most batch-level stages.

| Property | Type | Description |
|---|---|---|
| `CurrentStep` | `long` | Zero-based index of the current batch. |
| `TotalSteps` | `long?` | Total number of batches (if known). |
| `BatchSize` | `long` | Configured batch size. |
| `TotalCount` | `long?` | Total item count (if known). |
| `CurrentRetryCount` | `long` | Number of retries so far for this batch. |
| `MaxRetryCount` | `long` | Maximum retries allowed. |
| `ActionType` | `SyncActionType` | The current action type (Add, Update, Delete, Upsert). |

---

### SyncGetBatchDataInput&lt;T&gt;

Input for `GetSourceBatchItems`.

| Property | Type | Description |
|---|---|---|
| `PreviousItems` | `IEnumerable<T?>?` | On retry, contains the previous batch items. `null` on first attempt. |
| `Status` | `SyncActionStatus` | Current batch progress. |

---

### SyncMappingInput&lt;TSource, TDestination&gt;

Input for the `Mapping` stage.

| Property | Type | Description |
|---|---|---|
| `SourceItems` | `IEnumerable<TSource?>?` | The current batch of source items. |
| `PreviousMappedItem` | `IEnumerable<TDestination?>?` | On retry, the previously mapped items. `null` on first attempt. |
| `Status` | `SyncActionStatus` | Current batch progress. |

---

### SyncStoreDataInput&lt;T&gt;

Input for `StoreBatchData`.

| Property | Type | Description |
|---|---|---|
| `Items` | `IEnumerable<T?>?` | The mapped items to write to the destination. |
| `PreviousResult` | `SyncStoreDataResult<T>?` | On retry, the result from the previous attempt. `null` on first attempt. |
| `Status` | `SyncActionStatus` | Current batch progress. |

---

### SyncStoreDataResult&lt;T&gt;

Returned by `StoreBatchData`.

| Property | Type | Description |
|---|---|---|
| `SucceededItems` | `IEnumerable<T?>?` | Items that were written successfully. |
| `FailedItems` | `IEnumerable<T?>?` | Items that failed. |
| `SkippedItems` | `IEnumerable<T?>?` | Items that were skipped. |
| `RetryException` | `RetryException?` | Set to trigger a retry. `null` to skip retry. |
| `ResultType` | `SyncStoreDataResultType` | Computed from the items — `Succeeded`, `Failed`, `Partial`, or `Skipped`. |

---

### SyncBatchCompleteRetryInput&lt;TSource, TDestination&gt;

Input for `BatchCompleted` and `BatchRetry`.

| Property | Type | Description |
|---|---|---|
| `SourceItems` | `IEnumerable<TSource?>?` | The source items from this batch. |
| `StoreDataResult` | `SyncStoreDataResult<TDestination>?` | The storage result (succeeded/failed/skipped items). |
| `Status` | `SyncActionStatus` | Current batch progress. |
| `Exception` | `Exception?` | The exception that caused the failure (for `BatchRetry`). `null` for `BatchCompleted`. |

---

### SyncActionCompletedInput

Input for `ActionCompleted`.

| Property | Type | Description |
|---|---|---|
| `ActionType` | `SyncActionType` | The action type that just completed. |
| `Succeeded` | `bool` | Whether all batches in this action completed successfully. |

---

## Adapter-Specific Configurations

Refer to the [Data Adapters](data-adapters.md) page for configuration details specific to each built-in adapter:

- [CSV Source Configuration](data-adapters.md#csv-source-configuration)
- [EF Core Source Configuration](data-adapters.md#ef-core-source-configuration)
- [Cosmos DB Destination Configuration](data-adapters.md#cosmos-db-destination-configuration)
- [EF Core Destination Configuration](data-adapters.md#ef-core-destination-configuration)
- [DuckDB Destination Configuration](data-adapters.md#duckdb-destination-configuration)
