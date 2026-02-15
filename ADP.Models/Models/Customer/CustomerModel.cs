using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

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
    public long? CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public string FullName { get; set; }
    public IEnumerable<string> PhoneNumbers { get; set; } = [];
    public IEnumerable<string> Address { get; set; } = [];
    public Genders Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }


    /// <summary>
    /// Social ID, Driver License Number, Passport Number, or any other government-issued ID number that can be used to uniquely identify a customer across different systems.
    /// </summary>
    public string IDNumber { get; set; }
}

/// https://dataladder.com/guide-to-data-survivorship-how-to-build-the-golden-record/
/// https://www.stibosystems.com/blog/benefits-of-creating-golden-customer-records
/// https://www.melissa.com/address-experts/what-is-survivorship