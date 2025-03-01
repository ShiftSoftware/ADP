using AutoMapper;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ShiftSoftware.ADP.SyncAgent;
using ShiftSoftware.ADP.SyncAgent.Entities;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Net;

namespace ShiftSoftware.ADP.SyncAgent.Services;

public class TableSyncService<TEntity, TCosmos>
    where TEntity : class
    where TCosmos : class
{
    private DbContext db;
    private readonly IServiceProvider serviceProvider;
    private readonly CosmosClient client;
    private readonly IMapper mapper;

    // Create an instance of builder that exposes various extensions for adding resilience strategies
    private readonly ResiliencePipeline ResiliencePipeline;

    public TableSyncService(IServiceProvider serviceProvider, CosmosClient client, IMapper mapper)
    {
        this.serviceProvider = serviceProvider;
        this.client = client;
        this.mapper = mapper;

        ResiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions()) // Add retry using the default options
            .Build(); // Builds the resilience pipeline
    }

    public async Task<bool> RunAsync<TDbContext>(
        Expression<Func<TEntity, object>> tableKey,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query,
        string databaseId,
        string containerId,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression = null,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression = null,
        Func<TEntity, ValueTask<TCosmos>>? mapping = null,
        ILogger? logger = null,
        Expression<Func<TEntity, object?>>? replicationDateProperty = null)
        where TDbContext : DbContext
    {
        //this.db = serviceProvider.GetRequiredService<TDbContext>();

        //var replicationDate = DateTime.UtcNow;

        //// Do the delete from cosmos
        //var deletedRowLogs = await GetDeletedRowLogs(containerId);

        //var deleteSuccessIds = await DeleteFromCosmosAsync(deletedRowLogs, successKeys, databaseId, containerId, logger);

        //await db.Set<DeletedRowLog>().Where(x => deleteSuccessIds.Contains(x.ID))
        //    .ExecuteDeleteAsync();

        //// Do the upsert to cosmos
        //var dbSet = db.Set<TEntity>();

        //var items = await GetItemsAsync(dbSet, query);

        //var successIds = await UpsertBtachToCosmosAsync(items, successKeys, databaseId, containerId, mapping,
        //    partitionKeyLevel1Expression, partitionKeyLevel2Expression, partitionKeyLevel3Expression, logger);

        //string keyName = Utility.GetPropertyName(successKeys);

        //var replicationDatePropertyName = nameof(BaseEntity.LastReplicationDate);
        //if(replicationDateProperty is not null)
        //    replicationDatePropertyName = Utility.GetPropertyName(replicationDateProperty!);

        //await dbSet.Where(x => successIds.Contains(EF.Property<long>(x, keyName)))
        //    .ExecuteUpdateAsync(x => x.SetProperty(p => EF.Property<DateTime?>(p, replicationDatePropertyName), replicationDate));

        //logger?.LogInformation($"Synced {successIds.Count()} items to Cosmos");

        //return items.LongCount() == successIds.Count();

        return await RunAsync(
            typeof(TDbContext),
            tableKey,
            query,
            x => [tableKey.Compile().Invoke(x)],
            databaseId,
            containerId,
            partitionKeyLevel1Expression,
            partitionKeyLevel2Expression,
            partitionKeyLevel3Expression,
            mapping,
            logger,
            replicationDateProperty);
    }

    protected async Task<bool> RunAsync<TQueryResult>(
        Type dbContext,
        Expression<Func<TEntity, object>> tableKey,
        Func<IQueryable<TEntity>, IQueryable<TQueryResult>>? query,
        Func<TQueryResult, IEnumerable<object>> successKeys,
        string databaseId,
        string containerId,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression = null,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression = null,
        Func<TQueryResult, ValueTask<TCosmos>>? mapping = null,
        ILogger? logger = null,
        Expression<Func<TEntity, object?>>? replicationDateProperty = null)
        where TQueryResult : class
    {
        db = (DbContext)serviceProvider.GetRequiredService(dbContext);

        var replicationDate = DateTime.UtcNow;

        // Do the delete from cosmos
        var deletedRowLogs = await GetDeletedRowLogs(containerId);

        var deleteSuccessIds = await DeleteFromCosmosAsync(deletedRowLogs, databaseId, containerId, logger);

        await db.Set<DeletedRowLog>().Where(x => deleteSuccessIds.Contains(x.ID))
            .ExecuteDeleteAsync();

        // Do the upsert to cosmos
        var dbSet = db.Set<TEntity>();

        var items = await GetItemsAsync(dbSet, query);

        var successIds = await UpsertToCosmosAsync(items, successKeys, databaseId, containerId, mapping,
            partitionKeyLevel1Expression, partitionKeyLevel2Expression, partitionKeyLevel3Expression, logger);

        var replicationDatePropertyName = "LastReplicationDate";
        if (replicationDateProperty is not null)
            replicationDatePropertyName = Utility.GetPropertyName(replicationDateProperty!);

        string keyName = Utility.GetPropertyName(tableKey!);

        await dbSet.Where(x => successIds.Contains(EF.Property<object>(x, keyName)))
            .ExecuteUpdateAsync(x => x.SetProperty(p => EF.Property<DateTime?>(p, replicationDatePropertyName), replicationDate));

        logger?.LogInformation($"Synced {successIds.Count()} items to Cosmos");

        return items.LongCount() == successIds.Count();
    }

    private async Task<IEnumerable<TQueryResult>> GetItemsAsync<TQueryResult>(
        DbSet<TEntity> dbSet,
        Func<IQueryable<TEntity>, IQueryable<TQueryResult>>? query)
    {
        var queryable = dbSet.AsQueryable().AsNoTracking();

        if (query is not null)
            return await query(queryable).ToListAsync();

        return await ((IQueryable<TQueryResult>)queryable).ToListAsync();
    }

    private async Task<IEnumerable<DeletedRowLog>> GetDeletedRowLogs(string container)
    {
        return await db.Set<DeletedRowLog>().Where(x => x.ContainerName == container).ToListAsync();
    }

    private async Task<IEnumerable<object>> UpsertToCosmosAsync<TQueryResult>(
        IEnumerable<TQueryResult> items,
        Func<TQueryResult, IEnumerable<object>> successKeys,
        string databaseId,
        string containerId,
        Func<TQueryResult, ValueTask<TCosmos>>? mapping,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression,
        ILogger? logger)
    {
        ConcurrentBag<object> successIds = new();

        var container = client.GetContainer(databaseId, containerId);

        List<Task> tasks = new();

        tasks.AddRange(items.Select(x => Task.Run(async () =>
        {
            try
            {

                await ResiliencePipeline.ExecuteAsync(async token =>
                {
                    var mappedItem = mapping is null ? mapper.Map<TCosmos>(x) : await mapping(x);

                    if (mappedItem is null)
                        return;

                    var partitionKey = Utility.GetPartitionKey(mappedItem, partitionKeyLevel1Expression, partitionKeyLevel2Expression, partitionKeyLevel3Expression);

                    await container.UpsertItemAsync(mappedItem, partitionKey,
                            new ItemRequestOptions { EnableContentResponseOnWrite = false });

                    var keys = successKeys(x);
                    foreach (var key in keys)
                        successIds.Add(key);
                });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex.ToString());
            }
        })));

        await Task.WhenAll(tasks);

        return successIds;
    }

    private async Task<IEnumerable<long>> DeleteFromCosmosAsync(
        IEnumerable<DeletedRowLog> deletedRowLogs,
        string databaseId,
        string containerId,
        ILogger? logger)
    {
        ConcurrentBag<long> successIds = new();

        var container = client.GetContainer(databaseId, containerId);

        List<Task> tasks = new();

        tasks.AddRange(deletedRowLogs.Select(x => Task.Run(async () =>
        {
            try
            {
                var partitionKey = Utility.GetPartitionKey(x);

                await ResiliencePipeline.Execute(token =>
                {
                    return container.DeleteItemAsync<TCosmos>(x.RowID, partitionKey,
                        new ItemRequestOptions { EnableContentResponseOnWrite = false })
                    .ContinueWith(c =>
                    {
                        CosmosException ex = null;

                        if (c.Exception != null)
                            foreach (var innerException in c.Exception.InnerExceptions)
                                if (innerException is CosmosException customException)
                                    ex = customException;

                        if (c.IsCompletedSuccessfully || ex?.StatusCode == HttpStatusCode.NotFound)
                            successIds.Add(x.ID);
                    });

                });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex.ToString());
            }
        })));

        await Task.WhenAll(tasks);

        return successIds;
    }
}

public class TableSyncService<TEntity, TCosmos, TDBContext> : TableSyncService<TEntity, TCosmos>
    where TEntity : class
    where TCosmos : class
    where TDBContext : DbContext
{
    public TableSyncService(IServiceProvider serviceProvider, CosmosClient client, IMapper mapper) : base(serviceProvider, client, mapper)
    {
    }

    public async Task<bool> RunAsync<TQueryResult>(
        Expression<Func<TEntity, object>> tableKey,
        Func<IQueryable<TEntity>, IQueryable<TQueryResult>>? query,
        Func<TQueryResult, IEnumerable<object>> successKeys,
        string databaseId,
        string containerId,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression = null,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression = null,
        Func<TQueryResult, ValueTask<TCosmos>>? mapping = null,
        ILogger? logger = null,
        Expression<Func<TEntity, object?>>? replicationDateProperty = null)
        where TQueryResult : class
    {
        return await RunAsync(
            typeof(TDBContext),
            tableKey,
            query,
            successKeys,
            databaseId,
            containerId,
            partitionKeyLevel1Expression,
            partitionKeyLevel2Expression,
            partitionKeyLevel3Expression,
            mapping,
            logger,
            replicationDateProperty);
    }
}
