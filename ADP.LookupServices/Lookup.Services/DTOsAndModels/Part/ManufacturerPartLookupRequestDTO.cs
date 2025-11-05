using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

[TypeScriptModel]
public class ManufacturerPartLookupRequestDTO
{
    public string PartNumber { get; set; }
    public decimal Quantity { get; set; }
    public ManufacturerOrderType OrderType { get; set; }
    public string? LogId { get; set; }
}