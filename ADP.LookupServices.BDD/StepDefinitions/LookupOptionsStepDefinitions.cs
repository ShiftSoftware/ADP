using System.Globalization;
using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

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

    [Given("LookupOptions has include-inactivated-free-service-items enabled")]
    public void GivenIncludeInactivatedFreeServiceItemsEnabled()
    {
        _context.Options.IncludeInactivatedFreeServiceItems = true;
    }

    [Given("LookupOptions has end-of-day service item expiry enabled")]
    public void GivenEndOfDayServiceItemExpiryEnabled()
    {
        _context.Options.TreatServiceItemExpiryAsEndOfDay = true;
    }

    /// <summary>
    /// Freezes the lookup clock. The timestamp is read as UTC, so "2028-01-15 09:00:00" is
    /// 09:00 UTC on that date regardless of where the test runs.
    /// </summary>
    [Given("the current UTC time is {string}")]
    public void GivenTheCurrentUtcTimeIs(string timestamp)
    {
        var instant = DateTime.Parse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.None);
        _context.Options.TimeProvider = new FixedTimeProvider(new DateTimeOffset(instant, TimeSpan.Zero));
    }

    [Given("LookupOptions has signature validity duration of {int} minutes")]
    public void GivenSignatureValidityDurationMinutes(int minutes)
    {
        _context.Options.SignatureValidityDuration = TimeSpan.FromMinutes(minutes);
    }

    [Given("an inspection pre-claim voucher URL resolver is configured")]
    public void GivenInspectionPreClaimVoucherUrlResolverIsConfigured()
    {
        _context.Options.VehicleInspectionPreClaimVoucherPrintingURLResolver = (model) =>
        {
            var url = $"inspection/{model.Value.VehicleInspectionID}/{model.Value.ServiceItemID}";
            return new ValueTask<string?>(url);
        };
    }

    [Given("a service activation pre-claim voucher URL resolver is configured")]
    public void GivenServiceActivationPreClaimVoucherUrlResolverIsConfigured()
    {
        _context.Options.ServiceActivationPreClaimVoucherPrintingURLResolver = (model) =>
        {
            var url = $"activation/{model.Value.ServiceActivationID}/{model.Value.ServiceItemID}";
            return new ValueTask<string?>(url);
        };
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

    // Comma-separated so a VIN can pass through more than one intermediary (e.g. "7, 8").
    [Given("intermediary companies are {string}")]
    public void GivenIntermediaryCompaniesAre(string commaSeparatedIds)
    {
        _context.Options.IntermediaryCompanyIDs = commaSeparatedIds
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(long.Parse)
            .ToList();
    }

    // Comma-separated account numbers that mark an entry as a direct sale to an end customer even though its
    // company is a supply-chain one. Scoped per company, since an account number only means something within
    // the company that issued it. Configuring this turns the feature on for that company.
    [Given("company {long} has direct end-customer sale account numbers {string}")]
    public void GivenCompanyHasDirectEndCustomerSaleAccountNumbers(long companyId, string commaSeparatedAccounts)
    {
        _context.Options.DirectEndCustomerSaleAccountNumbersByCompany[companyId] = commaSeparatedAccounts
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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

    // The ID→name stubs must tolerate a null ID like production resolvers do — the sale
    // evaluator invokes them with whatever the resolved ownership carries, which can be null
    // (e.g. an activation-owned vehicle with no branch).

    [Given("company {long} is named {string}")]
    public void GivenCompanyIsNamed(long companyId, string name)
    {
        _companyNames[companyId] = name;
        _context.Options.CompanyNameResolver = (model) =>
        {
            var resolved = model.Value is { } id && _companyNames.TryGetValue(id, out var n) ? n : null;
            return new ValueTask<string?>(resolved);
        };
    }

    [Given("branch {long} is named {string}")]
    public void GivenBranchIsNamed(long branchId, string name)
    {
        _branchNames[branchId] = name;
        _context.Options.CompanyBranchNameResolver = (model) =>
        {
            var resolved = model.Value is { } id && _branchNames.TryGetValue(id, out var n) ? n : null;
            return new ValueTask<string?>(resolved);
        };
    }

    [Given("country {long} is named {string}")]
    public void GivenCountryIsNamed(long countryId, string name)
    {
        _countryNames[countryId] = name;
        _context.Options.CountryNameResolver = (model) =>
        {
            var resolved = model.Value is { } id && _countryNames.TryGetValue(id, out var n) ? n : null;
            return new ValueTask<string?>(resolved);
        };
    }

    [Given("region {long} is named {string}")]
    public void GivenRegionIsNamed(long regionId, string name)
    {
        _regionNames[regionId] = name;
        _context.Options.RegionNameResolver = (model) =>
        {
            var resolved = model.Value is { } id && _regionNames.TryGetValue(id, out var n) ? n : null;
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

    [Given("standard item claim warnings:")]
    public void GivenStandardItemClaimWarnings(DataTable dataTable)
    {
        _context.Options.StandardItemClaimWarnings = dataTable.Rows.Select(row => new VehicleItemWarning
        {
            Key = row["Key"],
            BodyContent = GetOptionalString(row, "BodyContent"),
            ConfirmationText = GetOptionalString(row, "ConfirmationText"),
        }).ToList();
    }

    /// <summary>
    /// Stub that surfaces which items the evaluator passed in: the warning body is the
    /// comma-separated ServiceItemIDs of the skipped items, so Then steps can assert them.
    /// </summary>
    [Given("a skipped items claim warning resolver is configured")]
    public void GivenASkippedItemsClaimWarningResolverIsConfigured()
    {
        _context.Options.SkippedItemsClaimWarningResolver = (model) =>
        {
            var warning = new VehicleItemWarning
            {
                Key = "skippedItems",
                BodyContent = string.Join(", ", model.Value.SkippedItems.Select(x => x.ServiceItemID)),
                ConfirmationText = "Confirm skipping the above items",
            };
            return new ValueTask<VehicleItemWarning?>(warning);
        };
    }

    /// <summary>Stub whose warning body is the broker name, so Then steps can assert it.</summary>
    [Given("an un-invoiced broker claim warning resolver is configured")]
    public void GivenAnUnInvoicedBrokerClaimWarningResolverIsConfigured()
    {
        _context.Options.UnInvoicedBrokerClaimWarningResolver = (model) =>
        {
            var warning = new VehicleItemWarning
            {
                Key = "unInvoicedBroker",
                BodyContent = model.Value.BrokerName,
                ConfirmationText = "Confirm claiming without a broker invoice",
            };
            return new ValueTask<VehicleItemWarning?>(warning);
        };
    }

    private static string? GetOptionalString(DataTableRow row, string column)
    {
        if (!row.ContainsKey(column))
            return null;
        var value = row[column];
        return string.IsNullOrWhiteSpace(value) ? null : value;
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
