namespace ShiftSoftware.ADP.SyncAgent;

public class SyncStoreDataResult<T> where T : class
{
    public IEnumerable<T?>? SucceededItems { get; set; }
    public IEnumerable<T?>? FailedItems { get; set; }

    /// <summary>
    /// Set true to trigger retry proccess, false to skip the retry process
    /// </summary>
    public bool NeedRetry { get; set; }

    public SyncStoreDataResultType ResultType =>
        (SucceededItems, FailedItems) switch
        {
            (null, _) => SyncStoreDataResultType.Failed,
            (_, null) => SyncStoreDataResultType.Failed,
            (IEnumerable<T?> s, IEnumerable<T?> f) when !s.Any() && f.Any() => SyncStoreDataResultType.Failed,
            (IEnumerable<T?> s, IEnumerable<T?> f) when s.Any() && !f.Any() => SyncStoreDataResultType.Succeeded,
            (IEnumerable<T?> s, IEnumerable<T?> f) when s.Any() && f.Any() => SyncStoreDataResultType.Partial,
            _ => SyncStoreDataResultType.Failed
        };

    public SyncStoreDataResult(IEnumerable<T?>? succededItems, IEnumerable<T?>? failedItems, bool retry) : this(retry)
    {
        this.FailedItems = failedItems;
        this.SucceededItems = succededItems;
    }

    public SyncStoreDataResult(bool retry)
    {
        this.NeedRetry = retry;
    }

    public SyncStoreDataResult()
    {
        
    }
}
