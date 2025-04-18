using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum CustomerTypes
{
    [Description("Retail")]
    Retail = 1,

    [Description("Organization")]
    Organization = 2,
}