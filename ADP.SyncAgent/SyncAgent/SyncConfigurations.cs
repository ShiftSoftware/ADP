namespace ShiftSoftware.ADP.SyncAgent;

public class SyncConfigurations
{
    public long BatchSize { get; private set; }
    public long MaxRetryCount { get; private set; }
    public long OperationTimeoutInSeconds { get; private set; }

    public SyncConfigurations(
        int batchSize,
        int maxRetryCount,
        int operationTimeoutInSecond)
    {
        BatchSize = batchSize;
        MaxRetryCount = maxRetryCount;
        OperationTimeoutInSeconds = operationTimeoutInSecond;
    }
}
