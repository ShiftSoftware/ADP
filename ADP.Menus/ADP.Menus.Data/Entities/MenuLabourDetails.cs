using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[Index(nameof(MenuVariantID), nameof(ServiceIntervalGroupID))]
[TemporalShiftEntity]
public class MenuLabourDetails : ShiftEntity<MenuLabourDetails>, IShiftEntityReplication
{
    public long MenuVariantID { get; set; }
    public MenuVariant MenuVariant { get; set; } = default!;

    public long ServiceIntervalGroupID { get; set; }
    public ServiceIntervalGroup ServiceIntervalGroup { get; set; }

    [Precision(6, 2)]
    public decimal AllowedTime { get; set; }

    [Precision(6, 2)]
    public decimal Consumable { get; set; }

    // Cosmos replication bookkeeping (IShiftEntityReplication). Written only by the replication
    // pipeline's MarkReplicated — never by application code.
    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }
}
