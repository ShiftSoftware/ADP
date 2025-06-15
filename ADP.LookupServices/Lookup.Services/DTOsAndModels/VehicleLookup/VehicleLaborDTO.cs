using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class VehicleLaborDTO
{
    public string RTSCode { get; set; }
    public string MenuCode { get; set; }
    public string ServiceCode { get; set; }
    public string Description { get; set; }
}