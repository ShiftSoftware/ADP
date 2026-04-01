using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Customer;

/// <summary>
/// Represents a customer record from a dealer's system.
/// Each dealer may have its own customer record for the same person. The <see cref="GoldenCustomerModel">Golden Customer</see> links these into a unified identity.
/// </summary>
[Docable]
public class CustomerModel :
    IPartitionedItem,
    ICustomerProps,
    ICompanyProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The corresponding <see cref="GoldenCustomerModel">Golden Customer</see> ID that this dealer customer is linked to.
    /// </summary>
    public string GoldenCustomerID { get; set; }

    /// <summary>
    /// The original customer ID from the dealer's system.
    /// </summary>
    public string CustomerID { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.DealerCustomer;

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    /// <summary>
    /// The customer's full name.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// A list of phone numbers associated with the customer.
    /// </summary>
    public IEnumerable<string> PhoneNumbers { get; set; } = [];

    /// <summary>
    /// The customer's address lines.
    /// </summary>
    public IEnumerable<string> Address { get; set; } = [];

    /// <summary>
    /// The customer's gender.
    /// </summary>
    public Genders Gender { get; set; }

    /// <summary>
    /// The customer's date of birth.
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Social ID, Driver License Number, Passport Number, or any other government-issued ID number that can be used to uniquely identify a customer across different systems.
    /// </summary>
    public string IDNumber { get; set; }
}

/// https://dataladder.com/guide-to-data-survivorship-how-to-build-the-golden-record/
/// https://www.stibosystems.com/blog/benefits-of-creating-golden-customer-records
/// https://www.melissa.com/address-experts/what-is-survivorship