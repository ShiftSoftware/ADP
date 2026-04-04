using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Represents a labor line in a vehicle's service history.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleLaborDTO
{
    /// <summary>The labor operation code from the manufacturer's service catalog.</summary>
    public string LaborCode { get; set; }
    /// <summary>The package code if this labor is part of a service package.</summary>
    public string PackageCode { get; set; }
    /// <summary>The distributor-level service code.</summary>
    public string ServiceCode { get; set; }
    /// <summary>A description of the labor operation performed.</summary>
    public string ServiceDescription { get; set; }
}