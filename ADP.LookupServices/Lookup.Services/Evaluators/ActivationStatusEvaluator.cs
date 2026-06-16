using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Composes the company-scoped <see cref="WarrantyActivationStatus"/> for the warranty card from the
/// (company-agnostic) activation-due flag and the requesting user's company.
///
/// <para>With the allocation guard off the behaviour is unchanged — activation is offered
/// (<see cref="WarrantyActivationStatus.Required"/>) whenever it is due. With the guard on, activation is only
/// offered to a requester whose company has a <see cref="ShiftSoftware.ADP.Models.Vehicle.VehicleEntryModel"/>
/// for the vehicle (it has been allocated/shipped/delivered to them); when it is due but the vehicle is not
/// allocated to the requester it is <see cref="WarrantyActivationStatus.BlockedNotAllocated"/>. With no requesting
/// company (anonymous/bulk callers) the affordance is suppressed to <see cref="WarrantyActivationStatus.NotRequired"/>,
/// because activation is a dealer-authenticated action.</para>
/// </summary>
public class ActivationStatusEvaluator
{
    private readonly CompanyDataAggregateModel CompanyDataAggregate;
    private readonly LookupOptions Options;

    public ActivationStatusEvaluator(CompanyDataAggregateModel companyDataAggregate, LookupOptions options)
    {
        this.CompanyDataAggregate = companyDataAggregate;
        this.Options = options;
    }

    public WarrantyActivationStatus Evaluate(bool activationRequired, long? requestingCompanyID)
    {
        if (!activationRequired)
            return WarrantyActivationStatus.NotRequired;

        if (Options is null || !Options.RequireAllocationForActivation)
            return WarrantyActivationStatus.Required;

        if (requestingCompanyID is null)
            return WarrantyActivationStatus.NotRequired;

        var allocatedToRequester = CompanyDataAggregate.VehicleEntries?
            .Any(e => e.CompanyID == requestingCompanyID) ?? false;

        return allocatedToRequester
            ? WarrantyActivationStatus.Required
            : WarrantyActivationStatus.BlockedNotAllocated;
    }
}
