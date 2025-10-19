using Newtonsoft.Json;
using ShiftSoftware.ADP.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

[TypeScriptModel]
public class PartLookupDTO
{
    public string PartNumber { get; set; }
    public string PartDescription { get; set; }
    public string LocalDescription { get; set; }
    public string ProductGroup { get; set; }
    public string PNC { get; set; }
    public string BinType { get; set; }
    public decimal? DistributorPurchasePrice { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? CubicMeasure { get; set; }
    public string HSCode { get; set; }
    public string Origin { get; set; }
    public IEnumerable<string> SupersededTo { get; set; }
    public IEnumerable<string> SupersededFrom { get; set; }
    public IEnumerable<StockPartDTO> StockParts { get; set; }
    public IEnumerable<PartPriceDTO> Prices { get; set; }
    public IEnumerable<HSCodeDTO> HSCodes { get; set; }
    public IEnumerable<DeadStockDTO> DeadStock { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? LogId { get; set; }
}
