using System;

namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents an invoice in the Third-Party Broker (TBP) system for a vehicle sale from a broker to an end customer.
/// </summary>
[Docable]
public class TBP_Invoice
{
    /// <summary>
    /// The unique identifier for this invoice.
    /// </summary>
    public long ID { get; set; }

    /// <summary>
    /// Indicates whether this invoice has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The date of the invoice.
    /// </summary>
    public DateTimeOffset InvoiceDate { get; set; }

    /// <summary>
    /// Indicates whether the invoice has been completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// The invoice number.
    /// </summary>
    public long? InvoiceNumber { get; set; }

    /// <summary>
    /// The name of the end customer on this invoice.
    /// </summary>
    public string CustomerName { get; set; }

    /// <summary>
    /// The phone number of the end customer.
    /// </summary>
    public string CustomerPhone { get; set; }

    /// <summary>
    /// The government-issued ID number of the end customer.
    /// </summary>
    public string CustomerIDNumber { get; set; }
}
