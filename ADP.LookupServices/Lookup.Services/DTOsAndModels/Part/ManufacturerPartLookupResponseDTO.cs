using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class ManufacturerPartLookupResponseDTO
{
    public string id { get; set; }
    public string PartNumber { get; set; }
    public ManufacturerOrderType OrderType { get; set; }
    public ManufacturerPartLookupStatus Status { get; set; }
}
