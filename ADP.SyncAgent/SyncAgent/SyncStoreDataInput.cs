namespace ShiftSoftware.ADP.SyncAgent;

public class SyncStoreDataInput<T> where T : class
{
    public IEnumerable<T>? Items { get; private set; }
    public SyncActionStatus SyncActionStatus { get; private set; }

    public SyncStoreDataInput(
        IEnumerable<T>? items,
        SyncActionStatus syncActionStatus)
    {
        Items = items;
        SyncActionStatus = syncActionStatus;
    }
}
