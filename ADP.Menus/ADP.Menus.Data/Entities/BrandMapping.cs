using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Flags;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

public class BrandMapping : ShiftEntity<BrandMapping>, IEntityHasBrand<BrandMapping>
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
}



