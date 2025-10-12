using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleAuthorizationEvaluator
{
    private readonly CompanyDataAggregateCosmosModel CompanyDataAggregate;
    
    public VehicleAuthorizationEvaluator(CompanyDataAggregateCosmosModel companyDataAggregate)
    {
        CompanyDataAggregate = companyDataAggregate;
    }

    public bool Evaluate()
    {
        return
            CompanyDataAggregate?.InitialOfficialVINs?.Any() == true ||
            CompanyDataAggregate?.VehicleEntries?.Any() == true ||
            CompanyDataAggregate?.SSCAffectedVINs?.Any() == true;
    }
}