using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum DurationType
{
    [Description("Seconds")]
    Seconds,

    [Description("Minutes")]
    Minutes,

    [Description("Hours")]
    Hours,

    [Description("Days")]
    Days,

    [Description("Weeks")]
    Weeks,

    [Description("Months")]
    Months,

    [Description("Years")]
    Years
}