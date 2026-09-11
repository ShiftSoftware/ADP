using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Milestones;
using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ADP.TestData.Generator;

public class GeneratorEnvironment
{
    public GeneratorLookupOptions LookupOptions { get; set; } = new();

    /// <summary>
    /// The options a host passes on every request rather than configuring once. A fixture is one
    /// host's answer, so they are declared per environment; see <see cref="GeneratorRequestOptions"/>.
    /// </summary>
    public GeneratorRequestOptions RequestOptions { get; set; } = new();

    public List<GeneratorCompany> Companies { get; set; } = new();
    public List<GeneratorCountry> Countries { get; set; } = new();
    public List<GeneratorRegion> Regions { get; set; } = new();
    public Dictionary<string, CompanyDataAggregateModel> Vehicles { get; set; } = new();
    public List<BrokerInitialVehicleModel> BrokerInitialVehicles { get; set; } = new();
    public List<BrokerInvoiceModel> BrokerInvoices { get; set; } = new();
    public Dictionary<string, List<TBP_StockModel>> BrokerStocks { get; set; } = new();

    /// <summary>
    /// The Brokers container. Served by both <c>IVehicleLookupStorageService.GetBrokerAsync</c> overloads
    /// (by account number + company, by id). Note that today no evaluator calls either: the sale
    /// information's broker name comes from the <see cref="TBP_StockModel.Broker"/> embedded in the
    /// broker stock row, so this table only matters once an evaluator reads the container directly.
    /// </summary>
    public List<BrokerModel> Brokers { get; set; } = new();

    /// <summary>
    /// The VehicleModels container. Resolved by <c>VariantCode + BrandID</c> (the default lookup) or by
    /// <c>Katashiki</c> when the environment's <see cref="GeneratorRequestOptions.UseKatashikiLookup"/>
    /// is on — the two ways a host keys its models table.
    /// </summary>
    public List<VehicleModelModel> VehicleModels { get; set; } = new();

    /// <summary>
    /// The ExteriorColors container, keyed by <c>Code + BrandID</c> exactly like the storage point-read.
    /// Feeds the specification's exterior colour and the paint-thickness certificate's colour description;
    /// with no matching row the codes still flow through and only the descriptions stay null.
    /// </summary>
    public List<ColorModel> ExteriorColors { get; set; } = new();

    /// <summary>The InteriorColors container, keyed by the trim code + <c>BrandID</c>. See <see cref="ExteriorColors"/>.</summary>
    public List<ColorModel> InteriorColors { get; set; } = new();

    /// <summary>
    /// The Customers container (dealer customer records), matched on <c>CustomerID + CompanyID</c> like the
    /// storage query. Read only when <see cref="GeneratorRequestOptions.LookupEndCustomer"/> is on and the
    /// vehicle is not held or sold by a broker; a vehicle entry whose customer has no row here shows no end
    /// customer, which is a state of its own. Everything here is synthetic — names, phones and ID numbers
    /// are PII in any real estate.
    /// </summary>
    public List<CustomerModel> Customers { get; set; } = new();

    public List<ServiceItemModel> ServiceItems { get; set; } = new();
    public Dictionary<string, PartAggregateCosmosModel> Parts { get; set; } = new();
}

/// <summary>
/// The request-scoped options a host sends with every lookup (<see cref="VehicleLookupRequestOptions"/>
/// and the part lookup's requested quantity), declared once per environment because a generated fixture
/// is that host's answer. Every default is what the generator hard-coded before these were declarable, so
/// an environment that says nothing generates exactly as it always did.
/// </summary>
public class GeneratorRequestOptions
{
    /// <summary>
    /// Resolve the end customer (name, phone, ID) onto the sale information — from the Customers table for a
    /// dealer sale, from the completed broker invoice for a broker sale. Hosts gate this on a permission;
    /// the fixture decides once. Off means the sale information never carries an end customer.
    /// </summary>
    public bool LookupEndCustomer { get; set; }

    /// <summary>
    /// Skip broker stock when dating free service: with it off (the default) a broker-held vehicle's items
    /// may date from its earliest claim (the de-facto start); with it on they date from the dealer invoice,
    /// which is what a broker-market host passes.
    /// </summary>
    public bool IgnoreBrokerStock { get; set; }

    /// <summary>
    /// Resolve the specification's model by <c>Katashiki</c> instead of <c>VariantCode + BrandID</c>. For a
    /// host whose <see cref="GeneratorEnvironment.VehicleModels"/> table is keyed by katashiki.
    /// </summary>
    public bool UseKatashikiLookup { get; set; }

    /// <summary>
    /// The company making the lookup — the allocation guard's input. With
    /// <see cref="GeneratorLookupOptions.RequireAllocationForActivation"/> on, activation is offered only
    /// when this company holds a vehicle entry for the VIN (else <c>BlockedNotAllocated</c>); null models
    /// the anonymous caller, for whom the activation affordance is suppressed altogether.
    /// </summary>
    public long? RequestingCompanyID { get; set; }

