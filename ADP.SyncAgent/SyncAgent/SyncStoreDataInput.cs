namespace ShiftSoftware.ADP.SyncAgent;

public class SyncStoreDataInput<T> where T : class
{
    public IEnumerable<T?>? Items { get; private set; }
    public SyncActionStatus Status { get; private set; }

    public SyncStoreDataInput(
        IEnumerable<T?>? items,
        SyncActionStatus status)
    {
        Items = items;
        Status = status;
    }
}
