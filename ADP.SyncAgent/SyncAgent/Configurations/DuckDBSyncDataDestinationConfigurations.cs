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

    /// <summary>
    /// Called from Preparing when the live table had to be changed to match the model (columns added).
    /// The natural use is to re-queue changes a diff queue gave up on while the table was behind —
    /// see <c>DuckDbCsvSyncDataSource.RequeueDeadChangesAsync</c>. Runs before any batch of the run.
    /// </summary>
    public Func<DuckDBSchemaChange, Task>? SchemaChanged { get; set; }
}

/// <summary>What <see cref="Services.DuckDBSyncDataDestination{TSource, TDestination, DuckDB}"/> changed on a live table to match its model.</summary>
/// <param name="TableName">The live table.</param>
/// <param name="AddedColumns">Model columns the table lacked and now has (NULL in every existing row).</param>
/// <param name="IgnoredColumns">Table columns the model no longer declares; kept, never written.</param>
public sealed record DuckDBSchemaChange(string TableName, IReadOnlyList<string> AddedColumns, IReadOnlyList<string> IgnoredColumns)
{
    public override string ToString()
        => $"{TableName}: added [{string.Join(", ", AddedColumns)}]" + (IgnoredColumns.Count > 0 ? $", ignored [{string.Join(", ", IgnoredColumns)}]" : string.Empty);
}
