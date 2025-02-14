using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class PartLookupDTO
{
    public string PartNumber { get; set; }
    public string? PartDescription { get; set; }
    public string? LocalDescription { get; set; }
    public string? Group { get; set; }
    public string? PNC { get; set; }
    public string? PNCLocalName { get; set; }
    public string? BinCode { get; set; }
    public decimal? dimension1 { get; set; }
    public decimal? dimension2 { get; set; }
    public decimal? dimension3 { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? CubicMeasure { get; set; }
    public string? HSCode { get; set; }
    public string? UZHSCode { get; set; }
    public string? Origin { get; set; }
    public IEnumerable<string>? SupersededTo { get; set; }

    public IEnumerable<StockPartDTO> StockParts { get; set; }
    public IEnumerable<PartPriceDTO> Prices { get; set; }
    public IEnumerable<DeadStockDTO> DeadStock { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? LogId { get; set; }
}
