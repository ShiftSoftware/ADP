using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class ReplacementItemVehicleModelPart : ShiftEntity<ReplacementItemVehicleModelPart>
{
    public long ReplacementItemVehicleModelID { get; set; }
    public ReplacementItemVehicleModel ReplacementItemVehicleModel { get; set; } = default!;

    public int SortOrder { get; set; }

    public string PartNumber { get; set; } = default!;

    [Precision(6, 3)]
    public decimal? DefaultPeriodicQuantity { get; set; }

    [Precision(6, 3)]
    public decimal? DefaultStandaloneQuantity { get; set; }
}
