namespace ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

public interface ISyncDataAdapter<TSource, TDestination> : IAsyncDisposable
    where TSource : class, new()
    where TDestination : class, new()
{
    public ISyncService<TSource, TDestination> SyncService { get; }


    public ISyncDataAdapter<TSource, TDestination> SetSyncService(ISyncService<TSource, TDestination> syncService);
    public ValueTask<bool> Preparing(SyncFunctionInput input);
    public ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input);
    public ValueTask<IEnumerable<TSource?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TSource>> input);
    public ValueTask<IEnumerable<TDestination?>?> Mapping(SyncFunctionInput<SyncMappingInput<TSource, TDestination>> input);
    public ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input);
    public ValueTask<RetryAction> BatchRetry(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input);
    public ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input);
    public ValueTask OperationCompleted(SyncFunctionInput input);
    public ValueTask Failed();
    public ValueTask Succeeded();
    public ValueTask Finished();
    ValueTask Reset();
}
