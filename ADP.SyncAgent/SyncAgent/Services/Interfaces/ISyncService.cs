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

    /// <summary>
    /// Return true to mark the action as completed successfully,
    /// or false to mark it as failed.
    /// </summary>
    Func<SyncFunctionInput<SyncActionCompletedInput>, ValueTask<bool>>? ActionCompleted { get; }

    Func<ValueTask>? Failed { get; }

    Func<ValueTask>? Succeeded { get; }

    Func<ValueTask>? Finished { get; }

    ISyncService<TSource, TDestination> Configure(
        int batchSize,
        int maxRetryCount = 0,
        int operationTimeoutInSeconds = 300);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchSize"></param>
    /// <param name="actionExecutionAndOrder">
    /// This is decide which action to be run and in which order,
    /// the default is [SyncActionType.Delete, SyncActionType.Update, SyncActionType.Add]</param>
    /// <param name="maxRetryCount"></param>
    /// <param name="operationTimeoutInSeconds"></param>
    /// <returns></returns>
    public ISyncService<TSource, TDestination> Configure(
        int batchSize, 
        IEnumerable<SyncActionType> actionExecutionAndOrder,
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

    ISyncService<TSource, TDestination> SetupActionCompleted(Func<SyncFunctionInput<SyncActionCompletedInput>, ValueTask<bool>>? actionCompletedFunc);

    ISyncService<TSource, TDestination> SetupFailed(Func<ValueTask> failedFunc);

    ISyncService<TSource, TDestination> SetupSucceeded(Func<ValueTask> succeededFunc);

    ISyncService<TSource, TDestination> SetupFinished(Func<ValueTask> finishedFunc);

    CancellationToken GetCancellationToken();

    ValueTask Reset();

    Task<bool> RunAsync();
}
