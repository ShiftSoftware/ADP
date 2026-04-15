using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class VehicleModelLabourRate : ShiftEntity<VehicleModelLabourRate>
{
    public long VehicleModelID { get; set; }
    public VehicleModel VehicleModel { get; set; } = default!;

    public long CountryID { get; set; }

    [Precision(12, 2)]
    public decimal LabourRate { get; set; }
}
