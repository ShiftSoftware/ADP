using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[Index(nameof(MenuVariantID), nameof(ServiceIntervalGroupID))]
[TemporalShiftEntity]
public class MenuLabourDetails : ShiftEntity<MenuLabourDetails>
{
    public long MenuVariantID { get; set; }
    public MenuVariant MenuVariant { get; set; } = default!;

    public long ServiceIntervalGroupID { get; set; }
    public ServiceIntervalGroup ServiceIntervalGroup { get; set; }

    [Precision(6, 2)]
    public decimal AllowedTime { get; set; }

    [Precision(6, 2)]
    public decimal Consumable { get; set; }
}
