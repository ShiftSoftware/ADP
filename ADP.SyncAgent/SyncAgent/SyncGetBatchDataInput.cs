namespace ShiftSoftware.ADP.SyncAgent;

public class SyncGetBatchDataInput<T> where T : class
{
    /// <summary>
    /// In retry this contains the previous items for the same step,
    /// otherwise it will be null
    /// </summary>
    public IEnumerable<T?>? PreviousItems { get; private set; }
    public SyncActionStatus Status { get; private set; }

    internal SyncGetBatchDataInput(IEnumerable<T?>?  previousItems, SyncActionStatus status)
    {
       this.PreviousItems = previousItems;
       this.Status = status;
    }
}
