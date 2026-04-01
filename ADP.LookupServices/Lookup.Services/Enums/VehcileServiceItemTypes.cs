using ShiftSoftware.ADP.Models;
using System.ComponentModel;

namespace ShiftSoftware.ADP.Lookup.Services.Enums;

/// <summary>
/// The type of a vehicle service item (free or paid).
/// </summary>
[Docable]
public enum VehcileServiceItemTypes
{
    /// <summary>A free service item provided through a campaign.</summary>
    [Description("Free")]
    Free = 0,
    /// <summary>A paid service item purchased via a service invoice.</summary>
    [Description("Paid")]
    Paid = 1,
}