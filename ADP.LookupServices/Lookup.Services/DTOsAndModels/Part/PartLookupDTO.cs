using Newtonsoft.Json;
using ShiftSoftware.ADP.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// The main result returned by the part lookup service.
/// Contains part catalog information, pricing, stock availability, supersessions, HS codes, and dead stock data.
/// </summary>
[TypeScriptModel]
[Docable]
public class PartLookupDTO
{
    /// <summary>The unique part number.</summary>
    public string PartNumber { get; set; }
    /// <summary>The part description from the catalog.</summary>
    public string PartDescription { get; set; }
    /// <summary>The localized description of the part.</summary>
    public string LocalDescription { get; set; }
    /// <summary>The product group code the part belongs to.</summary>
    public string ProductGroup { get; set; }
    /// <summary>The Product Number Code (PNC).</summary>
    public string PNC { get; set; }
    /// <summary>The bin type for storage classification.</summary>
    public string BinType { get; set; }
    /// <summary>Whether the manufacturer part lookup feature is available for this part.</summary>
    public bool ShowManufacturerPartLookup { get; set; }
    /// <summary>The purchase price that the distributor pays for this part.</summary>
    public decimal? DistributorPurchasePrice { get; set; }
    /// <summary>The length of the part.</summary>
    public decimal? Length { get; set; }
    /// <summary>The width of the part.</summary>
    public decimal? Width { get; set; }
    /// <summary>The height of the part.</summary>
    public decimal? Height { get; set; }
    /// <summary>The net weight of the part.</summary>
    public decimal? NetWeight { get; set; }
    /// <summary>The gross weight of the part.</summary>
    public decimal? GrossWeight { get; set; }
    /// <summary>The cubic measure of the part.</summary>
    public decimal? CubicMeasure { get; set; }
    /// <summary>The Harmonized System (HS) code for the part.</summary>
    public string HSCode { get; set; }
    /// <summary>The country of origin of the part.</summary>
    public string Origin { get; set; }
    /// <summary>Part numbers that this part has been superseded to (newer replacements).</summary>
    public IEnumerable<string> SupersededTo { get; set; }
    /// <summary>Part numbers that have been superseded by this part (older parts this replaces).</summary>
    public IEnumerable<string> SupersededFrom { get; set; }
    /// <summary>Stock availability across locations.</summary>
    public IEnumerable<StockPartDTO> StockParts { get; set; }
    /// <summary>Pricing information by country and region.</summary>
    public IEnumerable<PartPriceDTO> Prices { get; set; }
    /// <summary>HS codes by country.</summary>
    public IEnumerable<HSCodeDTO> HSCodes { get; set; }
    /// <summary>Dead stock information by company and branch.</summary>
    public IEnumerable<DeadStockDTO> DeadStock { get; set; }

    [DocIgnore]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? LogId { get; set; }
}
