using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Models.Vehicle;
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
        await EvaluateSaleInformation(vin, language, lookupEndCustomer: false);
    }

    [When("evaluating sale information for {string} with language {string} and end customer lookup")]
    public async Task WhenEvaluatingSaleInformationForWithEndCustomer(string vin, string language)
    {
        await EvaluateSaleInformation(vin, language, lookupEndCustomer: true);
    }

    private async Task EvaluateSaleInformation(string vin, string language, bool lookupEndCustomer)
    {
        _context.Aggregate.VIN = vin;

        var (vehicle, ownership) = _context.ResolveVehicle();

        var requestOptions = new VehicleLookupRequestOptions
        {
            LanguageCode = language,
            LookupEndCustomer = lookupEndCustomer,
        };

        var evaluator = new VehicleSaleInformationEvaluator(
            _context.Aggregate, _context.Options, _context.ServiceProvider, _context.StorageService);

        _result = await evaluator.Evaluate(vehicle, ownership, requestOptions);
        _context.SaleInformation = _result;
        _evaluated = true;
    }

    [Then("the sale company is {string}")]
    public void ThenTheSaleCompanyIs(string expectedCompany)
    {
        Assert.NotNull(_result);
        Assert.Equal(expectedCompany, _result.CompanyName);
    }

    [Then("the sale country is {string}")]
    public void ThenTheSaleCountryIs(string expectedCountry)
    {
        Assert.NotNull(_result);
        Assert.Equal(expectedCountry, _result.CountryName);
    }

    [Then("the sale branch is {string}")]
    public void ThenTheSaleBranchIs(string expectedBranch)
    {
        Assert.NotNull(_result);
        Assert.Equal(expectedBranch, _result.BranchName);
    }

    [Then("the sale has no branch")]
    public void ThenTheSaleHasNoBranch()
    {
        Assert.NotNull(_result);
        Assert.Null(_result.BranchID);
        Assert.Null(_result.BranchName);
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

    [Then("the end customer name is {string}")]
    public void ThenTheEndCustomerNameIs(string expectedName)
    {
        Assert.NotNull(_result);
        Assert.NotNull(_result.EndCustomer);
        Assert.Equal(expectedName, _result.EndCustomer.Name);
    }

    [Then("the end customer phone is {string}")]
    public void ThenTheEndCustomerPhoneIs(string expectedPhone)
    {
        Assert.NotNull(_result);
        Assert.NotNull(_result.EndCustomer);
        Assert.Equal(expectedPhone, _result.EndCustomer.Phone);
    }

    [Then("no sale information is available")]
    public void ThenNoSaleInformationIsAvailable()
    {
        Assert.True(_evaluated);
        Assert.Null(_result);
    }

    // --- Supply chain (distributor + intermediaries) ---

    // The "vehicles in dealer stock" table builds flat entries; this attaches the inline intermediary
    // leg that single-entry sources carry, so the embedded-shape assembler path can be exercised.
    [Given("vehicle {string} has an embedded intermediary leg with company {long} invoice {string} dated {string}")]
    public void GivenVehicleHasEmbeddedIntermediaryLeg(string vin, long companyId, string invoiceNumber, string invoiceDate)
    {
        var entry = _context.Aggregate.VehicleEntries.FirstOrDefault(e => e.VIN == vin)
            ?? throw new InvalidOperationException($"No vehicle entry found for VIN '{vin}'.");

        entry.Intermediary = new IntermediarySaleLeg
        {
            CompanyID = companyId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = DateTime.Parse(invoiceDate),
        };
    }

    // Attaches the inline distributor leg a single-entry direct (distributor→dealer) source carries.
    [Given("vehicle {string} has an embedded distributor leg with invoice {string} dated {string}")]
    public void GivenVehicleHasEmbeddedDistributorLeg(string vin, string invoiceNumber, string invoiceDate)
    {
        var entry = _context.Aggregate.VehicleEntries.FirstOrDefault(e => e.VIN == vin)
            ?? throw new InvalidOperationException($"No vehicle entry found for VIN '{vin}'.");

        entry.Distributor = new DistributorSaleLeg
        {
            InvoiceNumber = invoiceNumber,
            InvoiceDate = DateTime.Parse(invoiceDate),
        };
    }

    [Then("the distributor is {string}")]
    public void ThenTheDistributorIs(string expectedName)
    {
        Assert.NotNull(_result);
        Assert.NotNull(_result.Distributor);
        Assert.Equal(expectedName, _result.Distributor.CompanyName);
    }

    [Then("the distributor invoice number is {string}")]
    public void ThenTheDistributorInvoiceNumberIs(string expectedInvoiceNumber)
    {
        Assert.NotNull(_result);
        Assert.NotNull(_result.Distributor);
        Assert.Equal(expectedInvoiceNumber, _result.Distributor.InvoiceNumber);
    }

    [Then("the distributor invoice date is {string}")]
    public void ThenTheDistributorInvoiceDateIs(string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.NotNull(_result.Distributor);
        Assert.Equal(DateTime.Parse(expectedDate), _result.Distributor.InvoiceDate);
    }

    [Then("there is no distributor")]
    public void ThenThereIsNoDistributor()
    {
        Assert.NotNull(_result);
        Assert.Null(_result.Distributor);
    }

    [Then("the distributor invoice number is empty")]
    public void ThenTheDistributorInvoiceNumberIsEmpty()
    {
        Assert.NotNull(_result);
        Assert.NotNull(_result.Distributor);
        Assert.Null(_result.Distributor.InvoiceNumber);
    }

    [Then("the intermediaries count is {int}")]
    public void ThenTheIntermediariesCountIs(int expectedCount)
    {
        Assert.NotNull(_result);
        Assert.Equal(expectedCount, _result.Intermediaries.Count);
    }

    [Then("there are no intermediaries")]
    public void ThenThereAreNoIntermediaries()
    {
        Assert.NotNull(_result);
        Assert.Empty(_result.Intermediaries);
    }

    [Then("intermediary {int} is {string}")]
    public void ThenIntermediaryIs(int position, string expectedName)
    {
        Assert.NotNull(_result);
        Assert.True(_result.Intermediaries.Count >= position,
            $"Expected at least {position} intermediaries but found {_result.Intermediaries.Count}.");
        Assert.Equal(expectedName, _result.Intermediaries[position - 1].CompanyName);
    }

    [Then("intermediary {int} invoice number is {string}")]
    public void ThenIntermediaryInvoiceNumberIs(int position, string expectedInvoiceNumber)
    {
        Assert.NotNull(_result);
        Assert.True(_result.Intermediaries.Count >= position,
            $"Expected at least {position} intermediaries but found {_result.Intermediaries.Count}.");
        Assert.Equal(expectedInvoiceNumber, _result.Intermediaries[position - 1].InvoiceNumber);
    }

    [Then("intermediary {int} invoice date is {string}")]
    public void ThenIntermediaryInvoiceDateIs(int position, string expectedDate)
    {
        Assert.NotNull(_result);
        Assert.True(_result.Intermediaries.Count >= position,
            $"Expected at least {position} intermediaries but found {_result.Intermediaries.Count}.");
        Assert.Equal(DateTime.Parse(expectedDate), _result.Intermediaries[position - 1].InvoiceDate);
    }
}
