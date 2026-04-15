using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class ReplacementItemVehicleModel : ShiftEntity<ReplacementItemVehicleModel>
{
    public long ReplacementItemID { get; set; }
    public long VehicleModelID { get; set; }

    [Precision(6, 2)]
    public decimal StandaloneAllowedTime { get; set; }

    [Precision(9, 3)]
    public decimal? DefaultPartPriceMarginPercentage { get; set; }

    public ReplacementItem ReplacementItem { get; set; } = default!;
    public VehicleModel VehicleModel { get; set; } = default!;

    public virtual ICollection<ReplacementItemVehicleModelPart> DefaultParts { get; set; } = new HashSet<ReplacementItemVehicleModelPart>();
    public virtual ICollection<MenuItem> MenuItems { get; set; } = new HashSet<MenuItem>();
}
