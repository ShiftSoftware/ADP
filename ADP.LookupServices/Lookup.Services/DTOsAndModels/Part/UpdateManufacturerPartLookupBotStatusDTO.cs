using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class UpdateManufacturerPartLookupBotStatusDTO
{
    public string id { get; set; }
    public string PartNumber { get; set; }
    public ManufacturerPartLookupBotStatus BotStatus { get; set; }
    public Dictionary<string, string>? lookupResult { get; set; }
}
