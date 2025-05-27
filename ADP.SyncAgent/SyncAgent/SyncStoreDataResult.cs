namespace ShiftSoftware.ADP.SyncAgent;

public class SyncStoreDataResult<T> where T : class
{
    public IEnumerable<T?>? SucceededItems { get; set; }
    public IEnumerable<T?>? FailedItems { get; set; }
    public IEnumerable<T?>? SkippedItems { get; set; }

    /// <summary>
    /// Set true to trigger retry proccess, false to skip the retry process
    /// </summary>
    public bool NeedRetry { get; set; }

    public SyncStoreDataResultType ResultType =>
        (SucceededItems, FailedItems, SkippedItems) switch
        {
            (null, _, _) => SyncStoreDataResultType.Failed,
            (_, null, _) => SyncStoreDataResultType.Failed,
            (IEnumerable<T?> s, IEnumerable<T?> f, IEnumerable<T?> k)
                when !(s?.Any() ?? false) && !(f?.Any() ?? false) && (k?.Any() ?? false)
                    => SyncStoreDataResultType.Skipped,
            (IEnumerable<T?> s, IEnumerable<T?> f, _)
                when !(s?.Any() ?? false) && (f?.Any() ?? false)
                    => SyncStoreDataResultType.Failed,
            (IEnumerable<T?> s, IEnumerable<T?> f, _)
                when (s?.Any() ?? false) && !(f?.Any() ?? false)
                    => SyncStoreDataResultType.Succeeded,
            (IEnumerable<T?> s, IEnumerable<T?> f, _)
                when (s?.Any() ?? false) && (f?.Any() ?? false)
                    => SyncStoreDataResultType.Partial,
            _ => SyncStoreDataResultType.Failed
        };

    public SyncStoreDataResult(IEnumerable<T?>? succededItems, IEnumerable<T?>? failedItems, IEnumerable<T?>? skippedItems, bool retry) : this(retry)
    {
        this.FailedItems = failedItems;
        this.SucceededItems = succededItems;
        this.SkippedItems = skippedItems;
    }

    public SyncStoreDataResult(bool retry)
    {
        this.NeedRetry = retry;
    }

    public SyncStoreDataResult()
    {
        
    }

    public bool IsEligibleToUseItAsRetryInput(long? expectedCount)
    {
        return (SucceededItems?.LongCount() ?? 0) + (FailedItems?.LongCount() ?? 0) + (SkippedItems?.LongCount() ?? 0) == expectedCount;
    }
}
