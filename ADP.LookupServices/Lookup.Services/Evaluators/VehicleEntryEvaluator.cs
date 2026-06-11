using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Selects the vehicle entry that anchors a lookup — the one still in stock (no invoice date)
/// if any, otherwise the most recently invoiced. The entry provides spec and sale/invoice
/// fields only; the owning company/country/region is resolved separately by
/// <see cref="VehicleOwnershipEvaluator"/>, because the entry's own ownership fields can be
/// stale or empty (e.g. national stock that has not been allocated to the activating company yet).
/// </summary>
public class VehicleEntryEvaluator
{
    private readonly CompanyDataAggregateModel CompanyDataAggregate;

    public VehicleEntryEvaluator(CompanyDataAggregateModel companyDataAggregate)
    {
        this.CompanyDataAggregate = companyDataAggregate;
    }

    public VehicleEntryModel Evaluate()
    {
        VehicleEntryModel vehicle = null;

        var vehicles = this.CompanyDataAggregate.VehicleEntries;

        if (vehicles?.Count > 0)
            if (vehicles.Any(x => x.InvoiceDate is null))
                vehicle = vehicles.FirstOrDefault(x => x.InvoiceDate is null);
            else
                vehicle = vehicles.OrderByDescending(x => x.InvoiceDate).FirstOrDefault();

        return vehicle;
    }
}
