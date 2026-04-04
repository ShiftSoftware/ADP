using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// Gender options used for customer and contact records.
/// </summary>
[Docable]
public enum Genders
{
    /// <summary>
    /// Gender not specified.
    /// </summary>
    [Description("Not Specified")]
    NotSpecified = 0,

    /// <summary>
    /// Male.
    /// </summary>
    [Description("Male")]
    Male = 1,

    /// <summary>
    /// Female.
    /// </summary>
    [Description("Female")]
    Female = 2
}