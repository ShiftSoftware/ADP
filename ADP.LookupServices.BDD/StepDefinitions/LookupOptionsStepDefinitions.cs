using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class LookupOptionsStepDefinitions
{
    private readonly TestContext _context;
    private readonly Dictionary<string, string> _accessoryImageMappings = new();
    private readonly Dictionary<long?, string> _companyNames = new();
    private readonly Dictionary<long?, string> _branchNames = new();

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

    [Given("the accessory image resolver maps {string} to {string}")]
    public void GivenAccessoryImageResolverMaps(string from, string to)
    {
        _accessoryImageMappings[from] = to;
        _context.Options.AccessoryImageUrlResolver = (model) =>
        {
            var url = _accessoryImageMappings.TryGetValue(model.Value, out var mapped) ? mapped : model.Value;
            return new ValueTask<string?>(url);
        };
    }

    [Given("company {long} is named {string}")]
    public void GivenCompanyIsNamed(long companyId, string name)
    {
        _companyNames[companyId] = name;
        _context.Options.CompanyNameResolver = (model) =>
        {
            var resolved = _companyNames.TryGetValue(model.Value, out var n) ? n : null;
            return new ValueTask<string?>(resolved);
        };
    }

    [Given("branch {long} is named {string}")]
    public void GivenBranchIsNamed(long branchId, string name)
    {
        _branchNames[branchId] = name;
        _context.Options.CompanyBranchNameResolver = (model) =>
        {
            var resolved = _branchNames.TryGetValue(model.Value, out var n) ? n : null;
            return new ValueTask<string?>(resolved);
        };
    }
}
