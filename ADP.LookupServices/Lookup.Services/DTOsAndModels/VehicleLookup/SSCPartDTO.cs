using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

public class SSCPartDTO
{
    public string PartNumber { get; set; } = default!;
    public string PartDescription { get; set; }
    public bool IsAvailable { get; set; }
}
