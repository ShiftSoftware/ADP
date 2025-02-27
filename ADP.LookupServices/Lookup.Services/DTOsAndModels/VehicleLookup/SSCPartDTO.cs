namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

public class SSCPartDTO
{
    public string PartNumber { get; set; } = default!;
    public string PartDescription { get; set; }
    public bool IsAvailable { get; set; }
}