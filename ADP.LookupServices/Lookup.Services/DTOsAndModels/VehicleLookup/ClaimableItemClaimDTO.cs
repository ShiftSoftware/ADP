namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class ClaimableItemClaimDTO
{
    public string Vin { get; set; }
    public string Invoice { get; set; }
    public string JobNumber { get; set; }
    public VehicleSaleInformation SaleInformation { get; set; }
    public VehicleServiceItemDTO ServiceItem { get; set; }
    public VehicleServiceItemDTO[] CancelledServiceItems { get; set; } = [];
}