namespace ShiftSoftware.ADP.SyncAgent.ConfigurationModels;

internal class SyncConfigurations<TCSV, TData>
    where TCSV : CacheableCSV
    where TData : class
{
    public int? BatchSize { get; set; }
    public int? RetryCount { get; set; }
    public int OperationTimeoutInSeconds { get; set; }

    public string? SyncId { get; set; } = null;
    public Func<IEnumerable<TCSV>, DataProcessActionType, ValueTask<IEnumerable<TData>>>? Mapping { get; set; }

    public SyncConfigurations()
    {

    }

    public SyncConfigurations(
        int? batchSize,
        int? retryCount,
        int operationTimeoutInSecond,
        Func<IEnumerable<TCSV>, DataProcessActionType, ValueTask<IEnumerable<TData>>>? mapping,
        string? syncId)
    {
        BatchSize = batchSize;
        RetryCount = retryCount;
        OperationTimeoutInSeconds = operationTimeoutInSecond;
        Mapping = mapping;
        SyncId = syncId;
    }
}
