namespace ShiftSoftware.ADP.SyncAgent;

public class SyncBatchCompleteRetryInput<TSource, TDestination> 
    where TDestination : class
    where TSource : class
{
    public IEnumerable<TSource?>? SourceItems { get; private set; }
    public SyncStoreDataResult<TDestination>? StoreDataResult { get; private set; }
    public SyncActionStatus Status { get; private set; }
    public Exception? Exception { get; set; }

    public SyncBatchCompleteRetryInput(
        IEnumerable<TSource?>? sourceItems,
        SyncStoreDataResult<TDestination>? storeDataResult,
        SyncActionStatus status,
        Exception? exception)
    {
        this.SourceItems = sourceItems;
        StoreDataResult = storeDataResult;
        Status = status;
        Exception = exception;
    }
}
