using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehicleServiceHistoryStepDefinitions
{
    private readonly Support.TestContext _context;
    private IEnumerable<VehicleServiceHistoryDTO>? _result;

    public VehicleServiceHistoryStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [When("evaluating service history with language {string}")]
    public async Task WhenEvaluatingServiceHistoryWithLanguage(string language)
    {
        _result = await new VehicleServiceHistoryEvaluator(
            _context.Aggregate, _context.Options, _context.ServiceProvider)
            .Evaluate(language, ConsistencyLevels.Eventual);
    }

    [When("evaluating service history with language {string} and strong consistency")]
    public async Task WhenEvaluatingServiceHistoryWithStrongConsistency(string language)
    {
        _result = await new VehicleServiceHistoryEvaluator(
            _context.Aggregate, _context.Options, _context.ServiceProvider)
            .Evaluate(language, ConsistencyLevels.Strong);
    }

    [Then("there is {int} service history invoice")]
    [Then("there are {int} service history invoices")]
    public void ThenThereAreServiceHistoryInvoices(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result.Count());
    }

    [Then("invoice {string} has {int} labor line and {int} part line")]
    [Then("invoice {string} has {int} labor lines and {int} part lines")]
    public void ThenInvoiceHasLines(string invoiceNumber, int laborCount, int partCount)
    {
        var invoice = GetInvoice(invoiceNumber);
        Assert.Equal(laborCount, invoice.LaborLines?.Count() ?? 0);
        Assert.Equal(partCount, invoice.PartLines?.Count() ?? 0);
    }

    [Then("invoice {string} company is {string}")]
    public void ThenInvoiceCompanyIs(string invoiceNumber, string expectedCompany)
    {
        var invoice = GetInvoice(invoiceNumber);
        Assert.Equal(expectedCompany, invoice.CompanyName);
    }

    [Then("invoice {string} branch is {string}")]
    public void ThenInvoiceBranchIs(string invoiceNumber, string expectedBranch)
    {
        var invoice = GetInvoice(invoiceNumber);
        Assert.Equal(expectedBranch, invoice.BranchName);
    }

    private VehicleServiceHistoryDTO GetInvoice(string invoiceNumber)
    {
        Assert.NotNull(_result);
        var invoice = _result.FirstOrDefault(i => i.InvoiceNumber == invoiceNumber);
        Assert.NotNull(invoice);
        return invoice;
    }
}
