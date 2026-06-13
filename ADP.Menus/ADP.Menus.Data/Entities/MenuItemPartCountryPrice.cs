using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class MenuItemPartCountryPrice : ShiftEntity<MenuItemPartCountryPrice>
{
    public long MenuItemPartID { get; set; }
    public MenuItemPart MenuItemPart { get; set; } = default!;

    public long CountryID { get; set; }

    [Precision(12, 3)]
    public decimal? PartPrice { get; set; }

    [Precision(9, 3)]
    public decimal? PartPriceMarginPercentage { get; set; }

    [Precision(12, 3)]
    public decimal PartFinalPrice { get; set; }

    /// <summary>The retail unit selected for this price (e.g. each, box). The available units are not persisted.</summary>
    [MaxLength(100)]
    public string? SelectedUnitName { get; set; }
}
