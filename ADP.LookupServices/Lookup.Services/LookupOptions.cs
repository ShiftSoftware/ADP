using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services;

/// <summary>
/// The main configuration class for the lookup services.
/// Contains resolver delegates for resolving images, names, and prices; feature flags; warranty settings; and storage options.
/// </summary>
[Docable]
public class LookupOptions
{
    /// <summary>Resolver delegate that converts a multilingual image dictionary to a resolved image URL for service items.</summary>
    public Func<LookupOptionResolverModel<Dictionary<string,string>>, ValueTask<string?>>? ServiceItemImageUrlResolver { get; set; }
    /// <summary>A dictionary mapping brand IDs to their standard warranty period in years.</summary>
    public Dictionary<long?, int> BrandStandardWarrantyPeriodsInYears { get; set; } = new Dictionary<long?, int>();
    /// <summary>Resolver delegate that converts a paint thickness image path to a full URL.</summary>
    public Func<LookupOptionResolverModel<string>,ValueTask<string?>>? PaintThickneesImageUrlResolver { get; set; }
    /// <summary>
    /// Resolver delegate that derives the Paint Thickness Certificate's serial number from the chosen
    /// inspection's <c>id</c>. Must be deterministic — the same inspection should always produce the same
    /// serial so re-printed certificates match. When unset, <c>PaintThicknessCertificateModel.SerialNumber</c>
    /// is <c>null</c>.
    /// </summary>
    public Func<LookupOptionResolverModel<string>, ValueTask<string?>>? PaintThicknessCertificateSerialNumberResolver { get; set; }
    /// <summary>
    /// Resolver delegate that generates the Paint Thickness Certificate's signed public URLs for a
    /// VIN — one entry per print language the host supports (the host owns the certificate
    /// templates, so it declares the supported set by what it returns here; list order is display
    /// order). The same kind of links the printed certificate's QR carries. Invoked only when the
    /// certificate is available and the request opted in via
    /// <see cref="VehicleLookupRequestOptions.GeneratePaintThicknessCertificateUrls"/>. Receives the
    /// VIN as the value and the request's language code. When unset,
    /// <c>VehicleLookupDTO.PaintThicknessCertificateUrls</c> stays null.
    /// </summary>
    public Func<LookupOptionResolverModel<string>, ValueTask<List<PaintThicknessCertificateUrlDTO>?>>? PaintThicknessCertificateUrlsResolver { get; set; }
    /// <summary>Resolver delegate that converts an accessory image path to a full URL.</summary>
    public Func<LookupOptionResolverModel<string>, ValueTask<string?>>? AccessoryImageUrlResolver { get; set; }
    /// <summary>Resolver delegate that resolves company logo images.</summary>
    public Func<LookupOptionResolverModel<List<ShiftFileDTO>?>, ValueTask<List<ShiftFileDTO>?>>? CompanyLogoImageResolver { get; set; }
    /// <summary>Resolver delegate that resolves a part location identifier to a human-readable name.</summary>
    public Func<LookupOptionResolverModel<PartLocationNameResolverModel>, ValueTask<string?>>? PartLocationNameResolver { get; set; }
    /// <summary>Resolver delegate that resolves a country ID to its name.</summary>
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? CountryNameResolver { get; set; }
    /// <summary>Resolver delegate that resolves a brand ID to its name. Used by the diagnostic trace renderer; not required for normal lookups.</summary>
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? BrandNameResolver { get; set; }
    /// <summary>Resolver delegate that resolves a region ID to its name.</summary>
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? RegionNameResolver { get; set; }
    /// <summary>Resolver delegate that resolves a city ID to its name.</summary>
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? CityNameResolver { get; set; }
    /// <summary>Resolver delegate that resolves a company branch ID to the city ID the branch belongs to.</summary>
    public Func<LookupOptionResolverModel<long?>, ValueTask<long?>>? CityFromBranchIDResolver { get; set; }
    /// <summary>Resolver delegate that resolves a company ID to its name.</summary>
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? CompanyNameResolver { get; set; }
    /// <summary>Resolver delegate that resolves a company branch ID to its name.</summary>
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? CompanyBranchNameResolver { get; set; }
    /// <summary>Resolver delegate that resolves a company ID to its logo URL.</summary>
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? CompanyLogoResolver { get; set; }
    /// <summary>Resolver delegate that processes and returns part pricing (distributor purchase price and per-region prices).</summary>
    public Func<LookupOptionResolverModel<PartLookupPriceResoulverModel>, ValueTask<(decimal? distributorPurchasePrice, IEnumerable<PartPriceDTO> prices)>>? PartLookupPriceResolver { get; set; }
    /// <summary>Resolver delegate that processes and returns part stock availability data.</summary>
    public Func<LookupOptionResolverModel<IEnumerable<StockPartDTO>>, ValueTask<IEnumerable<StockPartDTO>>>? PartLookupStocksResolver { get; set; }
    /// <summary>Whether to include free service items that have not yet been activated (e.g., awaiting warranty activation).</summary>
    public bool IncludeInactivatedFreeServiceItems { get; set; }
    /// <summary>
    /// When enabled, warranty activation is only offered to a requester whose company has a vehicle entry for the
    /// vehicle (i.e. it has been allocated/shipped/delivered to them). When activation is due but the vehicle is not
    /// allocated to the requesting company, <c>VehicleWarrantyDTO.ActivationStatus</c> becomes <c>BlockedNotAllocated</c>
    /// instead of <c>Required</c>. Requires the caller to supply <c>VehicleLookupRequestOptions.RequestingCompanyID</c>;
    /// with no requesting company the activation affordance is suppressed. Layered on top of
    /// <see cref="IncludeInactivatedFreeServiceItems"/>; defaults to false.
    /// </summary>
    public bool RequireAllocationForActivation { get; set; }
    /// <summary>Whether the warranty start date should default to the invoice date when no explicit activation date is set. Defaults to true.</summary>
    public bool WarrantyStartDateDefaultsToInvoiceDate { get; set; } = true;
    /// <summary>
    /// The Identity <c>CompanyID</c> of the distributor company. A VIN can have multiple <c>VehicleEntries</c> —
    /// one for the distributor and one (or more) for the selling dealer(s) — and the dealer's sale invoice date
    /// is later than the distributor's. Distributor-scoped logic such as the Paint Thickness Certificate anchors
    /// on the distributor's invoice date, so it selects the entry whose <c>CompanyID</c> equals this value.
    /// <b>Required</b> for the Paint Thickness Certificate: if this is unset, or the VIN has no invoiced entry for
    /// this company, no certificate is produced — it never falls back to a dealer's invoice.
    /// </summary>
    public long? DistributorCompanyID { get; set; }
    /// <summary>The HMAC secret key used for signing service item claim requests.</summary>
    public string SigningSecretKey { get; set; } = string.Empty;
    /// <summary>How long a generated claim signature remains valid.</summary>
    public TimeSpan SignatureValidityDuration { get; set; }
    /// <summary>Clock used when stamping <c>SignatureExpiry</c>. Defaults to <see cref="TimeProvider.System"/>; override with a fixed provider to produce deterministic signatures for sample/doc generation or tests.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>Resolver delegate that generates a pre-claim voucher printing URL for vehicle inspection-based claims.</summary>
    public Func<LookupOptionResolverModel<(string VehicleInspectionID, string ServiceItemID)>, ValueTask<string?>>? VehicleInspectionPreClaimVoucherPrintingURLResolver { get; set; }
    /// <summary>Resolver delegate that generates a pre-claim voucher printing URL for service activation-based claims.</summary>
    public Func<LookupOptionResolverModel<(string ServiceActivationID, string ServiceItemID)>, ValueTask<string?>>? ServiceActivationPreClaimVoucherPrintingURLResolver { get; set; }

