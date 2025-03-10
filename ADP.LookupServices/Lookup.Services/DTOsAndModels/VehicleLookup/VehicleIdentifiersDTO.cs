using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class VehicleIdentifiersDTO
{
    public string VIN { get; set; } = default!;
    public string Variant { get; set; } = default!;
    public string Katashiki { get; set; } = default!;
    public string Color { get; set; } = default!;
    public string Trim { get; set; } = default!;
    public Brands? Brand { get; set; } = default!;
    public string BrandID { get; set; } = default!;
}