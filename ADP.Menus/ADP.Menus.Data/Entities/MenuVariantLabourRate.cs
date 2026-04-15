using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class MenuVariantLabourRate : ShiftEntity<MenuVariantLabourRate>
{
    public long MenuVariantID { get; set; }
    public MenuVariant MenuVariant { get; set; } = default!;

    public long CountryID { get; set; }

    [Precision(12, 2)]
    public decimal LabourRate { get; set; }
}
