using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum DurationType
{
    [Description("Not Specified")]
    NotSpecified = 0,

    [Description("Seconds")]
    Seconds = 1,

    [Description("Minutes")]
    Minutes = 2,

    [Description("Hours")]
    Hours = 3,

    [Description("Days")]
    Days = 4,

    [Description("Weeks")]
    Weeks = 5,

    [Description("Months")]
    Months = 6,

    [Description("Years")]
    Years = 7
}