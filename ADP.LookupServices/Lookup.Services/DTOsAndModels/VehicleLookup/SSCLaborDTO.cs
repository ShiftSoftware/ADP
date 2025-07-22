using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class SSCLaborDTO
{
    public string LaborCode { get; set; }
    public string LaborDescription { get; set; }
    public decimal? AllowedTime { get; set; }
}