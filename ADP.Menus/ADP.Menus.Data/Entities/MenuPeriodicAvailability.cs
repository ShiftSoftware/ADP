using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[Index(nameof(MenuVariantID), nameof(ServiceIntervalID), IsUnique = true)]
[TemporalShiftEntity]
public class MenuPeriodicAvailability : ShiftEntity<MenuPeriodicAvailability>
{
    public long MenuVariantID { get; set; }
    public MenuVariant MenuVariant { get; set; } = default!;

    public long ServiceIntervalID { get; set; }
    public ServiceInterval ServiceInterval { get; set; }
}