    /// <summary>
    /// <c>Strong</c> drops any service invoice whose stored line counts disagree with its actual lines;
    /// <c>Eventual</c> keeps them. An estate whose sync stamps the counts as -1 needs <c>Eventual</c> or
    /// its service history renders empty.
    /// </summary>
    public ConsistencyLevels VehicleServiceHistoryConsistencyLevel { get; set; } = ConsistencyLevels.Strong;

    /// <summary>
    /// Attach the repair-status trace to every SSC record. On by default: the SSC demo pages show the trace.
    /// </summary>
    public bool TraceSSCEvaluation { get; set; } = true;

    /// <summary>
    /// Produce the paint-thickness certificate's print URLs when the certificate is available. On by
    /// default so the mocks carry the print menu.
    /// </summary>
    public bool GeneratePaintThicknessCertificateUrls { get; set; } = true;

    /// <summary>
    /// The quantity the part lookup asks for at the distributor's stock locations — the input that decides
    /// <c>Available</c> / <c>PartiallyAvailable</c> / <c>QuantityNotWithinLookupThreshold</c>. An explicit
    /// <c>null</c> asks for no quantity (<c>LookupIsSkipped</c>). Defaults to 1, the generator's historic
    /// value. Per-part exceptions go in <see cref="DistributorStockLookupQuantityByPart"/>.
    /// </summary>
    public int? DistributorStockLookupQuantity { get; set; } = 1;

    /// <summary>
    /// Per part number (the key in <see cref="GeneratorEnvironment.Parts"/>), the quantity to ask for
    /// instead of <see cref="DistributorStockLookupQuantity"/>, so one environment can show every stock
    /// verdict: a large quantity for <c>PartiallyAvailable</c>, one at or above the threshold for
    /// <c>QuantityNotWithinLookupThreshold</c>, an explicit <c>null</c> for <c>LookupIsSkipped</c>.
    /// </summary>
    public Dictionary<string, int?> DistributorStockLookupQuantityByPart { get; set; } = new();

    public VehicleLookupRequestOptions ToVehicleLookupRequestOptions() => new()
    {
        LanguageCode = "en",
        LookupEndCustomer = LookupEndCustomer,
        IgnoreBrokerStock = IgnoreBrokerStock,
        UseKatashikiLookup = UseKatashikiLookup,
        RequestingCompanyID = RequestingCompanyID,
        VehicleServiceHistoryConsistencyLevel = VehicleServiceHistoryConsistencyLevel,
        TraceSSCEvaluation = TraceSSCEvaluation,
        GeneratePaintThicknessCertificateUrls = GeneratePaintThicknessCertificateUrls,
    };

    public int? DistributorStockLookupQuantityFor(string partNumber) =>
        DistributorStockLookupQuantityByPart.TryGetValue(partNumber, out var quantity)
            ? quantity
            : DistributorStockLookupQuantity;
}

/// <summary>
/// How a deployment keys its Parts container relative to the manufacturer form a recall names a part in
/// (<c>04007-07212</c>). The declarative stand-in for <c>LookupOptions.PartNumberStorageKeyResolver</c>.
/// </summary>
public enum PartNumberStorageKeyMode
{
    /// <summary>The manufacturer form is the key (<c>04007-07212</c>).</summary>
    AsIs,
    /// <summary>Hyphens removed (<c>0400707212</c>) — some deployments store parts this way with no prefix.</summary>
    DashStripped,
    /// <summary>A <c>T</c> prefix and hyphens removed (<c>T0400707212</c>) — other deployments store parts T-prefixed.</summary>
    TPrefixedDashStripped,
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

    /// <summary>
    /// How this environment's service codes name a scheduled service, declared the way a host
    /// declares it: patterns with named <c>milestone</c>, <c>program</c> and <c>qualifier</c> groups,
    /// tried in order. ADP ships none — a convention belongs to a source system, and one presented
    /// as a framework default reads a fraction of any estate that does not share it while looking
    /// configured — so an environment with milestone conditions has to declare one here or its
    /// rewards read no history at all.
    /// </summary>
    public List<ServiceCodeConvention> ServiceMilestoneConventions { get; set; } = new();

    /// <summary>Groups of labor codes used interchangeably for SSC repairs, exactly as in production LookupOptions.</summary>
    public List<List<string>> SSCInterchangeableLaborCodeGroups { get; set; } = new();

