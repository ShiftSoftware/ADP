namespace ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

public interface ISyncService<TSource, TDestination> : IDisposable
    where TSource : class, new()
    where TDestination : class, new()
{
    SyncConfigurations<TSource, TDestination>? SyncConfigurations { get; }
    Func<SyncFunctionInput, ValueTask<bool>>? Preparing { get; }
    Func<SyncFunctionInput<SyncActionType>, ValueTask<long?>?>? GetSourceTotalItemCount { get; }
    Func<SyncFunctionInput<SyncGetBatchDataInput<TSource>>, ValueTask<IEnumerable<TSource?>?>?> GetSourceBatchItems { get; }
    Func<SyncFunctionInput<SyncStoreDataInput<TDestination>>, ValueTask<SyncStoreDataResult<TDestination>>> StoreBatchData { get; }

    /// <summary>
    /// Return true to continue retrying or the last retry to continue the sync process,
    /// Return false to stop the sync process,
    /// Return null to continue the sync process without retrying.
    /// </summary>
    Func<SyncFunctionInput<SyncBatchCompleteInput<TDestination>>, ValueTask<bool?>> BatchRetry { get; }

    /// <summary>
    /// Return true to continue the sync process,
    /// Return false to stop the sync process.
    /// </summary>
    Func<SyncFunctionInput<SyncBatchCompleteInput<TDestination>>, ValueTask<bool>> BatchCompleted { get; }

    /// <summary>
    /// Return true to mark the sync process as Success,
    /// Return false to mark the sync process as Failed.
    /// </summary>
    Func<SyncFunctionInput<SyncStoreDataResult<TDestination>>, ValueTask<bool>> Completed { get; }


    ISyncService<TSource, TDestination> Configure(
        int? batchSize = null,
        int? retryCount = 0,
        int operationTimeoutInSeconds = 300,
        Func<IEnumerable<TSource>, SyncActionType, ValueTask<IEnumerable<TDestination>>>? mapping = null,
        string? syncId = null);

    ISyncService<TSource, TDestination> SetupPreparing(Func<SyncFunctionInput, ValueTask<bool>> preparingFunc);

    ISyncService<TSource, TDestination> SetupSourceTotalItemCount(Func<SyncFunctionInput<SyncActionType>, ValueTask<long?>?>? sourceTotalItemCountFunc);

    ISyncService<TSource, TDestination> SetupGetSourceBatchItems(Func<SyncFunctionInput<SyncGetBatchDataInput<TSource>>, ValueTask<IEnumerable<TSource?>?>?> sourceBatchItemsFunc);

    ISyncService<TSource, TDestination> SetupStoreBatchData(Func<SyncFunctionInput<SyncStoreDataInput<TDestination>>, ValueTask<SyncStoreDataResult<TDestination>>> storeBatchDataFunc);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchRetryFunc">
    /// Return true to continue retrying or the last retry to continue the sync process,
    /// Return false to stop the sync process,
    /// Return null to continue the sync process without retrying. 
    /// </param>
    /// <returns></returns>
    ISyncService<TSource, TDestination> SetupBatchRetry(Func<SyncFunctionInput<SyncBatchCompleteInput<TDestination>>, ValueTask<bool?>> batchRetryFunc);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchCompletedFunc">
    /// Return true to continue the sync process,
    /// Return false to stop the sync process.
    /// </param>
    /// <returns></returns>
    ISyncService<TSource, TDestination> SetupBatchCompleted(Func<SyncFunctionInput<SyncBatchCompleteInput<TDestination>>, ValueTask<bool>> batchCompletedFunc);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="completedFunc">
    /// Return true to mark the sync process as Success,
    /// Return false to mark the sync process as Failed.
    /// </param>
    /// <returns></returns>
    ISyncService<TSource, TDestination> SetupCompleted(Func<SyncFunctionInput<SyncStoreDataResult<TDestination>>, ValueTask<bool>> completedFunc);

    CancellationToken GetCancellationToken();

    ValueTask Reset();

    Task<bool> RunAsync();
}
