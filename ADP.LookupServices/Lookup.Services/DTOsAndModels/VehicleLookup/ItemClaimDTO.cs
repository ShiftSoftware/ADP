using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Represents a service item claim request/result, containing the claim details and associated vehicle information.
/// </summary>
[TypeScriptModel]
[Docable]
public class ItemClaimDTO
{
    /// <summary>The Vehicle Identification Number.</summary>
    public string? VIN { get; set; }
    /// <summary>The invoice number provided for the claim.</summary>
    public string? Invoice { get; set; }
    /// <summary>The job number provided for the claim.</summary>
    public string? JobNumber { get; set; }
    /// <summary>The QR code scanned for the claim.</summary>
    public string? QRCode { get; set; }
    /// <summary>The vehicle's <see cref="VehicleSaleInformation">sale information</see>.</summary>
    public VehicleSaleInformation? SaleInformation { get; set; }
    /// <summary>The <see cref="VehicleServiceItemDTO">service item</see> being claimed.</summary>
    public VehicleServiceItemDTO? ServiceItem { get; set; }
    /// <summary>The vehicle's <see cref="VehicleIdentifiersDTO">identifiers</see>.</summary>
    public VehicleIdentifiersDTO? Identifiers { get; set; }
    /// <summary>The vehicle's <see cref="VehicleVariantInfoDTO">variant information</see>.</summary>
    public VehicleVariantInfoDTO? VehicleVariantInfo { get; set; }
    /// <summary>The vehicle's <see cref="VehicleSpecificationDTO">technical specifications</see>.</summary>
    public VehicleSpecificationDTO? VehicleSpecification { get; set; }
}