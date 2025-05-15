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
    Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<RetryAction>>? BatchRetry { get; }

    /// <summary>
    /// Return true to continue the sync process,
    /// Return false to stop the sync process.
    /// </summary>
    Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<bool>>? BatchCompleted { get; }

    Func<SyncFunctionInput<SyncActionType>, ValueTask>? ActionCompleted { get; }

    Func<ValueTask>? Failed { get; }

    Func<ValueTask>? Succeeded { get; }

    Func<ValueTask>? Finished { get; }

    ISyncService<TSource, TDestination> Configure(
        int batchSize,
        int maxRetryCount = 0,
        int operationTimeoutInSeconds = 300);

    ISyncService<TSource, TDestination> SetupPreparing(Func<SyncFunctionInput, ValueTask<bool>> preparingFunc);

    ISyncService<TSource, TDestination> SetupSourceTotalItemCount(Func<SyncFunctionInput<SyncActionType>, ValueTask<long?>>? sourceTotalItemCountFunc);

    ISyncService<TSource, TDestination> SetupGetSourceBatchItems(Func<SyncFunctionInput<SyncGetBatchDataInput<TSource>>, ValueTask<IEnumerable<TSource?>?>> getSourceBatchItemsFunc);

    ISyncService<TSource, TDestination> SetupMapping(Func<SyncFunctionInput<SyncMappingInput<TSource, TDestination>>, ValueTask<IEnumerable<TDestination?>?>> mappingFunc);

    ISyncService<TSource, TDestination> SetupStoreBatchData(Func<SyncFunctionInput<SyncStoreDataInput<TDestination>>, ValueTask<SyncStoreDataResult<TDestination>>> storeBatchDataFunc);

    ISyncService<TSource, TDestination> SetupBatchRetry(Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<RetryAction>> batchRetryFunc);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchCompletedFunc">
    /// Return true to continue the sync process,
    /// Return false to stop the sync process.
    /// </param>
    /// <returns></returns>
    ISyncService<TSource, TDestination> SetupBatchCompleted(Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<bool>> batchCompletedFunc);

    ISyncService<TSource, TDestination> SetupActionCompleted(Func<SyncFunctionInput<SyncActionType>, ValueTask> actionCompletedFunc);

    ISyncService<TSource, TDestination> SetupFailed(Func<ValueTask> failedFunc);

    ISyncService<TSource, TDestination> SetupSucceeded(Func<ValueTask> succeededFunc);

    ISyncService<TSource, TDestination> SetupFinished(Func<ValueTask> finishedFunc);

    CancellationToken GetCancellationToken();

    ValueTask Reset();

    Task<bool> RunAsync();
}
