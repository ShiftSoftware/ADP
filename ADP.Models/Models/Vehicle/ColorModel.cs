namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a vehicle color option available for a brand.
/// Each color has a code and a human-readable description.
/// </summary>
[Docable]
public class ColorModel : IBrandProps
{
    [DocIgnore]
    public string id { get; set; }

    [DocIgnore]
    public long? BrandID { get; set; }

    /// <summary>
    /// The Brand Hash ID from the Identity System.
    /// </summary>
    public string BrandHashID { get; set; }

    /// <summary>
    /// The color code used to identify this color in manufacturer systems.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// A human-readable description of the color (e.g., "Pearl White", "Midnight Black").
    /// </summary>
    public string Description { get; set; }
}