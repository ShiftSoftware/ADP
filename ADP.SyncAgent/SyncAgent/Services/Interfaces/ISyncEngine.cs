using ShiftSoftware.ADP.SyncAgent.Configurations;

namespace ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

public interface ISyncEngine<TSource, TDestination> : IAsyncDisposable
    where TSource : class
    where TDestination : class
{
    SyncConfigurations? Configurations { get; }
    Func<SyncFunctionInput, ValueTask<SyncPreparingResponseAction>>? Preparing { get; }

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

    Func<SyncFunctionInput<Exception?>, ValueTask>? Failed { get; }

    Func<SyncFunctionInput, ValueTask>? Succeeded { get; }

    Func<SyncFunctionInput, ValueTask>? Finished { get; }

    ISyncEngine<TSource, TDestination> RegisterLogger(ISyncEngineLogger syncProgressIndicator);

    ISyncEngine<TSource, TDestination> Configure(
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
    public ISyncEngine<TSource, TDestination> Configure(
        IEnumerable<SyncActionType> actionExecutionAndOrder,
        long? batchSize = null, 
        long maxRetryCount = 0, 
        long operationTimeoutInSeconds = 300,
        RetryAction defaultRetryAction = RetryAction.RetryAndStopAfterLastRetry);

    ISyncEngine<TSource, TDestination> SetupPreparing(Func<SyncFunctionInput, ValueTask<SyncPreparingResponseAction>> preparingFunc);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="actionStartedFunc">
    /// Return true to continue the process,
    /// or false to stop the proccess.
    /// </param>
    /// <returns></returns>
    ISyncEngine<TSource, TDestination> SetupActionStarted(Func<SyncFunctionInput<SyncActionType>, ValueTask<bool>>? actionStartedFunc);

    ISyncEngine<TSource, TDestination> SetupSourceTotalItemCount(Func<SyncFunctionInput<SyncActionType>, ValueTask<long?>>? sourceTotalItemCountFunc);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchStartedFunc">
    /// Return true to continue the sync process,
    /// Return false to stop the proccess.
    /// </param>
    /// <returns></returns>
    ISyncEngine<TSource, TDestination> SetupBatchStarted(Func<SyncFunctionInput<SyncActionStatus>, ValueTask<bool>> batchStartedFunc);

    ISyncEngine<TSource, TDestination> SetupGetSourceBatchItems(Func<SyncFunctionInput<SyncGetBatchDataInput<TSource>>, ValueTask<IEnumerable<TSource?>?>> getSourceBatchItemsFunc);

    ISyncEngine<TSource, TDestination> SetupAdvancedMapping(Func<SyncFunctionInput<SyncMappingInput<TSource, TDestination>>, ValueTask<IEnumerable<TDestination?>?>> mappingFunc);

    /// <summary>
    /// This is using the advanced mapping too, but for simplify,if the operation is not retyr and previous mapped items not null, it return the previous mapped items., otherwise it will mapp it with this function.
    /// </summary>
    /// <param name="mappingFunc"></param>
    /// <returns></returns>
    ISyncEngine<TSource, TDestination> SetupMapping(Func<IEnumerable<TSource?>?, SyncActionType, ValueTask<IEnumerable<TDestination?>?>> mappingFunc);

    ISyncEngine<TSource, TDestination> SetupStoreBatchData(Func<SyncFunctionInput<SyncStoreDataInput<TDestination>>, ValueTask<SyncStoreDataResult<TDestination>>> storeBatchDataFunc);

    ISyncEngine<TSource, TDestination> SetupBatchRetry(Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<RetryAction>> batchRetryFunc);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchCompletedFunc">
    /// Return true to continue the sync process,
    /// Return false to retry.
    /// </param>
    /// <returns></returns>
    ISyncEngine<TSource, TDestination> SetupBatchCompleted(Func<SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>>, ValueTask<bool>> batchCompletedFunc);

    ISyncEngine<TSource, TDestination> SetupActionCompleted(Func<SyncFunctionInput<SyncActionCompletedInput>, ValueTask<bool>> actionCompletedFunc);

    ISyncEngine<TSource, TDestination> SetupFailed(Func<SyncFunctionInput<Exception?>, ValueTask> failedFunc);

    ISyncEngine<TSource, TDestination> SetupSucceeded(Func<SyncFunctionInput, ValueTask> succeededFunc);

    ISyncEngine<TSource, TDestination> SetupFinished(Func<SyncFunctionInput, ValueTask> finishedFunc);

    CancellationToken GetCancellationToken();

    ValueTask Reset();

    ISyncEngine<TSource, TDestination> SetDataAddapter<TConfigurations, TDataAdapter>(ISyncDataAdapter<TSource, TDestination, TConfigurations, TDataAdapter> dataAdapter, TConfigurations configurations)
        where TDataAdapter : ISyncDataAdapter<TSource, TDestination, TConfigurations, TDataAdapter>;

    TDataAdapter SetDataAddapter<TDataAdapter>(IServiceProvider services)
        where TDataAdapter : ISyncDataAdapter<TSource, TDestination, TDataAdapter>;

    TDataAdapter SetDataAddapter<TDataAdapter>()
        where TDataAdapter : ISyncDataAdapter<TSource, TDestination, TDataAdapter>;

    Task<bool> RunAsync();
}
