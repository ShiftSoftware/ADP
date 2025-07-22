using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class VehicleVariantInfoDTO
{
    public string ModelCode { get; set; } = default!;
    public string SFX { get; set; } = default!;
    public int? ModelYear { get; set; }
}