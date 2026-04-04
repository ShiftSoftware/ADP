using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class LookupOptionsStepDefinitions
{
    private readonly TestContext _context;
    private readonly Dictionary<string, string> _accessoryImageMappings = new();
    private readonly Dictionary<long?, string> _companyNames = new();
    private readonly Dictionary<long?, string> _branchNames = new();
    private readonly Dictionary<long?, string> _countryNames = new();
    private readonly Dictionary<long?, string> _regionNames = new();
    private readonly Dictionary<string, string> _locationNames = new();

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

    [Given("country {long} is named {string}")]
    public void GivenCountryIsNamed(long countryId, string name)
    {
        _countryNames[countryId] = name;
        _context.Options.CountryNameResolver = (model) =>
        {
            var resolved = _countryNames.TryGetValue(model.Value, out var n) ? n : null;
            return new ValueTask<string?>(resolved);
        };
    }

    [Given("region {long} is named {string}")]
    public void GivenRegionIsNamed(long regionId, string name)
    {
        _regionNames[regionId] = name;
        _context.Options.RegionNameResolver = (model) =>
        {
            var resolved = _regionNames.TryGetValue(model.Value, out var n) ? n : null;
            return new ValueTask<string?>(resolved);
        };
    }

    [Given("location {string} is named {string}")]
    public void GivenLocationIsNamed(string locationId, string name)
    {
        _locationNames[locationId] = name;
        _context.Options.PartLocationNameResolver = (model) =>
        {
            var resolved = _locationNames.TryGetValue(model.Value.LocationID, out var n) ? n : null;
            return new ValueTask<string?>(resolved);
        };
    }

    [Given("the part price resolver passes through")]
    public void GivenThePartPriceResolverPassesThrough()
    {
        _context.Options.PartLookupPriceResolver = (model) =>
        {
            return new ValueTask<(decimal?, IEnumerable<PartPriceDTO>)>(
                (model.Value.DistributorPurchasePrice, model.Value.Prices));
        };
    }

    [Given("LookupOptions distributor stock threshold is {int}")]
    public void GivenDistributorStockThreshold(int threshold)
    {
        _context.Options.DistributorStockPartLookupQuantityThreshold = threshold;
    }

    [Given("LookupOptions show stock quantity is enabled")]
    public void GivenShowStockQuantityIsEnabled()
    {
        _context.Options.ShowPartLookupStockQauntity = true;
    }
}
