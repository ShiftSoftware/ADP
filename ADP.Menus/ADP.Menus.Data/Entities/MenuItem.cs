using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class MenuItem : ShiftEntity<MenuItem>, IShiftEntityReplication
{
    public long MenuVariantID { get; set; }
    public MenuVariant MenuVariant { get; set; } = default!;

    public long? ReplacementItemVehicleModelID { get; set; }
    public ReplacementItemVehicleModel? ReplacementItemVehicleModel { get; set; }

    [Precision(6, 2)]
    public decimal StandaloneAllowedTime { get; set; }

    /// <summary>
    /// When the values on this MenuItem were last reconciled against the vehicle-model
    /// replacement-item defaults. Compared with <see cref="ReplacementItemVehicleModel.PendingSince"/>
    /// to tell whether this row still needs propagation after a defaults change.
    /// </summary>
    public DateTimeOffset? LastPropagatedAt { get; set; }

    public virtual ICollection<MenuItemPart> Parts { get; set; } = new HashSet<MenuItemPart>();

    // Cosmos replication bookkeeping (IShiftEntityReplication). Written only by the replication
    // pipeline's MarkReplicated — never by application code.
    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }
}
