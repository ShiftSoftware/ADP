using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Represents a labor operation required for a safety recall (SSC) repair.
/// </summary>
[TypeScriptModel]
[Docable]
public class SSCLaborDTO
{
    /// <summary>The labor operation code.</summary>
    public string LaborCode { get; set; }
    /// <summary>A description of the labor operation.</summary>
    public string LaborDescription { get; set; }
    /// <summary>The allowed time in hours for this labor operation.</summary>
    public decimal? AllowedTime { get; set; }
}