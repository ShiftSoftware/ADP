using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleAuthorizationEvaluator
{
    private readonly CompanyDataAggregateModel CompanyDataAggregate;

    public VehicleAuthorizationEvaluator(CompanyDataAggregateModel companyDataAggregate)
    {
        CompanyDataAggregate = companyDataAggregate;
    }

    public bool Evaluate()
    {
        return
            CompanyDataAggregate?.InitialOfficialVINs?.Any(x => x.VIN.Equals(this.CompanyDataAggregate.VIN, System.StringComparison.InvariantCultureIgnoreCase)) == true ||
            CompanyDataAggregate?.VehicleEntries?.Any(x => x.VIN.Equals(this.CompanyDataAggregate.VIN, System.StringComparison.InvariantCultureIgnoreCase)) == true ||
            CompanyDataAggregate?.SSCAffectedVINs?.Any(x => x.VIN.Equals(this.CompanyDataAggregate.VIN, System.StringComparison.InvariantCultureIgnoreCase)) == true;
    }
}