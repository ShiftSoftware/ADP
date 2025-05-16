using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent;

public class CosmosSyncDataDestinationConfigurations<TCosmos> where TCosmos : class, new()
{
    public required string DatabaseId { get; set; } = default!;
    public required string ContainerId { get; set; } = default!;

    /// <summary>
    /// If true the sync process will stop when one of the items fails to be upserted after retries,
    /// and false to continue the sync after items failed to be upserted,
    /// The default is true.
    /// </summary>
    public bool StopOperationWhenOneFailed { get; set; } = true;

    public required Expression<Func<TCosmos, object>> PartitionKeyLevel1Expression { get; set; } = default!;
    public Expression<Func<TCosmos, object>>? PartitionKeyLevel2Expression { get; set; }
    public Expression<Func<TCosmos, object>>? PartitionKeyLevel3Expression { get; set; }
    public Func<SyncAgentCosmosAction<TCosmos>, ValueTask<IEnumerable<SyncAgentCosmosAction<TCosmos>?>?>>? CosmosAction { get; set; }
}