    /// <summary>
    /// Per company (JSON key = Identity <c>CompanyID</c>), the account numbers that mark one of that supply-chain
    /// company's entries as a direct sale to an end customer — the distributor's own retail account. Mirrors
    /// <c>LookupOptions.DirectEndCustomerSaleAccountNumbersByCompany</c>; the sets are built case-insensitive,
    /// as that option's own guidance recommends.
    /// </summary>
    public Dictionary<long, List<string>> DirectEndCustomerSaleAccountNumbersByCompany { get; set; } = new();

    /// <summary>A service item stays claimable through the whole of its expiry date, exactly as in production LookupOptions.</summary>
    public bool TreatServiceItemExpiryAsEndOfDay { get; set; }

    /// <summary>
    /// Activation is offered only to a requester whose company holds a vehicle entry for the VIN, exactly as in
    /// production LookupOptions. Needs <see cref="GeneratorRequestOptions.RequestingCompanyID"/> to mean anything.
    /// </summary>
    public bool RequireAllocationForActivation { get; set; }

    /// <summary>
    /// Master switch for SSC part availability, as in production LookupOptions. Off (the default): every SSC part
    /// stays <c>isAvailable = null</c>, the "not checked" chip. On: availability is <b>seeded</b> demonstratively —
    /// each open recall's parts alternate in-stock / out-of-stock, repaired recalls stay null — unless
    /// <see cref="SSCPartStockScope"/> is also set, in which case the real availability rule runs against this
    /// environment's own stock rows.
    /// </summary>
    public bool EnableSSCPartAvailability { get; set; }

    /// <summary>
    /// The stock <c>Location</c> keys whose inventory counts as "in stock" for the requester — the declarative
    /// stand-in for <c>LookupOptions.SSCPartStockScopeResolver</c> (a region key, a warehouse key). When set together
    /// with <see cref="EnableSSCPartAvailability"/>, the generator runs the enricher's own availability rule over the
    /// <c>StockParts</c> of every part in <see cref="GeneratorEnvironment.Parts"/>, matching recall part numbers to
    /// stock rows through <see cref="PartNumberStorageKeyMode"/>; a recall part with no stock row in scope is
    /// <c>false</c>, so an environment that wants a truthful "in stock" needs the part in its Parts table.
    /// </summary>
    public List<string>? SSCPartStockScope { get; set; }

    /// <summary>
    /// How this deployment keys its Parts container — the stand-in for <c>LookupOptions.PartNumberStorageKeyResolver</c>.
    /// Null leaves the resolver unset and ADP applies its neutral default (hyphens removed, upper-cased).
    /// </summary>
    public PartNumberStorageKeyMode? PartNumberStorageKeyMode { get; set; }

    /// <summary>Configured extended-warranty definitions, exactly as in production LookupOptions (same declarative condition contract as service items).</summary>
    public List<ExtendedWarrantyDefinitionModel> ExtendedWarrantyDefinitions { get; set; } = new();

    /// <summary>The company reported as provider of every <b>persisted</b> extended-warranty entry, exactly as in production LookupOptions.</summary>
    public long? ExtendedWarrantyProviderCompanyID { get; set; }

    /// <summary>
    /// Display name for coverages that carry none — the persisted entries, whose stored model has no name field.
    /// The declarative stand-in for <c>LookupOptions.ExtendedWarrantyNameResolver</c>: a host that runs extended
    /// warranty as one purchased programme returns that programme's label. Placeholder: <c>{ProviderCompanyName}</c>
    /// (the resolved name of the coverage's provider company, empty when unknown). Null leaves them unnamed and the
    /// consumer falls back to its generic wording.
    /// </summary>
    public string? ExtendedWarrantyName { get; set; }

    /// <summary>Per-VIN validity overrides supplied by configuration rather than a stored record, exactly as in production LookupOptions.</summary>
    public List<FreeServiceItemValidityOverrideModel> FreeServiceItemValidityOverrides { get; set; } = new();

    /// <summary>
    /// URL template for the pre-claim voucher of an inspection-triggered item — the mock stand-in for
    /// <c>LookupOptions.VehicleInspectionPreClaimVoucherPrintingURLResolver</c> (hosts return a signed, short-lived
    /// URL; a fixture wants a stable one). Placeholders: <c>{VehicleInspectionID}</c>, <c>{ServiceItemID}</c>,
    /// <c>{Language}</c>. Null leaves <c>printUrl</c> unset.
    /// </summary>
    public string? VehicleInspectionPreClaimVoucherPrintingURL { get; set; }

