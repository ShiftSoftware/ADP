using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class PartStockStepDefinitions
{
    private readonly Support.TestContext _context;
    private IEnumerable<StockPartDTO>? _result;

    public PartStockStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [When("evaluating stock for part {string} with quantity {int}")]
    public async Task WhenEvaluatingStockForPartWithQuantity(string partNumber, int quantity)
    {
        var evaluator = new PartStockEvaluator(
            _context.PartAggregate, _context.Options, _context.ServiceProvider);
        _result = await evaluator.Evaluate(distributorStockLookupQuantity: quantity, language: "en");
    }

    [When("evaluating stock for part {string} without quantity")]
    public async Task WhenEvaluatingStockForPartWithoutQuantity(string partNumber)
    {
        var evaluator = new PartStockEvaluator(
            _context.PartAggregate, _context.Options, _context.ServiceProvider);
        _result = await evaluator.Evaluate(distributorStockLookupQuantity: null, language: "en");
    }

    [Then("there are {int} stock entries")]
    public void ThenThereAreStockEntries(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result.Count());
    }

    [Then("stock entry {string} has result {string}")]
    public void ThenStockEntryHasResult(string locationId, string expectedResult)
    {
        Assert.NotNull(_result);
        var entry = _result.FirstOrDefault(s => s.LocationID == locationId);
        Assert.NotNull(entry);
        var expected = Enum.Parse<QuantityLookUpResults>(expectedResult);
        Assert.Equal(expected, entry.QuantityLookUpResult);
    }

    [Then("stock entry {string} has available quantity {decimal}")]
    public void ThenStockEntryHasAvailableQuantity(string locationId, decimal expected)
    {
        Assert.NotNull(_result);
        var entry = _result.FirstOrDefault(s => s.LocationID == locationId);
        Assert.NotNull(entry);
        Assert.NotNull(entry.AvailableQuantity);
        Assert.Equal(expected, entry.AvailableQuantity.Value);
    }

    [Then("stock entry {string} has no available quantity")]
    public void ThenStockEntryHasNoAvailableQuantity(string locationId)
    {
        Assert.NotNull(_result);
        var entry = _result.FirstOrDefault(s => s.LocationID == locationId);
        Assert.NotNull(entry);
        Assert.Null(entry.AvailableQuantity);
    }

    [Then("stock entry {string} has location name {string}")]
    public void ThenStockEntryHasLocationName(string locationId, string expectedName)
    {
        Assert.NotNull(_result);
        var entry = _result.FirstOrDefault(s => s.LocationID == locationId);
        Assert.NotNull(entry);
        Assert.Equal(expectedName, entry.LocationName);
    }
}
