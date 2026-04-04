using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a comprehensive vehicle inspection record.
/// Captures inspection details, technician information, vehicle photos, and customer information at the time of the inspection.
/// </summary>
[Docable]
public class VehicleInspectionModel : IPartitionedItem, ICompanyProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The type of vehicle inspection performed.
    /// </summary>
    public long VehicleInspectionTypeID { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) of the inspected vehicle.
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// The date the inspection was performed.
    /// </summary>
    public DateTimeOffset InspectionDate { get; set; }

    /// <summary>
    /// The vehicle model description.
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// The model year of the inspected vehicle.
    /// </summary>
    public int ModelYear { get; set; }

    /// <summary>
    /// The model code of the inspected vehicle.
    /// </summary>
    public string ModelCode { get; set; } = default!;

    /// <summary>
    /// The job number from the dealer's service system for this inspection.
    /// </summary>
    public string JobNumber { get; set; }

    /// <summary>
    /// The name of the technician who performed the inspection.
    /// </summary>
    public string TechnicianName { get; set; }

    /// <summary>
    /// The name of the quality control reviewer for this inspection.
    /// </summary>
    public string QualityControlName { get; set; }

    /// <summary>
    /// URL of the front photo of the vehicle taken during the inspection.
    /// </summary>
    public string FrontPhoto { get; set; }

    /// <summary>
    /// URL of the rear photo of the vehicle taken during the inspection.
    /// </summary>
    public string RearPhoto { get; set; }

    /// <summary>
    /// The country ID of the customer at the time of inspection.
    /// </summary>
    public long? CustomerCountryID { get; set; }

    /// <summary>
    /// The city ID of the customer at the time of inspection.
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
    /// Indicates whether this inspection record has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.VehicleInspection;

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }
}