    /// <summary>Standard warning messages displayed to users before claiming any service item.</summary>
    public List<VehicleItemWarning> StandardItemClaimWarnings { get; set; }

    /// <summary>Resolver delegate that builds the warning shown when claiming a free sequential item would cancel lower-mileage pending items (e.g. claiming the 50K service while the 45K is still pending). Receives the item being claimed and the pending items that claiming it would cancel (the pre-claim mirror of dynamic cancellation). Return null to suppress the warning. When not configured, no skipped-items warning is attached.</summary>
    public Func<LookupOptionResolverModel<(VehicleServiceItemDTO ItemBeingClaimed, List<VehicleServiceItemDTO> SkippedItems)>, ValueTask<VehicleItemWarning?>>? SkippedItemsClaimWarningResolver { get; set; }

    /// <summary>Resolver delegate that builds the warning shown when claiming items on a vehicle that is held by a broker (TBP) that has not invoiced it yet. Receives the broker sale information. Return null to suppress the warning. When not configured, no broker warning is attached.</summary>
    public Func<LookupOptionResolverModel<VehicleBrokerSaleInformation>, ValueTask<VehicleItemWarning?>>? UnInvoicedBrokerClaimWarningResolver { get; set; }

    /// <summary>The minimum stock quantity threshold for distributor stock lookup. Quantities below this are reported as QuantityNotWithinLookupThreshold.</summary>
    public int? DistributorStockPartLookupQuantityThreshold { get; set; }
    /// <summary>Whether to show the actual stock quantity in part lookup results (vs. just availability status). Defaults to false.</summary>
    public bool ShowPartLookupStockQauntity { get; set; } = false;
    /// <summary>Whether the manufacturer part lookup feature is enabled.</summary>
    public bool EnableManufacturerLookup { get; set; }
    /// <summary>Whether catalog part data should only come from stock records (vs. the dedicated catalog).</summary>
    public bool CatalogPartShouldComeFromStock { get; set; }
    /// <summary>Whether to look up broker stock data for vehicles.</summary>
    public bool LookupBrokerStock { get; set; }
    /// <summary>The storage backend to use for vehicle lookups (CosmosDB or DuckDB).</summary>
    public StorageSources VehicleLookupStorageSource { get; set; }
}

