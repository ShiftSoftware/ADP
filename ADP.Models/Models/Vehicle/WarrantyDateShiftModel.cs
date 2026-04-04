using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents an adjustment to the warranty end date for a specific vehicle.
/// Used to override the calculated warranty expiration date (which is typically derived from the warranty start date plus the brand's warranty period).
/// </summary>
[Docable]
public class WarrantyDateShiftModel : IPartitionedItem, ICompanyProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) this warranty date shift applies to.
    /// </summary>
    public string VIN { get; set; }

    /// <summary>
    /// The new warranty end date for this vehicle.
    /// </summary>
    public DateTime NewDate { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.WarrantyDateShift;

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