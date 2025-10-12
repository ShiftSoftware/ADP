using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleEntryEvaluator
{
    private readonly CompanyDataAggregateCosmosModel CompanyDataAggregate;
    
    public VehicleEntryEvaluator(CompanyDataAggregateCosmosModel companyDataAggregate)
    {
        this.CompanyDataAggregate = companyDataAggregate;
    }

    public VehicleEntryModel Evaluate()
    {
        VehicleEntryModel vehicle = null;

        var vehicles = this.CompanyDataAggregate
            .VehicleEntries
            //.Select(x => (VehicleEntryModel)x)
            //.Concat(companyDataAggregate.VehicleServiceActivations.Select(x => (VehicleEntryModel)x))
            .ToList();

        if (vehicles?.Count() > 0)
            if (vehicles.Any(x => x.InvoiceDate is null))
                vehicle = vehicles.FirstOrDefault(x => x.InvoiceDate is null);
            else
                vehicle = vehicles.OrderByDescending(x => x.InvoiceDate).FirstOrDefault();

        return vehicle;
    }
}