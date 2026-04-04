using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Contains parsed variant information extracted from the vehicle's variant code string.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleVariantInfoDTO
{
    /// <summary>The model code portion of the variant.</summary>
    public string ModelCode { get; set; } = default!;
    /// <summary>The suffix code differentiating trim or equipment levels.</summary>
    public string SFX { get; set; } = default!;
    /// <summary>The model year extracted from the variant.</summary>
    public int? ModelYear { get; set; }
}