using System;

namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents the initial vehicle record when a vehicle first enters a broker's inventory.
/// This is created when a broker receives a vehicle before any dealer system entries exist.
/// </summary>
[Docable]
public class BrokerInitialVehicleModel : IPartitionedItem
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The unique identifier for this initial vehicle record.
    /// </summary>
    public long ID { get; set; }

    /// <summary>
    /// The ID of the broker that received this vehicle.
    /// </summary>
    public long BrokerID { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN).
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// Indicates whether this initial vehicle record has been deleted.
    /// </summary>
    public bool Deleted { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.BrokerInitialVehicle;
}