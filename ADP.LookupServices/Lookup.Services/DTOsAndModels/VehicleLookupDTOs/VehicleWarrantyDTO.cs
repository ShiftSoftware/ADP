using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Models.JsonConverters;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

public class VehicleWarrantyDTO
{
    public bool HasActiveWarranty =>
        WarrantyEndDate.HasValue && WarrantyEndDate.Value >= DateTime.UtcNow;

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? WarrantyStartDate { get; set; }

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? WarrantyEndDate { get; set; }

    public bool HasExtendedWarranty =>
        ExtendedWarrantyEndDate.HasValue && ExtendedWarrantyEndDate.Value >= DateTime.UtcNow;

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? ExtendedWarrantyStartDate { get; set; }

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? ExtendedWarrantyEndDate { get; set; }
}
