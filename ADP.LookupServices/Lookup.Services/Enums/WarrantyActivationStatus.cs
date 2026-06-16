using ShiftSoftware.ADP.Models;
using System.ComponentModel;

namespace ShiftSoftware.ADP.Lookup.Services.Enums;

/// <summary>
/// The company-scoped warranty activation state surfaced to the lookup UI for the requesting dealer.
/// Distinct from <c>VehicleWarrantyDTO.ActivationIsRequired</c>, which is the company-agnostic
/// "activation is due" fact used by reporting.
/// </summary>
[Docable]
public enum WarrantyActivationStatus
{
    /// <summary>No activation affordance is shown (not due, or suppressed for an anonymous/bulk caller).</summary>
    [Description("Not Required")]
    NotRequired = 0,

    /// <summary>Activation is due and offered — the vehicle is allocated to the requesting company.</summary>
    [Description("Required")]
    Required = 1,

    /// <summary>
    /// Activation is due but cannot be offered — the vehicle is not allocated to the requesting company
    /// (no vehicle entry for it). Surfaced as a non-actionable warning.
    /// </summary>
    [Description("Blocked — Not Allocated")]
    BlockedNotAllocated = 2,
}
