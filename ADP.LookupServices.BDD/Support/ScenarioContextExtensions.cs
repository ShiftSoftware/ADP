using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;

namespace LookupServices.BDD;

public static class ScenarioContextExtensions
{
    public static CompanyDataAggregateModel? GetCompanyDataAggregate(this ScenarioContext scenarioContext)
    {
        CompanyDataAggregateModel? _companyDataAggregate = null;

        scenarioContext.TryGetValue("companyData", out _companyDataAggregate);

        return _companyDataAggregate;
    }
}