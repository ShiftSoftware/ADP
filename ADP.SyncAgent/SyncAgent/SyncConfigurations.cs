namespace ShiftSoftware.ADP.SyncAgent;

public class SyncConfigurations<TCSV, TData>
    where TCSV : class
    where TData : class
{
    public long? BatchSize { get; private set; }
    public long? RetryCount { get; private set; }
    public long OperationTimeoutInSeconds { get; private set; }

    /// <summary>
    /// If ture, when one setp is failed it try to read the data again from the source, but if false, it uses the last read data from the source.
    /// </summary>
    public bool TryToReadDataAgainWhenRetry { get; private set; }

    public string? SyncId { get; private set; }
    public Func<IEnumerable<TCSV>, SyncActionType, ValueTask<IEnumerable<TData>>>? Mapping { get; private set; }

    public SyncConfigurations()
    {

    }

    public SyncConfigurations(
        int? batchSize,
        int? retryCount,
        int operationTimeoutInSecond,
        Func<IEnumerable<TCSV>, SyncActionType, ValueTask<IEnumerable<TData>>>? mapping,
        string? syncId,
        bool tryToReadDataAgainWhenRetry)
    {
        BatchSize = batchSize;
        RetryCount = retryCount;
        OperationTimeoutInSeconds = operationTimeoutInSecond;
        Mapping = mapping;
        SyncId = syncId;
        TryToReadDataAgainWhenRetry = tryToReadDataAgainWhenRetry;
    }
}
