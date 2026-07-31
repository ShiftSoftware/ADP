using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class StandaloneReplacementItemGroup : ShiftEntity<StandaloneReplacementItemGroup>, IShiftEntityReplication
{
    public string Name { get; set; } = default!;
    public string MenuCode { get; set; } = default!;
    public string LabourCode { get; set; } = default!;
    public IEnumerable<ReplacementItem> ReplacementItems { get; set; }

    // Cosmos replication bookkeeping (IShiftEntityReplication). Written only by the replication
    // pipeline's MarkReplicated — never by application code.
    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }
}
