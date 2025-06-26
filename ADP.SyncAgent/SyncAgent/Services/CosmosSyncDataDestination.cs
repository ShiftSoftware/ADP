using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Collections.Concurrent;

namespace ShiftSoftware.ADP.SyncAgent.Services;

/// <summary>
/// Treat Add and Update action as Upsert to cosmos
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TCosmosClinet"></typeparam>
public class CosmosSyncDataDestination<T, TCosmosClinet> : CosmosSyncDataDestination<T, T, T, TCosmosClinet>,
    ISyncDataAdapter<T, T, CosmosSyncDataDestinationConfigurations<T>, CosmosSyncDataDestination<T, TCosmosClinet>>
    where T : class
    where TCosmosClinet : CosmosClient
{
    public CosmosSyncDataDestination(IServiceProvider services) : base(services)
    {
    }

    public virtual new CosmosSyncDataDestinationConfigurations<T>? Configurations => (CosmosSyncDataDestinationConfigurations<T>?)base.Configurations;

    public virtual ISyncEngine<T, T> Configure(CosmosSyncDataDestinationConfigurations<T> configurations, bool configureSyncService = true)
    {
        base.Configure(configurations, configureSyncService);
        return base.SyncService;
    }

    public virtual new CosmosSyncDataDestination<T, TCosmosClinet> SetSyncService(ISyncEngine<T, T> syncService)
    {
        base.SetSyncService(syncService);
        return this;
    }
}

/// <summary>
/// Treat Add and Update action as Upsert to cosmos
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TCosmos"></typeparam>
/// <typeparam name="TCosmosClinet"></typeparam>
public class CosmosSyncDataDestination<T, TCosmos, TCosmosClinet> : CosmosSyncDataDestination<T, T, TCosmos, TCosmosClinet>,
    ISyncDataAdapter<T, T, CosmosSyncDataDestinationConfigurations<T, TCosmos>, CosmosSyncDataDestination<T, TCosmos, TCosmosClinet>>
    where T : class
    where TCosmos : class
    where TCosmosClinet : CosmosClient
{
    public CosmosSyncDataDestination(IServiceProvider services) : base(services)
    {
    }

    public virtual new CosmosSyncDataDestinationConfigurations<T, TCosmos>? Configurations => base.Configurations;

    public virtual new ISyncEngine<T, T> Configure(CosmosSyncDataDestinationConfigurations<T, TCosmos> configurations, bool configureSyncService = true)
    {
        base.Configure(configurations, configureSyncService);
        return base.SyncService;
    }

    public virtual new CosmosSyncDataDestination<T, TCosmos, TCosmosClinet> SetSyncService(ISyncEngine<T, T> syncService)
    {
        base.SetSyncService(syncService);
        return this;
    }
}

