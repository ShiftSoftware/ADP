using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class PaintThicknessImageDTO
{
    public string Name { get; set; }
    public IEnumerable<string> Images { get; set; }
}