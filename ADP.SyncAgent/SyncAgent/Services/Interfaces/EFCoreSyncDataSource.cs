using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.SyncAgent.Configurations;

namespace ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

public class EFCoreSyncDataSource<TSource, TDestination, TDbContext> : EFCoreSyncDataSource<TSource, TSource, TDestination, TDbContext>
    where TSource : class, new()
    where TDestination : class, new()
    where TDbContext : DbContext
{
    public EFCoreSyncDataSource(TDbContext db) : base(db)
    {
    }
}

public class EFCoreSyncDataSource<TEntity, TSource, TDestination, TDbContext> 
    : ISyncDataAdapter<TSource, TDestination, EFCoreSyncDataSourceConfigurations<TEntity, TSource, TDestination>, EFCoreSyncDataSource<TEntity, TSource, TDestination, TDbContext>>
    where TEntity : class, new()
    where TSource : class, new()
    where TDestination : class, new()
    where TDbContext : DbContext
{
    private readonly TDbContext db;
    private readonly DbSet<TEntity> dbSet;

    private DateTimeOffset? lastSyncTimestamp = null;

    public EFCoreSyncDataSourceConfigurations<TEntity, TSource, TDestination>? Configurations { get; private set; }
    public ISyncService<TSource, TDestination> SyncService { get; private set; }

    public EFCoreSyncDataSource(TDbContext db)
    {
        this.db = db;
        this.dbSet = db.Set<TEntity>();
    }

    public EFCoreSyncDataSource<TEntity, TSource, TDestination, TDbContext> SetSyncService(ISyncService<TSource, TDestination> syncService)
    {
        this.SyncService = syncService;
        return this;
    }

    public ISyncService<TSource, TDestination> Configure(EFCoreSyncDataSourceConfigurations<TEntity, TSource, TDestination> configurations, bool configureSyncService = true)
    {
        this.Configurations = configurations;
        var previousBatchStarted = this.SyncService.BatchStarted;
        var previousBatchCompleted = this.SyncService.BatchCompleted;

        if (configureSyncService)
            this.SyncService
                .SetupSourceTotalItemCount(this.SourceTotalItemCount)
                .SetupBatchStarted(async (x) =>
                {
                    if (previousBatchStarted is not null)
                        return (await this.BatchStarted(x)) && (await previousBatchStarted(x));

                    return await this.BatchStarted(x);
                })
                .SetupGetSourceBatchItems(this.GetSourceBatchItems)
                .SetupBatchCompleted(async x =>
                {
                    if (previousBatchCompleted is not null)
                        return (await this.BatchCompleted(x)) && (await previousBatchCompleted(x));

                    return await this.BatchCompleted(x);
                });

        return this.SyncService;
    }

    public ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input)
    {
        return new((long?)null);
    }

    public ValueTask<bool> BatchStarted(SyncFunctionInput<SyncActionStatus> input)
    {
        this.lastSyncTimestamp = DateTimeOffset.UtcNow;
        return new(true);
    }

    public async ValueTask<IEnumerable<TSource?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TSource>> input)
    {
        var query = dbSet.AsQueryable().AsNoTracking();
        var queryMapped = Configurations!.Query(query, input.Input.Status.ActionType);
        return await queryMapped.Take((int)(this.SyncService?.Configurations?.BatchSize ?? 0)).ToListAsync();
    }

    public async ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        try
        {
            // Get the id of the succeeded items
            var succeededIds = input.Input?.StoreDataResult?.SucceededItems?.Select(x => this.Configurations?.DestinationKey?.Compile()(x!));

            // Update the last sync timestamp for the succeeded items
            await this.dbSet
                .Where(x => succeededIds!.Contains(this.Configurations!.EntityKey!.Compile()(x)))
                .ExecuteUpdateAsync(x => x.SetProperty(x => this.Configurations!.EntityKey!.Compile()(x), lastSyncTimestamp), cancellationToken: input.CancellationToken);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public ValueTask Reset()
    {
        this.lastSyncTimestamp = null;
        this.Configurations = null;

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Reset();

        db?.Dispose();
        this.SyncService = null;

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
