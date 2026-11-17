using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication;
using ShiftSoftware.ShiftEntity.Model.Flags;

namespace ShiftSoftware.ADP.Menus.Data.Entities;

[TemporalShiftEntity]
public class MenuVariant : ShiftEntity<MenuVariant>, IShiftEntityReplication
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

    /// <summary>
    /// The types the variant's menu is offered under — any of paid, free, under contract — at least one;
    /// stored as a JSON array of the enum values, e.g. [0,1]. Carried as a label only: it changes no
    /// generated price or code. With more than one, each period and item lists which of them it is
    /// visible under, and only the lookup reads that.
    /// </summary>
    public List<MenuType> MenuTypes { get; set; } = [MenuType.Paid];

    /// <summary>The first of <see cref="MenuTypes"/> in enum order. Setting it makes the variant single-typed.</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public MenuType MenuType
    {
        get => MenuTypeSet.Normalize(MenuTypes).FirstOrDefault();
        set => MenuTypes = [value];
    }

    public bool HasStandaloneItems { get; set; }

    public virtual ICollection<MenuVariantLabourRate> LabourRates { get; set; } = new HashSet<MenuVariantLabourRate>();
    public virtual ICollection<MenuLabourDetails> LabourDetails { get; set; } = new HashSet<MenuLabourDetails>();
    public virtual ICollection<MenuPeriodicAvailability> PeriodicAvailabilities { get; set; } = new HashSet<MenuPeriodicAvailability>();
    public virtual ICollection<MenuItem> Items { get; set; } = new HashSet<MenuItem>();

    // Cosmos replication bookkeeping (IShiftEntityReplication). Written only by the replication
    // pipeline's MarkReplicated — never by application code.
    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }
}
