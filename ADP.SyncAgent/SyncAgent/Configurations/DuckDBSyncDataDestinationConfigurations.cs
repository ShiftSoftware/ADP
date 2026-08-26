using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent.Configurations;

public class DuckDBSyncDataDestinationConfigurations<TSource, TDestination>
    where TSource : class
    where TDestination : class
{
    public required string TableName { get; set; }

    public Expression<Func<TDestination, object>>? PrimaryKey { get; set; }

    public bool ContinueAfterFail { get; set; }

    /// <summary>
    /// Secondary indexes created with the table, for the queries that will READ it — the destination
    /// itself never needs them. See <see cref="DuckDBIndexDefinition{TDestination}"/>.
    /// </summary>
    public IReadOnlyList<DuckDBIndexDefinition<TDestination>>? Indexes { get; set; }
}
