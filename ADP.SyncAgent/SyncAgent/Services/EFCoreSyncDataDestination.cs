using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

namespace ShiftSoftware.ADP.SyncAgent.Services;

public class EFCoreSyncDataDestination<T, TDbContext> : EFCoreSyncDataDestination<T, T, T, TDbContext>, ISyncDataAdapter<T, T, EFCoreSyncDataDestinationConfigurations, EFCoreSyncDataDestination<T, TDbContext>>
    where T : class
    where TDbContext : DbContext
{
    public EFCoreSyncDataDestination(TDbContext db) : base(db)
    {
    }

    public virtual new EFCoreSyncDataDestination<T, TDbContext> SetSyncService(ISyncEngine<T, T> syncService)
    {
        base.SetSyncService(syncService);
        return this;
    }
}

public class EFCoreSyncDataDestination<TSource, TDestination, TDbContext> : EFCoreSyncDataDestination<TSource, TDestination, TDestination, TDbContext>, ISyncDataAdapter<TSource, TDestination, EFCoreSyncDataDestinationConfigurations, EFCoreSyncDataDestination<TSource, TDestination, TDbContext>>
    where TSource : class
    where TDestination : class
    where TDbContext : DbContext
{
    public EFCoreSyncDataDestination(TDbContext db) : base(db)
    {
    }

    public virtual new EFCoreSyncDataDestination<TSource, TDestination, TDbContext> SetSyncService(ISyncEngine<TSource, TDestination> syncService)
    {
        base.SetSyncService(syncService);
        return this;
    }
}

public class EFCoreSyncDataDestination<TSource, TDestination, TEntity, TDbContext>
    : ISyncDataAdapter<TSource, TDestination, EFCoreSyncDataDestinationConfigurations, EFCoreSyncDataDestination<TSource, TDestination, TEntity, TDbContext>>
    where TEntity : class
    where TSource : class
    where TDestination : class
    where TDbContext : DbContext
{
    private readonly TDbContext db;
    private readonly DbSet<TEntity> dbSet;

    public EFCoreSyncDataDestinationConfigurations? Configurations { get; private set; }

    public ISyncEngine<TSource, TDestination> SyncService { get; private set; }

    private readonly BulkConfig bulkConfig;
    private readonly ILogger<EFCoreSyncDataDestination<TSource, TDestination, TEntity, TDbContext>>? logger;

    public EFCoreSyncDataDestination(TDbContext db)
    {
        this.db = db;
        dbSet = db.Set<TEntity>();
        this.bulkConfig = new BulkConfig
        {
            SetOutputIdentity = true,
            BulkCopyTimeout = 0
        };

        this.logger = db.GetService<ILogger<EFCoreSyncDataDestination<TSource, TDestination, TEntity, TDbContext>>>();
    }

    public EFCoreSyncDataDestination<TSource, TDestination, TEntity, TDbContext> SetSyncService(ISyncEngine<TSource, TDestination> syncService)
    {
        SyncService = syncService;
        return this;
    }

    public ISyncEngine<TSource, TDestination> Configure(EFCoreSyncDataDestinationConfigurations configurations, bool configureSyncService = true)
    {
        Configurations = configurations;
        var previousStoreBatchDataa = SyncService.StoreBatchData;

        if (configureSyncService)
            SyncService.SetupStoreBatchData(this.StoreBatchData);

        return SyncService;
    }

    public async ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input)
    {
        var items = input.Input.Items ?? [];

        try
        {
            await db.BulkInsertOrUpdateAsync(items, this.Configurations?.BulkConfig, cancellationToken: input.CancellationToken);
            await db.BulkSaveChangesAsync(this.bulkConfig);

            return new SyncStoreDataResult<TDestination>(items, null, null, null);
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex.Message);
            return new SyncStoreDataResult<TDestination>(null, null, items, new RetryException(ex));
        }
    }

    public ValueTask Reset()
    {
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

    public ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<RetryAction> BatchRetry(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask Failed(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    public ValueTask Finished(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<TSource?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TSource>> input)
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

    public ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask Succeeded(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    #endregion
}
