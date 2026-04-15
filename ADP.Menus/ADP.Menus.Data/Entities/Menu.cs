using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Flags;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class Menu : ShiftEntity<Menu>, IEntityHasBrand<Menu>
{
    public string BasicModelCode { get; set; } = default!;

    public long? BrandID { get; set; }

    public long? VehicleModelID { get; set; }
    public VehicleModel? VehicleModel { get; set; }

    public virtual ICollection<MenuVariant> Variants { get; set; } = new HashSet<MenuVariant>();
}
