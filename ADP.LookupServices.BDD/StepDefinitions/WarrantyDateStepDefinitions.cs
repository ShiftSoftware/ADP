using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class WarrantyDateStepDefinitions
{
    private readonly Support.TestContext _context;
    private VehicleWarrantyDTO? _result;

    public WarrantyDateStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [When("evaluating warranty dates for {string}")]
    public void WhenEvaluatingWarrantyDatesFor(string vin)
    {
        _context.Aggregate.VIN = vin;

        var vehicle = new VehicleEntryEvaluator(_context.Aggregate).Evaluate();
        _context.CurrentVehicle = vehicle;

        // Build VehicleSaleInformation from the selected vehicle entry
        // (In production, VehicleSaleInformationEvaluator does this — Phase 4)
        var saleInfo = _context.SaleInformation ?? new VehicleSaleInformation
        {
            InvoiceDate = vehicle?.InvoiceDate,
            WarrantyActivationDate = vehicle?.WarrantyActivationDate,
        };

        _result = new WarrantyAndFreeServiceDateEvaluator(_context.Aggregate, _context.Options)
            .Evaluate(vehicle!, saleInfo, ignoreBrokerStock: false);
    }

    [Then("the warranty start date is {string}")]
    public void ThenTheWarrantyStartDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(expectedDate), _result.WarrantyStartDate);
    }

    [Then("the warranty start date is empty")]
    public void ThenTheWarrantyStartDateIsEmpty()
    {
        Assert.NotNull(_result);
        Assert.Null(_result.WarrantyStartDate);
    }

    [Then("the warranty end date is {string}")]
    public void ThenTheWarrantyEndDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(expectedDate), _result.WarrantyEndDate);
    }

    [Then("the extended warranty start date is {string}")]
    public void ThenTheExtendedWarrantyStartDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(expectedDate), _result.ExtendedWarrantyStartDate);
    }

    [Then("the extended warranty end date is {string}")]
    public void ThenTheExtendedWarrantyEndDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(expectedDate), _result.ExtendedWarrantyEndDate);
    }

    [Then("the free service start date is {string}")]
    public void ThenTheFreeServiceStartDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(expectedDate), _result.FreeServiceStartDate);
    }
}
