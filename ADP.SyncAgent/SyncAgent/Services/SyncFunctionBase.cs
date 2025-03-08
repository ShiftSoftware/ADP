using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent.Services;

public class SyncFunctionBase<TFunction, TCSV, TCosmos>
    where TCSV : CacheableCSV
    where TCosmos : class
{
    private readonly ILogger<TFunction> _logger;
    private readonly CSVSyncService<TCSV, TCosmos> syncAgentService;

    public SyncFunctionBase(ILoggerFactory loggerFactory, ISyncProgressIndicator? syncProgressIndicator, CSVSyncServiceFactory repositoryServiceFactory)
    {
        _logger = loggerFactory.CreateLogger<TFunction>();
        syncAgentService = repositoryServiceFactory.Create<TCSV, TCosmos>(_logger, syncProgressIndicator);
    }

    public async Task RunAsync(
        string csvFileName,
        FileLocationSetting fileLocationSetting,
        string databaseId,
        string containerId,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression = null,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression = null,
        Func<IEnumerable<TCSV>, CosmosActionType, ValueTask<IEnumerable<TCosmos>>>? mapping = null,
        Func<SyncCosmosAction<TCosmos>, ValueTask<SyncCosmosAction<TCosmos>?>>? cosmosAction = null,
        int? batchSize = null,
        int? retryCount = 0,
        int operationTimeoutInSecond = 300,
        string? syncId = null)
    {
        await RunAsync(
            csvFileName,
            fileLocationSetting.SourceContainerOrShareName,
            fileLocationSetting.SourceDirectory,
            fileLocationSetting.DestinationContainerOrShareName,
            fileLocationSetting.DestinationDirectory,
            databaseId, containerId,
            partitionKeyLevel1Expression,
            partitionKeyLevel2Expression,
            partitionKeyLevel3Expression,
            mapping,
            cosmosAction,
            batchSize,
            retryCount,
            operationTimeoutInSecond,
            syncId
        );
    }

    public async Task RunAsync(
        string csvFileName,
        string? sourceDirectory,
        string? destinationDirectory,
        string databaseId,
        string containerId,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression = null,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression = null,
        Func<IEnumerable<TCSV>, CosmosActionType, ValueTask<IEnumerable<TCosmos>>>? mapping = null,
        Func<SyncCosmosAction<TCosmos>, ValueTask<SyncCosmosAction<TCosmos>?>>? cosmosAction = null,
        int? batchSize = null,
        int? retryCount = 0,
        int operationTimeoutInSecond = 300,
        string? syncId = null)
    {
        await RunAsync(
            csvFileName,
            null,
            sourceDirectory,
            null,
            destinationDirectory,
            databaseId, containerId,
            partitionKeyLevel1Expression,
            partitionKeyLevel2Expression,
            partitionKeyLevel3Expression,
            mapping,
            cosmosAction,
            batchSize,
            retryCount,
            operationTimeoutInSecond,
            syncId
        );
    }

    public async Task RunAsync(
        string csvFileName,
        string? sourceContainerOrShareName,
        string? sourceDirectory,
        string? destinationContainerOrShareName,
        string? destinationDirectory,
        string databaseId,
        string containerId,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression = null,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression = null,
        Func<IEnumerable<TCSV>, CosmosActionType, ValueTask<IEnumerable<TCosmos>>>? mapping = null,
        Func<SyncCosmosAction<TCosmos>, ValueTask<SyncCosmosAction<TCosmos>?>>? cosmosAction = null,
        int? batchSize = null,
        int? retryCount = 0,
        int operationTimeoutInSecond = 300,
        string? syncId = null)
    {
        await syncAgentService.StartSyncAsync(
            csvFileName,
            sourceContainerOrShareName,
            sourceDirectory,
            destinationContainerOrShareName,
            destinationDirectory,
            databaseId,
            containerId,
            partitionKeyLevel1Expression,
            partitionKeyLevel2Expression,
            partitionKeyLevel3Expression,
            mapping,
            cosmosAction,
            batchSize,
            retryCount,
            operationTimeoutInSecond,
            syncId
        );
    }
}

public class SyncFunctionBase<TFunction, T> : SyncFunctionBase<TFunction, T, T>
    where T : CacheableCSV
{
    public SyncFunctionBase(ILoggerFactory loggerFactory, ISyncProgressIndicator syncProgressIndicator, CSVSyncServiceFactory repositoryServiceFactory)
        : base(loggerFactory, syncProgressIndicator, repositoryServiceFactory)
    {
    }
}