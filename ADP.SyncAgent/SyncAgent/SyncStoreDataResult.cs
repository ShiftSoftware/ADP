namespace ShiftSoftware.ADP.SyncAgent;

public class SyncStoreDataResult<T> where T : class
{
    public IEnumerable<T?>? SucceededItems { get; private set; }
    public IEnumerable<T?>? FailedItems { get; private set; }
    public SyncStoreDataResultType ResultType
    =>
        (SucceededItems?.Any() ?? false) == false && (FailedItems?.Any() ?? false) == false ? SyncStoreDataResultType.Skipped :
        SucceededItems?.Any() == true && (FailedItems?.Any() ?? false) == false ? SyncStoreDataResultType.Succeeded :
        (SucceededItems?.Any() ?? false) && FailedItems?.Any() ==  true ? SyncStoreDataResultType.Failed :
        SyncStoreDataResultType.Partial;

    public SyncStoreDataResult(IEnumerable<T?>? succeededItems, IEnumerable<T?>? failedItems)
    {
        SucceededItems = succeededItems;
        FailedItems = failedItems;
    }
}

public enum SyncStoreDataResultType
{
    Succeeded,
    Failed,
    Partial,
    Skipped
}
