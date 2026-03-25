using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehicleEntryStepDefinitions
{
    private readonly Support.TestContext _context;

    public VehicleEntryStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [Then("the selected vehicle has no invoice date")]
    public void ThenTheSelectedVehicleHasNoInvoiceDate()
    {
        var result = new VehicleEntryEvaluator(_context.Aggregate).Evaluate();
        _context.CurrentVehicle = result;

        Assert.NotNull(result);
        Assert.Null(result.InvoiceDate);
    }

    [Then("the selected vehicle has invoice date {string}")]
    public void ThenTheSelectedVehicleHasInvoiceDate(string expectedDate)
    {
        var result = new VehicleEntryEvaluator(_context.Aggregate).Evaluate();
        _context.CurrentVehicle = result;

        Assert.NotNull(result);
        Assert.Equal(DateTime.Parse(expectedDate), result.InvoiceDate);
    }

    [Then("no vehicle is selected")]
    public void ThenNoVehicleIsSelected()
    {
        var result = new VehicleEntryEvaluator(_context.Aggregate).Evaluate();
        _context.CurrentVehicle = result;

        Assert.Null(result);
    }
}
