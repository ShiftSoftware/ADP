namespace ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

public interface ISyncService<TSource, TDestination> : IAsyncDisposable
    where TSource : class, new()
    where TDestination : class, new()
{
    SyncConfigurations? SyncConfigurations { get; }
    Func<SyncFunctionInput, ValueTask<bool>>? Preparing { get; }
    Func<SyncFunctionInput<SyncActionType>, ValueTask<long?>>? SourceTotalItemCount { get; }
    Func<SyncFunctionInput<SyncGetBatchDataInput<TSource>>, ValueTask<IEnumerable<TSource?>?>>? GetSourceBatchItems { get; }
    Func<SyncFunctionInput<SyncMappingInput<TSource, TDestination>>, ValueTask<IEnumerable<TDestination?>?>>? Mapping { get; }
    Func<SyncFunctionInput<SyncStoreDataInput<TDestination>>, ValueTask<SyncStoreDataResult<TDestination>>>? StoreBatchData { get; }

    /// <summary>
    /// Return RetryAndContinueAfterLastRetry to continue retrying or the last retry to continue the sync process,
    /// Return Stop to stop the sync process and does not trigger BatchCompleted function,
    /// Return Skip to continue the sync process without retrying,
    /// And the default value if not setup retry is RetryAndContinueAfterLastRetry.
    /// </summary>
    Func<SyncFunctionInput<SyncBatchCompleteInput<TSource, TDestination>>, ValueTask<RetryActionType>>? BatchRetry { get; }

    /// <summary>
    /// Return true to continue the sync process,
    /// Return false to stop the sync process.
    /// </summary>
    Func<SyncFunctionInput<SyncBatchCompleteInput<TSource, TDestination>>, ValueTask<bool>>? BatchCompleted { get; }

    Func<SyncFunctionInput, ValueTask>? OperationCompleted { get; }

    Func<ValueTask>? Failed { get; }

    Func<ValueTask>? Succeeded { get; }

    Func<ValueTask>? Finished { get; }

    ISyncService<TSource, TDestination> Configure(
        int batchSize,
        int maxRetryCount = 0,
        int operationTimeoutInSeconds = 300,
        string? syncId = null);

    ISyncService<TSource, TDestination> SetupPreparing(Func<SyncFunctionInput, ValueTask<bool>> preparingFunc);

    ISyncService<TSource, TDestination> SetupSourceTotalItemCount(Func<SyncFunctionInput<SyncActionType>, ValueTask<long?>>? sourceTotalItemCountFunc);

    ISyncService<TSource, TDestination> SetupGetSourceBatchItems(Func<SyncFunctionInput<SyncGetBatchDataInput<TSource>>, ValueTask<IEnumerable<TSource?>?>> getSourceBatchItemsFunc);

    ISyncService<TSource, TDestination> SetupMapping(Func<SyncFunctionInput<SyncMappingInput<TSource, TDestination>>, ValueTask<IEnumerable<TDestination?>?>> mappingFunc);

    ISyncService<TSource, TDestination> SetupStoreBatchData(Func<SyncFunctionInput<SyncStoreDataInput<TDestination>>, ValueTask<SyncStoreDataResult<TDestination>>> storeBatchDataFunc);

    ISyncService<TSource, TDestination> SetupBatchRetry(Func<SyncFunctionInput<SyncBatchCompleteInput<TSource, TDestination>>, ValueTask<RetryActionType>> batchRetryFunc);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchCompletedFunc">
    /// Return true to continue the sync process,
    /// Return false to stop the sync process.
    /// </param>
    /// <returns></returns>
    ISyncService<TSource, TDestination> SetupBatchCompleted(Func<SyncFunctionInput<SyncBatchCompleteInput<TSource, TDestination>>, ValueTask<bool>> batchCompletedFunc);

    ISyncService<TSource, TDestination> SetupOperationCompleted(Func<SyncFunctionInput, ValueTask> operationCompletedFunc);

    ISyncService<TSource, TDestination> SetupFailed(Func<ValueTask> failedFunc);

    ISyncService<TSource, TDestination> SetupSucceeded(Func<ValueTask> succeededFunc);

    ISyncService<TSource, TDestination> SetupFinished(Func<ValueTask> finishedFunc);

    CancellationToken GetCancellationToken();

    ValueTask Reset();

    Task<bool> RunAsync();
}
