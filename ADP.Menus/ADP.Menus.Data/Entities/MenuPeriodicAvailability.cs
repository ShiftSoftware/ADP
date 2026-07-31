using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[Index(nameof(MenuVariantID), nameof(ServiceIntervalID), IsUnique = true)]
[TemporalShiftEntity]
public class MenuPeriodicAvailability : ShiftEntity<MenuPeriodicAvailability>, IShiftEntityReplication
{
    public long MenuVariantID { get; set; }
    public MenuVariant MenuVariant { get; set; } = default!;

    public long ServiceIntervalID { get; set; }
    public ServiceInterval ServiceInterval { get; set; }

    // Cosmos replication bookkeeping (IShiftEntityReplication). Written only by the replication
    // pipeline's MarkReplicated — never by application code.
    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }
}
