namespace ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

public interface ISyncDataAdapter<TSource, TDestination, TImplementer> : IAsyncDisposable
    where TSource : class, new()
    where TDestination : class, new()
    where TImplementer : ISyncDataAdapter<TSource, TDestination, TImplementer>
{
    public ISyncService<TSource, TDestination> SyncService { get; }


    public TImplementer SetSyncService(ISyncService<TSource, TDestination> syncService);
    public ValueTask<bool> Preparing(SyncFunctionInput input);
    public ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input);
    public ValueTask<IEnumerable<TSource?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TSource>> input);
    public ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input);
    public ValueTask<RetryAction> BatchRetry(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input);
    public ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input);
    public ValueTask ActionCompleted(SyncFunctionInput<SyncActionType> input);
    public ValueTask Failed();
    public ValueTask Succeeded();
    public ValueTask Finished();
    ValueTask Reset();
}
