using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication;
using ShiftSoftware.ShiftEntity.Model.Flags;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class LabourRateMapping : ShiftEntity<LabourRateMapping>, IEntityHasBrand<LabourRateMapping>, IShiftEntityReplication
{
    [Precision(12, 2)]
    public decimal LabourRate { get; set; }
    public string Code { get; set; } = default!;

    public long? BrandID { get; set; }

    public LabourRateMapping()
    {
        
    }

    public LabourRateMapping(long id) : base(id)
    {
        
    }

    // Cosmos replication bookkeeping (IShiftEntityReplication). Written only by the replication
    // pipeline's MarkReplicated — never by application code.
    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }
}

