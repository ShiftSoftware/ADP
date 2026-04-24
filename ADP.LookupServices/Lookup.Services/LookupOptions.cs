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
    /// <summary>Resolver delegate that converts an accessory image path to a full URL.</summary>
    public Func<LookupOptionResolverModel<string>, ValueTask<string?>>? AccessoryImageUrlResolver { get; set; }
    /// <summary>Resolver delegate that resolves company logo images.</summary>
    public Func<LookupOptionResolverModel<List<ShiftFileDTO>?>, ValueTask<List<ShiftFileDTO>?>>? CompanyLogoImageResolver { get; set; }
    /// <summary>Resolver delegate that resolves a part location identifier to a human-readable name.</summary>
    public Func<LookupOptionResolverModel<PartLocationNameResolverModel>, ValueTask<string?>>? PartLocationNameResolver { get; set; }
    /// <summary>Resolver delegate that resolves a branch ID to its country ID and name.</summary>
    public Func<LookupOptionResolverModel<long?>, ValueTask<(long? countryID, string countryName)?>>? CountryFromBranchIDResolver { get; set; }
    /// <summary>Resolver delegate that resolves a country ID to its name.</summary>
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? CountryNameResolver { get; set; }
    /// <summary>Resolver delegate that resolves a region ID to its name.</summary>
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? RegionNameResolver { get; set; }
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
    /// <summary>Whether the warranty start date should default to the invoice date when no explicit activation date is set. Defaults to true.</summary>
    public bool WarrantyStartDateDefaultsToInvoiceDate { get; set; } = true;
    /// <summary>The HMAC secret key used for signing service item claim requests.</summary>
    public string SigningSecreteKey { get; set; } = string.Empty;
    /// <summary>How long a generated claim signature remains valid.</summary>
    public TimeSpan SignatureValidityDuration { get; set; }

    /// <summary>Resolver delegate that generates a pre-claim voucher printing URL for vehicle inspection-based claims.</summary>
    public Func<LookupOptionResolverModel<(string VehicleInspectionID, string ServiceItemID)>, ValueTask<string?>>? VehicleInspectionPreClaimVoucherPrintingURLResolver { get; set; }
    /// <summary>Resolver delegate that generates a pre-claim voucher printing URL for service activation-based claims.</summary>
    public Func<LookupOptionResolverModel<(string ServiceActivationID, string ServiceItemID)>, ValueTask<string?>>? ServiceActivationPreClaimVoucherPrintingURLResolver { get; set; }

    /// <summary>Standard warning messages displayed to users before claiming any service item.</summary>
    public List<VehicleItemWarning> StandardItemClaimWarnings { get; set; }

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