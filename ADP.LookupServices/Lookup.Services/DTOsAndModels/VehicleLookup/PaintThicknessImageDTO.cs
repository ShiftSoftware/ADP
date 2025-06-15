using ShiftSoftware.ADP.Models;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class PaintThicknessImageDTO
{
    public string Name { get; set; }
    public IEnumerable<string> Images { get; set; }
}