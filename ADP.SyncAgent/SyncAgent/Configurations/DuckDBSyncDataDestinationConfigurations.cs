using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent.Configurations;

public class DuckDBSyncDataDestinationConfigurations<TSource, TDestination>
    where TSource : class
    where TDestination : class
{
    public required string TableName { get; set; }

    public Expression<Func<TDestination, object>>? PrimaryKey { get; set; }

    public bool ContinueAfterFail { get; set; }
}
