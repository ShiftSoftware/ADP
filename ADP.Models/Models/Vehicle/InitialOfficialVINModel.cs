using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents the initial official VIN registration record for a vehicle.
/// This captures the first time a vehicle's VIN is officially recorded in the system, typically upon data entry by the distributor.
/// </summary>
[Docable]
public class InitialOfficialVINModel : IPartitionedItem, ICompanyProps
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The Vehicle Identification Number (VIN).
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// The vehicle model description at the time of initial registration.
    /// </summary>
    public string Model { get; set; } = default!;

    /// <summary>
    /// The date of the initial official VIN registration.
    /// </summary>
    public DateTime Date { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.InitialOfficialVIN;

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }
}