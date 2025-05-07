namespace ShiftSoftware.ADP.SyncAgent;

public class SyncGetBatchDataInput<T> where T : class
{
    public IEnumerable<T>? PreviousBatchItems { get; private set; }
    public SyncActionStatus Status { get; private set; }
}
