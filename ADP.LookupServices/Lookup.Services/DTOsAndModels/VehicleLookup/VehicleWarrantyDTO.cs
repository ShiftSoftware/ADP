using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Contains the warranty status and dates for a vehicle, including standard warranty, extended warranty, and free service eligibility.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleWarrantyDTO
{
    // Stored (not a wall-clock getter): stamped by WarrantyAndFreeServiceDateEvaluator from
    // LookupOptions.TimeProvider so generated sample/doc data stays byte-stable. Do not convert
    // back to `=> WarrantyEndDate >= DateTime.UtcNow` — that reintroduces date-drift in the samples.
    /// <summary>
    /// Whether the vehicle currently has an active standard warranty (end date is in the future).
    /// </summary>
    public bool HasActiveWarranty { get; set; }

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
    /// Why the warranty has or has not started. When this is not <see cref="WarrantyStartState.Started"/>
    /// there is no coverage period yet and <see cref="WarrantyStartDate"/> is null — the reason is the
    /// vehicle's possession state, not missing data, and is meant to be shown rather than hidden.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WarrantyStartState StartState { get; set; }

    /// <summary>
    /// The broker whose invoice anchored the warranty, when the vehicle was sold through one. Null in
    /// every other case, including a broker that has not invoiced yet.
    /// <para>This is a warranty fact, not a sale fact: <c>SaleInformation.Broker</c> is present whether or
    /// not the broker has invoiced, and a <c>WarrantyDateShift</c> can move the start date afterwards, so a
    /// consumer cannot safely re-derive it by comparing dates. The dealer company is deliberately not
    /// duplicated here — it is <c>SaleInformation.CompanyName</c>, already resolved from the vehicle's
    /// ownership.</para>
    /// </summary>
    public string? ActivatedByBrokerName { get; set; }

    /// <summary>
    /// Indicates whether warranty activation is due for this vehicle (it has pending warranty-activation–triggered
    /// free service items). Company-agnostic — it does not consider which dealer is asking. Consumed by bulk
    /// reporting/exports. For the dealer-facing activation affordance use <see cref="ActivationStatus"/>.
    /// </summary>
    public bool ActivationIsRequired { get; set; }

    /// <summary>
    /// The company-scoped activation state for the requesting dealer, used to drive the lookup UI.
    /// <see cref="WarrantyActivationStatus.Required"/> offers activation (the vehicle is allocated to the requester's
    /// company); <see cref="WarrantyActivationStatus.BlockedNotAllocated"/> warns that activation is due but the
    /// vehicle is not allocated to the requester; <see cref="WarrantyActivationStatus.NotRequired"/> shows nothing.
    /// Driven by <c>LookupOptions.RequireAllocationForActivation</c> and the caller-supplied
    /// <c>VehicleLookupRequestOptions.RequestingCompanyID</c>; with the guard off it mirrors
    /// <see cref="ActivationIsRequired"/>, and with no requesting company the affordance is suppressed.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WarrantyActivationStatus ActivationStatus { get; set; }

    // Stored (not a wall-clock getter): see note on HasActiveWarranty.
    /// <summary>
    /// Whether the vehicle currently has an active extended warranty (end date is in the future).
    /// </summary>
    public bool HasExtendedWarranty { get; set; }

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
    /// Every extended-warranty coverage awarded to the vehicle: persisted entries plus any
    /// awarded by a configured <c>LookupOptions.ExtendedWarrantyDefinitions</c> definition.
    /// The flat <c>ExtendedWarranty*</c> fields above are unrelated legacy output describing
    /// only the latest-ending persisted entry, and are not a summary of this collection.
    /// </summary>
    public List<VehicleExtendedWarrantyDTO> ExtendedWarranties { get; set; } = new();

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
