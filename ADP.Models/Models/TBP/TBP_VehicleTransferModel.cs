using System;

namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents a vehicle transfer between two brokers in the TBP system.
/// Transfers affect the stock quantity calculation — the seller loses one unit and the buyer gains one.
/// </summary>
[Docable]
public class TBP_VehicleTransferModel
{
    /// <summary>
    /// The unique identifier for this transfer.
    /// </summary>
    public long ID { get; set; }

    /// <summary>
    /// Indicates whether this transfer has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The broker ID of the seller in this transfer.
    /// </summary>
    public long SellerBrokerID { get; set; }

    /// <summary>
    /// The broker ID of the buyer in this transfer.
    /// </summary>
    public long BuyerBrokerID { get; set; }

    /// <summary>
    /// The date the transfer was executed.
    /// </summary>
    public DateTimeOffset TransferDate { get; set; }
}
