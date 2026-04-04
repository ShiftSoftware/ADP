using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// The type of customer in the system.
/// </summary>
[Docable]
public enum CustomerTypes
{
    /// <summary>
    /// An individual retail customer.
    /// </summary>
    [Description("Retail")]
    Retail = 1,

    /// <summary>
    /// A business or organization customer.
    /// </summary>
    [Description("Organization")]
    Organization = 2,
}