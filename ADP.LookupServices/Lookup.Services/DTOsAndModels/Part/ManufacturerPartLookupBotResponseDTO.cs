using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class ManufacturerPartLookupBotResponseDTO
{
    public string id { get; set; }
    public string PartNumber { get; set; }
    public ManufacturerOrderType OrderType { get; set; }
    public ManufacturerPartLookupBotStatus BotStatus { get; set; }
}
