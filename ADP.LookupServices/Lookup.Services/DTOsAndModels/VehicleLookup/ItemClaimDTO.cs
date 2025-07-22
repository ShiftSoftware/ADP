namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class ItemClaimDTO
{
    public string Vin { get; set; }
    public string Invoice { get; set; }
    public string JobNumber { get; set; }
    public string QRCode { get; set; }
    public VehicleSaleInformation SaleInformation { get; set; }
    public VehicleServiceItemDTO ServiceItem { get; set; }
    public VehicleServiceItemDTO[] CancelledServiceItems { get; set; } = [];
}