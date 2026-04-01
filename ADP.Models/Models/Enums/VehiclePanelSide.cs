using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// The lateral side of a vehicle body panel (left to right).
/// </summary>
[Docable]
public enum VehiclePanelSide
{
    /// <summary>
    /// Left side of the vehicle.
    /// </summary>
    [Description("Left")]
    Left = 1,

    /// <summary>
    /// Center of the vehicle.
    /// </summary>
    [Description("Center")]
    Center = 2,

    /// <summary>
    /// Right side of the vehicle.
    /// </summary>
    [Description("Right")]
    Right = 3,
}