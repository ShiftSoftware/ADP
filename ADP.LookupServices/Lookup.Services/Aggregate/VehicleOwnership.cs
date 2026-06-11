using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.Aggregate;

/// <summary>
/// The resolved company/country/region/branch that owns a vehicle for lookup purposes.
/// Produced by <c>VehicleOwnershipEvaluator</c>: when a service activation exists it is the
/// sole owner — completed only by the activating company's own entry, refusing
/// (<c>IncompleteVehicleServiceActivationException</c>) when the country cannot be resolved;
/// without an activation, the selected entry is the source. Passed to the ownership-sensitive
/// evaluators (Sale Information, Service Item eligibility) so they all agree on the same
/// owner instead of each reading a possibly stale <c>VehicleEntryModel</c>.
/// Carries numeric IDs only — consumers hash at serialization via the DTO converters, and a
/// per-field (ID, HashID) pair resolved from different sources could silently mismatch.
/// </summary>
[Docable]
public class VehicleOwnership
{
    /// <summary>The owning company.</summary>
    public long? CompanyID { get; set; }

    /// <summary>The owning company's country.</summary>
    public long? CountryID { get; set; }

    /// <summary>The owning company's region.</summary>
    public long? RegionID { get; set; }

    /// <summary>The owning branch.</summary>
    public long? BranchID { get; set; }
}
