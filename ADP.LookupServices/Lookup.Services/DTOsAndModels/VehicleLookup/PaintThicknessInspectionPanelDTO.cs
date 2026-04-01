using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Represents a single panel measurement from a paint thickness inspection.
/// </summary>
[TypeScriptModel]
[Docable]
public class PaintThicknessInspectionPanelDTO
{
    /// <summary>The type of vehicle panel (e.g., Door, Fender, Hood, Roof, Trunk).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehiclePanelType PanelType { get; set; }
    /// <summary>The measured paint thickness on this panel, in microns.</summary>
    public decimal MeasuredThickness { get; set; }
    /// <summary>The side of the vehicle this panel is on (Left, Center, Right).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehiclePanelSide? PanelSide { get; set; }
    /// <summary>The position of the panel on the vehicle (Front, Middle, Rear).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehiclePanelPosition? PanelPosition { get; set; }
    /// <summary>Photo URLs of this panel taken during the inspection.</summary>
    public IEnumerable<string> Images { get; set; } = [];
}