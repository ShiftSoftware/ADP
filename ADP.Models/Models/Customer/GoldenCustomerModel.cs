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
}

/// https://dataladder.com/guide-to-data-survivorship-how-to-build-the-golden-record/
/// https://www.stibosystems.com/blog/benefits-of-creating-golden-customer-records
/// https://www.melissa.com/address-experts/what-is-survivorship