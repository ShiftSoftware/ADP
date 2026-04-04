using System;

namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents a broker invoice record stored in the vehicle aggregate.
/// Tracks the sale of a vehicle from a broker to an end customer, including customer references.
/// </summary>
[Docable]
public class BrokerInvoiceModel : IPartitionedItem
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The Vehicle Identification Number (VIN) this invoice is for.
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// The date of the broker invoice.
    /// </summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>
    /// The ID of the official broker customer who purchased the vehicle.
    /// </summary>
    public long? BrokerCustomerID { get; set; }

    /// <summary>
    /// Indicates whether this invoice has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The unique identifier for this invoice.
    /// </summary>
    public long ID { get; set; }

    /// <summary>
    /// The invoice number.
    /// </summary>
    public long InvoiceNumber { get; set; }

    /// <summary>
    /// The ID of a non-official broker customer, if the buyer is not in the official customer database.
    /// </summary>
    public long? NonOfficialBrokerCustomerID { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.BrokerInvoice;
}