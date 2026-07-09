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
    /// <summary>
    /// Whether this part is currently in stock for the requester: <c>true</c> = in stock, <c>false</c> = not in
    /// stock, <c>null</c> = availability was not checked (the requester has no Hub stock scope, or the recall is
    /// already repaired). The UI renders these three states as a green check, a red cross, and a neutral grey
    /// chip respectively.
    /// </summary>
    public bool? IsAvailable { get; set; }
}