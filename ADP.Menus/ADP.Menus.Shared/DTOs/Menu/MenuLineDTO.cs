namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

/// <summary>
/// One generated menu line, as the DMS export's report rows see it.
///
/// PLAIN DATA. The derived margin / cost / profit arithmetic that used to live here now lives with the
/// report layer (<c>ShiftSoftware.ADP.Menus.Data.DataServices.MenuLineMargins</c>) as extension
/// members — import that namespace to keep reading <c>line.MenuTotalPrice</c> and friends, and see
/// that type for why it moved and for the one caveat (serializers do not see extension members).
///
/// This type is the export's own shape (layer 3). It is mapped FROM
/// <c>ShiftSoftware.ADP.Menus.Generation.GeneratedMenuLine</c> — see COSMOS_REPLICATION_PLAN.md §1.1.
/// </summary>
public class MenuLineDTO
{
    public string Code { get; set; }
    public long? BrandID { get; set; }
    public string BasicModelCode { get; set; }
    public string Model { get; set; }

    /// <summary>
    /// The line's description as the generator chose it: for a scheduled line, the service interval's
    /// description for the variant's menu type; for a standalone one, the item's or group's name.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The scheduled line's service interval descriptions, all three of them, whatever the variant's menu
    /// type — for a report that needs one other than <see cref="Description"/>. Null on standalone lines,
    /// which have no interval.
    /// </summary>
    public string? PaidDescription { get; set; }

    /// <inheritdoc cref="PaidDescription"/>
    public string? FreeDescription { get; set; }

    /// <inheritdoc cref="PaidDescription"/>
    public string? ContractDescription { get; set; }

    public string LabourCode { get; set; }
    public decimal LabourRate { get; set; }
    public decimal AllowedTime { get; set; }
    public decimal Consumable { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public bool IsStandalone { get; set; }
    public IEnumerable<MenuLinePartDTO> Parts { get; set; }
}

public class MenuLinePartDTO
{
    public string PartNumber { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>Dealer cost. Export-only — never expose this outside the report layer.</summary>
    public decimal Cost { get; set; }

    /// <summary>Retail price.</summary>
    public decimal Price { get; set; }

    public decimal TotalPrice => Price * Quantity;
    public decimal TotalCost => Cost * Quantity;
}
