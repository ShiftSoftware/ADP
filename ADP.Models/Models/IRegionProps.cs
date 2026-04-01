namespace ShiftSoftware.ADP.Models;

/// <summary>
/// Defines region-level properties for models that are associated with a specific geographic region.
/// </summary>
public interface IRegionProps
{
    /// <summary>
    /// The internal region ID.
    /// </summary>
    public long? RegionID { get; set; }

    /// <summary>
    /// The Region Hash ID from the Identity System.
    /// </summary>
    public string RegionHashID { get; set; }
}