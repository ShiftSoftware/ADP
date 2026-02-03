using ShiftSoftware.ADP.Models;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[Obsolete("This class is deprecated. Use PaintThicknessInspectionDTO instead.")]
[TypeScriptModel]
public class LegacyPaintThicknessDTO
{
    public IEnumerable<LegacyPaintThicknessPartDTO> Parts { get; set; } = new List<LegacyPaintThicknessPartDTO>();
    public IEnumerable<LegacyPaintThicknessImageGroupDTO> ImageGroups { get; set; } = new List<LegacyPaintThicknessImageGroupDTO>();
}