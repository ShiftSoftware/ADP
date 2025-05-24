using System.Text.Json.Serialization;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

[TypeScriptModel]
public class StockPartDTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuantityLookUpResults QuantityLookUpResult { get; set; }
    public string LocationID { get; set; }
    public string LocationName { get; set; }
}
