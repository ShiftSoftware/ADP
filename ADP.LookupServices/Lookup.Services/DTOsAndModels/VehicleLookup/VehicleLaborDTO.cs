using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class VehicleLaborDTO
{
    public string LaborCode { get; set; }
    public string PackageCode { get; set; }
    public string ServiceCode { get; set; }
    public string ServiceDescription { get; set; }
}