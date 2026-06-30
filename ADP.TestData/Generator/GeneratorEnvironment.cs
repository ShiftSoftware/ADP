using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
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
    public Dictionary<string, List<TBP_StockModel>> BrokerStocks { get; set; } = new();
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

    /// <summary>
    /// The distributor's CompanyID — the Paint Thickness Certificate's strict invoice
    /// anchor (only an invoiced VehicleEntry of this company can anchor a certificate).
    /// </summary>
    public long? DistributorCompanyID { get; set; }

    /// <summary>
    /// CompanyIDs of any intermediary companies (e.g. a regional importer between the distributor and the
    /// dealer). Like the distributor, they never make the end-customer sale; they are surfaced as
    /// intermediary supply-chain legs on the sale information.
    /// </summary>
    public List<long> IntermediaryCompanyIDs { get; set; } = new();

    public int? DistributorStockPartLookupQuantityThreshold { get; set; }
    public bool ShowPartLookupStockQauntity { get; set; }
    public bool EnableManufacturerLookup { get; set; }

    /// <summary>Static warnings attached to every service item, exactly as in production LookupOptions.</summary>
    public List<VehicleItemWarning>? StandardItemClaimWarnings { get; set; }

    /// <summary>
    /// Template for the skipped-items claim warning (claiming a higher-mileage item cancels lower pending ones).
    /// When set, the generator wires <c>SkippedItemsClaimWarningResolver</c> from it. Placeholders in
    /// <c>BodyContent</c>: <c>{ItemName}</c> (item being claimed) and <c>{SkippedItems}</c> (a ul-list of the
    /// pending items that would be cancelled).
    /// </summary>
    public VehicleItemWarning? SkippedItemsClaimWarning { get; set; }

    /// <summary>
    /// Template for the un-invoiced broker claim warning. When set, the generator wires
    /// <c>UnInvoicedBrokerClaimWarningResolver</c> from it. Placeholder in <c>BodyContent</c>: <c>{BrokerName}</c>.
    /// </summary>
    public VehicleItemWarning? UnInvoicedBrokerClaimWarning { get; set; }

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
            DistributorCompanyID = DistributorCompanyID,
            IntermediaryCompanyIDs = IntermediaryCompanyIDs,
            DistributorStockPartLookupQuantityThreshold = DistributorStockPartLookupQuantityThreshold,
            ShowPartLookupStockQauntity = ShowPartLookupStockQauntity,
            EnableManufacturerLookup = EnableManufacturerLookup,
        };

        // Resolve paint-thickness image keys to deterministic placeholder photos so the
        // web-component mocks and docs demos render a working gallery (the keys are
        // real-shaped blob paths that don't exist in any storage account).
        options.PaintThickneesImageUrlResolver = (model) =>
        {
            if (string.IsNullOrWhiteSpace(model.Value))
                return new ValueTask<string?>((string?)null);

            var seed = new string(model.Value.Where(char.IsLetterOrDigit).ToArray());
            seed = seed.Length > 40 ? seed[^40..] : seed;

            return new ValueTask<string?>($"https://picsum.photos/seed/{seed}/640/480");
        };

        // Certificate serial numbers: mirrors the production wiring in LookUpFunctions —
        // Hashids (0-9A-F alphabet, min length 10) over the NUMERIC inspection id,
        // displayed XXXXX-XXXXX. Bijective, so collision-free; non-numeric ids yield no
        // serial (fail visible, not wrong) exactly like production.
        var serialHashids = new HashidsNet.Hashids("paint-thickness-certificate-serial", 10, "0123456789ABCDEF");

        options.PaintThicknessCertificateSerialNumberResolver = (model) =>
        {
            if (!long.TryParse(model.Value, out var inspectionId) || inspectionId < 0)
                return new ValueTask<string?>((string?)null);

            var code = serialHashids.EncodeLong(inspectionId);

            return new ValueTask<string?>(code.Length >= 10 ? $"{code[..5]}-{code[5..]}" : code);
        };

        // Certificate print URLs: deterministic, production-shaped landing links (one per
        // print language, mirroring the LookUpFunctions wiring) so the web-component mocks
        // and docs demos render the print menu. The host is fake on purpose — mocks only
        // need the menu to appear and carry plausible hrefs.
        options.PaintThicknessCertificateUrlsResolver = (model) =>
            new ValueTask<List<PaintThicknessCertificateUrlDTO>?>(
                new[] { ("en", "English"), ("ar", "العربية"), ("ku", "کوردی") }.Select(language => new PaintThicknessCertificateUrlDTO
                {
                    Language = language.Item1,
                    Name = language.Item2,
                    Url = $"https://lookup.example/paint-thickness-certificate/{model.Value}?expires=9999-12-31.23-59-59-9999&token=mock-token&lang={language.Item1}",
                }).ToList());

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

        if (StandardItemClaimWarnings is not null)
            options.StandardItemClaimWarnings = StandardItemClaimWarnings;

        // Each resolver call builds a fresh VehicleItemWarning — the evaluator attaches the
        // result per item, so reusing/mutating the template instance would leak across items.
        if (SkippedItemsClaimWarning is { } skippedTemplate)
        {
            options.SkippedItemsClaimWarningResolver = (model) =>
            {
                var skippedItemsList = "<ul>" + string.Join("", model.Value.SkippedItems.Select(x => $"<li>{x.Name}</li>")) + "</ul>";

                return new ValueTask<VehicleItemWarning?>(new VehicleItemWarning
                {
                    Key = skippedTemplate.Key,
                    ImageUrl = skippedTemplate.ImageUrl,
                    BodyContent = skippedTemplate.BodyContent?
                        .Replace("{ItemName}", model.Value.ItemBeingClaimed.Name)
                        .Replace("{SkippedItems}", skippedItemsList),
                    ConfirmationText = skippedTemplate.ConfirmationText,
                });
            };
        }

        if (UnInvoicedBrokerClaimWarning is { } brokerTemplate)
        {
            options.UnInvoicedBrokerClaimWarningResolver = (model) =>
            {
                return new ValueTask<VehicleItemWarning?>(new VehicleItemWarning
                {
                    Key = brokerTemplate.Key,
                    ImageUrl = brokerTemplate.ImageUrl,
                    BodyContent = brokerTemplate.BodyContent?.Replace("{BrokerName}", model.Value.BrokerName),
                    ConfirmationText = brokerTemplate.ConfirmationText,
                });
            };
        }

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
