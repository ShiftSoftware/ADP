using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Lookup.Services.Features;

[Binding]
public class AuthorizedStepDefinitions
{
    private string _vin = string.Empty;
    private readonly CompanyDataAggregateCosmosModel _companyDataAggregate;
    
    private readonly ScenarioContext _scenarioContext;
    private readonly ITestOutputHelper _testOutputHelper;

    public AuthorizedStepDefinitions(ScenarioContext scenarioContext, ITestOutputHelper testOutputHelper)
    {
        _scenarioContext = scenarioContext;
        _testOutputHelper = testOutputHelper;
        this._companyDataAggregate = new();
    }


    [Given("a dealer with the following vehicles as initial stock:")]
    public void GivenADealerWithTheFollowingVehiclesInInitialStock(DataTable dataTable)
    {
        this._testOutputHelper.WriteLine("ROw Count is: " + dataTable.Rows.Count);

        var vins = dataTable.Rows.Select(x => x.Values.ElementAtOrDefault(0));

        _companyDataAggregate.InitialOfficialVINs.AddRange(vins.Select(x => new InitialOfficialVINModel { VIN = x }));
    }


    [Given("the following vehicles in their dealer stock \\(coming from their DMS):")]
    public void GivenTheFollowingVehiclesInDealerStock(DataTable dataTable)
    {
        var vins = dataTable.Rows.Select(x => x.Values.ElementAtOrDefault(0));

        _companyDataAggregate.VehicleEntries.AddRange(vins.Select(x => new VehicleEntryModel { VIN = x }));
    }


    [Given("the following vehicles in official SSC Vehciles \\(Provided by the vehicle manufacturer):")]
    public void GivenTheFollowingVehiclesInSsc(DataTable dataTable)
    {
        var vins = dataTable.Rows.Select(x => x.Values.ElementAtOrDefault(0));

        _companyDataAggregate.SSCAffectedVINs.AddRange(vins.Select(x => new SSCAffectedVINModel { VIN = x }));
    }


    [When("Checking {string}")]
    public void WhenCheckingWhichIsInInitialStock(string vin)
    {
        this._vin = vin;
    }

    [Then("The Vehicle is considered Authroized")]
    public void ThenTheVehicleIsConsideredAuthroized()
    {
        var checker = new VehicleAuthorizationEvaluator();

        var authorizedResult = checker.Evaluate(
            _vin,
            _companyDataAggregate
        );

        this._testOutputHelper.WriteLine("");
        this._testOutputHelper.WriteLine("");
        this._testOutputHelper.WriteLine("");

        this._testOutputHelper.WriteLine("VIN is: " + _vin);
        this._testOutputHelper.WriteLine("Data is: " + JsonSerializer.Serialize(
            new
            {
                Initial = _companyDataAggregate.InitialOfficialVINs.Count(),
                Vehicles = _companyDataAggregate.VehicleEntries.Count(),
                SSC = _companyDataAggregate.SSCAffectedVINs.Count(),
            },
            new JsonSerializerOptions { WriteIndented = true })
        );

        Assert.True(authorizedResult);
    }


    [Then("The Vehicle is considered Unauthroized")]
    public void ThenTheVehicleIsConsideredUnauthroized()
    {
        var checker = new VehicleAuthorizationEvaluator();

        var authorizedResult = checker.Evaluate(
            _vin,
            _companyDataAggregate
        );

        Assert.False(authorizedResult);
    }

}
