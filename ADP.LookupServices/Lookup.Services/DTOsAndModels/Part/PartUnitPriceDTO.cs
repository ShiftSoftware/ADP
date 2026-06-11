using ShiftSoftware.ADP.Models;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// Represents the retail price of a part for a specific selling unit (e.g. each, box, pack).
/// </summary>
[TypeScriptModel]
[Docable]
public class PartUnitPriceDTO
{
    /// <summary>The name of the selling unit (e.g. "each", "box").</summary>
    public string UnitName { get; set; }
    /// <summary>The retail price value for this unit. Uses the currency/culture metadata of the parent <see cref="PriceDTO"/>.</summary>
    public decimal? Price { get; set; }
    /// <summary>Whether this is the default retail unit price. Exactly one entry in the list is the default.</summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Back-reference to the owning price, used to read the currency/culture metadata when formatting.
    /// Set lazily by the <see cref="PriceDTO.UnitPrices"/> getter, so it is wired up whenever the list is
    /// read (including during serialization) regardless of when the item was added. Not serialized.
    /// </summary>
    [DocIgnore]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    internal PriceDTO Parent { get; set; }

    /// <summary>The unit price formatted as a currency string using the parent price's culture.</summary>
    public string FormattedValue
    {
        get
        {
            if (Price is null || Parent?.Culture is null)
                return null;

            try
            {
                return Price.Value.ToString("C", Parent.Culture);
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
