using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[Index(nameof(VehicleModelID), nameof(ServiceIntervalGroupID))]
[TemporalShiftEntity]
public class VehicleModelLabourDetails : ShiftEntity<VehicleModelLabourDetails>
{
    public long VehicleModelID { get; set; }
    public VehicleModel VehicleModel { get; set; }

    public long ServiceIntervalGroupID { get; set; }
    public ServiceIntervalGroup ServiceIntervalGroup { get; set; }

    [Precision(6, 2)]
    public decimal AllowedTime { get; set; }

    [Precision(6, 2)]
    public decimal Consumable { get; set; }
}
