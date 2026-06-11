using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Models.Part;


/// <summary>
/// Used to define the price of a part in a specific region.
/// </summary>
[Docable]
public class RegionPriceModel : IRegionProps
{

    [DocIgnore]
    public long? RegionID { get; set; }


    /// <summary>
    /// The Region Hash ID from the Identity System.
    /// </summary>
    public string RegionHashID { get; set; }

    /// <summary>
    /// The Retail Price of the part in the region.
    /// </summary>
    public decimal? RetailPrice { get; set; }

    /// <summary>
    /// The retailer purchase price of the part in the region. (Alos known as the distributor sell price)
    /// </summary>
    public decimal? PurchasePrice { get; set; }

    /// <summary>
    /// The warranty price of the part in the region. (As reimbursed by the distributor)
    /// </summary>
    public decimal? WarrantyPrice { get; set; }

    /// <summary>
    /// The retail price of the part in the region broken down by selling unit (e.g. each, box).
    /// When provided, unit names must be unique and at most one entry may be marked as the default.
    /// These rules are validated on read (the path consumers and serializers go through) so they
    /// hold against the current contents of the list, including items added after assignment;
    /// a violation throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    public IEnumerable<PartUnitPriceModel> RetailUnitPrices
    {
        get
        {
            if (field is not null)
            {
                var duplicateName = field
                    .GroupBy(x => x.UnitName, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(g => g.Count() > 1);
                if (duplicateName is not null)
                    throw new InvalidOperationException($"Duplicate retail unit price name '{duplicateName.Key}'. Unit names must be unique.");

                if (field.Count(x => x.IsDefault) > 1)
                    throw new InvalidOperationException("More than one default retail unit price was found. Only one unit price can be marked as the default.");
            }

            return field;
        }
        set => field = value;
    }
}