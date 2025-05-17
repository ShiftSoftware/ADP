namespace ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

public interface ISyncService<TSource, TDestination> : IAsyncDisposable
    where TSource : class, new()
    where TDestination : class, new()
{
    SyncConfigurations? Configurations { get; }
    Func<SyncFunctionInput, ValueTask<bool>>? Preparing { get; }

    /// <summary>
    /// Return true to continue the process,
    /// or false to stop the proccess.
    /// </summary>
    Func<SyncFunctionInput<SyncActionType>, ValueTask<bool>>? ActionStarted { get; }

    Func<SyncFunctionInput<SyncActionType>, ValueTask<long?>>? SourceTotalItemCount { get; }

    /// <summary>
    /// Return true to continue the sync process,
    /// Return false to stop the proccess.
    /// </summary>
    Func<SyncFunctionInput<SyncActionStatus>, ValueTask<bool>>? BatchStarted { get; }

    Func<SyncFunctionInput<SyncGetBatchDataInput<TSource>>, ValueTask<IEnumerable<TSource?>?>>? GetSourceBatchItems { get; }
    Func<SyncFunctionInput<SyncMappingInput<TSource, TDestination>>, ValueTask<IEnumerable<TDestination?>?>>? Mapping { get; }
    Func<SyncFunctionInput<SyncStoreDataInput<TDestination>>, ValueTask<SyncStoreDataResult<TDestination>>>? StoreBatchData { get; }
    Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<RetryAction>>? BatchRetry { get; }

    /// <summary>
    /// Return true to continue the sync process,
    /// Return false to retry.
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
        long? batchSize = null,
        long maxRetryCount = 0,
        long operationTimeoutInSeconds = 300,
        RetryAction defaultRetryAction = RetryAction.RetryAndStopAfterLastRetry);

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
        IEnumerable<SyncActionType> actionExecutionAndOrder,
        long? batchSize = null, 
        long maxRetryCount = 0, 
        long operationTimeoutInSeconds = 300,
        RetryAction defaultRetryAction = RetryAction.RetryAndStopAfterLastRetry);

    ISyncService<TSource, TDestination> SetupPreparing(Func<SyncFunctionInput, ValueTask<bool>> preparingFunc);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="actionStartedFunc">
    /// Return true to continue the process,
    /// or false to stop the proccess.
    /// </param>
    /// <returns></returns>
    ISyncService<TSource, TDestination> SetupActionStarted(Func<SyncFunctionInput<SyncActionType>, ValueTask<bool>>? actionStartedFunc);

    ISyncService<TSource, TDestination> SetupSourceTotalItemCount(Func<SyncFunctionInput<SyncActionType>, ValueTask<long?>>? sourceTotalItemCountFunc);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchStartedFunc">
    /// Return true to continue the sync process,
    /// Return false to stop the proccess.
    /// </param>
    /// <returns></returns>
    ISyncService<TSource, TDestination> SetupBatchStarted(Func<SyncFunctionInput<SyncActionStatus>, ValueTask<bool>> batchStartedFunc);

    ISyncService<TSource, TDestination> SetupGetSourceBatchItems(Func<SyncFunctionInput<SyncGetBatchDataInput<TSource>>, ValueTask<IEnumerable<TSource?>?>> getSourceBatchItemsFunc);

    ISyncService<TSource, TDestination> SetupAdvancedMapping(Func<SyncFunctionInput<SyncMappingInput<TSource, TDestination>>, ValueTask<IEnumerable<TDestination?>?>> mappingFunc);

    /// <summary>
    /// This is using the advanced mapping too, but for simplify,if the operation is not retyr and previous mapped items not null, it return the previous mapped items., otherwise it will mapp it with this function.
    /// </summary>
    /// <param name="mappingFunc"></param>
    /// <returns></returns>
    ISyncService<TSource, TDestination> SetupMapping(Func<IEnumerable<TSource?>?, SyncActionType, ValueTask<IEnumerable<TDestination?>?>> mappingFunc);

    ISyncService<TSource, TDestination> SetupStoreBatchData(Func<SyncFunctionInput<SyncStoreDataInput<TDestination>>, ValueTask<SyncStoreDataResult<TDestination>>> storeBatchDataFunc);

    ISyncService<TSource, TDestination> SetupBatchRetry(Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<RetryAction>> batchRetryFunc);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchCompletedFunc">
    /// Return true to continue the sync process,
    /// Return false to retry.
    /// </param>
    /// <returns></returns>
    ISyncService<TSource, TDestination> SetupBatchCompleted(Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<bool>> batchCompletedFunc);

    ISyncService<TSource, TDestination> SetupActionCompleted(Func<SyncFunctionInput<SyncActionCompletedInput>, ValueTask<bool>> actionCompletedFunc);

    ISyncService<TSource, TDestination> SetupFailed(Func<ValueTask> failedFunc);

    ISyncService<TSource, TDestination> SetupSucceeded(Func<ValueTask> succeededFunc);

    ISyncService<TSource, TDestination> SetupFinished(Func<ValueTask> finishedFunc);

    CancellationToken GetCancellationToken();

    ValueTask Reset();

    ISyncService<TSource, TDestination> SetDataAddapter<TConfigurations, TDataAdapter>(ISyncDataAdapter<TSource, TDestination, TConfigurations, TDataAdapter> dataAdapter, TConfigurations configurations)
        where TDataAdapter : ISyncDataAdapter<TSource, TDestination, TConfigurations, TDataAdapter>;

    TDataAdapter SetDataAddapter<TDataAdapter>(IServiceProvider services)
        where TDataAdapter : ISyncDataAdapter<TSource, TDestination, TDataAdapter>;

    Task<bool> RunAsync();
}
