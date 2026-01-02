namespace ShiftSoftware.ADP.SyncAgent;

public class RetryException(Exception ex) : Exception
{
    public override string ToString()
    {
        return $"Retry Required: {ex.ToString()}";
    }
}

public class SyncStoreDataResult<T> where T : class
{
    public IEnumerable<T?>? SucceededItems { get; set; }
    public IEnumerable<T?>? FailedItems { get; set; }
    public IEnumerable<T?>? SkippedItems { get; set; }

    
    public SyncStoreDataResultType ResultType =>
        (SucceededItems, FailedItems, SkippedItems) switch
        {
            // success and fail items not null and but both empty
            (IEnumerable<T?> s, IEnumerable<T?> f, _)
                when !(s?.Any() ?? true) && !(f?.Any() ?? true)
                    => SyncStoreDataResultType.Succeeded,
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

    public SyncStoreDataResult(IEnumerable<T?>? succededItems, IEnumerable<T?>? failedItems, IEnumerable<T?>? skippedItems, RetryException retry) : this(retry)
    {
        this.FailedItems = failedItems;
        this.SucceededItems = succededItems;
        this.SkippedItems = skippedItems;
    }

    /// <summary>
    /// Set true to trigger retry proccess, null to skip the retry process
    /// </summary>
    public RetryException? RetryException { get; set; }

    public SyncStoreDataResult(RetryException retryException)
    {
        this.RetryException = retryException;
    }

    public SyncStoreDataResult()
    {

    }

    public bool IsEligibleToUseItAsRetryInput(long? expectedCount)
    {
        return (SucceededItems?.LongCount() ?? 0) + (FailedItems?.LongCount() ?? 0) + (SkippedItems?.LongCount() ?? 0) == expectedCount;
    }
}
