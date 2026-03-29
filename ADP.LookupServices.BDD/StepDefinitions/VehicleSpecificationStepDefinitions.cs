using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehicleSpecificationStepDefinitions
{
    private readonly Support.TestContext _context;
    private VehicleSpecificationDTO? _result;

    public VehicleSpecificationStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [When("evaluating specification for {string}")]
    public async Task WhenEvaluatingSpecificationFor(string vin)
    {
        _context.Aggregate.VIN = vin;

        var vehicle = new VehicleEntryEvaluator(_context.Aggregate).Evaluate();
        _context.CurrentVehicle = vehicle;

        _result = await new VehicleSpecificationEvaluator(_context.StorageService)
            .Evaluate(vehicle!);
    }

    [Then("the specification model is {string}")]
    public void ThenTheSpecificationModelIs(string expectedModel)
    {
        Assert.NotNull(_result);
        Assert.Equal(expectedModel, _result.ModelDescription);
    }

    [Then("the specification model is empty")]
    public void ThenTheSpecificationModelIsEmpty()
    {
        Assert.NotNull(_result);
        Assert.Null(_result.ModelDescription);
    }

    [Then("the specification body type is {string}")]
    public void ThenTheSpecificationBodyTypeIs(string expectedBodyType)
    {
        Assert.NotNull(_result);
        Assert.Equal(expectedBodyType, _result.BodyType);
    }

    [Then("the specification engine is {string}")]
    public void ThenTheSpecificationEngineIs(string expected)
    {
        Assert.NotNull(_result);
        Assert.Equal(expected, _result.Engine);
    }

    [Then("the specification transmission is {string}")]
    public void ThenTheSpecificationTransmissionIs(string expected)
    {
        Assert.NotNull(_result);
        Assert.Equal(expected, _result.Transmission);
    }
}
