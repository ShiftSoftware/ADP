namespace ShiftSoftware.ADP.SyncAgent;

public class SyncStoreDataResult<T> where T : class
{
    public IEnumerable<T?>? SucceededItems { get; set; }
    public IEnumerable<T?>? FailedItems { get; set; }
    public bool Retry { get; set; }
    public SyncStoreDataResultType ResultType
    =>
        (SucceededItems?.Any() ?? false) == false && (FailedItems?.Any() ?? false) == false ? SyncStoreDataResultType.Skipped :
        SucceededItems?.Any() == true && (FailedItems?.Any() ?? false) == false ? SyncStoreDataResultType.Succeeded :
        (SucceededItems?.Any() ?? false) && FailedItems?.Any() ==  true ? SyncStoreDataResultType.Failed :
        SyncStoreDataResultType.Partial;
}
