using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(FullName), IsUnique = true)]
[Index(nameof(ValueInMeter), IsUnique = true)]
public class ServiceInterval : ShiftEntity<ServiceInterval>, IShiftEntityReplication
{
    public string Code { get; set; }
    public string FullName { get; set; }
    public int ValueInMeter { get; set; }

    /// <summary>
    /// The interval's description, one per menu type: a scheduled line takes the one matching its
    /// variant's <c>MenuType</c>. Used verbatim — never language-resolved.
    /// </summary>
    public string PaidDescription { get; set; }

    /// <inheritdoc cref="PaidDescription"/>
    public string FreeDescription { get; set; }

    /// <inheritdoc cref="PaidDescription"/>
    public string ContractDescription { get; set; }

    public long ServiceIntervalGroupID { get; set; }
    public ServiceIntervalGroup ServiceIntervalGroup { get; set; }

    public ICollection<MenuPeriodicAvailability> MenuPeriodicAvailabilities { get; set; } = new HashSet<MenuPeriodicAvailability>();

    public ServiceInterval()
    {
        
    }

    public ServiceInterval(long id) : base(id)
    {
        
    }

    // Cosmos replication bookkeeping (IShiftEntityReplication). Written only by the replication
    // pipeline's MarkReplicated — never by application code.
    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }
}
