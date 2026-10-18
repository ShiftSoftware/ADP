using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehicleAccessoriesStepDefinitions
{
    private readonly Support.TestContext _context;
    private IEnumerable<AccessoryDTO>? _result;

    public VehicleAccessoriesStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [When("evaluating accessories with language {string}")]
    public async Task WhenEvaluatingAccessoriesWithLanguage(string language)
    {
        _result = await new VehicleAccessoriesEvaluator(
            _context.Aggregate, _context.Options, _context.ServiceProvider)
            .Evaluate(language);
    }

    [Then("there are {int} accessories")]
    public void ThenThereAreAccessories(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result.Count());
    }

    [Then("accessory {string} has image {string}")]
    public void ThenAccessoryHasImage(string partNumber, string expectedImage)
    {
        Assert.NotNull(_result);
        var accessory = _result.FirstOrDefault(a => a.PartNumber == partNumber);
        Assert.NotNull(accessory);
        Assert.Equal(expectedImage, accessory.Image);
    }

    private AccessoryDTO Find(string partNumber)
    {
        Assert.NotNull(_result);
        var accessory = _result.FirstOrDefault(a => a.PartNumber == partNumber);
        Assert.NotNull(accessory);
        return accessory;
    }

    [Then("accessory {string} has job number {int}")]
    public void ThenAccessoryHasJobNumber(string partNumber, int expected)
        => Assert.Equal(expected, Find(partNumber).JobNumber);

    [Then("accessory {string} has no job number")]
    public void ThenAccessoryHasNoJobNumber(string partNumber)
        => Assert.Null(Find(partNumber).JobNumber);

    [Then("accessory {string} has invoice number {int}")]
    public void ThenAccessoryHasInvoiceNumber(string partNumber, int expected)
        => Assert.Equal(expected, Find(partNumber).InvoiceNumber);

    [Then("accessory {string} has no invoice number")]
    public void ThenAccessoryHasNoInvoiceNumber(string partNumber)
        => Assert.Null(Find(partNumber).InvoiceNumber);

    [Then("accessory {string} has company name {string}")]
    public void ThenAccessoryHasCompanyName(string partNumber, string expected)
        => Assert.Equal(expected, Find(partNumber).CompanyName);

    [Then("accessory {string} has no company name")]
    public void ThenAccessoryHasNoCompanyName(string partNumber)
        => Assert.Null(Find(partNumber).CompanyName);
}
