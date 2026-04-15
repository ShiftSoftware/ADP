using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class MenuItemPart : ShiftEntity<MenuItemPart>
{
    public long MenuItemID { get; set; }
    public MenuItem MenuItem { get; set; } = default!;

    public int SortOrder { get; set; }

    public string PartNumber { get; set; } = default!;

    [Precision(6,3)]
    public decimal? PeriodicQuantity { get; set; }

    [Precision(6, 3)]
    public decimal? StandaloneQuantity { get; set; }

    public virtual ICollection<MenuItemPartCountryPrice> CountryPrices { get; set; } = new HashSet<MenuItemPartCountryPrice>();
}
