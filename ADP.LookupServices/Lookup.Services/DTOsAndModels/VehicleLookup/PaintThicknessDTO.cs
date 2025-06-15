using ShiftSoftware.ADP.Models;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class PaintThicknessDTO
{
    public IEnumerable<PaintThicknessPartDTO> Parts { get; set; }

    public IEnumerable<PaintThicknessImageDTO> ImageGroups { get; set; } = new List<PaintThicknessImageDTO>();
}