using ShiftSoftware.ADP.Models;
namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class VehicleServiceItemGroup
{
    public string Name { get; set; }
    public int TabOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsSequential { get; set; }
}