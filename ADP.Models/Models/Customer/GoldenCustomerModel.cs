namespace ShiftSoftware.ADP.Models.Customer;

public class GoldenCustomerModel :
    IPartitionedItem,
    ICustomerProps
{
    public string id { get; set; }
    public string GoldenCustomerID { get; set; } //Will be the same as CustomerID
    public string CustomerID { get; set; } //Will be the same as GoldenCustomerID
    public string ItemType => ModelTypes.GoldenCustomer;
}


/// https://dataladder.com/guide-to-data-survivorship-how-to-build-the-golden-record/
/// https://www.stibosystems.com/blog/benefits-of-creating-golden-customer-records
/// https://www.melissa.com/address-experts/what-is-survivorship