namespace ShiftSoftware.ADP.SyncAgent;

public class SyncConfigurations
{
    public long? BatchSize { get; private set; }
    public long MaxRetryCount { get; private set; }
    public long OperationTimeoutInSeconds { get; private set; }
    public IEnumerable<SyncActionType> ActionExecutionAndOrder { get; private set; } = [SyncActionType.Delete, SyncActionType.Update, SyncActionType.Add];

    public SyncConfigurations(
        long? batchSize,
        long maxRetryCount,
        long operationTimeoutInSecond)
    {
        BatchSize = batchSize;
        MaxRetryCount = maxRetryCount;
        OperationTimeoutInSeconds = operationTimeoutInSecond;
    }

    public SyncConfigurations(
        long? batchSize,
        long maxRetryCount,
        long operationTimeoutInSecond,
        IEnumerable<SyncActionType> actionExecutionAndOrder): this(batchSize, maxRetryCount, operationTimeoutInSecond)
    {
        ActionExecutionAndOrder = actionExecutionAndOrder;
    }
}
