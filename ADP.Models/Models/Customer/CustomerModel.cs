namespace ShiftSoftware.ADP.Models.Customer;


public class CustomerModel : 
    IPartitionedItem,
    ICustomerProps,
    ICompanyProps
{
    public string id { get; set; }
    public string GoldenCustomerID { get; set; } //Will be assigned a corresponding GoldenCustomerID
    public string CustomerID { get; set; } // The original customer ID from dealer system
    public string ItemType => ModelTypes.DealerCustomer;
    public string CompanyID { get; set; }
    public string CompanyHashID { get; set; }
}

/// https://dataladder.com/guide-to-data-survivorship-how-to-build-the-golden-record/
/// https://www.stibosystems.com/blog/benefits-of-creating-golden-customer-records
/// https://www.melissa.com/address-experts/what-is-survivorship