using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;

/// <summary>
/// One part on a menu line, priced for the requested country.
///
/// Retail only — there is deliberately no dealer cost here. See <see cref="ServiceMenuLineDTO"/>.
/// </summary>
[Docable]
public class ServiceMenuPartDTO
{
    /// <summary>The part number as authored on the menu item.</summary>
    public string PartNumber { get; set; }

    /// <summary>The authored display order within its menu item.</summary>
    public int SortOrder { get; set; }

    /// <summary>How many are used by this service.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Retail unit price for the requested country. 0 when the part has no price row there.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary><see cref="UnitPrice"/> × <see cref="Quantity"/>.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// False when no price row matched the requested country — the prices above are 0 by fallback rather
    /// than because the part is free. Distinguishing the two is the caller's business, so it is surfaced
    /// rather than hidden.
    /// </summary>
    public bool HasCountryPrice { get; set; }
}
