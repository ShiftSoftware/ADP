using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehicleAuthorizationStepDefinitions
{
    private readonly Support.TestContext _context;

    public VehicleAuthorizationStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [Then("The Vehicle is considered Authorized")]
    public void ThenTheVehicleIsConsideredAuthorized()
    {
        var authorizedResult = new VehicleAuthorizationEvaluator(_context.Aggregate)
            .Evaluate();

        Assert.True(authorizedResult);
    }

    [Then("The Vehicle is considered Unauthorized")]
    public void ThenTheVehicleIsConsideredUnauthorized()
    {
        var authorizedResult = new VehicleAuthorizationEvaluator(_context.Aggregate)
            .Evaluate();

        Assert.False(authorizedResult);
    }
}
