using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Represents a part required for a safety recall (SSC) repair.
/// </summary>
[TypeScriptModel]
[Docable]
public class SSCPartDTO
{
    /// <summary>The part number required for the recall repair.</summary>
    public string PartNumber { get; set; } = default!;
    /// <summary>A description of the part.</summary>
    public string PartDescription { get; set; }
    /// <summary>Whether this part is currently available in stock.</summary>
    public bool IsAvailable { get; set; }
}