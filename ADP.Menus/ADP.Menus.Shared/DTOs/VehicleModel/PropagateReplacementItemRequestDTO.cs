using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVariant;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;

public class PropagateReplacementItemRequestDTO
{
    [ReplacementItemHashId]
    public string ReplacementItemID { get; set; } = default!;

    public List<PropagateReplacementItemVariantDTO> Variants { get; set; } = new();
}

public class PropagateReplacementItemVariantDTO
{
    [MenuVariantHashId]
    public string VariantID { get; set; } = default!;

    /// <summary>
    /// Null = create a new MenuItem on the variant. Non-null = update the existing one.
    /// </summary>
    public long? MenuItemID { get; set; }

    public decimal? StandaloneAllowedTime { get; set; }

    public List<PropagateReplacementItemPartDTO> Parts { get; set; } = new();
}

public class PropagateReplacementItemPartDTO
{
    public long? MenuItemPartID { get; set; }

    public int SortOrder { get; set; }

    public string PartNumber { get; set; } = string.Empty;

    public decimal? PeriodicQuantity { get; set; }

    public decimal? StandaloneQuantity { get; set; }

    public List<PartPriceByCountryDTO> CountryPrices { get; set; } = new();
}

public class PropagateReplacementItemResponseDTO
{
    /// <summary>
    /// True when, after this propagation, the replacement-item row no longer carries
    /// a pending-propagation flag (every variant under the vehicle model is covered).
    /// </summary>
    public bool PendingCleared { get; set; }
}
