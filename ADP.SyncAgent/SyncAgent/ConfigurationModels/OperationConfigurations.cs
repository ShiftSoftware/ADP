namespace ShiftSoftware.ADP.SyncAgent.ConfigurationModels;

internal class OperationConfigurations
{
    public int? BatchSize { get; set; }
    public int? RetryCount { get; set; }
    public int OperationTimeoutInSeconds { get; set; }
    public OperationConfigurations()
    {

    }
    public OperationConfigurations(int? batchSize, int? retryCount, int operationTimeoutInSecond)
    {
        BatchSize = batchSize;
        RetryCount = retryCount;
        OperationTimeoutInSeconds = operationTimeoutInSecond;
    }
}
