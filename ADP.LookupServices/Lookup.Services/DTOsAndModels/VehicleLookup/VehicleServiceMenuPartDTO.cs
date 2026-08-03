using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// One part on a vehicle service-menu line, priced for the country the menu was generated for.
///
/// <para>Retail only. There is deliberately no dealer cost, margin or profit here and none may be added —
/// see <see cref="VehicleServiceMenuLineDTO"/>. This type is served to public web components.</para>
///
/// <para>Field-for-field the same as the service-menu lookup's own
/// <c>DTOsAndModels.ServiceMenu.ServiceMenuPartDTO</c>, and a test holds the two in step. It is a separate
/// type rather than a reuse because the TypeScript generator emits same-directory imports, so everything
/// reachable from <see cref="VehicleLookupDTO"/> has to live beside it.</para>
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleServiceMenuPartDTO
{
    /// <summary>The part number as authored on the menu item.</summary>
    public string PartNumber { get; set; }

    /// <summary>The authored display order within its menu item.</summary>
    public int SortOrder { get; set; }

    /// <summary>How many are used by this service.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Retail unit price for the country the menu was generated for. 0 when there is no price row.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary><see cref="UnitPrice"/> × <see cref="Quantity"/>.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// False when no price row matched the country — the prices above are 0 by fallback rather than because
    /// the part is free. A UI quoting a total needs to be able to tell those apart, so it is surfaced.
    /// </summary>
    public bool HasCountryPrice { get; set; }
}
