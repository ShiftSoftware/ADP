namespace ShiftSoftware.ADP.SyncAgent;

public class SyncStoreDataInput<T> where T : class
{
    public IEnumerable<T?>? Items { get; private set; }

    /// <summary>
    /// In retry this contains the previous result of the storing of the same step,
    /// otherwise it will be null.
    /// </summary>
    public SyncStoreDataResult<T>? PreviousResult { get; set; }
    public SyncActionStatus Status { get; private set; }

    public SyncStoreDataInput(
        IEnumerable<T?>? items,
        SyncStoreDataResult<T>? previousResult,
        SyncActionStatus status)
    {
        Items = items;
        this.PreviousResult = previousResult;
        Status = status;
    }
}
