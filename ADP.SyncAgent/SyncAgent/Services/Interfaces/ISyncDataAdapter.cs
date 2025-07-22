namespace ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

public interface ISyncDataAdapter<TSource, TDestination, TConfigurations, TSelf> : ISyncDataAdapter<TSource, TDestination, TSelf>, IAsyncDisposable
    where TSource : class
    where TDestination : class
    where TSelf : ISyncDataAdapter<TSource, TDestination, TConfigurations, TSelf>
{
    public TConfigurations? Configurations { get; }

    /// <summary>
    /// To avoid unexpected behavior, call the destination adapter's Configure method after the source adapter is configured.
    /// </summary>
    /// <param name="configurations"></param>
    /// <param name="configureSyncService">
    /// If set false just configure DataAdapter and skip the configuration of the SyncEngine, 
    /// then you may be configure SyncEngine by your self
    /// </param>
    /// <returns></returns>
    public ISyncEngine<TSource, TDestination> Configure(TConfigurations configurations, bool configureSyncService = true);
}

public interface ISyncDataAdapter<TSource, TDestination, TSelf> : IAsyncDisposable
    where TSource : class
    where TDestination : class
    where TSelf : ISyncDataAdapter<TSource, TDestination, TSelf>
{
    public ISyncEngine<TSource, TDestination> SyncService { get; }

    public TSelf SetSyncService(ISyncEngine<TSource, TDestination> syncService);

    public ValueTask<SyncPreparingResponseAction> Preparing(SyncFunctionInput input);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns>True for mark the action as success, false to mark it as fail</returns>
    public ValueTask<bool> ActionStarted(SyncFunctionInput<SyncActionType> input);

    public ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input);
    public ValueTask<IEnumerable<TSource?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TSource>> input);
    public ValueTask<IEnumerable<TDestination?>?> AdvancedMapping(SyncFunctionInput<SyncMappingInput<TSource, TDestination>> input);
    public ValueTask<IEnumerable<TDestination?>?> Mapping(IEnumerable<TSource?>? sourceItems, SyncActionType actionType);
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

    public ValueTask Failed(SyncFunctionInput input);
    public ValueTask Succeeded(SyncFunctionInput input);
    public ValueTask Finished(SyncFunctionInput input);
    ValueTask Reset();
}
