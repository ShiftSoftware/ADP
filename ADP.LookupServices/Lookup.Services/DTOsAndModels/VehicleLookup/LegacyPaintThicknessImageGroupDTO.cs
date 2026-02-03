using ShiftSoftware.ADP.Models;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class LegacyPaintThicknessImageGroupDTO
{
    public string Name { get; set; }
    public List<string> Images { get; set; } = new List<string>();
}