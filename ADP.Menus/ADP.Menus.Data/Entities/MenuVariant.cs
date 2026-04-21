using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Flags;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class MenuVariant : ShiftEntity<MenuVariant>
{
    public long MenuID { get; set; }
    public Menu Menu { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string MenuPrefix { get; set; } = default!;

    public string? MenuPostfix { get; set; }

    public string? StandaloneMenuPrefix { get; set; }

    public string? StandaloneMenuPostfix { get; set; }

    [Precision(12, 2)]
    public decimal LabourRate { get; set; }

    [Precision(5, 2)]
    public decimal? DiscountPercentage { get; set; }

    public bool HasStandaloneItems { get; set; }

    public virtual ICollection<MenuVariantLabourRate> LabourRates { get; set; } = new HashSet<MenuVariantLabourRate>();
    public virtual ICollection<MenuLabourDetails> LabourDetails { get; set; } = new HashSet<MenuLabourDetails>();
    public virtual ICollection<MenuPeriodicAvailability> PeriodicAvailabilities { get; set; } = new HashSet<MenuPeriodicAvailability>();
    public virtual ICollection<MenuItem> Items { get; set; } = new HashSet<MenuItem>();
}
