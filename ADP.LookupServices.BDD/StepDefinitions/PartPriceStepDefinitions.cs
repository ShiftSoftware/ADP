using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class PartPriceStepDefinitions
{
    private readonly Support.TestContext _context;
    private decimal? _distributorPrice;
    private IEnumerable<PartPriceDTO> _prices = [];

    public PartPriceStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [When("evaluating price for part {string}")]
    public async Task WhenEvaluatingPriceForPart(string partNumber)
    {
        var evaluator = new PartPriceEvaluator(
            _context.PartAggregate, _context.Options, _context.ServiceProvider);
        (_distributorPrice, _prices) = await evaluator.Evaluate(source: null, language: "en");
    }

    [Then("the distributor price is {decimal}")]
    public void ThenTheDistributorPriceIs(decimal expected)
    {
        Assert.NotNull(_distributorPrice);
        Assert.Equal(expected, _distributorPrice.Value);
    }

    [Then("the distributor price is empty")]
    public void ThenTheDistributorPriceIsEmpty()
    {
        Assert.Null(_distributorPrice);
    }

    [Then("there are {int} price entries")]
    public void ThenThereArePriceEntries(int count)
    {
        Assert.Equal(count, _prices.Count());
    }

    [Then("price entry for country {string} region {string} has retail price {decimal}")]
    public void ThenPriceEntryHasRetailPrice(string countryId, string regionId, decimal expected)
    {
        var entry = _prices.FirstOrDefault(p => p.CountryID == countryId && p.RegionID == regionId);
        Assert.NotNull(entry);
        Assert.NotNull(entry.RetailPrice);
        Assert.Equal(expected, entry.RetailPrice.Value);
    }

    [Then("price entry for country {string} region {string} has country name {string}")]
    public void ThenPriceEntryHasCountryName(string countryId, string regionId, string expected)
    {
        var entry = _prices.FirstOrDefault(p => p.CountryID == countryId && p.RegionID == regionId);
        Assert.NotNull(entry);
        Assert.Equal(expected, entry.CountryName);
    }

    [Then("price entry for country {string} region {string} has region name {string}")]
    public void ThenPriceEntryHasRegionName(string countryId, string regionId, string expected)
    {
        var entry = _prices.FirstOrDefault(p => p.CountryID == countryId && p.RegionID == regionId);
        Assert.NotNull(entry);
        Assert.Equal(expected, entry.RegionName);
    }
}
