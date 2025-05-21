namespace ShiftSoftware.ADP.SyncAgent;

public class SyncBatchCompleteRetryInput<TSource, TDestination> 
    where TDestination : class
    where TSource : class
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
