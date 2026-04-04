namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a vehicle exclusion from free service item campaigns.
/// When a VIN is listed here, it is not eligible for any free service items regardless of other criteria.
/// </summary>
[Docable]
public class FreeServiceItemExcludedVINModel : IPartitionedItem, ICompanyProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) that is excluded from free service items.
    /// </summary>
    public string VIN { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.FreeServiceItemExcludedVIN;

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    /// <summary>
    /// Indicates whether this exclusion record has been deleted (effectively re-enabling the VIN for free service items).
    /// </summary>
    public bool IsDeleted { get; set; }
}