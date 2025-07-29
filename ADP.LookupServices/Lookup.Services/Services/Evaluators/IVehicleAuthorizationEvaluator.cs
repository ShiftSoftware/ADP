using ShiftSoftware.ADP.Lookup.Services.Aggregate;

namespace ShiftSoftware.ADP.Lookup.Services.Services.Evaluators;

public interface IVehicleAuthorizationEvaluator
{
    /// <summary>
    /// Evaluates a given VIN against the company data aggregate and returns true if the vehicle is authorized otherwise false if the vehicle is unauthorized.
    /// </summary>
    /// <param name="vin">The VIN to evaluate.</param>
    /// <param name="companyDataAggregate">The company data aggregate to evaluate against.</param>
    /// <returns></returns>
    bool Evaluate(string vin, CompanyDataAggregateCosmosModel companyDataAggregate);
}