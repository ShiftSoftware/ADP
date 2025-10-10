using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class AuthorizedStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly CompanyDataAggregateCosmosModel? CompanyDataAggregate;

    public AuthorizedStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        this.CompanyDataAggregate = _scenarioContext.GetCompanyDataAggregate();
    }


    [Then("The Vehicle is considered Authroized")]
    public void ThenTheVehicleIsConsideredAuthroized()
    {
        var authorizedResult = new 
            VehicleAuthorizationEvaluator(this.CompanyDataAggregate)
            .Evaluate();

        Assert.True(authorizedResult);
    }


    [Then("The Vehicle is considered Unauthroized")]
    public void ThenTheVehicleIsConsideredUnauthroized()
    {
        var authorizedResult = 
            new VehicleAuthorizationEvaluator(this.CompanyDataAggregate)
            .Evaluate();

        Assert.False(authorizedResult);
    }

}