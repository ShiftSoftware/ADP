using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class AccessoryDTO
{
    public string PartNumber { get; set; }
    public string Description { get; set; }
    public string Image { get; set; }
}
