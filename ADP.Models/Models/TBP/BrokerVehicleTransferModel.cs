using System;

namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents a vehicle transfer record stored in the vehicle aggregate.
/// Tracks the movement of a vehicle from one broker to another, with a unique transfer number.
/// </summary>
[Docable]
public class BrokerVehicleTransferModel
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The unique identifier for this transfer.
    /// </summary>
    public long ID { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) being transferred.
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// The date the transfer was executed.
    /// </summary>
    public DateTime TransferDate { get; set; }

    /// <summary>
    /// The unique transfer number for tracking.
    /// </summary>
    public long TransferNumber { get; set; }

    /// <summary>
    /// Indicates whether this transfer has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The broker ID of the seller (source) in this transfer.
    /// </summary>
    public long? FromBrokerID { get; set; }

    /// <summary>
    /// The broker ID of the buyer (destination) in this transfer.
    /// </summary>
    public long? ToBrokerID { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.BrokerVehicleTransfer;
}