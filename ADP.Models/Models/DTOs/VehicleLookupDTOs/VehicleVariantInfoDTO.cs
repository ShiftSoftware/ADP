using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

public class VehicleVariantInfoDTO
{
    public string ModelCode { get; set; } = default!;
    public string SFX { get; set; } = default!;
    public int ModelYear { get; set; }
}
