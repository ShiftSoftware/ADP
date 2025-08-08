using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;

namespace ADP.LookupServices.BDD;

public static class ScenarioContextExtensions
{
    public static CompanyDataAggregateCosmosModel? GetCompanyDataAggregate(this ScenarioContext scenarioContext)
    {
        CompanyDataAggregateCosmosModel? _companyDataAggregate = null;

        scenarioContext.TryGetValue("companyData", out _companyDataAggregate);

        return _companyDataAggregate;
    }
}