using System;

namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents a vehicle entry from the dealer system as it appears in the TBP stock context.
/// Used to validate broker stock quantities against dealer records.
/// </summary>
[Docable]
public class TBP_VehicleEntryModel
{
    /// <summary>
    /// The invoice date from the dealer system.
    /// </summary>
    public DateTime? InvoiceDate { get; set; }

    /// <summary>
    /// The customer account number on the dealer invoice.
    /// </summary>
    public string CustomerAccountNumber { get; set; }

    /// <summary>
    /// The order status from the dealer system.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// The line item status from the dealer system.
    /// </summary>
    public string LineStatus { get; set; }

    /// <summary>
    /// The location/warehouse identifier from the dealer system.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// The company ID from the dealer system.
    /// </summary>
    public long? CompanyID { get; set; }

    /// <summary>
    /// The region identifier from the dealer system.
    /// </summary>
    public string? Region { get; set; }
}
