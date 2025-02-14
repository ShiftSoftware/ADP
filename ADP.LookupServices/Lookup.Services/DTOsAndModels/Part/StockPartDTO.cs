using System.Text.Json.Serialization;
using ShiftSoftware.ADP.Lookup.Services.Enums;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class StockPartDTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuantityLookUpResults QuantityLookUpResult { get; set; }

    public string LocationID { get; set; }
    public string LocationName { get; set; }
}
