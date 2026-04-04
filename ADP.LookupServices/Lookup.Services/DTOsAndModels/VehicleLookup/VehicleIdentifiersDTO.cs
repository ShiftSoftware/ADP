using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Contains the core identification properties for a vehicle — VIN, variant code, Katashiki, and colors.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleIdentifiersDTO
{
    /// <summary>
    /// The Vehicle Identification Number.
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// The variant code string (encodes ModelCode + SFX + ModelYear).
    /// </summary>
    public string Variant { get; set; } = default!;

    /// <summary>
    /// The Katashiki (manufacturer-specific model identifier).
    /// </summary>
    public string Katashiki { get; set; } = default!;

    /// <summary>
    /// The exterior color code of the vehicle.
    /// </summary>
    public string Color { get; set; } = default!;

    /// <summary>
    /// The interior trim/color code of the vehicle.
    /// </summary>
    public string Trim { get; set; } = default!;

    /// <summary>
    /// The Brand Hash ID from the Identity System.
    /// </summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public string BrandID { get; set; } = default!;
}