/// <summary>
/// Input model for the part location name resolver, containing the part number, item type, and location ID to resolve.
/// </summary>
public class PartLocationNameResolverModel
{
    /// <summary>The part number being looked up.</summary>
    public string PartNumber { get; internal set; }
    /// <summary>The item type (e.g., StockPart, CompanyDeadStockPart).</summary>
    public string ItemType { get; internal set; }
    /// <summary>The location/warehouse identifier to resolve into a name.</summary>
    public string LocationID { get; internal set; }

    public PartLocationNameResolverModel(string partNumber, string itemType, string locationID)
    {
        this.PartNumber = partNumber;
        this.ItemType = itemType;
        this.LocationID = locationID;
    }
}

/// <summary>
/// Input model for the part price resolver, containing the current prices and the lookup source for context-dependent pricing.
/// </summary>
public class PartLookupPriceResoulverModel
{
    /// <summary>The distributor purchase price from the catalog.</summary>
    public decimal? DistributorPurchasePrice { get; set; }
    /// <summary>The per-country/region prices calculated by the evaluator.</summary>
    public IEnumerable<PartPriceDTO> Prices { get; set; }
    /// <summary>The authentication source of the lookup request (used for context-dependent pricing).</summary>
    public PartLookupSource? Source { get; set; }

    public PartLookupPriceResoulverModel()
    {

    }

    public PartLookupPriceResoulverModel(decimal? distributorPurchasePrice, IEnumerable<PartPriceDTO> prices, PartLookupSource? source)
    {
        this.DistributorPurchasePrice = distributorPurchasePrice;
        this.Prices = prices;
        this.Source = source;
    }
}

/// <summary>
/// Generic context model passed to all resolver delegates, providing the DI service provider, the value to resolve, and the current language.
/// </summary>
public class LookupOptionResolverModel<T>
{
    /// <summary>The DI service provider for accessing registered services within the resolver.</summary>
    public IServiceProvider Services { get; internal set; }
    /// <summary>The value to be resolved (type depends on the specific resolver).</summary>
    public T? Value { get; set; }
    /// <summary>The current language code for localized resolution.</summary>
    public string? Language { get; set; }

    public LookupOptionResolverModel(T value,string? language, IServiceProvider services)
    {
        this.Services = services;
        this.Value = value;
        this.Language = language;
    }

    public LookupOptionResolverModel()
    {

    }
}

/// <summary>
/// The department type for categorizing lookups.
/// </summary>
[Docable]
public enum DepartmentType
{
    /// <summary>Sales department.</summary>
    Sales,
    /// <summary>Service/workshop department.</summary>
    Service,
}

/// <summary>
/// The authentication source/method used for part lookup requests.
/// </summary>
[Docable]
public enum PartLookupSource
{
    /// <summary>Authenticated via Google ReCaptcha.</summary>
    ReCaptcha,
    /// <summary>Authenticated via Azure Function key.</summary>
    AzureFunctionKey,
    /// <summary>Authenticated via Firebase App Check.</summary>
    AppCheck,
    /// <summary>Anonymous (unauthenticated) access.</summary>
    Anonymous,
    /// <summary>Authenticated via bearer token.</summary>
    Token,
    /// <summary>Internal system-to-system call.</summary>
    Internal,
}