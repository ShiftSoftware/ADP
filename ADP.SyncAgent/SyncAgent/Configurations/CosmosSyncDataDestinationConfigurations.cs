using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent.Configurations;

public class CosmosSyncDataDestinationConfigurations<TCosmos> where TCosmos : class
{
    public required string DatabaseId { get; set; } = default!;
    public required string ContainerId { get; set; } = default!;

    public required Expression<Func<TCosmos, object>> PartitionKeyLevel1Expression { get; set; } = default!;
    public Expression<Func<TCosmos, object>>? PartitionKeyLevel2Expression { get; set; }
    public Expression<Func<TCosmos, object>>? PartitionKeyLevel3Expression { get; set; }
    public Func<SyncAgentCosmosAction<TCosmos>, ValueTask<IEnumerable<SyncAgentCosmosAction<TCosmos>?>?>>? CosmosAction { get; set; }
}
