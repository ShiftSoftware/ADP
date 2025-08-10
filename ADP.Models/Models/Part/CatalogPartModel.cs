using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Part;

/// <summary>
/// Catalog Name refers to a specific part in the Panels Catalog.  
/// It is used to define the properties and information of a part.
/// </summary>
[Docable]
public class CatalogPartModel : 
    IPartitionedItem
{
    /// <summary>
    /// The ADP ID of the part. This is a unique identifier that ADP generates for each part.
    /// </summary>
    [DocIgnore]
    public string id { get; set; } = default!;


    /// <summary>
    /// The unique identifier for the catalog part. If an ID is not available, then the part number should be used as the ID.
    /// </summary>
    public string ID { get; set; }

    /// <summary>
    /// Catalog Name does not actually have a location. But it is used in the partition key on the database
    /// </summary>
    [DocIgnore]
    public string Location { get; set; } = default!;

    /// <summary>
    /// Each part has a unique part number that is used to identify it in the catalog and other related documents/systems.
    /// </summary>
    public string PartNumber { get; set; } = default!;

    /// <summary>
    /// The name of the part as it appears in the catalog.
    /// </summary>
    public string PartName { get; set; } = default!;

    /// <summary>
    /// The product group code to which the part belongs.
    /// </summary>
    public string ProductGroup { get; set; } = default!;

    /// <summary>
    /// The description of the product group to which the part belongs.
    /// </summary>
    public string ProductGroupDescription { get; set; }

    /// <summary>
    /// The type of the bin in which the part is stored.
    /// </summary>
    public string BinType { get; set; }

    /// <summary>
    /// The purchase price that the distributor pays for the part.
    /// </summary>
    public decimal? DistributorPurchasePrice { get; set; }


    /// <summary>
    /// The product code that is used to identify the part in the catalog.
    /// </summary>
    public string ProductCode { get; set; }

    /// <summary>
    /// The Product Number Code (PNC)
    /// </summary>
    public string PNC { get; set; }


    /// <summary>
    /// The length of the part.
    /// </summary>
    public decimal? Length { get; set; }


    /// <summary>
    /// The width of the part.
    /// </summary>
    public decimal? Width { get; set; }

    /// <summary>
    /// The height of the part.
    /// </summary>
    public decimal? Height { get; set; }


    [DocIgnore]
    [Obsolete("Changed to Length bu kept for compatibility.")]
    public decimal? Dimension1 { get; set; }
    
    [DocIgnore]
    [Obsolete("Changed to Width bu kept for compatibility.")]
    public decimal? Dimension2 { get; set; }


    [DocIgnore]
    [Obsolete("Changed to Height bu kept for compatibility.")]
    public decimal? Dimension3 { get; set; }


    /// <summary>
    /// The weight of the part.
    /// </summary>
    public decimal? NetWeight { get; set; }

    /// <summary>
    /// The cubic measure of the part.
    /// </summary>
    public decimal? CubicMeasure { get; set; }

    /// <summary>
    /// The gross weight of the part.
    /// </summary>
    public decimal? GrossWeight { get; set; }

    /// <summary>
    /// The country of origin of the part.
    /// </summary>
    public string Origin { get; set; }

    /// <summary>
    /// A list of all the <see cref="PartSupersessionModel">Supersessions</see> that the part has.
    /// </summary>
    public IEnumerable<PartSupersessionModel> SupersededTo { get; set; }

    /// <summary>
    /// The localized description of the part.
    /// </summary>
    public string LocalDescription { get; set; }

    /// <summary>
    /// The Harmonized System (HS) code for the part.
    /// </summary>
    public string HSCode { get; set; }

    /// <summary>
    /// <see cref="PartCountryDataModel">Per Country</see> data for the part.
    /// </summary>
    public IEnumerable<PartCountryDataModel> CountryData { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.CatalogPart;
}