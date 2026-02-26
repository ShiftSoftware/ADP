using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class PaintThicknessInspectionPanelDTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehiclePanelType PanelType { get; set; }
    public decimal MeasuredThickness { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehiclePanelSide? PanelSide { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehiclePanelPosition? PanelPosition { get; set; }
    public IEnumerable<string> Images { get; set; } = [];
}