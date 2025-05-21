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
        Expression<Func<TSource, DateTimeOffset?>> syncTimestamp) 
        : base(query, sourceKey, destinationKey, syncTimestamp)
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
    public Expression<Func<TDestination, object>>? DestinationKey { get; set; }

    /// <summary>
    /// Should be a DateTime or DateTimeOffset property.
    /// </summary>
    public Expression<Func<TEntity, DateTimeOffset?>>? SyncTimestamp { get; set; }

    public EFCoreSyncDataSourceConfigurations()
    {
        
    }

    public EFCoreSyncDataSourceConfigurations(Func<IQueryable<TEntity>, SyncActionType, IQueryable<TSource>> query)
    {
        this.Query = query;
    }

    public EFCoreSyncDataSourceConfigurations(
        Func<IQueryable<TEntity>, SyncActionType, IQueryable<TSource>> query, 
        Expression<Func<TEntity, object>> entityKey,
        Expression<Func<TDestination, object>> destinationKey,
        Expression<Func<TEntity, DateTimeOffset?>> syncTimestamp) : this(query)
    {
        this.EntityKey = entityKey;
        this.DestinationKey = destinationKey;
        this.SyncTimestamp = syncTimestamp;
    }
}
