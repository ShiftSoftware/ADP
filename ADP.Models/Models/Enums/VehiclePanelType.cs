using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// The type of vehicle body panel, used in paint thickness inspections.
/// </summary>
[Docable]
public enum VehiclePanelType
{
    /// <summary>
    /// A vehicle door panel.
    /// </summary>
    [Description("Door")]
    Door = 1,

    /// <summary>
    /// A vehicle fender panel.
    /// </summary>
    [Description("Fender")]
    Fender = 2,

    /// <summary>
    /// The vehicle roof panel.
    /// </summary>
    [Description("Roof")]
    Roof = 3,

    /// <summary>
    /// The vehicle hood (bonnet) panel.
    /// </summary>
    [Description("Hood")]
    Hood = 4,

    /// <summary>
    /// The vehicle tail gate (trunk lid / rear hatch) panel.
    /// </summary>
    [Description("Tail Gate")]
    TailGate = 5,
}
