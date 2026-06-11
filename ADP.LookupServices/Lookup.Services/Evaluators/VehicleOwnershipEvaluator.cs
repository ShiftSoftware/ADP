using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Resolves the <see cref="VehicleOwnership"/> for a VIN.
///
/// <para>Once a vehicle has been service-activated, the activation is the authoritative record
/// of which company owns it for service purposes — the activating company's own
/// <see cref="VehicleEntryModel"/> may not have synced yet (the vehicle can still sit in
/// national stock with a null CompanyID). Location fields the activation does not carry may be
/// supplied <b>only by the activating company's own entry</b>: a different company's entry
/// describes a different company's location, and substituting it would mis-scope service item
/// eligibility and pricing — the exact failure this evaluator exists to prevent. When the
/// country cannot be resolved from those two sources, the lookup refuses with
/// <see cref="IncompleteVehicleServiceActivationException"/> rather than guess.</para>
///
/// <para>When the vehicle has never been activated, the selected entry is the only — and
/// correct — ownership source.</para>
/// </summary>
public class VehicleOwnershipEvaluator
{
    private readonly CompanyDataAggregateModel CompanyDataAggregate;

    public VehicleOwnershipEvaluator(CompanyDataAggregateModel companyDataAggregate)
    {
        this.CompanyDataAggregate = companyDataAggregate;
    }

    public VehicleOwnership Evaluate(VehicleEntryModel vehicle)
    {
        var activation = this.CompanyDataAggregate.VehicleServiceActivations?.FirstOrDefault();

        if (activation is null)
            return new VehicleOwnership
            {
                CompanyID = vehicle?.CompanyID,
                CountryID = vehicle?.CountryID,
                RegionID = vehicle?.RegionID,
                BranchID = vehicle?.BranchID,
            };

        if (activation.CompanyID is null)
            throw new IncompleteVehicleServiceActivationException(
                this.CompanyDataAggregate.VIN, activation.id, nameof(activation.CompanyID));

        // The activating company's own entry — the only record allowed to complete location
        // fields the activation does not carry. Null while the vehicle awaits allocation.
        var activationCompanyEntry = this.CompanyDataAggregate.VehicleEntries?
            .FirstOrDefault(x => x.CompanyID == activation.CompanyID);

        var countryID = activation.CountryID ?? activationCompanyEntry?.CountryID;

        if (countryID is null)
            throw new IncompleteVehicleServiceActivationException(
                this.CompanyDataAggregate.VIN, activation.id, nameof(activation.CountryID));

        return new VehicleOwnership
        {
            CompanyID = activation.CompanyID,
            CountryID = countryID,
            RegionID = activation.RegionID ?? activationCompanyEntry?.RegionID,
            BranchID = activation.BranchID ?? activationCompanyEntry?.BranchID,
        };
    }
}
