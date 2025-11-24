using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class ItemClaimDTO
{
    public string? VIN { get; set; }
    public string? Invoice { get; set; }
    public string? JobNumber { get; set; }
    public string? QRCode { get; set; }
    public VehicleSaleInformation? SaleInformation { get; set; }
    public VehicleServiceItemDTO? ServiceItem { get; set; }
    public VehicleIdentifiersDTO? Identifiers { get; set; }
    public VehicleVariantInfoDTO? VehicleVariantInfo { get; set; }
    public VehicleSpecificationDTO? VehicleSpecification { get; set; }
}