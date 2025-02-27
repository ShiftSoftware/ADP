using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

public class PaintThicknessDTO
{
    public IEnumerable<PaintThicknessPartDTO> Parts { get; set; }

    public IEnumerable<PaintThicknessImageDTO> ImageGroups { get; set; } = new List<PaintThicknessImageDTO>();
}