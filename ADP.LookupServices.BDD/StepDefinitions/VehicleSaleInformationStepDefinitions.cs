using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehicleSaleInformationStepDefinitions
{
    private readonly Support.TestContext _context;
    private VehicleSaleInformation? _result;
    private bool _evaluated;

    public VehicleSaleInformationStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [When("evaluating sale information for {string} with language {string}")]
    public async Task WhenEvaluatingSaleInformationFor(string vin, string language)
    {
        _context.Aggregate.VIN = vin;

        var vehicle = new VehicleEntryEvaluator(_context.Aggregate).Evaluate();
        _context.CurrentVehicle = vehicle;

        var requestOptions = new VehicleLookupRequestOptions
        {
            LanguageCode = language,
            LookupEndCustomer = false,
        };

        var evaluator = new VehicleSaleInformationEvaluator(
            _context.Aggregate, _context.Options, _context.ServiceProvider, _context.StorageService);

        _result = await evaluator.Evaluate(requestOptions);
        _context.SaleInformation = _result;
        _evaluated = true;
    }

    [Then("the sale company is {string}")]
    public void ThenTheSaleCompanyIs(string expectedCompany)
    {
        Assert.NotNull(_result);
        Assert.Equal(expectedCompany, _result.CompanyName);
    }

    [Then("the sale branch is {string}")]
    public void ThenTheSaleBranchIs(string expectedBranch)
    {
        Assert.NotNull(_result);
        Assert.Equal(expectedBranch, _result.BranchName);
    }

    [Then("the sale invoice date is {string}")]
    public void ThenTheSaleInvoiceDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(expectedDate), _result.InvoiceDate);
    }

    [Then("the broker is {string}")]
    public void ThenTheBrokerIs(string expectedBrokerName)
    {
        Assert.NotNull(_result);
        Assert.NotNull(_result.Broker);
        Assert.Equal(expectedBrokerName, _result.Broker.BrokerName);
    }

    [Then("the broker invoice date is empty")]
    public void ThenTheBrokerInvoiceDateIsEmpty()
    {
        Assert.NotNull(_result);
        Assert.NotNull(_result.Broker);
        Assert.Null(_result.Broker.InvoiceDate);
    }

    [Then("the broker invoice date is {string}")]
    public void ThenTheBrokerInvoiceDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.NotNull(_result.Broker);
        Assert.Equal(DateTime.Parse(expectedDate), _result.Broker.InvoiceDate);
    }

    [Then("no sale information is available")]
    public void ThenNoSaleInformationIsAvailable()
    {
        Assert.True(_evaluated);
        Assert.Null(_result);
    }
}
