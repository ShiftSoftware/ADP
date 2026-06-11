using System;

namespace ShiftSoftware.ADP.Lookup.Services;

/// <summary>
/// Thrown when a VIN has a <c>VehicleServiceActivation</c> that cannot yield a complete owner:
/// the activation is missing <c>CompanyID</c>, or its country cannot be resolved (not stamped on
/// the activation and the activating company has no vehicle entry of its own to supply it — a
/// different company's entry must never substitute, it describes a different company's location).
/// Service item eligibility and pricing key off the owner, so guessing here would silently list
/// the wrong items; the lookup refuses instead.
/// Remediation: the upstream system must stamp the activating company's location
/// (CountryID/RegionID/BranchID + HashIDs) on activations at creation time and backfill
/// existing documents.
/// </summary>
public class IncompleteVehicleServiceActivationException : Exception
{
    public string VIN { get; }
    public string ServiceActivationID { get; }

    public IncompleteVehicleServiceActivationException(string vin, string serviceActivationID, string missingField)
        : base(
            $"VehicleServiceActivation '{serviceActivationID}' for VIN '{vin}' has no resolvable {missingField}: " +
            "it is not stamped on the activation and the activating company has no vehicle entry of its own to supply it. " +
            "Refusing to evaluate ownership — falling back to another company's entry would mis-scope service item " +
            "eligibility and pricing. Stamp the activating company's location on the activation (upstream) and backfill.")
    {
        VIN = vin;
        ServiceActivationID = serviceActivationID;
    }
}
