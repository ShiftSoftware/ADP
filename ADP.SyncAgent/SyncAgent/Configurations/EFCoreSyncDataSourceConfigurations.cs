using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent.Configurations;

public class EFCoreSyncDataSourceConfigurations<TSource, TDestination> : EFCoreSyncDataSourceConfigurations<TSource, TSource, TDestination>
    where TSource : class
    where TDestination : class
{
    public EFCoreSyncDataSourceConfigurations(): base()
    {
    }

    public EFCoreSyncDataSourceConfigurations(Func<IQueryable<TSource>, SyncActionType, IQueryable<TSource>> query) : base(query)
    {
    }

    public EFCoreSyncDataSourceConfigurations(
        Func<IQueryable<TSource>, SyncActionType, IQueryable<TSource>> query,
        Expression<Func<TSource, object>> sourceKey,
        Expression<Func<TDestination, object>> destinationKey,
        Expression<Func<TSource, DateTimeOffset?>> syncTimestamp,
        bool updateSyncTimeStampForSkippedItems = false) 
        : base(query, sourceKey, sourceKey, destinationKey, syncTimestamp, updateSyncTimeStampForSkippedItems)
    {
    }
}

public class EFCoreSyncDataSourceConfigurations<TEntity, TSource, TDestination>
    where TEntity : class
    where TSource : class
    where TDestination : class
{
    public Func<IQueryable<TEntity>, SyncActionType, IQueryable<TSource>> Query { get; set; }
    public Expression<Func<TEntity, object>>? EntityKey { get; set; }
    public Expression<Func<TSource, object>>? SourceKey { get; set; }
    public Expression<Func<TDestination, object>>? DestinationKey { get; set; }

    /// <summary>
    /// IF true, the sync timestamp will be updated for items that were skipped during the store proccess.
    /// Default is false.
    /// </summary>
    public bool UpdateSyncTimeStampForSkippedItems { get; set; } = false;

    /// <summary>
    /// Should be a DateTime or DateTimeOffset property.
    /// </summary>
    public Expression<Func<TEntity, DateTimeOffset?>>? SyncTimestamp { get; set; }

    public EFCoreSyncDataSourceConfigurations()
    {
        
    }

    public EFCoreSyncDataSourceConfigurations(Func<IQueryable<TEntity>, SyncActionType, IQueryable<TSource>> query, bool updateSyncTimeStampForSkippedItems = false)
    {
        this.Query = query;
        this.UpdateSyncTimeStampForSkippedItems = updateSyncTimeStampForSkippedItems;
    }

    public EFCoreSyncDataSourceConfigurations(
        Func<IQueryable<TEntity>, SyncActionType, IQueryable<TSource>> query, 
        Expression<Func<TEntity, object>> entityKey,
        Expression<Func<TSource, object>> sourceKey,
        Expression<Func<TDestination, object>> destinationKey,
        Expression<Func<TEntity, DateTimeOffset?>> syncTimestamp,
        bool updateSyncTimeStampForSkippedItems = false) : this(query, updateSyncTimeStampForSkippedItems)
    {
        this.EntityKey = entityKey;
        this.SourceKey = sourceKey;
        this.DestinationKey = destinationKey;
        this.SyncTimestamp = syncTimestamp;
    }
}
