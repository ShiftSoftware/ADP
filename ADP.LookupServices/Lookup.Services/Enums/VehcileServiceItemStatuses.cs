using System.ComponentModel;

namespace ShiftSoftware.ADP.Lookup.Services.Enums;

public enum VehcileServiceItemStatuses
{
    [Description("Processed")]
    Processed = 0,
    [Description("Expired")]
    Expired = 1,
    [Description("Pending")]
    Pending = 2,
    [Description("Cancelled")]
    Cancelled = 3,
    [Description("Activation Required")]
    ActivationRequired = 4,
}