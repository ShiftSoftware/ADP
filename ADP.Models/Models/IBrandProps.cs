namespace ShiftSoftware.ADP.Models;

/// <summary>
/// Defines brand-level properties for models that belong to a specific automotive brand.
/// </summary>
internal interface IBrandProps
{
    /// <summary>
    /// The internal brand ID.
    /// </summary>
    public long? BrandID { get; set; }

    /// <summary>
    /// The Brand Hash ID from the Identity System.
    /// </summary>
    public string BrandHashID { get; set; }
}