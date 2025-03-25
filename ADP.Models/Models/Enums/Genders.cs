using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum Genders
{
    [Description("Not Specified")]
    NotSpecified = 0,

    [Description("Male")]
    Male = 1,

    [Description("Female")]
    Female = 2
}