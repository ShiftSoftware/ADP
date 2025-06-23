using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class VehicleIdentifiersDTO
{
    public string VIN { get; set; } = default!;
    public string Variant { get; set; } = default!;
    public string Katashiki { get; set; } = default!;
    public string Color { get; set; } = default!;
    public string Trim { get; set; } = default!;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Brands? Brand { get; set; } = default!;
    public string BrandID { get; set; } = default!;
}