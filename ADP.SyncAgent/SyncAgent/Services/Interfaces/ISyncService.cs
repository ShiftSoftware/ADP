namespace ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

public interface ISyncService<TSource, TDestination> : IDisposable
    where TSource : class, new()
    where TDestination : class, new()
{
    SyncConfigurations<TSource, TDestination>? SyncConfigurations { get; }
    Func<ValueTask<bool>>? Preparing { get; }
    Func<SyncActionType, ValueTask<long?>?>? GetSourceTotalItemCount { get; }
    Func<SyncActionStatus, ValueTask<IEnumerable<TSource?>?>?> GetSourceBatchItems { get; }
    Func<SyncStoreDataInput<TDestination>, ValueTask<SyncStoreDataResult<TDestination>>>? StoreBatchData { get; }
    Func<SyncStoreDataResult<TDestination>, ValueTask<bool>>? BatchCompleted { get; }
    Func<SyncStoreDataResult<TDestination>, ValueTask<bool>>? Completed { get; }


    ISyncService<TSource, TDestination> Configure(
        int? batchSize = null,
        int? retryCount = 0,
        int operationTimeoutInSeconds = 300,
        Func<IEnumerable<TSource>, SyncActionType, ValueTask<IEnumerable<TDestination>>>? mapping = null,
        string? syncId = null);

    ISyncService<TSource, TDestination> SetupPreparing(Func<SyncFunctionInput, ValueTask<bool>> preparingFunc);

    ISyncService<TSource, TDestination> SetupSourceTotalItemCount(Func<SyncFunctionInput<SyncActionType>, ValueTask<long?>?>? sourceTotalItemCountFunc);

    ISyncService<TSource, TDestination> SetupGetSourceBatchItems(Func<SyncFunctionInput<SyncActionStatus>, ValueTask<IEnumerable<TSource?>?>?> sourceBatchItemsFunc);

    ISyncService<TSource, TDestination> SetupStoreBatchData(Func<SyncFunctionInput<SyncStoreDataInput<TDestination>>, ValueTask<SyncStoreDataResult<TDestination>>> storeBatchDataFunc);

    ISyncService<TSource, TDestination> SetupBatchCompleted(Func<SyncFunctionInput<SyncBatchCompleteInput<TDestination>>, ValueTask<bool>> batchCompletedFunc);

    ISyncService<TSource, TDestination> SetupCompleted(Func<SyncFunctionInput<SyncStoreDataResult<TDestination>>, ValueTask<bool>> completedFunc);

    CancellationToken GetCancellationToken();

    ValueTask Reset();

    Task RunAsync();
}
