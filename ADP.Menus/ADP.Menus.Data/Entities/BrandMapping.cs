using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication;
using ShiftSoftware.ShiftEntity.Model.Flags;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class BrandMapping : ShiftEntity<BrandMapping>, IEntityHasBrand<BrandMapping>, IShiftEntityReplication
{
    public string Code { get; set; } = default!;
    public string BrandAbbreviation { get; set; } = default!;
    public long? BrandID { get; set; }

    public BrandMapping()
    {
        
    }

    public BrandMapping(long id) : base(id)
    {
        
    }

    // Cosmos replication bookkeeping (IShiftEntityReplication). Written only by the replication
    // pipeline's MarkReplicated — never by application code.
    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }
}



