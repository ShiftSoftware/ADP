using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehicleServiceItemStepDefinitions
{
    private readonly Support.TestContext _context;
    private IEnumerable<VehicleServiceItemDTO>? _result;
    private DateTime? _freeServiceStartDate;

    public VehicleServiceItemStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [Given("the free service start date is {string}")]
    public void GivenTheFreeServiceStartDateIs(string date)
    {
        _freeServiceStartDate = DateTime.Parse(date);
    }

    [When("evaluating service items for {string} with language {string}")]
    public async Task WhenEvaluatingServiceItemsFor(string vin, string language)
    {
        _context.Aggregate.VIN = vin;

        var vehicle = new VehicleEntryEvaluator(_context.Aggregate).Evaluate();
        _context.CurrentVehicle = vehicle;

        var saleInfo = _context.SaleInformation ?? new VehicleSaleInformation
        {
            InvoiceDate = vehicle?.InvoiceDate,
            WarrantyActivationDate = vehicle?.WarrantyActivationDate,
        };

        var evaluator = new VehicleServiceItemEvaluator(
            _context.StorageService, _context.Aggregate, _context.Options, _context.ServiceProvider);

        var (serviceItems, _) = await evaluator.Evaluate(vehicle!, _freeServiceStartDate, saleInfo, language);
        _result = serviceItems;
    }

    [Then("service item {string} has status {string}")]
    public void ThenServiceItemHasStatus(string serviceItemId, string expectedStatus)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.Equal(expectedStatus, item.Status);
    }

    [Then("service item {string} has type {string}")]
    public void ThenServiceItemHasType(string serviceItemId, string expectedType)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.Equal(expectedType, item.Type);
    }

    [Then("there are {int} service items")]
    public void ThenThereAreServiceItems(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result.Count());
    }
}
