using ShiftSoftware.ADP.Models.JsonConverters;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

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