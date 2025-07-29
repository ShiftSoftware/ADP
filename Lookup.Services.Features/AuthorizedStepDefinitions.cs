using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Lookup.Services.Services.Evaluators;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Lookup.Services.Features;

[Binding]
public class AuthorizedStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ITestOutputHelper _testOutputHelper;

    public AuthorizedStepDefinitions(ScenarioContext scenarioContext, ITestOutputHelper testOutputHelper)
    {
        _scenarioContext = scenarioContext;
        _testOutputHelper = testOutputHelper;
    }


    [Then("The Vehicle is considered Authroized")]
    public void ThenTheVehicleIsConsideredAuthroized()
    {
        this._scenarioContext.TryGetValue("vin", out string _vin);

        this._scenarioContext.TryGetValue("companyData", out CompanyDataAggregateCosmosModel _companyDataAggregate);

        var checker = new VehicleAuthorizationEvaluator();

        var authorizedResult = checker.Evaluate(
            _vin,
            _companyDataAggregate
        );

        this._testOutputHelper.WriteLine("");
        this._testOutputHelper.WriteLine("");
        this._testOutputHelper.WriteLine("");

        this._testOutputHelper.WriteLine("VIN is: " + _vin);
        this._testOutputHelper.WriteLine("Data is: " +
            JsonSerializer.Serialize(
                new
                {
                    InitialOfficialVINs = _companyDataAggregate.InitialOfficialVINs.Count,
                    VehicleEntries = _companyDataAggregate.VehicleEntries.Count,
                    SSCAffectedVINs = _companyDataAggregate.SSCAffectedVINs.Count,
                },
                new JsonSerializerOptions { WriteIndented = true }
        ));

        Assert.True(authorizedResult);
    }


    [Then("The Vehicle is considered Unauthroized")]
    public void ThenTheVehicleIsConsideredUnauthroized()
    {
        this._scenarioContext.TryGetValue("vin", out string _vin);

        this._scenarioContext.TryGetValue("companyData", out CompanyDataAggregateCosmosModel _companyDataAggregate);

        var checker = new VehicleAuthorizationEvaluator();

        var authorizedResult = checker.Evaluate(
            _vin,
            _companyDataAggregate
        );

        Assert.False(authorizedResult);
    }

}
