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
}
