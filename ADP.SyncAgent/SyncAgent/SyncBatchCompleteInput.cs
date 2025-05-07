namespace ShiftSoftware.ADP.SyncAgent;

public class SyncBatchCompleteInput<T> where T : class
{
    public SyncStoreDataResult<T> SyncStoreDataResult { get; private set; }
    public SyncActionStatus Status { get; private set; }

    public SyncBatchCompleteInput(
        SyncStoreDataResult<T> syncStoreDataResult,
        SyncActionStatus status)
    {
        SyncStoreDataResult = syncStoreDataResult;
        Status = status;
    }
}
