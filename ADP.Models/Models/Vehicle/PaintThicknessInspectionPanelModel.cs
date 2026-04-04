using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a single panel measurement from a <see cref="PaintThicknessInspectionModel">Paint Thickness Inspection</see>.
/// Each panel is identified by its type, side, and position on the vehicle, with a measured thickness value.
/// </summary>
[Docable]
public class PaintThicknessInspectionPanelModel
{
    /// <summary>
    /// The type of vehicle panel (e.g., Door, Fender, Hood, Roof, Trunk).
    /// </summary>
    public VehiclePanelType PanelType { get; set; }

    /// <summary>
    /// The measured paint thickness on this panel, in microns.
    /// </summary>
    public decimal MeasuredThickness { get; set; }

    /// <summary>
    /// The side of the vehicle this panel is on (Left, Center, or Right).
    /// </summary>
    public VehiclePanelSide? PanelSide { get; set; }

    /// <summary>
    /// The position of the panel on the vehicle (Front, Middle, or Rear).
    /// </summary>
    public VehiclePanelPosition? PanelPosition { get; set; }

    /// <summary>
    /// Photo URLs of this panel taken during the inspection.
    /// </summary>
    public IEnumerable<string> Images { get; set; } = [];
}