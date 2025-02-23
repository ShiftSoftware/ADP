using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

public class VehicleIdentifiersDTO
{
    public string VIN { get; set; } = default!;
    public string Variant { get; set; } = default!;
    public string Katashiki { get; set; } = default!;
    public string Color { get; set; } = default!;
    public string Trim { get; set; } = default!;
    public Franchises? Brand { get; set; } = default!;
    public string BrandIntegrationID { get; set; } = default!;
}
