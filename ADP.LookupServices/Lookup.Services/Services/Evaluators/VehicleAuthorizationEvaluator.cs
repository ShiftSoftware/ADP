using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Services.Evaluators;

public class VehicleAuthorizationEvaluator : IVehicleAuthorizationEvaluator
{
    public bool Evaluate(string vin, CompanyDataAggregateCosmosModel? companyDataAggregate)
    {
        return
            companyDataAggregate?.InitialOfficialVINs?.Any(x => x.VIN.Equals(vin, System.StringComparison.InvariantCultureIgnoreCase)) == true ||
            companyDataAggregate?.VehicleEntries?.Any(x => x.VIN.Equals(vin, System.StringComparison.InvariantCultureIgnoreCase)) == true ||
            companyDataAggregate?.SSCAffectedVINs?.Any(x => x.VIN.Equals(vin, System.StringComparison.InvariantCultureIgnoreCase)) == true;
    }
}