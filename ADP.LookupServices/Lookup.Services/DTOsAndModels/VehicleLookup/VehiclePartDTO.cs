using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class VehiclePartDTO
{
    public string PartNumber { get; set; }
    public decimal? QTY { get; set; }
    public string MenuCode { get; set; }
    public string PartDescription { get; set; }
}