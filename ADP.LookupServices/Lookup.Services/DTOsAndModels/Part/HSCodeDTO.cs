using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

[TypeScriptModel]
public class HSCodeDTO
{
    public string CountryIntegrationID { get; set; }
    public string CountryName { get; set; }
    public string HSCode { get; set; }
}