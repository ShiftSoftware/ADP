using ShiftSoftware.ADP.Models;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>The states an item can be shown in without being claimable.</summary>
[Docable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VehicleServiceItemLockState
{
    /// <summary>
    /// Not earned yet, and still earnable. Shown from the first lookup, so a customer can see what
    /// returning for their services is worth before they have had any.
    /// </summary>
    Locked = 0,

    /// <summary>
    /// Was earnable and no longer is. Told rather than hidden, because an item that quietly vanishes
    /// is exactly the confusion these states exist to remove.
    /// </summary>
    Missed = 1,
}
