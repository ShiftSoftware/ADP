using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Collections;

namespace ShiftSoftware.ADP.SyncAgent.Services;

public class EFCoreSyncDataSource<T, TDbContext> : EFCoreSyncDataSource<T, T, T, TDbContext>,
    ISyncDataAdapter<T, T, EFCoreSyncDataSourceConfigurations<T>, EFCoreSyncDataSource<T, TDbContext>>
    where T : class
    where TDbContext : DbContext
{
    public EFCoreSyncDataSource(TDbContext db) : base(db)
    {
    }

    public virtual new EFCoreSyncDataSourceConfigurations<T>? Configurations => (EFCoreSyncDataSourceConfigurations<T>?)base.Configurations;

    public virtual ISyncEngine<T, T> Configure(EFCoreSyncDataSourceConfigurations<T> configurations, bool configureSyncService = true)
    {
        base.Configure(configurations, configureSyncService);
        return this.SyncService;
    }

    public virtual new EFCoreSyncDataSource<T, TDbContext> SetSyncService(ISyncEngine<T, T> syncService)
    {
        base.SetSyncService(syncService);
        return this;
    }
}

public class EFCoreSyncDataSource<TSource, TDestination, TDbContext> : EFCoreSyncDataSource<TSource, TSource, TDestination, TDbContext>,
    ISyncDataAdapter<TSource, TDestination, EFCoreSyncDataSourceConfigurations<TSource, TDestination>, EFCoreSyncDataSource<TSource, TDestination, TDbContext>>
    where TSource : class
    where TDestination : class
    where TDbContext : DbContext
{
    public EFCoreSyncDataSource(TDbContext db) : base(db)
    {
    }

    public virtual new EFCoreSyncDataSourceConfigurations<TSource, TDestination>? Configurations => (EFCoreSyncDataSourceConfigurations<TSource, TDestination>?)base.Configurations;

    public virtual ISyncEngine<TSource, TDestination> Configure(EFCoreSyncDataSourceConfigurations<TSource, TDestination> configurations, bool configureSyncService = true)
    {
        base.Configure(configurations, configureSyncService);
        return this.SyncService;
    }

    public virtual new EFCoreSyncDataSource<TSource, TDestination, TDbContext> SetSyncService(ISyncEngine<TSource, TDestination> syncService)
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

    public virtual EFCoreSyncDataSourceConfigurations<TEntity, TSource, TDestination>? Configurations { get; private set; }
    public virtual ISyncEngine<TSource, TDestination> SyncService { get; private set; }

    public EFCoreSyncDataSource(TDbContext db)
    {
        this.db = db;
        dbSet = db.Set<TEntity>();
    }

    public virtual EFCoreSyncDataSource<TEntity, TSource, TDestination, TDbContext> SetSyncService(ISyncEngine<TSource, TDestination> syncService)
    {
        SyncService = syncService;
        return this;
    }

    public virtual ISyncEngine<TSource, TDestination> Configure(EFCoreSyncDataSourceConfigurations<TEntity, TSource, TDestination> configurations, bool configureSyncService = true)
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

    public virtual ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input)
    {
        return new((long?)null);
    }

    public virtual ValueTask<bool> BatchStarted(SyncFunctionInput<SyncActionStatus> input)
    {
        lastSyncTimestamp = DateTimeOffset.UtcNow;
        return new(true);
    }

    public virtual async ValueTask<IEnumerable<TSource?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TSource>> input)
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
            .ToListAsync(input.CancellationToken);

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
                return query.Where(x => EF.Property<Guid>(x, keyName) > lastGuid); // Use '>' if your logic requires ordering
            case string lastString:
                return query.Where(x => string.Compare(EF.Property<string>(x, keyName), lastString) > 0);
            default:
                throw new NotSupportedException($"Key type '{lastSyncId.GetType()}' is not supported for batching.");
        }
    }

    public virtual async ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        if(this.Configurations?.UpdateTimeStampFilter is null && this.Configurations?.SyncTimestamp is null)
            return true;

        var queryable = dbSet.AsQueryable().AsNoTracking();

        try
        {
            if (this.Configurations?.UpdateTimeStampFilter is null)
            {
                // Skip the process if the SyncTimestamp is not set
                if (this.Configurations?.SyncTimestamp is null)
                    return true;

                var succeededIds = input.Input?.StoreDataResult?.SucceededItems?
                    .SelectMany(x =>
                    {
                        var key = Configurations?.DestinationKey?.Compile()(x!);
                        return key is IEnumerable collection && key is not string
                            ? collection.Cast<object>()
                            : [key!];
                    })?
                    .Distinct() ?? [];
                var failedIds = input.Input?.StoreDataResult?.FailedItems?
                    .SelectMany(x =>
                    {
                        var key = Configurations?.DestinationKey?.Compile()(x!);
                        return key is IEnumerable collection && key is not string
                            ? collection.Cast<object>()
                            : [key!];
                    })?
                    .Distinct() ?? [];
                var skippedIds = input.Input?.StoreDataResult?.SkippedItems?
                    .SelectMany(x =>
                    {
                        var key = Configurations?.DestinationKey?.Compile()(x!);
                        return key is IEnumerable collection && key is not string
                            ? collection.Cast<object>()
                            : [key!];
                    })?
                    .Distinct() ?? [];

                if (this.Configurations?.UpdateSyncTimeStampForSkippedItems ?? false)
                    succeededIds = succeededIds?.Union(skippedIds);

                // Exclude failed items from the succeeded items
                if (failedIds?.Any() == true && succeededIds?.Any() == true)
                    succeededIds = succeededIds.Except(failedIds);  // This is helpful if one source item have multiple reference items in destination

                var keyName = Utility.GetPropertyName(Configurations!.EntityKey!);

                queryable = queryable.Where(x => succeededIds!.Contains(EF.Property<object>(x, keyName)));
            }
            else
            {
                queryable = Configurations!.UpdateTimeStampFilter!(queryable, input);
            }

            var syncTimestampName = Utility.GetPropertyName(Configurations!.SyncTimestamp!);
            var syncTimestampProperty = typeof(TEntity).GetProperty(syncTimestampName);

            if (syncTimestampProperty != null)
            {
                var propertyType = syncTimestampProperty.PropertyType;

                if (propertyType == typeof(DateTimeOffset) || propertyType == typeof(DateTimeOffset?))
                {
                    await queryable
                        .ExecuteUpdateAsync(
                            x => x.SetProperty(
                                p => EF.Property<DateTimeOffset?>(p, syncTimestampName),
                                this.lastSyncTimestamp
                            ),
                            cancellationToken: input.CancellationToken
                        );
                }
                else if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
                {
                    await queryable
                        .ExecuteUpdateAsync(
                            x => x.SetProperty(
                                p => EF.Property<DateTime?>(p, syncTimestampName),
                                this.lastSyncTimestamp.GetValueOrDefault().UtcDateTime
                            ),
                            cancellationToken: input.CancellationToken
                        );
                }
                else
                {
                    throw new NotSupportedException($"SyncTimestamp property type '{propertyType}' is not supported. Only DateTimeOffset and DateTime are allowed.");
                }
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public virtual ValueTask Reset()
    {
        lastSyncTimestamp = null;
        Configurations = null;

        return ValueTask.CompletedTask;
    }

    public virtual ValueTask DisposeAsync()
    {
        Reset();

        db?.Dispose();
        SyncService = null;

        return ValueTask.CompletedTask;
    }

    #region Not Implemented
    public virtual ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask Succeeded(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask<IEnumerable<TDestination?>?> Mapping(IEnumerable<TSource?>? sourceItems, SyncActionType actionType)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask<SyncPreparingResponseAction> Preparing(SyncFunctionInput input)
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

    public virtual ValueTask<RetryAction> BatchRetry(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask<bool> ActionCompleted(SyncFunctionInput<SyncActionCompletedInput> input)
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
    #endregion
}
