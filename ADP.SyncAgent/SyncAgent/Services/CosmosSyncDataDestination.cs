using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Polly;
using Polly.Retry;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent.Services;

/// <summary>
/// Treat Add and Update action as Upsert to cosmos
/// </summary>
/// <typeparam name="TSource"></typeparam>
/// <typeparam name="TCosmos"></typeparam>
public class CosmosSyncDataDestination<TSource, TCosmos> : ISyncDataAdapter<TSource, TCosmos, CosmosSyncDataDestinationConfigurations<TCosmos>, CosmosSyncDataDestination<TSource, TCosmos>>
    where TSource : CacheableCSV, new()
    where TCosmos : class, new()
{
    private readonly CosmosClient cosmosClient;

    public ISyncService<TSource, TCosmos> SyncService { get; private set; }
    public CosmosSyncDataDestinationConfigurations<TCosmos>? Configurations { get; private set; }

    private ResiliencePipeline resiliencePipeline;

    public CosmosSyncDataDestination(SyncCosmosClient syncCosmosClient)
    {
        this.cosmosClient = syncCosmosClient.Client;
        this.resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions()) // Update retry using the default options
            .Build(); // Builds the resilience pipeline
    }

    public CosmosSyncDataDestination<TSource, TCosmos> SetSyncService(ISyncService<TSource, TCosmos> syncService)
    {
        this.SyncService = syncService;
        return this;
    }

    /// <summary>
    /// To avoid unexpected behavior, call this after the source adapter is configured
    /// </summary>
    /// <param name="configurations"></param>
    /// <param name="configureSyncService">
    /// If set false just configure DataAdapter and skip the configuration of the SyncService, 
    /// then you may be configure SyncService by your self
    /// </param>
    /// <returns></returns>
    public ISyncService<TSource, TCosmos> Configure(CosmosSyncDataDestinationConfigurations<TCosmos> configurations , bool configureSyncService = true)
    {
        this.Configurations = configurations;

        if (configureSyncService)
            this.SyncService.SetupStoreBatchData(this.StoreBatchData);

        return this.SyncService;
    }


    /// <summary>
    /// Treat Add and Update action as upsert to cosmos
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async ValueTask<SyncStoreDataResult<TCosmos>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TCosmos>> input)
    {
        try
        {
            IEnumerable<SyncAgentCosmosAction<TCosmos>?>? items = null;
            var inputItems = input.Input.Items;

            // If it is retry, skip the succeeded items to try store them again
            if(input.Input.Status.CurrentRetryCount > 0 && input.Input.PreviousResult?.SucceededItems?.Any() == true && inputItems?.Any() == true)
            {
                // Map the succeeded items to composite keys
                var succeededKeys = new HashSet<(string? Id, object? Key1, object? Key2, object? Key3)>(
                    input.Input.PreviousResult!.SucceededItems
                    .Select(x => GetCompositeKey(x)));

                // Filter the input items to exclude the succeeded items
                inputItems = inputItems?.Where(x => !succeededKeys.Contains(GetCompositeKey(x)));
            }

            if (this.Configurations!.CosmosAction is null)
                items = inputItems?.Select(y => new SyncAgentCosmosAction<TCosmos>(y, input.Input.Status.ActionType, input.CancellationToken));
            else
                foreach (var item in inputItems ?? [])
                    items = (items ?? []).Concat((await this.Configurations!.CosmosAction(new(item, input!.Input.Status.ActionType, input.CancellationToken))) ?? []);

            //Some times the CSV comparer marks an item as deleted. But the SyncCosmosAction has the ability to change that to an upsert (This is done outside the sync agent by the programmer.) 
            var deleteResult = await DeleteFromCosmosAsync(
                items?.Where(x => x.ActionType == SyncActionType.Delete) ?? [],
                input!.CancellationToken);

            var upsertResult = await UpsertToCosmosAsync(
                items?.Where(x => x.ActionType == SyncActionType.Update || x.ActionType == SyncActionType.Add) ?? [],
                input!.CancellationToken);

            var result = new SyncStoreDataResult<TCosmos>
            {
                FailedItems = deleteResult.FailedItems.Concat(upsertResult.FailedItems),
                SucceededItems = deleteResult.SucceededItems.Concat(upsertResult.SucceededItems).Concat(input.Input.PreviousResult?.SucceededItems ?? []),
            };

            if (result.ResultType == SyncStoreDataResultType.Failed || result.ResultType == SyncStoreDataResultType.Partial)
                result.NeedRetry = true;
            else
                result.NeedRetry = false;

            return result;
        }
        catch (Exception)
        {
            return new SyncStoreDataResult<TCosmos>(true);
        }
    }

    private (string? Id, object? Key1, object? Key2, object? Key3) GetCompositeKey(TCosmos? item)
    {
        if (item is null)
            return (Id: null, Key1: null, Key2: null, Key3: null);

        var key1Value = this.Configurations!.PartitionKeyLevel1Expression.Compile()(item);
        var key2Value = this.Configurations!.PartitionKeyLevel2Expression != null ? this.Configurations!.PartitionKeyLevel2Expression.Compile()(item) : null;
        var key3Value = this.Configurations!.PartitionKeyLevel3Expression != null ? this.Configurations!.PartitionKeyLevel3Expression.Compile()(item) : null;
        return (Id: item.GetType().GetProperty("id")?.GetValue(item)?.ToString() ?? string.Empty, Key1: key1Value, Key2: key2Value, Key3: key3Value);
    }

    private async Task<(IEnumerable<TCosmos> SucceededItems, IEnumerable<TCosmos> FailedItems)> UpsertToCosmosAsync(
            IEnumerable<SyncAgentCosmosAction<TCosmos>?>? items,
            CancellationToken cancellationToken
        )
    {
        IEnumerable<Task>? tasks = [];
        ConcurrentBag<TCosmos> succeededItems = new();
        ConcurrentBag<TCosmos> failedItems = new();

        var container = cosmosClient.GetContainer(this.Configurations!.DatabaseId, this.Configurations!.ContainerId);

        foreach (var item in items ?? [])
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            tasks = tasks.Concat([Task.Run(async () =>
            {
                if (item?.Mapping is not null)
                    item.Item = await item.Mapping(new(item.Item, cancellationToken));

                if (item?.Item is not null)
                {
                    var partitionKey = Utility.GetPartitionKey(item.Item, this.Configurations!.PartitionKeyLevel1Expression, this.Configurations!.PartitionKeyLevel2Expression, this.Configurations!.PartitionKeyLevel3Expression);

                    try
                    {
                        await this.resiliencePipeline.ExecuteAsync(async token =>
                        {
                            await container.UpsertItemAsync(item.Item, partitionKey,
                                new ItemRequestOptions { EnableContentResponseOnWrite = false }, cancellationToken);
                            succeededItems.Add(item.Item);
                        }, cancellationToken);
                    }
                    catch (Exception)
                    {
                        failedItems.Add(item.Item); // Only after all retries failed
                    }
                }
            }, cancellationToken)]);
        }

        await Task.WhenAll(tasks);

        tasks = null;
        items = null;

        return (succeededItems, failedItems);
    }

    private async Task<(IEnumerable<TCosmos> SucceededItems, IEnumerable<TCosmos> FailedItems)> DeleteFromCosmosAsync(
            IEnumerable<SyncAgentCosmosAction<TCosmos>?>? items,
            CancellationToken cancellationToken
        )
    {
        IEnumerable<Task>? tasks = [];
        ConcurrentBag<TCosmos> succeededItems = new();
        ConcurrentBag<TCosmos> failedItems = new();

        var container = cosmosClient.GetContainer(this.Configurations!.DatabaseId, this.Configurations!.ContainerId);

        foreach (var item in items ?? [])
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            tasks = tasks.Concat([Task.Run(async () =>
            {
                if (item?.Mapping is not null)
                    item.Item = await item.Mapping(new(item.Item, cancellationToken));

                if (item?.Item is not null)
                {
                    var partitionKey = Utility.GetPartitionKey(item.Item, this.Configurations!.PartitionKeyLevel1Expression, this.Configurations!.PartitionKeyLevel2Expression, this.Configurations!.PartitionKeyLevel3Expression);
                    try
                    {
                        await this.resiliencePipeline.ExecuteAsync(async token =>
                        {
                            try
                            {
                                var type = item.Item.GetType();
                                var id = (string?)type.GetProperty("id")?.GetValue(item.Item);
                                await container.DeleteItemAsync<TCosmos>(id, partitionKey);
                            }
                            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                succeededItems.Add(item.Item); // Item not found, but we consider it as succeeded
                            }

                            succeededItems.Add(item.Item);
                        }, cancellationToken);
                    }
                    catch (Exception)
                    {
                        failedItems.Add(item.Item); // Only after all retries failed
                    }
                }
            }, cancellationToken)]);
        }

        await Task.WhenAll(tasks);

        tasks = null;
        items = null;

        return (succeededItems, failedItems);
    }

    public ValueTask Reset()
    {
        this.Configurations = null;
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await Reset();
    }

    #region Not Implemented
    public ValueTask<bool> ActionCompleted(SyncFunctionInput<SyncActionCompletedInput> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TCosmos>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<RetryAction> BatchRetry(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TCosmos>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask Failed()
    {
        throw new NotImplementedException();
    }

    public ValueTask Finished()
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<TSource?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TSource>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<SyncPreparingResponseAction> Preparing(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask Succeeded()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> ActionStarted(SyncFunctionInput<SyncActionType> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<TCosmos?>?> AdvancedMapping(SyncFunctionInput<SyncMappingInput<TSource, TCosmos>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<TCosmos?>?> Mapping(IEnumerable<TSource?>? sourceItems, SyncActionType actionType)
    {
        throw new NotImplementedException();
    }
    #endregion
}
