using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class PaintThicknessPartDTO
{
    public string Part { get; set; }
    public string Left { get; set; }
    public string Right { get; set; }
}