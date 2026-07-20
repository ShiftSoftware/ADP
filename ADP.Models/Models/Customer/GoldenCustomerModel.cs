using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Customer;

/// <summary>
/// Represents a Golden Customer record — a unified, deduplicated identity that links multiple <see cref="CustomerModel">Dealer Customer</see> records for the same person across different systems.
/// The GoldenCustomerID and CustomerID are always the same value on this record.
/// </summary>
[Docable]
public class GoldenCustomerModel :
    IPartitionedItem,
    ICustomerProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The unique Golden Customer identifier. This is the same value as <see cref="CustomerID"/>.
    /// </summary>
    public string GoldenCustomerID { get; set; }

    /// <summary>
    /// The customer identifier. This is the same value as <see cref="GoldenCustomerID"/>.
    /// </summary>
    public string CustomerID { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.GoldenCustomer;

    /// <summary>
    /// The survived (golden) full name — the fullest consistent name chain across the linked
    /// source records, chosen by the identity-resolution engine's survivorship rules.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// The survived phone number(s) for this identity.
    /// </summary>
    public IEnumerable<string> PhoneNumbers { get; set; } = [];

    /// <summary>
    /// The survived city / district, when the linked sources carry one.
    /// </summary>
    public string City { get; set; }

    /// <summary>
    /// The survived national / social ID number, when any linked source carries one.
    /// </summary>
    public string IDNumber { get; set; }

    /// <summary>
    /// The survived e-mail address, when any linked source carries one (app registrations and
    /// CRM tickets do; dealer DMS extracts typically don't).
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Backlinks to the source records this identity unifies, as "sourceSystem|sourceRecordId"
    /// keys. Source records also carry the forward link via their own GoldenCustomerID property.
    /// Note: a golden identity is deliberately NOT company-scoped (it may span dealers), so this
    /// document carries no CompanyID — it lives at the container's undefined level-1 partition.
    /// </summary>
    public IEnumerable<string> SourceProfiles { get; set; } = [];
}

/// https://dataladder.com/guide-to-data-survivorship-how-to-build-the-golden-record/
/// https://www.stibosystems.com/blog/benefits-of-creating-golden-customer-records
/// https://www.melissa.com/address-experts/what-is-survivorship