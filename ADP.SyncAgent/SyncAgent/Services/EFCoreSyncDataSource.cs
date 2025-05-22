using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

namespace ShiftSoftware.ADP.SyncAgent.Services;

public class EFCoreSyncDataSource<TSource, TDestination, TDbContext> : EFCoreSyncDataSource<TSource, TSource, TDestination, TDbContext> , 
    ISyncDataAdapter<TSource, TDestination, EFCoreSyncDataSourceConfigurations<TSource, TSource, TDestination>, EFCoreSyncDataSource<TSource, TDestination, TDbContext>>
    where TSource : class
    where TDestination : class
    where TDbContext : DbContext
{
    public EFCoreSyncDataSource(TDbContext db) : base(db)
    {
    }

    EFCoreSyncDataSource<TSource, TDestination, TDbContext> ISyncDataAdapter<TSource, TDestination, EFCoreSyncDataSource<TSource, TDestination, TDbContext>>.SetSyncService(ISyncService<TSource, TDestination> syncService)
    {
        base.SetSyncService(syncService);
        return this;
    }
}

public class EFCoreSyncDataSource<TEntity, TSource, TDestination, TDbContext> 
    : ISyncDataAdapter<TSource, TDestination, EFCoreSyncDataSourceConfigurations<TEntity, TSource, TDestination>, EFCoreSyncDataSource<TEntity, TSource, TDestination, TDbContext>>
    where TEntity : class
    where TSource : class
    where TDestination : class
    where TDbContext : DbContext
{
    private readonly TDbContext db;
    private readonly DbSet<TEntity> dbSet;

    private DateTimeOffset? lastSyncTimestamp = null;

    private object? lastSyncId = null;

    public EFCoreSyncDataSourceConfigurations<TEntity, TSource, TDestination>? Configurations { get; private set; }
    public ISyncService<TSource, TDestination> SyncService { get; private set; }

    public EFCoreSyncDataSource(TDbContext db)
    {
        this.db = db;
        dbSet = db.Set<TEntity>();
    }

    public EFCoreSyncDataSource<TEntity, TSource, TDestination, TDbContext> SetSyncService(ISyncService<TSource, TDestination> syncService)
    {
        SyncService = syncService;
        return this;
    }

    public ISyncService<TSource, TDestination> Configure(EFCoreSyncDataSourceConfigurations<TEntity, TSource, TDestination> configurations, bool configureSyncService = true)
    {
        Configurations = configurations;
        var previousBatchStarted = SyncService.BatchStarted;
        var previousBatchCompleted = SyncService.BatchCompleted;

        if (configureSyncService)
            SyncService
                .SetupSourceTotalItemCount(SourceTotalItemCount)
                .SetupBatchStarted(async (x) =>
                {
                    if (previousBatchStarted is not null)
                        return await BatchStarted(x) && await previousBatchStarted(x);

                    return await BatchStarted(x);
                })
                .SetupGetSourceBatchItems(GetSourceBatchItems)
                .SetupBatchCompleted(async x =>
                {
                    if (previousBatchCompleted is not null)
                        return await BatchCompleted(x) && await previousBatchCompleted(x);

                    return await BatchCompleted(x);
                });

        return SyncService;
    }

    public ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input)
    {
        return new((long?)null);
    }

    public ValueTask<bool> BatchStarted(SyncFunctionInput<SyncActionStatus> input)
    {
        lastSyncTimestamp = DateTimeOffset.UtcNow;
        return new(true);
    }

    public async ValueTask<IEnumerable<TSource?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TSource>> input)
    {
        if (input.Input.Status.CurrentRetryCount > 0 && input.Input.PreviousItems is not null)
            return input.Input.PreviousItems;

        var query = dbSet.AsQueryable().AsNoTracking();

        // Order by the key property to ensure consistent batching
        var keyName = Utility.GetPropertyName(Configurations!.EntityKey!);
        query = query.OrderBy(x => EF.Property<object>(x, keyName));

        // Get the items with newer key that last batch
        if (this.lastSyncId is not null)
            query = ApplyBatchKeyFilter(query, keyName, this.lastSyncId);

        var queryMapped = Configurations!.Query(query, input.Input.Status.ActionType);

        var items = await queryMapped
            .Take((int)(SyncService?.Configurations?.BatchSize ?? 0))
            .ToListAsync();

        // Store the last key of the current batch
        this.lastSyncId = items?.Select(x => this.Configurations.SourceKey?.Compile()(x))?.Max();

        return items;
    }

    private IQueryable<TEntity> ApplyBatchKeyFilter(IQueryable<TEntity> query, string keyName, object lastSyncId)
    {
        switch (lastSyncId)
        {
            case int lastInt:
                return query.Where(x => EF.Property<int>(x, keyName) > lastInt);
            case long lastLong:
                return query.Where(x => EF.Property<long>(x, keyName) > lastLong);
            case Guid lastGuid:
                return query.Where(x => EF.Property<Guid>(x, keyName) != lastGuid); // Use '>' if your logic requires ordering
            case string lastString:
                return query.Where(x => string.Compare(EF.Property<string>(x, keyName), lastString) > 0);
            default:
                throw new NotSupportedException($"Key type '{lastSyncId.GetType()}' is not supported for batching.");
        }
    }

    public async ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        try
        {
            var succeededIds = input.Input?.StoreDataResult?.SucceededItems?.Select(x => Configurations?.DestinationKey?.Compile()(x!)).Distinct();
            var failedIds = input.Input?.StoreDataResult?.FailedItems?.Select(x => Configurations?.DestinationKey?.Compile()(x!)).Distinct();

            // Exclude failed items from the succeeded items
            if (failedIds?.Any() == true && succeededIds?.Any() == true)
                succeededIds = succeededIds.Except(failedIds);  // This is helpful if one source item have multiple reference items in destination

            // Update the last sync timestamp for the succeeded items
            if(succeededIds?.Any() == true)
            {
                var keyName = Utility.GetPropertyName(Configurations!.EntityKey!);
                var syncTimestampName = Utility.GetPropertyName(Configurations!.SyncTimestamp!);

                await dbSet
                    .Where(x => succeededIds!.Contains(EF.Property<object>(x, keyName)))
                    .ExecuteUpdateAsync(x => x.SetProperty(p => EF.Property<DateTimeOffset?>(p, syncTimestampName), this.lastSyncTimestamp), cancellationToken: input.CancellationToken);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public ValueTask Reset()
    {
        lastSyncTimestamp = null;
        Configurations = null;

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Reset();

        db?.Dispose();
        SyncService = null;

        return ValueTask.CompletedTask;
    }

    #region Not Implemented
    public ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask Succeeded()
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<TDestination?>?> Mapping(IEnumerable<TSource?>? sourceItems, SyncActionType actionType)
    {
        throw new NotImplementedException();
    }

    public ValueTask<SyncPreparingResponseAction> Preparing(SyncFunctionInput input)
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

    public ValueTask<RetryAction> BatchRetry(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> ActionCompleted(SyncFunctionInput<SyncActionCompletedInput> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> ActionStarted(SyncFunctionInput<SyncActionType> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<TDestination?>?> AdvancedMapping(SyncFunctionInput<SyncMappingInput<TSource, TDestination>> input)
    {
        throw new NotImplementedException();
    }
    #endregion
}
