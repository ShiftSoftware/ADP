using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// The longitudinal position of a vehicle body panel (front to rear).
/// </summary>
[Docable]
public enum VehiclePanelPosition
{
    /// <summary>
    /// Front section of the vehicle.
    /// </summary>
    [Description("Front")]
    Front = 1,

    /// <summary>
    /// Middle section of the vehicle.
    /// </summary>
    [Description("Middle")]
    Middle = 2,

    /// <summary>
    /// Rear section of the vehicle.
    /// </summary>
    [Description("Rear")]
    Rear = 3,
}