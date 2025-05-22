using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Retry;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Collections.Concurrent;

namespace ShiftSoftware.ADP.SyncAgent.Services;

/// <summary>
/// Treat Add and Update action as Upsert to cosmos
/// </summary>
/// <typeparam name="TSource"></typeparam>
/// <typeparam name="TCosmos"></typeparam>
public class CosmosSyncDataDestination<TSource, TCosmos> : ISyncDataAdapter<TSource, TCosmos, CosmosSyncDataDestinationConfigurations<TCosmos>, CosmosSyncDataDestination<TSource, TCosmos>>
    where TSource : class
    where TCosmos : class
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
            this.SyncService.SetupStoreBatchData(async (x)=> await this.StoreBatchData(x));

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
            IEnumerable<SyncCosmosAction<TCosmos>?>? items = null;
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
                items = inputItems?.Select(y => new SyncCosmosAction<TCosmos>(y, input.Input.Status.ActionType, input.CancellationToken));
            else
                foreach (var item in inputItems ?? [])
                    items = (items ?? []).Concat((await this.Configurations!.CosmosAction(new(item, input!.Input.Status.ActionType, input.CancellationToken))) ?? []);

            //Some times the CSV comparer marks an item as deleted. But the SyncAgentCosmosAction has the ability to change that to an upsert (This is done outside the sync agent by the programmer.) 
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
                SkippedItems = deleteResult.SkippedItems.Concat(upsertResult.SkippedItems),
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

    private async Task<(IEnumerable<TCosmos> SucceededItems, IEnumerable<TCosmos> FailedItems, IEnumerable<TCosmos> SkippedItems)> UpsertToCosmosAsync(
            IEnumerable<SyncCosmosAction<TCosmos>?>? items,
            CancellationToken cancellationToken
        )
    {
        IEnumerable<Task>? tasks = [];
        ConcurrentBag<TCosmos> succeededItems = new();
        ConcurrentBag<TCosmos> failedItems = new();
        ConcurrentBag<TCosmos> skippedItems = new();

        var container = cosmosClient.GetContainer(this.Configurations!.DatabaseId, this.Configurations!.ContainerId);

        foreach (var item in items ?? [])
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            tasks = tasks.Concat([Task.Run(async () =>
            {
                IEnumerable<TCosmos?>? mappedItems= [item?.Item];

                if (item?.Mapping is not null)
                    mappedItems = await item.Mapping(new(item.Item, cancellationToken));

                // Skip null items
                mappedItems = mappedItems?.Where(x => x is not null);

                if (mappedItems?.Any() == true)
                {
                    IEnumerable<Task>? innedTasks = [];

                    foreach (var mappedItem in mappedItems)
                    {
                        innedTasks.Concat([Task.Run(async () =>
                        {
                            var partitionKey = Utility.GetPartitionKey(mappedItem!, this.Configurations!.PartitionKeyLevel1Expression, this.Configurations!.PartitionKeyLevel2Expression, this.Configurations!.PartitionKeyLevel3Expression);

                            try
                            {
                                await this.resiliencePipeline.ExecuteAsync(async token =>
                                {
                                    await container.UpsertItemAsync(mappedItem, partitionKey,
                                        new ItemRequestOptions { EnableContentResponseOnWrite = false }, cancellationToken);
                                    succeededItems.Add(mappedItem!);
                                }, cancellationToken);
                            }
                            catch (Exception)
                            {
                                failedItems.Add(mappedItem!); // Only after all retries failed
                            }
                        }, cancellationToken)]);
                    }

                    // Wait for all inner tasks to complete
                    await Task.WhenAll(innedTasks);
                }
                else
                {
                    skippedItems.Add(item?.Item!);
                }
            }, cancellationToken)]);
        }

        await Task.WhenAll(tasks);

        tasks = null;
        items = null;

        return (succeededItems, failedItems, skippedItems);
    }

    private async Task<(IEnumerable<TCosmos> SucceededItems, IEnumerable<TCosmos> FailedItems, IEnumerable<TCosmos> SkippedItems)> DeleteFromCosmosAsync(
            IEnumerable<SyncCosmosAction<TCosmos>?>? items,
            CancellationToken cancellationToken
        )
    {
        IEnumerable<Task>? tasks = [];
        ConcurrentBag<TCosmos> succeededItems = new();
        ConcurrentBag<TCosmos> failedItems = new();
        ConcurrentBag<TCosmos> skippedItems = new();

        var container = cosmosClient.GetContainer(this.Configurations!.DatabaseId, this.Configurations!.ContainerId);

        foreach (var item in items ?? [])
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            tasks = tasks.Concat([Task.Run(async () =>
            {
                IEnumerable<TCosmos?>? mappedItems = [item?.Item];

                if (item?.Mapping is not null)
                    mappedItems = await item.Mapping(new(item.Item, cancellationToken));

                // Skip null items
                mappedItems = mappedItems?.Where(x => x is not null);

                if (mappedItems?.Any() == true)
                {
                    IEnumerable<Task>? innedTasks = [];

                    foreach (var mappedItem in mappedItems)
                    {
                        innedTasks.Concat([Task.Run(async () =>
                        {
                            var partitionKey = Utility.GetPartitionKey(mappedItem!, this.Configurations!.PartitionKeyLevel1Expression, this.Configurations!.PartitionKeyLevel2Expression, this.Configurations!.PartitionKeyLevel3Expression);

                            try
                            {
                                await this.resiliencePipeline.ExecuteAsync(async token =>
                                {
                                    try
                                    {
                                        var type = mappedItem!.GetType();
                                        var id = (string?)type.GetProperty("id")?.GetValue(mappedItem!);
                                        await container.DeleteItemAsync<TCosmos>(id, partitionKey);
                                    }
                                    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                                    {
                                        succeededItems.Add(mappedItem!); // Item not found, but we consider it as succeeded
                                    }

                                    succeededItems.Add(mappedItem!);
                                }, cancellationToken);
                            }
                            catch (Exception)
                            {
                                failedItems.Add(mappedItem!); // Only after all retries failed
                            }
                        })]);
                    }

                    // Wait for all inner tasks to complete
                    await Task.WhenAll(innedTasks);
                }
                else
                {
                    skippedItems.Add(item?.Item!);
                }
            }, cancellationToken)]);
        }

        await Task.WhenAll(tasks);

        tasks = null;
        items = null;

        return (succeededItems, failedItems, skippedItems);
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
