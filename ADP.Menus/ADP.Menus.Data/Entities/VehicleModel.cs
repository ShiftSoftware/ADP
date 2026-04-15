using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Flags;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class VehicleModel : ShiftEntity<VehicleModel>, IEntityHasBrand<VehicleModel>
{
    public string Name { get; set; } = default!;

    public long? BrandID { get; set; }

    [Precision(12, 2)]
    public decimal LabourRate { get; set; }
    public ICollection<VehicleModelLabourRate> LabourRates { get; set; } = new HashSet<VehicleModelLabourRate>();

    public ICollection<VehicleModelLabourDetails> LabourDetails { get; set; } = new HashSet<VehicleModelLabourDetails>();
    public ICollection<ReplacementItemVehicleModel>? ReplacementItemVehicleModels { get; set; }
}
