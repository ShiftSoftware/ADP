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

    /// <summary>
    /// The earliest non-deleted <c>ItemClaim.ClaimDate</c> for this vehicle. Always populated
    /// when at least one non-deleted claim exists, regardless of whether it ends up being used.
    /// When the regular fallback chain (service activation / sale warranty / sale invoice /
    /// broker invoice) would otherwise leave <see cref="FreeServiceStartDate"/> null, this
    /// value is used as the effective <see cref="FreeServiceStartDate"/> so downstream items
    /// project as if activation had occurred — the act of claiming is itself evidence the
    /// vehicle has been serviced. <see cref="FreeServiceItemDateShiftModel"/> overrides still
    /// win over this fallback.
    /// </summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? DeFactoServiceStartDate { get; set; }
}