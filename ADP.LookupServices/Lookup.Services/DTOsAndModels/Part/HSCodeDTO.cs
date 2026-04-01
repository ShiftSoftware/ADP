using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// Represents a Harmonized System (HS) code for a part in a specific country.
/// </summary>
[TypeScriptModel]
[Docable]
public class HSCodeDTO
{
    /// <summary>The country's integration ID.</summary>
    public string CountryIntegrationID { get; set; }
    /// <summary>The resolved country name.</summary>
    public string CountryName { get; set; }
    /// <summary>The HS code for this part in this country.</summary>
    public string HSCode { get; set; }
}