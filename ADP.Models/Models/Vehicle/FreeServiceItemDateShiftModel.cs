using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents an adjustment to the eligibility start date of free service items for a specific vehicle.
/// Used to override the default free service start date (which is typically derived from the warranty activation or invoice date).
/// </summary>
[Docable]
public class FreeServiceItemDateShiftModel : IPartitionedItem, ICompanyProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) this date shift applies to.
    /// </summary>
    public string VIN { get; set; }

    /// <summary>
    /// The new free service eligibility start date for this vehicle.
    /// </summary>
    public DateTime NewDate { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.FreeServiceItemDateShift;

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    /// <summary>
    /// Indicates whether this date shift record has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }
}