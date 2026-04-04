using ShiftSoftware.ADP.Models;
using System.ComponentModel;

namespace ShiftSoftware.ADP.Lookup.Services.Enums;

/// <summary>
/// The status of a vehicle service item in the claiming lifecycle.
/// </summary>
[Docable]
public enum VehcileServiceItemStatuses
{
    /// <summary>The service item has been claimed/processed.</summary>
    [Description("Processed")]
    Processed = 0,
    /// <summary>The service item's validity period has expired.</summary>
    [Description("Expired")]
    Expired = 1,
    /// <summary>The service item is available and waiting to be claimed.</summary>
    [Description("Pending")]
    Pending = 2,
    /// <summary>The service item has been cancelled.</summary>
    [Description("Cancelled")]
    Cancelled = 3,
    /// <summary>The service item requires warranty activation before it can be claimed.</summary>
    [Description("Activation Required")]
    ActivationRequired = 4,
}