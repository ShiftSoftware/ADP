namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents the cost of a <see cref="ServiceItemModel">Service Item</see> for a specific vehicle variant or package.
/// A service item may have different costs depending on the vehicle's variant, Katashiki, or package code.
/// </summary>
[Docable]
public class ServiceItemCostModel
{
    /// <summary>
    /// The unique identifier for this service item cost entry.
    /// </summary>
    public long ID { get; set; }

    /// <summary>
    /// The ID of the <see cref="ServiceItemModel">Service Item</see> this cost applies to.
    /// </summary>
    public long? ServiceItemID { get; set; }

    /// <summary>
    /// The vehicle variant code this cost applies to.
    /// </summary>
    public string Variant { get; set; }

    /// <summary>
    /// The Katashiki (model-specific identifier) this cost applies to.
    /// </summary>
    public string Katashiki { get; set; }

    /// <summary>
    /// The package code this cost applies to.
    /// </summary>
    public string PackageCode { get; set; }

    /// <summary>
    /// Cost in dollars.
    /// </summary>
    public decimal? Cost { get; set; }
}