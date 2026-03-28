using LookupServices.BDD.Support;
using Reqnroll;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class LookupOptionsStepDefinitions
{
    private readonly TestContext _context;

    public LookupOptionsStepDefinitions(TestContext context)
    {
        _context = context;
    }

    [Given("warranty start date defaults to invoice date")]
    public void GivenWarrantyStartDateDefaultsToInvoiceDate()
    {
        _context.Options.WarrantyStartDateDefaultsToInvoiceDate = true;
    }

    [Given("warranty start date does not default to invoice date")]
    public void GivenWarrantyStartDateDoesNotDefaultToInvoiceDate()
    {
        _context.Options.WarrantyStartDateDefaultsToInvoiceDate = false;
    }

    [Given("brand {long} has a warranty period of {int} years")]
    public void GivenBrandHasWarrantyPeriodOfYears(long brandId, int years)
    {
        _context.Options.BrandStandardWarrantyPeriodsInYears[brandId] = years;
    }
}
