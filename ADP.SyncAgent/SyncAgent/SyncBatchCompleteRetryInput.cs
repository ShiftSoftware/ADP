namespace ShiftSoftware.ADP.SyncAgent;

public class SyncBatchCompleteRetryInput<TSource, TDestination> 
    where TDestination : class, new()
    where TSource : class, new()
{
    public IEnumerable<TSource?>? SourceItems { get; private set; }
    public SyncStoreDataResult<TDestination>? StoreDataResult { get; private set; }
    public SyncActionStatus Status { get; private set; }

    public SyncBatchCompleteRetryInput(
        IEnumerable<TSource?>? sourceItems,
        SyncStoreDataResult<TDestination>? storeDataResult,
        SyncActionStatus status)
    {
        this.SourceItems = sourceItems;
        StoreDataResult = storeDataResult;
        Status = status;
    }
}
