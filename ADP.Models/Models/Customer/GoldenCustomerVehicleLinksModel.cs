using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Customer;

/// <summary>
/// The by-customer projection of vehicle ownership: every VIN a <see cref="GoldenCustomerModel"/>
/// has been linked to, with role, period, and evidence — the customer-360 "vehicles owned" read.
/// One document per golden identity that has any vehicle links.
/// Lives in the same logical partition as the golden document itself (no CompanyID — a golden
/// identity may span dealers — and CustomerID = GoldenCustomerID), so a customer page can fetch
/// the golden and its vehicle links from one partition.
/// </summary>
[Docable]
public class GoldenCustomerVehicleLinksModel :
    IPartitionedItem,
    ICustomerProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The <see cref="GoldenCustomerModel">Golden Customer</see> these links belong to.
    /// </summary>
    public string GoldenCustomerID { get; set; }

    /// <summary>
    /// The customer identifier. Same value as <see cref="GoldenCustomerID"/> (keeps the document
    /// in the golden's partition).
    /// </summary>
    public string CustomerID { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.GoldenCustomerVehicleLinks;

    /// <summary>
    /// The customer's vehicle links, one per VIN, ordered by VIN.
    /// </summary>
    public IEnumerable<GoldenVehicleLinkModel> Links { get; set; } = [];
}
