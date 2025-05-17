namespace ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

public interface ISyncDataAdapter<TSource, TDestination, TConfigurations, TImplementer> : IAsyncDisposable
    where TSource : class, new()
    where TDestination : class, new()
    where TImplementer : ISyncDataAdapter<TSource, TDestination, TConfigurations, TImplementer>
{
    public ISyncService<TSource, TDestination> SyncService { get; }
    public TConfigurations? Configurations { get; }


    public TImplementer SetSyncService(ISyncService<TSource, TDestination> syncService);
    
    /// <summary>
    /// To avoid unexpected behavior, call the destination adapter's Configure method after the source adapter is configured.
    /// </summary>
    /// <param name="configurations"></param>
    /// <param name="configureSyncService">
    /// If set false just configure DataAdapter and skip the configuration of the SyncService, 
    /// then you may be configure SyncService by your self
    /// </param>
    /// <returns></returns>
    public ISyncService<TSource, TDestination> Configure(TConfigurations configurations, bool configureSyncService = true);

    public ValueTask<bool> Preparing(SyncFunctionInput input);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns>True for mark the action as success, false to mark it as fail</returns>
    public ValueTask<bool> ActionStarted(SyncFunctionInput<SyncActionType> input);

    public ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input);
    public ValueTask<IEnumerable<TSource?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TSource>> input);
    public ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input);
    public ValueTask<RetryAction> BatchRetry(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns>
    /// Return true to continue the sync process,
    /// Return false to retry.
    /// </returns>
    public ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns>True for mark the action as success, false to mark it as fail</returns>
    public ValueTask<bool> ActionCompleted(SyncFunctionInput<SyncActionCompletedInput> input);

    public ValueTask Failed();
    public ValueTask Succeeded();
    public ValueTask Finished();
    ValueTask Reset();
}
