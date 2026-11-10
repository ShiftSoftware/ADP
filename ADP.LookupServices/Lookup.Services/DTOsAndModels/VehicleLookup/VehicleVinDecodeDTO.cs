using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// What a VIN's first 8 characters say about a vehicle the distributor has no record of. These values are inferred from the
/// VIN, so they are kept apart from the vehicle's own identifiers and specification.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleVinDecodeDTO
{
    /// <summary>
    /// The base Katashiki the VIN's first 8 characters point to. Null when they do not settle on one code.
    /// </summary>
    public string Katashiki { get; set; }

    /// <summary>
    /// The model name the VIN's first 8 characters point to.
    /// </summary>
    public string ModelDescription { get; set; }

    /// <summary>
    /// The model year VIN character 10 stands for. Null when that character is <c>0</c> or not a year code.
    /// The code repeats every 30 years, so a vehicle older than that reads 30 years too late.
    /// </summary>
    public int? ModelYear { get; set; }
}
