using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a line item within a <see cref="PaidServiceInvoiceModel">Paid Service Invoice</see>.
/// Each line corresponds to a specific <see cref="ServiceItemModel">Service Item</see> that was performed and paid for.
/// </summary>
[Docable]
public class PaidServiceInvoiceLineModel : IIntegrationProps
{
    /// <summary>
    /// The ID of the <see cref="ServiceItemModel">Service Item</see> that was performed.
    /// </summary>
    public string ServiceItemID { get; set; }

    /// <summary>
    /// The cost charged for this service line item.
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// The expiration date for the service coverage provided by this line item.
    /// </summary>
    public DateTime? ExpireDate { get; set; }

    /// <summary>
    /// The package code grouping this line item with related services.
    /// </summary>
    public string PackageCode { get; set; }

    /// <summary>
    /// The <see cref="ServiceItemModel">Service Item</see> details associated with this line.
    /// </summary>
    public ServiceItemModel ServiceItem { get; set; }

    /// <summary>
    /// An external identifier used for system-to-system integration.
    /// </summary>
    public string IntegrationID { get; set; }
}