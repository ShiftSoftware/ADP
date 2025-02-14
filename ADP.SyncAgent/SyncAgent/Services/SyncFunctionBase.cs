using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.Models;
using System.Linq.Expressions;

namespace ADP.SyncAgent.Services;

public class SyncFunctionBase<TFunction, TCSV, TCosmos>
    where TCSV : CacheableCSV
    where TCosmos : class
{
    private readonly ILogger<TFunction> _logger;
    private readonly CSVSyncService<TCSV, TCosmos> syncAgentService;

    public SyncFunctionBase(ILoggerFactory loggerFactory, CSVSyncServiceFactory repositoryServiceFactory)
    {
        _logger = loggerFactory.CreateLogger<TFunction>();
        syncAgentService = repositoryServiceFactory.Create<TCSV, TCosmos>();
    }

    public async Task RunAsync(
        string csvFileName,
        FileLocationSetting fileLocationSetting,
        string databaseId,
        string containerId,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression = null,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression = null,
        Func<List<TCSV>, CosmosActionType, ValueTask<List<TCosmos>>>? mapping = null,
        Func<SyncCosmosAction<TCosmos>, ValueTask<SyncCosmosAction<TCosmos>?>>? cosmosAction = null)
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
            cosmosAction
        );
    }

    public async Task<string> RunAsync(
        string csvFileName, 
        string? sourceDirectory,
        string? destinationDirectory,
        string databaseId, 
        string containerId,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression = null,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression = null,
        Func<List<TCSV>, CosmosActionType, ValueTask<List<TCosmos>>>? mapping = null,
        Func<SyncCosmosAction<TCosmos>, ValueTask<SyncCosmosAction<TCosmos>?>>? cosmosAction = null)
    {
        return await RunAsync(
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
            cosmosAction
        );
    }

    public async Task<string> RunAsync(
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
        Func<List<TCSV>, CosmosActionType, ValueTask<List<TCosmos>>>? mapping = null,
        Func<SyncCosmosAction<TCosmos>, ValueTask<SyncCosmosAction<TCosmos>?>>? cosmosAction = null)
    {
        using (_logger.BeginScope("Syncing {csvFileName}", csvFileName))
        {
            _logger.LogInformation("Processing Files");

            await syncAgentService.ProcessFilesAsync(csvFileName, sourceContainerOrShareName, sourceDirectory, destinationContainerOrShareName, destinationDirectory);

            _logger.LogInformation("Processing Cosmos DB");

            await syncAgentService.ProcessCosmosAsync(
                destinationRelativePath: Path.Combine(destinationDirectory??"", csvFileName),
                destinationContainerOrShareName: destinationContainerOrShareName,
                databaseId: databaseId,
                containerId: containerId,
                mapping: mapping,
                partitionKeyLevel1Expression: partitionKeyLevel1Expression,
                partitionKeyLevel2Expression: partitionKeyLevel2Expression,
                partitionKeyLevel3Expression: partitionKeyLevel3Expression,
                cosmosAction: cosmosAction
            );

            var syncResult = syncAgentService.GetSyncResult();

            _logger.LogInformation(
                """
                
                -----------------------------------------------------------------------------------------
                Selected File:                          {file}
                Number of Added Lines:                  {ToInsert}
                Number of Deleted Lines:                {ToDelete}
                Original (Synced) File Download Time:   {TimeToDownloadOriginalFile}
                New File Download Time:                 {TimeToDownloadNewFile}
                Git Diff Process Time:                  {TimeToCompareWithGit}
                Write To Insert File Time:              {TimeToWriteToInsertFile}
                Write To Delete File Time:              {TimeToWriteToDeleteFile}
                Parse (To Insert File) Time:            {TimeToParseToInsertFile}
                Parse (To Delete File) Time:            {TimeToParseToDeleteFile}
                Initialize Cosmos Client Time:          {TimeToInitializeCosmosClient}
                Sync To Cosmos Time:                    {TimeToSyncToCosmos}
                Cosmos DB Inserted Count:               {CosmosDBInsertedCount}
                Cosmos DB Deleted Count:                {CosmosDBDeletedCount}
                Status:                                 {Status}
                Success:                                {Success}
                -----------------------------------------------------------------------------------------

                """
                ,
                csvFileName,
                syncResult.ToInsertCount,
                syncResult.ToDeleteCount,
                syncResult.TimeToDownloadOriginalFile,
                syncResult.TimeToDownloadNewFile,
                syncResult.TimeToCompareWithGit,
                syncResult.TimeToWriteToInsertFile,
                syncResult.TimeToWriteToDeleteFile,
                syncResult.TimeToParseToInsertFile,
                syncResult.TimeToParseToDeleteFile,
                syncResult.TimeToInitializeCosmosClient,
                syncResult.TimeToSyncToCosmos,
                syncResult.CosmosDBInsertedCount,
                syncResult.CosmosDBDeletedCount,
                syncResult.Status,
                syncResult.Success
            );

            return $"""
                
                -----------------------------------------------------------------------------------------
                Selected File:                          {csvFileName}
                Number of Added Lines:                  {syncResult.ToInsertCount}
                Number of Deleted Lines:                {syncResult.ToDeleteCount}
                Original (Synced) File Download Time:   {syncResult.TimeToDownloadOriginalFile}
                New File Download Time:                 {syncResult.TimeToDownloadNewFile}
                Git Diff Process Time:                  {syncResult.TimeToCompareWithGit}
                Write To Insert File Time:              {syncResult.TimeToWriteToInsertFile}
                Write To Delete File Time:              {syncResult.TimeToWriteToDeleteFile}
                Parse (To Insert File) Time:            {syncResult.TimeToParseToInsertFile}
                Parse (To Delete File) Time:            {syncResult.TimeToParseToDeleteFile}
                Initialize Cosmos Client Time:          {syncResult.TimeToInitializeCosmosClient}
                Sync To Cosmos Time:                    {syncResult.TimeToSyncToCosmos}
                Cosmos DB Inserted Count:               {syncResult.CosmosDBInsertedCount}
                Cosmos DB Deleted Count:                {syncResult.CosmosDBDeletedCount}
                Status:                                 {syncResult.Status}
                Success:                                {syncResult.Success}
                -----------------------------------------------------------------------------------------

                """;
        }
    }
}

public class SyncFunctionBase<TFunction, T> : SyncFunctionBase<TFunction, T, T>
    where T : CacheableCSV
{
    public SyncFunctionBase(ILoggerFactory loggerFactory, CSVSyncServiceFactory repositoryServiceFactory)
        : base(loggerFactory, repositoryServiceFactory)
    {
    }
}