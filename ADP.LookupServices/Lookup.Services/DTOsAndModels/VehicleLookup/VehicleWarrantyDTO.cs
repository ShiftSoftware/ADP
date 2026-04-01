using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Contains the warranty status and dates for a vehicle, including standard warranty, extended warranty, and free service eligibility.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleWarrantyDTO
{
    /// <summary>
    /// Whether the vehicle currently has an active standard warranty (end date is in the future).
    /// </summary>
    public bool HasActiveWarranty =>
        WarrantyEndDate.HasValue && WarrantyEndDate.Value >= DateTime.UtcNow;

    /// <summary>
    /// The start date of the standard warranty period.
    /// </summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? WarrantyStartDate { get; set; }

    /// <summary>
    /// The end date of the standard warranty period.
    /// </summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? WarrantyEndDate { get; set; }

    /// <summary>
    /// Indicates whether warranty activation is required before the warranty becomes effective.
    /// </summary>
    public bool ActivationIsRequired { get; set; }

    /// <summary>
    /// Whether the vehicle currently has an active extended warranty (end date is in the future).
    /// </summary>
    public bool HasExtendedWarranty =>
        ExtendedWarrantyEndDate.HasValue && ExtendedWarrantyEndDate.Value >= DateTime.UtcNow;

    /// <summary>
    /// The start date of the extended warranty period.
    /// </summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? ExtendedWarrantyStartDate { get; set; }

    /// <summary>
    /// The end date of the extended warranty period.
    /// </summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? ExtendedWarrantyEndDate { get; set; }

    /// <summary>
    /// The start date from which free service items become eligible.
    /// </summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? FreeServiceStartDate { get; set; }
}