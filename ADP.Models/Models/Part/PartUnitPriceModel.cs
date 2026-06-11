namespace ShiftSoftware.ADP.Models.Part;


/// <summary>
/// Represents the retail price of a part for a specific selling unit (e.g. each, box, pack).
/// </summary>
[Docable]
public class PartUnitPriceModel
{
    /// <summary>
    /// The name of the selling unit (e.g. "each", "box"). Must be unique within the unit price list.
    /// </summary>
    public string UnitName { get; set; }

    /// <summary>
    /// The retail price of the part for this unit.
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Indicates whether this unit price is the default retail unit price.
    /// Only one unit price within the list may be marked as the default.
    /// </summary>
    public bool IsDefault { get; set; }
}
