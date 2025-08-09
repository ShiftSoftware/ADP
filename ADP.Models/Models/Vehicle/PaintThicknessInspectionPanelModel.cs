using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class PaintThicknessInspectionPanelModel
{
    public VehiclePanelType PanelType { get; set; }
    public decimal MeasuredThickness { get; set; }
    public VehiclePanelSide? PanelSide { get; set; }
    public VehiclePanelPosition? PanelPosition { get; set; }
    public IEnumerable<string> Images { get; set; } = [];
}

