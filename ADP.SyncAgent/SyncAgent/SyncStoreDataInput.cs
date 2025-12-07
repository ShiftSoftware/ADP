namespace ShiftSoftware.ADP.SyncAgent;

public class SyncStoreDataInput<T> where T : class
{
    public IEnumerable<T?>? Items { get; private set; }

    /// <summary>
    /// In retry this contains the previous result of the storing of the same step,
    /// otherwise it will be null.
    /// </summary>
    public SyncStoreDataResult<T>? PreviousResult { get; private set; }
    public SyncActionStatus Status { get; private set; }

    internal SyncStoreDataInput(
        IEnumerable<T?>? items,
        SyncStoreDataResult<T>? previousResult,
        SyncActionStatus status)
    {
        Items = items;
        this.PreviousResult = previousResult;
        Status = status;
    }
}
