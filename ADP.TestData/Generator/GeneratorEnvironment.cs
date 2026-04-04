using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ADP.TestData.Generator;

public class GeneratorEnvironment
{
    public GeneratorLookupOptions LookupOptions { get; set; } = new();
    public List<GeneratorCompany> Companies { get; set; } = new();
    public List<GeneratorCountry> Countries { get; set; } = new();
    public List<GeneratorRegion> Regions { get; set; } = new();
    public Dictionary<string, CompanyDataAggregateModel> Vehicles { get; set; } = new();
    public List<BrokerInitialVehicleModel> BrokerInitialVehicles { get; set; } = new();
    public List<BrokerInvoiceModel> BrokerInvoices { get; set; } = new();
    public List<VehicleModelModel> VehicleModels { get; set; } = new();
    public List<ServiceItemModel> ServiceItems { get; set; } = new();
    public Dictionary<string, PartAggregateCosmosModel> Parts { get; set; } = new();
}

/// <summary>
/// Subset of LookupOptions that can be deserialized from JSON (no Func delegates).
/// </summary>
public class GeneratorLookupOptions
{
    public bool WarrantyStartDateDefaultsToInvoiceDate { get; set; } = true;
    public Dictionary<long?, int> BrandStandardWarrantyPeriodsInYears { get; set; } = new();
    public bool LookupBrokerStock { get; set; }
    public bool IncludeInactivatedFreeServiceItems { get; set; }
    public int? DistributorStockPartLookupQuantityThreshold { get; set; }
    public bool ShowPartLookupStockQauntity { get; set; }
    public bool EnableManufacturerLookup { get; set; }

    public LookupOptions ToLookupOptions(
        Dictionary<long, string> companyNames,
        Dictionary<long, string> branchNames,
        Dictionary<long, string> countryNames,
        Dictionary<long, string> regionNames)
    {
        var options = new LookupOptions
        {
            WarrantyStartDateDefaultsToInvoiceDate = WarrantyStartDateDefaultsToInvoiceDate,
            BrandStandardWarrantyPeriodsInYears = BrandStandardWarrantyPeriodsInYears,
            LookupBrokerStock = LookupBrokerStock,
            IncludeInactivatedFreeServiceItems = IncludeInactivatedFreeServiceItems,
            DistributorStockPartLookupQuantityThreshold = DistributorStockPartLookupQuantityThreshold,
            ShowPartLookupStockQauntity = ShowPartLookupStockQauntity,
            EnableManufacturerLookup = EnableManufacturerLookup,
        };

        options.CompanyNameResolver = (model) =>
        {
            var name = model.Value.HasValue && companyNames.TryGetValue(model.Value.Value, out var n) ? n : null;
            return new ValueTask<string?>(name);
        };

        options.CompanyBranchNameResolver = (model) =>
        {
            var name = model.Value.HasValue && branchNames.TryGetValue(model.Value.Value, out var n) ? n : null;
            return new ValueTask<string?>(name);
        };

        options.CountryNameResolver = (model) =>
        {
            var name = model.Value.HasValue && countryNames.TryGetValue(model.Value.Value, out var n) ? n : null;
            return new ValueTask<string?>(name);
        };

        options.RegionNameResolver = (model) =>
        {
            var name = model.Value.HasValue && regionNames.TryGetValue(model.Value.Value, out var n) ? n : null;
            return new ValueTask<string?>(name);
        };

        // Pass-through price resolver (returns evaluator-built data as-is)
        options.PartLookupPriceResolver = (model) =>
        {
            return new ValueTask<(decimal?, IEnumerable<PartPriceDTO>)>(
                (model.Value.DistributorPurchasePrice, model.Value.Prices));
        };

        return options;
    }
}

public class GeneratorCompany
{
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public List<GeneratorBranch> Branches { get; set; } = new();
}

public class GeneratorBranch
{
    public long BranchId { get; set; }
    public string BranchName { get; set; } = "";
}

public class GeneratorCountry
{
    public long CountryId { get; set; }
    public string CountryName { get; set; } = "";
}

public class GeneratorRegion
{
    public long RegionId { get; set; }
    public string RegionName { get; set; } = "";
}
