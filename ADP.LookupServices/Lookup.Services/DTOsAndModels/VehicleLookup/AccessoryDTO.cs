using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Represents an accessory installed on a vehicle, as returned by the vehicle lookup.
/// </summary>
[TypeScriptModel]
[Docable]
public class AccessoryDTO
{
    /// <summary>The part number of the installed accessory.</summary>
    public string PartNumber { get; set; }
    /// <summary>A description of the accessory.</summary>
    public string Description { get; set; }
    /// <summary>URL of an image of the installed accessory.</summary>
    public string Image { get; set; }
    /// <summary>The job number of the accessory installation. Null when the source has no job number.</summary>
    public int? JobNumber { get; set; }
    /// <summary>The invoice number of the accessory installation. Null when the source has no invoice number.</summary>
    public int? InvoiceNumber { get; set; }
    /// <summary>The name of the company that installed the accessory, resolved from the company ID. Null when no company name resolver is configured or the company cannot be resolved.</summary>
    public string CompanyName { get; set; }
}
