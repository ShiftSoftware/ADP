using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// Units of time duration used for validity periods, active durations, and other time-based configurations.
/// </summary>
[Docable]
public enum DurationType
{
    /// <summary>
    /// No duration type specified.
    /// </summary>
    [Description("Not Specified")]
    NotSpecified = 0,

    /// <summary>
    /// Duration measured in seconds.
    /// </summary>
    [Description("Seconds")]
    Seconds = 1,

    /// <summary>
    /// Duration measured in minutes.
    /// </summary>
    [Description("Minutes")]
    Minutes = 2,

    /// <summary>
    /// Duration measured in hours.
    /// </summary>
    [Description("Hours")]
    Hours = 3,

    /// <summary>
    /// Duration measured in days.
    /// </summary>
    [Description("Days")]
    Days = 4,

    /// <summary>
    /// Duration measured in weeks.
    /// </summary>
    [Description("Weeks")]
    Weeks = 5,

    /// <summary>
    /// Duration measured in months.
    /// </summary>
    [Description("Months")]
    Months = 6,

    /// <summary>
    /// Duration measured in years.
    /// </summary>
    [Description("Years")]
    Years = 7
}