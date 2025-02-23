using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

public class PaintThicknessImageDTO
{
    public string Name { get; set; }
    public IEnumerable<string> Images { get; set; }
}
