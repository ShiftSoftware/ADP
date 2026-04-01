using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Represents a paint thickness inspection record for a vehicle, as returned by the vehicle lookup.
/// </summary>
[TypeScriptModel]
[Docable]
public class PaintThicknessInspectionDTO
{
    /// <summary>The source system or process that performed this inspection.</summary>
    public string Source { get; set; }
    /// <summary>The date the inspection was performed.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InspectionDate { get; set; }
    /// <summary>The individual <see cref="PaintThicknessInspectionPanelDTO">panel measurements</see> taken during the inspection.</summary>
    public IEnumerable<PaintThicknessInspectionPanelDTO> Panels { get; set; }
}