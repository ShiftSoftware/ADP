namespace ShiftSoftware.ADP.SyncAgent;

public class SyncConfigurations
{
    public long? BatchSize { get; private set; }
    public long MaxRetryCount { get; private set; }
    public long OperationTimeoutInSeconds { get; private set; }
    public IEnumerable<SyncActionType> ActionExecutionAndOrder { get; private set; } = [SyncActionType.Delete, SyncActionType.Update, SyncActionType.Add];
    public RetryAction DefaultRetryAction { get; private set; }

    public SyncConfigurations(
        long? batchSize,
        long maxRetryCount,
        long operationTimeoutInSecond,
        RetryAction defaultRetryAction)
    {
        BatchSize = batchSize;
        MaxRetryCount = maxRetryCount;
        OperationTimeoutInSeconds = operationTimeoutInSecond;
        DefaultRetryAction = defaultRetryAction;
    }

    public SyncConfigurations(
        long? batchSize,
        long maxRetryCount,
        long operationTimeoutInSecond,
        RetryAction defaultRetryAction,
        IEnumerable<SyncActionType> actionExecutionAndOrder): this(batchSize, maxRetryCount, operationTimeoutInSecond, defaultRetryAction)
    {
        ActionExecutionAndOrder = actionExecutionAndOrder;
    }
}
