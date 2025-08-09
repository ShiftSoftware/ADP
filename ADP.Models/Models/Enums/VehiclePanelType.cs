using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum VehiclePanelType
{
    [Description("Door")]
    Door = 1,

    [Description("Fender")]
    Fender = 2,

    [Description("Roof")]
    Roof = 3,

    [Description("Hood")]
    Hood = 4,

    [Description("Tail Gate")]
    TailGate = 5,
}
