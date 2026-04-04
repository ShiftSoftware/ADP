using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents an invoice for paid service work performed on a vehicle.
/// Contains the invoice metadata and a collection of <see cref="PaidServiceInvoiceLineModel">line items</see> detailing the services performed.
/// </summary>
[Docable]
public class PaidServiceInvoiceModel :
    IPartitionedItem,
    IBrandProps,
    ICompanyProps,
    IBranchProps,
    IIntegrationProps
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The date of the service invoice.
    /// </summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>
    /// The date the service was started.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The invoice number for this paid service.
    /// </summary>
    public long InvoiceNumber { get; set; }

    /// <summary>
    /// Indicates whether this invoice has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) this invoice is for.
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// The <see cref="PaidServiceInvoiceLineModel">line items</see> on this invoice, each representing a service performed.
    /// </summary>
    public virtual IEnumerable<PaidServiceInvoiceLineModel> Lines { get; set; }

    [DocIgnore]
    public long? BrandID { get; set; }

    /// <summary>
    /// The Brand Hash ID from the Identity System.
    /// </summary>
    public string BrandHashID { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.PaidServiceInvoice;

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    [DocIgnore]
    public long? BranchID { get; set; }

    /// <summary>
    /// The Branch Hash ID from the Identity System.
    /// </summary>
    public string BranchHashID { get; set; }

    /// <summary>
    /// An external identifier used for system-to-system integration.
    /// </summary>
    public string IntegrationID { get; set; }
}