/// <summary>
/// Treat Add and Update action as Upsert to cosmos
/// </summary>
/// <typeparam name="TSource"></typeparam>
/// <typeparam name="TDestination"></typeparam>
/// <typeparam name="TCosmos"></typeparam>
/// <typeparam name="TCosmosClinet"></typeparam>
public class CosmosSyncDataDestination<TSource, TDestination, TCosmos, TCosmosClinet> : ISyncDataAdapter<TSource, TDestination, CosmosSyncDataDestinationConfigurations<TDestination, TCosmos>, CosmosSyncDataDestination<TSource, TDestination, TCosmos, TCosmosClinet>>
    where TSource : class
    where TDestination : class
    where TCosmos : class
    where TCosmosClinet : CosmosClient
{
    private readonly CosmosClient cosmosClient;

    public virtual ISyncEngine<TSource, TDestination> SyncService { get; private set; }
    public virtual CosmosSyncDataDestinationConfigurations<TDestination, TCosmos>? Configurations { get; private set; }

    private ResiliencePipeline resiliencePipeline;
    private ConcurrentDictionary<TDestination, CosmosActionResult<TCosmos>> cosmosActionResults = new();
    private IEnumerable<SyncCosmosAction<TDestination, TCosmos>?> actionItems = [];

    public CosmosSyncDataDestination(IServiceProvider services)
    {
        this.cosmosClient = services.GetRequiredService<TCosmosClinet>();
        this.resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions()) // Update retry using the default options
            .Build(); // Builds the resilience pipeline
    }

    public virtual CosmosSyncDataDestination<TSource, TDestination, TCosmos, TCosmosClinet> SetSyncService(ISyncEngine<TSource, TDestination> syncService)
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
    public virtual ISyncEngine<TSource, TDestination> Configure(CosmosSyncDataDestinationConfigurations<TDestination, TCosmos> configurations , bool configureSyncService = true)
    {
        this.Configurations = configurations;

        var previousBatchCompleted = this.SyncService.BatchCompleted;

        if (configureSyncService)
            this.SyncService
                .SetupStoreBatchData(async (x)=> await this.StoreBatchData(x))
                .SetupBatchCompleted(async (x) =>
                {
                    if (previousBatchCompleted is not null)
                        return await previousBatchCompleted(x) && await this.BatchCompleted(x);

                    return await this.BatchCompleted(x);
                });

        return this.SyncService;
    }


    /// <summary>
    /// Treat Add and Update action as upsert to cosmos
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public virtual async ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input)
    {
        try
        {
            var inputItems = input.Input.Items;

            // If it is retry, retry only the failed items from the previous result
            if (input.Input.Status.CurrentRetryCount > 0 && (input.Input.PreviousResult?.IsEligibleToUseItAsRetryInput(input.Input?.Items?.LongCount()) ?? false))
                inputItems = input.Input?.PreviousResult?.FailedItems;

            if (input.Input?.Status.CurrentRetryCount == 0 || !actionItems.Any())
            {
                if (this.Configurations!.CosmosAction is null)
                    actionItems = inputItems?.Select(y => new SyncCosmosAction<TDestination, TCosmos>(y, input.Input.Status.ActionType, input.CancellationToken)) ?? [];
                else
                    foreach (var item in inputItems ?? [])
                        actionItems = (actionItems ?? []).Append(await this.Configurations!.CosmosAction(new(item, input!.Input.Status.ActionType, input.CancellationToken)));
            }

            //Some times the CSV comparer marks an item as deleted. But the SyncAgentCosmosAction has the ability to change that to an upsert (This is done outside the sync agent by the programmer.) 
            var deleteResult = await DeleteFromCosmosAsync(
                actionItems?.Where(x => x.ActionType == SyncActionType.Delete) ?? [],
                input!.Input!.Status.CurrentRetryCount,
                input!.CancellationToken);

            var upsertResult = await UpsertToCosmosAsync(
                actionItems?.Where(x => x.ActionType == SyncActionType.Update || x.ActionType == SyncActionType.Add) ?? [],
                input!.Input.Status.CurrentRetryCount,
                input!.CancellationToken);

            var result = new SyncStoreDataResult<TDestination>
            {
                FailedItems = deleteResult.FailedItems.Union(upsertResult.FailedItems).Union(input.Input.PreviousResult?.FailedItems ?? []),
                SucceededItems = deleteResult.SucceededItems.Union(upsertResult.SucceededItems).Union(input.Input.PreviousResult?.SucceededItems ?? []),
                SkippedItems = deleteResult.SkippedItems.Union(upsertResult.SkippedItems).Union(input.Input.PreviousResult?.SkippedItems ?? []),
            };

            if (result.ResultType == SyncStoreDataResultType.Failed || result.ResultType == SyncStoreDataResultType.Partial)
                result.NeedRetry = true;
            else
                result.NeedRetry = false;

            return result;
        }
        catch (Exception)
        {
            return new SyncStoreDataResult<TDestination>(true);
        }
    }

    private async Task<(IEnumerable<TDestination> SucceededItems, IEnumerable<TDestination> FailedItems, IEnumerable<TDestination> SkippedItems)> UpsertToCosmosAsync(
            IEnumerable<SyncCosmosAction<TDestination, TCosmos>?>? items,
            long retryCount,
            CancellationToken cancellationToken
        )
    {
        IEnumerable<Task>? tasks = [];
        ConcurrentBag<TDestination> succeededItems = new();
        ConcurrentBag<TDestination> failedItems = new();
        ConcurrentBag<TDestination> skippedItems = new();

        var container = cosmosClient.GetContainer(this.Configurations!.DatabaseId, this.Configurations!.ContainerId);

        foreach (var item in items ?? [])
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            tasks = tasks.Append(Task.Run(async () =>
            {
                ConcurrentBag<TCosmos> innerSucceededItems = new();
                ConcurrentBag<TCosmos> innerFailedItems = new();

                IEnumerable<TCosmos?>? mappedItems = [];

                var previousResult = this.cosmosActionResults.GetValueOrDefault(item?.Item!);

                if(previousResult?.EligibleToUseItAsRetryInput == true && retryCount > 0)
                {
                    mappedItems = previousResult?.FailedItems;
                }
                else
                {
                    // If the item is TCosmos, we can directly use it as mapped item
                    if(item?.Item is TCosmos)
                        mappedItems = [item?.Item as TCosmos];
                    else if(item?.Mapping is null)
                        throw new ArgumentException("Mapping function is required in cosmos action.");

                    if (item?.Mapping is not null)
                        mappedItems = await item.Mapping(new(item.Item, cancellationToken));

                    // Skip null items
                    mappedItems = mappedItems?.Where(x => x is not null);
                }

                if (mappedItems?.Any() == true)
                {
                    IEnumerable<Task>? innerTasks = [];

                    foreach (var mappedItem in mappedItems)
                    {
                        innerTasks = innerTasks.Append(Task.Run(async () =>
                        {
                            var partitionKey = Utility.GetPartitionKey(mappedItem!, this.Configurations!.PartitionKeyLevel1Expression, this.Configurations!.PartitionKeyLevel2Expression, this.Configurations!.PartitionKeyLevel3Expression);

                            try
                            {
                                await this.resiliencePipeline.ExecuteAsync(async token =>
                                {
                                    await container.UpsertItemAsync(mappedItem, partitionKey,
                                        new ItemRequestOptions { EnableContentResponseOnWrite = false }, cancellationToken);

                                    innerSucceededItems.Add(mappedItem!);
                                }, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                innerFailedItems.Add(mappedItem!); // Only after all retries failed
                            }
                        }, cancellationToken));
                    }

                    // Wait for all inner tasks to complete
                    await Task.WhenAll(innerTasks);

                    var cosmosActionResult = new CosmosActionResult<TCosmos>(innerSucceededItems, innerFailedItems);

                    if(cosmosActionResult.IsSuccessful(mappedItems?.LongCount()??-1))
                        succeededItems.Add(item!.Item!);
                    else
                        failedItems.Add(item!.Item!);

                    this.cosmosActionResults.AddOrUpdate(item!.Item!, cosmosActionResult, (_,_)=> cosmosActionResult);
                }
                else
                {
                    skippedItems.Add(item?.Item!);
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        tasks = null;
        items = null;

        return (succeededItems, failedItems, skippedItems);
    }

    private async Task<(IEnumerable<TDestination> SucceededItems, IEnumerable<TDestination> FailedItems, IEnumerable<TDestination> SkippedItems)> DeleteFromCosmosAsync(
            IEnumerable<SyncCosmosAction<TDestination, TCosmos>?>? items,
            long retryCount,
            CancellationToken cancellationToken
        )
    {
        IEnumerable<Task>? tasks = [];
        ConcurrentBag<TDestination> succeededItems = new();
        ConcurrentBag<TDestination> failedItems = new();
        ConcurrentBag<TDestination> skippedItems = new();

        var container = cosmosClient.GetContainer(this.Configurations!.DatabaseId, this.Configurations!.ContainerId);

        foreach (var item in items ?? [])
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            tasks = tasks.Append(Task.Run(async () =>
            {
                ConcurrentBag<TCosmos> innerSucceededItems = new();
                ConcurrentBag<TCosmos> innerFailedItems = new();

                IEnumerable<TCosmos?>? mappedItems = null;

                var previousResult = this.cosmosActionResults.GetValueOrDefault(item?.Item!);

                if(previousResult?.EligibleToUseItAsRetryInput == true && retryCount > 0)
                {
                    mappedItems = previousResult.FailedItems;
                }
                else
                {
                    // If the item is TCosmos, we can directly use it as mapped item
                    if(item?.Item is TCosmos)
                        mappedItems = [item?.Item as TCosmos];
                    else if(item?.Mapping is null)
                        throw new ArgumentException("Mapping function is required in cosmos action.");

                    if (item?.Mapping is not null)
                        mappedItems = await item.Mapping(new(item.Item, cancellationToken));

                    // Skip null items
                    mappedItems = mappedItems?.Where(x => x is not null);
                }

                if (mappedItems?.Any() == true)
                {
                    IEnumerable<Task>? innerTasks = [];

                    foreach (var mappedItem in mappedItems)
                    {
                        innerTasks = innerTasks.Append(Task.Run(async () =>
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
                                        await container.DeleteItemAsync<TDestination>(id, partitionKey);
                                    }
                                    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                                    {
                                        innerSucceededItems.Add(mappedItem!); // Item not found, but we consider it as succeeded
                                    }

                                    innerSucceededItems.Add(mappedItem!);
                                }, cancellationToken);
                            }
                            catch (Exception)
                            {
                                innerFailedItems.Add(mappedItem!); // Only after all retries failed
                            }
                        }, cancellationToken));
                    }

                    // Wait for all inner tasks to complete
                    await Task.WhenAll(innerTasks);

                    var cosmosActionResult = new CosmosActionResult<TCosmos>(innerSucceededItems, innerFailedItems);

                    if(cosmosActionResult.IsSuccessful(mappedItems.LongCount()))
                        succeededItems.Add(item!.Item!);
                    else
                        failedItems.Add(item!.Item!);

                    this.cosmosActionResults.AddOrUpdate(item!.Item!, cosmosActionResult, (_,_)=> cosmosActionResult);
                }
                else
                {
                    skippedItems.Add(item?.Item!);
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        tasks = null;
        items = null;

        return (succeededItems, failedItems, skippedItems);
    }

    public virtual ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        this.cosmosActionResults.Clear(); // Clear the results for the next batch
        this.actionItems = []; // Clear the action items for the next batch
        return new(true);
    }

    public virtual ValueTask Reset()
    {
        this.Configurations = null;
        return ValueTask.CompletedTask;
    }

    public virtual async ValueTask DisposeAsync()
    {
        await Reset();
    }

    #region Not Implemented
    public virtual ValueTask<bool> ActionCompleted(SyncFunctionInput<SyncActionCompletedInput> input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask<RetryAction> BatchRetry(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask Failed(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask Finished(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask<IEnumerable<TSource?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TSource>> input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask<SyncPreparingResponseAction> Preparing(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask Succeeded(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask<bool> ActionStarted(SyncFunctionInput<SyncActionType> input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask<IEnumerable<TDestination?>?> AdvancedMapping(SyncFunctionInput<SyncMappingInput<TSource, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask<IEnumerable<TDestination?>?> Mapping(IEnumerable<TSource?>? sourceItems, SyncActionType actionType)
    {
        throw new NotImplementedException();
    }
    #endregion
}

internal class CosmosActionResult<T>
    where T : class
{
    public IEnumerable<T>? SucceededItems { get; set; } = [];
    public IEnumerable<T>? FailedItems { get; set; } = [];
    public bool EligibleToUseItAsRetryInput { get; private set; } = false;

    public CosmosActionResult() { }

    public CosmosActionResult(IEnumerable<T>? succeededItems, IEnumerable<T>? failedItems)
    {
        SucceededItems = succeededItems;
        FailedItems = failedItems;
    }

    public bool IsSuccessful(long expectedCount)
    {
        var totalCount = (SucceededItems?.LongCount() ?? 0) + (FailedItems?.LongCount() ?? 0);
        EligibleToUseItAsRetryInput = totalCount == expectedCount;

        if (!(FailedItems?.Any() ?? false) && SucceededItems?.LongCount() == expectedCount)
            return true;

        return false;
    }
}