    /// <summary>
    /// URL template for the pre-claim voucher of an item on a service activation — the stand-in for
    /// <c>LookupOptions.ServiceActivationPreClaimVoucherPrintingURLResolver</c>. Placeholders:
    /// <c>{ServiceActivationID}</c>, <c>{ServiceItemID}</c>, <c>{Language}</c>. Overrides the inspection URL when
    /// both apply, as the evaluator does.
    /// </summary>
    public string? ServiceActivationPreClaimVoucherPrintingURL { get; set; }

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
            SSCInterchangeableLaborCodeGroups = SSCInterchangeableLaborCodeGroups,
            TreatServiceItemExpiryAsEndOfDay = TreatServiceItemExpiryAsEndOfDay,
            RequireAllocationForActivation = RequireAllocationForActivation,
            EnableSSCPartAvailability = EnableSSCPartAvailability,
            ExtendedWarrantyDefinitions = ExtendedWarrantyDefinitions,
            ExtendedWarrantyProviderCompanyID = ExtendedWarrantyProviderCompanyID,
            FreeServiceItemValidityOverrides = FreeServiceItemValidityOverrides,
            DirectEndCustomerSaleAccountNumbersByCompany = DirectEndCustomerSaleAccountNumbersByCompany
                .ToDictionary(
                    x => x.Key,
                    x => new HashSet<string>(x.Value ?? new List<string>(), StringComparer.OrdinalIgnoreCase)),
        };

        options.ServiceMilestones.Conventions = ServiceMilestoneConventions;

        // The two SSC part-availability resolvers, declared instead of coded: the scope is a fixed list of
        // stock locations (a host derives it from the requester's identity) and the storage key is one of
        // three shapes (a host codes the same mapping). Left unset when the environment says nothing, so
        // the enricher's own defaults apply.
        if (SSCPartStockScope is { Count: > 0 } scope)
            options.SSCPartStockScopeResolver = (model) =>
                new ValueTask<IReadOnlyCollection<string>?>(scope.AsReadOnly());

        options.PartNumberStorageKeyResolver = PartNumberStorageKeyMode switch
        {
            Generator.PartNumberStorageKeyMode.AsIs => partNumber => partNumber?.Trim() ?? string.Empty,
            Generator.PartNumberStorageKeyMode.DashStripped => partNumber => StripDashes(partNumber),
            Generator.PartNumberStorageKeyMode.TPrefixedDashStripped => partNumber => "T" + StripDashes(partNumber),
            _ => null,
        };

        // Names for persisted extended-warranty coverages: one template, resolved per coverage so the
        // provider placeholder can differ between entries.
        if (ExtendedWarrantyName is { } warrantyNameTemplate)
        {
            options.ExtendedWarrantyNameResolver = (model) =>
            {
                var providerName = long.TryParse(model.Value?.ProviderCompanyID, out var providerCompanyId)
                    && companyNames.TryGetValue(providerCompanyId, out var n) ? n : string.Empty;

                return new ValueTask<string?>(warrantyNameTemplate.Replace("{ProviderCompanyName}", providerName));
            };
        }

        // Pre-claim voucher URLs: production signs these with a short validity; the fixture carries a
        // deterministic, production-shaped link so the print affordance renders and stays byte-stable.
        if (VehicleInspectionPreClaimVoucherPrintingURL is { } inspectionVoucherTemplate)
        {
            options.VehicleInspectionPreClaimVoucherPrintingURLResolver = (model) =>
                new ValueTask<string?>(inspectionVoucherTemplate
                    .Replace("{VehicleInspectionID}", Uri.EscapeDataString(model.Value.VehicleInspectionID ?? string.Empty))
                    .Replace("{ServiceItemID}", Uri.EscapeDataString(model.Value.ServiceItemID ?? string.Empty))
                    .Replace("{Language}", Uri.EscapeDataString(model.Language ?? string.Empty)));
        }

        if (ServiceActivationPreClaimVoucherPrintingURL is { } activationVoucherTemplate)
        {
            options.ServiceActivationPreClaimVoucherPrintingURLResolver = (model) =>
                new ValueTask<string?>(activationVoucherTemplate
                    .Replace("{ServiceActivationID}", Uri.EscapeDataString(model.Value.ServiceActivationID ?? string.Empty))
                    .Replace("{ServiceItemID}", Uri.EscapeDataString(model.Value.ServiceItemID ?? string.Empty))
                    .Replace("{Language}", Uri.EscapeDataString(model.Language ?? string.Empty)));
        }

        // The three image resolvers point at the neutral drawings bundled with the package (see
        // DemoAssets): the keys are real-shaped upload paths that exist in no storage account, and
        // the mapping from key to drawing is deterministic so the fixtures stay byte-stable.
        options.PaintThickneesImageUrlResolver = (model) =>
            new ValueTask<string?>(DemoAssets.PaintPanelImageUrl(model.Value));

        options.AccessoryImageUrlResolver = (model) =>
            new ValueTask<string?>(DemoAssets.AccessoryImageUrl(model.Value));

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

        options.CompanyLogoResolver = (model) =>
            new ValueTask<string?>(DemoAssets.CompanyBadgeUrl(model.Value));

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

    private static string StripDashes(string? partNumber) =>
        partNumber?.Trim().Replace("-", string.Empty) ?? string.Empty;
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
