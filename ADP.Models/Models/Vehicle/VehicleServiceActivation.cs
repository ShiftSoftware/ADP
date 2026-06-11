using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents the activation of warranty services for a vehicle.
/// Captures when the warranty was activated and the customer information at the time of activation.
/// </summary>
[Docable]
public class VehicleServiceActivation : IPartitionedItem, ICompanyProps, ICountryProps, IRegionProps, IBranchProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN).
    /// </summary>
    public string VIN { get; set; }

    /// <summary>
    /// The date when the vehicle's warranty services were activated.
    /// </summary>
    public DateTime? WarrantyActivationDate { get; set; }

    /// <summary>
    /// The country ID of the customer at the time of service activation.
    /// </summary>
    public long? CustomerCountryID { get; set; }

    /// <summary>
    /// The city ID of the customer at the time of service activation.
    /// </summary>
    public long? CustomerCityID { get; set; }

    /// <summary>
    /// The type of customer (e.g., Individual, Organization).
    /// </summary>
    public CustomerTypes? CustomerType { get; set; }

    /// <summary>
    /// The organization name if the customer is an organization.
    /// </summary>
    public string OrganizationName { get; set; }

    /// <summary>
    /// The customer's first name.
    /// </summary>
    public string CustomerFirstName { get; set; } = default!;

    /// <summary>
    /// The customer's middle name.
    /// </summary>
    public string CustomerMiddleName { get; set; } = default!;

    /// <summary>
    /// The customer's last name.
    /// </summary>
    public string CustomerLastName { get; set; } = default!;

    /// <summary>
    /// The customer's phone number.
    /// </summary>
    public string CustomerPhone { get; set; } = default!;

    /// <summary>
    /// The customer's email address.
    /// </summary>
    public string CustomerEmail { get; set; }

    /// <summary>
    /// The customer's gender.
    /// </summary>
    public Genders CustomerGender { get; set; }

    /// <summary>
    /// Indicates whether this activation record has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.VehicleServiceActivation;

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    // The activating company's location context (country / region / branch). Stamped by the
    // upstream system at activation time so the lookup pipeline can resolve the vehicle's
    // owning company/country/region from the activation alone — the matching VehicleEntry may
    // not have synced yet (vehicle still in national stock). See VehicleOwnership.

    [DocIgnore]
    public long? CountryID { get; set; }

    /// <summary>
    /// The Country Hash ID from the Identity System.
    /// </summary>
    public string CountryHashID { get; set; }

    [DocIgnore]
    public long? RegionID { get; set; }

    /// <summary>
    /// The Region Hash ID from the Identity System.
    /// </summary>
    public string RegionHashID { get; set; }

    [DocIgnore]
    public long? BranchID { get; set; }

    /// <summary>
    /// The Branch Hash ID from the Identity System.
    /// </summary>
    public string BranchHashID { get; set; }
}