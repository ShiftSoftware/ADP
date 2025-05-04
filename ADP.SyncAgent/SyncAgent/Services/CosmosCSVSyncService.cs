using AutoMapper;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Polly;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent.Services;

public class CosmosCSVSyncService<TCSV, TCosmos> : SyncService<TCSV, TCosmos>, IDisposable
    where TCSV : CacheableCSV
    where TCosmos : class
{
    private readonly CosmosClient cosmosClient;
    private readonly ILogger logger;
    private readonly ISyncProgressIndicator? syncProgressIndicator;

    public CosmosCSVSyncService(
        IServiceProvider services, 
        IStorageService storageService,
        SyncAgentOptions options,
        CosmosClient cosmosClient,
        IMapper mapper, 
        ILogger logger, 
        ISyncProgressIndicator? syncProgressIndicator) : base(services, storageService, options, mapper, logger, syncProgressIndicator)
    {
        this.cosmosClient = cosmosClient;
        this.logger = logger;
        this.syncProgressIndicator = syncProgressIndicator;
    }

    private async Task UpsertToCosmosAsync(
            string databaseId,
            string containerId,
            IEnumerable<SyncCosmosAction<TCosmos>?> items,
            Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression
        )
    {
        IEnumerable<Task> tasks = [];

        var container = cosmosClient.GetContainer(databaseId, containerId);

        foreach (var item in items)
        {
            base.CheckForCancellation();

            tasks = tasks.Concat([Task.Run(async () =>
            {
                if (item?.Mapping is not null)
                    item.Item = await item.Mapping(new(item.Item, GetCancellationToken()));

                if (item?.Item is not null)
                {
                    var partitionKey = Utility.GetPartitionKey(item.Item, partitionKeyLevel1Expression, partitionKeyLevel2Expression, partitionKeyLevel3Expression);
                    await base.ResiliencePipeline.ExecuteAsync(async token =>
                    {
                        await container.UpsertItemAsync(item.Item, partitionKey,
                            new ItemRequestOptions { EnableContentResponseOnWrite = false }, GetCancellationToken());
                    }, GetCancellationToken());
                }
            }, GetCancellationToken())]);
        }

        await Task.WhenAll(tasks);

        tasks = null;
        items = null;
    }

    private async Task DeleteFromCosmosAsync(
            string databaseId,
            string containerId,
            IEnumerable<SyncCosmosAction<TCosmos>?> items,
            Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression
        )
    {
        IEnumerable<Task> tasks = [];

        var container = cosmosClient.GetContainer(databaseId, containerId);

        foreach (var item in items)
        {
            CheckForCancellation();

            tasks = tasks.Concat([Task.Run(async () =>
            {
                if (item?.Mapping is not null)
                    item.Item = await item.Mapping(new(item.Item, GetCancellationToken()));

                if (item?.Item is not null)
                {
                    var partitionKey = Utility.GetPartitionKey(item.Item, partitionKeyLevel1Expression, partitionKeyLevel2Expression, partitionKeyLevel3Expression);
                    await base.ResiliencePipeline.ExecuteAsync(async token =>
                    {
                        try
                        {
                            var type = item.Item.GetType();
                            var id = (string?)type.GetProperty("id")?.GetValue(item.Item);
                            await container.DeleteItemAsync<TCosmos>(id, partitionKey);
                        }
                        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                        }
                    });
                }
            })]);
        }

        await Task.WhenAll(tasks);

        tasks = null;
        items = null;
    }

    public CosmosCSVSyncService<TCSV, TCosmos> ConfigureCosmosProcess(
        string databaseId,
        string containerId,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression = null,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression = null,
        Func<SyncCosmosAction<TCosmos>, ValueTask<SyncCosmosAction<TCosmos>?>>? cosmosAction = null)
    {
        base.ConfigureDataProcess(async (x) =>
        {
            IEnumerable<SyncCosmosAction<TCosmos>?>? items = null;

            if (cosmosAction is null)
                items = x.Items.Select(x => new SyncCosmosAction<TCosmos>(x, CosmosActionType.Upsert, GetCancellationToken()));
            else
                foreach (var item in x.Items)
                    items = (items ?? []).Concat([await cosmosAction(new(item, CosmosActionType.Upsert, GetCancellationToken()))]);

            logger.LogInformation("Completed comsos action.");

            base.UpdateProgress(x.TaskStatus, false);
            if (syncProgressIndicator is not null)
                await syncProgressIndicator.LogInformationAsync(
                    x.TaskStatus,
                    "Completed comsos action.\r\n\r\n");

            //Some times the CSV comparer marks an item as deleted. But the SyncCosmosAction has the ability to change that to an upsert (This is done outside the sync agent by the programmer.) 

            await DeleteFromCosmosAsync(
                databaseId,
                containerId,
                items?.Where(x => x.ActionType == CosmosActionType.Delete) ?? [],
                partitionKeyLevel1Expression,
                partitionKeyLevel2Expression,
                partitionKeyLevel3Expression);

            await UpsertToCosmosAsync(
                databaseId,
                containerId,
                items?.Where(x => x.ActionType == CosmosActionType.Upsert) ?? [],
                partitionKeyLevel1Expression,
                partitionKeyLevel2Expression,
                partitionKeyLevel3Expression);

            return true;
        });

        return this;
    }
}
