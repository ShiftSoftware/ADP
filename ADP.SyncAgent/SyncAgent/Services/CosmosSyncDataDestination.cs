using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Retry;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Collections.Concurrent;

namespace ShiftSoftware.ADP.SyncAgent.Services;

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
            .AddRetry(new RetryStrategyOptions()) // Upsert retry using the default options
            .Build(); // Builds the resilience pipeline
    }

    public CosmosSyncDataDestination<TSource, TCosmos> SetSyncService(ISyncService<TSource, TCosmos> syncService)
    {
        this.SyncService = syncService;
        return this;
    }

    /// <summary>
    /// To avoid unexpected behavior, call this after the source adapter is configured.
    /// </summary>
    /// <returns></returns>
    public ISyncService<TSource, TCosmos> Configure(CosmosSyncDataDestinationConfigurations<TCosmos> configurations)
    {
        this.Configurations = configurations;

        this.SyncService.SetupStoreBatchData(this.StoreBatchData);

        if(!this.Configurations.StopOperationWhenOneFailed)
        {
            var batchRetryFunc = this.SyncService.BatchRetry;

            this.SyncService.SetupBatchRetry(async (x) =>
            {
                RetryAction retryResult = RetryAction.RetryAndStopAfterLastRetry;

                if (batchRetryFunc is not null)
                    retryResult = await batchRetryFunc(x);

                if(!this.Configurations!.StopOperationWhenOneFailed)
                    retryResult = RetryAction.RetryAndContinueAfterLastRetry;

                return retryResult;
            });
        }

        return this.SyncService;
    }

    public async ValueTask<SyncStoreDataResult<TCosmos>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TCosmos>> input)
    {
        try
        {
            IEnumerable<SyncAgentCosmosAction<TCosmos>?>? items = null;

            if (this.Configurations!.CosmosAction is null)
                items = input?.Input?.Items?.Select(y => new SyncAgentCosmosAction<TCosmos>(y, input.Input.Status.ActionType, input.CancellationToken));
            else
                foreach (var item in input?.Input?.Items ?? [])
                    items = (items ?? []).Concat((await this.Configurations!.CosmosAction(new(item, input!.Input.Status.ActionType, input.CancellationToken))) ?? []);

            //Some times the CSV comparer marks an item as deleted. But the SyncCosmosAction has the ability to change that to an upsert (This is done outside the sync agent by the programmer.) 

            var deleteResult = await DeleteFromCosmosAsync(
                items?.Where(x => x.ActionType == SyncActionType.Delete) ?? [],
                input!.CancellationToken);

            var upsertResult = await UpsertToCosmosAsync(
                items?.Where(x => x.ActionType == SyncActionType.Upsert) ?? [],
                input!.CancellationToken);

            var result = new SyncStoreDataResult<TCosmos>
            {
                FailedItems = deleteResult.FailedItems.Concat(upsertResult.FailedItems),
                SucceededItems = deleteResult.SucceededItems.Concat(upsertResult.SucceededItems)
            };

            if (result.ResultType == SyncStoreDataResultType.Failed || result.ResultType == SyncStoreDataResultType.Partial)
                result.NeedRetry = true;
            else
                result.NeedRetry = false;
        }
        catch (Exception)
        {
            return new SyncStoreDataResult<TCosmos>(true);
        }

        return new SyncStoreDataResult<TCosmos>(true);
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

        var stopOperation = false;
        var lockObj = new object();

        foreach (var item in items ?? [])
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            // Stop further task creation if stopOperation is set and StopOperationWhenOneFailed is true
            if (this.Configurations?.StopOperationWhenOneFailed == true && stopOperation)
                break;

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

                        // Signal to stop operation
                        if (this.Configurations?.StopOperationWhenOneFailed == true)
                            lock (lockObj)
                                stopOperation = true;
                    }
                }
            }, cancellationToken)]);
        }

        // Wait for all tasks, but cancel remaining if stopOperation is set
        while (tasks.Any())
        {
            var runningTasks = tasks.ToArray();
            var completed = await Task.WhenAny(runningTasks);

            // Cancel all remaining tasks
            if (this.Configurations?.StopOperationWhenOneFailed == true && stopOperation)
                break;

            tasks = tasks.Except([completed]);
        }

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

        var stopOperation = false;
        var lockObj = new object();

        foreach (var item in items ?? [])
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            // Stop further task creation if stopOperation is set and StopOperationWhenOneFailed is true
            if (this.Configurations?.StopOperationWhenOneFailed == true && stopOperation)
                break;

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

                        // Signal to stop operation
                        if (this.Configurations?.StopOperationWhenOneFailed == true)
                            lock (lockObj)
                                stopOperation = true;
                    }
                }
            }, cancellationToken)]);
        }

        // Wait for all tasks, but cancel remaining if stopOperation is set
        while (tasks.Any())
        {
            var runningTasks = tasks.ToArray();
            var completed = await Task.WhenAny(runningTasks);

            // Cancel all remaining tasks
            if (this.Configurations?.StopOperationWhenOneFailed == true && stopOperation)
                break;

            tasks = tasks.Except([completed]);
        }

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
    public ValueTask ActionCompleted(SyncFunctionInput<SyncActionType> input)
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

    public ValueTask<bool> Preparing(SyncFunctionInput input)
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
    #endregion
}
