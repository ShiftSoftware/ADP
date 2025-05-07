namespace ShiftSoftware.ADP.SyncAgent;

public class SyncConfigurations<TCSV, TData>
    where TCSV : class
    where TData : class
{
    public long? BatchSize { get; private set; }
    public long? RetryCount { get; private set; }
    public long OperationTimeoutInSeconds { get; private set; }

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
        string? syncId)
    {
        BatchSize = batchSize;
        RetryCount = retryCount;
        OperationTimeoutInSeconds = operationTimeoutInSecond;
        Mapping = mapping;
        SyncId = syncId;
    }
}
