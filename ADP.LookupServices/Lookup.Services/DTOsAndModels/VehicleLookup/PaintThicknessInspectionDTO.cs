using ShiftSoftware.ADP.Models;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class PaintThicknessInspectionDTO
{
    public string Source { get; set; }
    public DateTime? InspectionDate { get; set; }
    public IEnumerable<PaintThicknessInspectionPanelDTO> Panels { get; set; }
}