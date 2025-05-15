using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent;

public class CosmosSyncDataDestinationConfigurations<T> where T : class, new()
{
    public required string DatabaseId { get; set; } = default!;
    public required string ContainerId { get; set; } = default!;
    public required Expression<Func<T, object>> PartitionKeyLevel1Expression { get; set; } = default!;

    /// <summary>
    /// If true the sync process will stop when one of the items fails to be upserted after retries,
    /// and false to continue the sync after items failed to be upserted,
    /// The default is true.
    /// </summary>
    public bool StopOperationWhenOneFailed { get; set; } = true;

    public Expression<Func<T, object>>? PartitionKeyLevel2Expression { get; set; }
    public Expression<Func<T, object>>? PartitionKeyLevel3Expression { get; set; }
    public Func<SyncAgentCosmosAction<T>, ValueTask<IEnumerable<SyncAgentCosmosAction<T>?>?>>? CosmosAction { get; set; }
}
