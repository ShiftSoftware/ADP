using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class VehicleIdentifiersDTO
{
    public string VIN { get; set; } = default!;
    public string Variant { get; set; } = default!;
    public string Katashiki { get; set; } = default!;
    public string Color { get; set; } = default!;
    public string Trim { get; set; } = default!;

    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public string BrandID { get; set; } = default!;
}