using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVariant;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;

public class MenuVariantReplacementItemUsageDTO
{
    [MenuHashId]
    public string MenuID { get; set; } = default!;

    public string MenuLabel { get; set; } = default!;

    [MenuVariantHashId]
    public string VariantID { get; set; } = default!;

    public string VariantName { get; set; } = default!;

    /// <summary>Localized prefix/postfix shown under the variant name (matches MenuForm's variant chip).</summary>
    public string? MenuPrefix { get; set; }

    /// <summary>Localized prefix/postfix shown under the variant name (matches MenuForm's variant chip).</summary>
    public string? MenuPostfix { get; set; }

    /// <summary>
    /// Null when the variant does not yet contain this replacement item ("Add" case).
    /// </summary>
    public long? MenuItemID { get; set; }

    public decimal? StandaloneAllowedTime { get; set; }

    public List<MenuItemPartUsageDTO> Parts { get; set; } = new();

    /// <summary>
    /// True when this variant's MenuItem has been reconciled against the current
    /// vehicle-model defaults (LastPropagatedAt &gt;= RIVM.PendingSince), or when the RIVM
    /// has no pending change at all. False for variants that don't yet contain the item
    /// (Add case) and for variants whose values are stale.
    /// </summary>
    public bool IsPropagated { get; set; }
}

public class MenuItemPartUsageDTO
{
    public long? ID { get; set; }

    public int SortOrder { get; set; }

    public string PartNumber { get; set; } = string.Empty;

    public decimal? PeriodicQuantity { get; set; }

    public decimal? StandaloneQuantity { get; set; }

    public List<PartPriceByCountryDTO> CountryPrices { get; set; } = new();
}
