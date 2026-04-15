using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Flags;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class LabourRateMapping : ShiftEntity<LabourRateMapping>, IEntityHasBrand<LabourRateMapping>
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
}

