using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Represents a part line in a vehicle's service history.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehiclePartDTO
{
    /// <summary>The part number used in the service.</summary>
    public string PartNumber { get; set; }
    /// <summary>The quantity of this part used.</summary>
    public decimal? QTY { get; set; }
    /// <summary>The package code if this part is part of a service package.</summary>
    public string PackageCode { get; set; }
    /// <summary>A description of the part.</summary>
    public string PartDescription { get; set; }
}