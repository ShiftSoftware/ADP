using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services;

public class LookupOptions
{
    public Func<LookupOptionResolverModel<Dictionary<string,string>>, ValueTask<string?>>? ServiceItemImageUrlResolver { get; set; }
    public Dictionary<long?, int> BrandStandardWarrantyPeriodsInYears { get; set; } = new Dictionary<long?, int>();
    public Func<LookupOptionResolverModel<string>,ValueTask<string?>>? PaintThickneesImageUrlResolver { get; set; }
    public Func<LookupOptionResolverModel<string>, ValueTask<string?>>? AccessoryImageUrlResolver { get; set; }
    public Func<LookupOptionResolverModel<List<ShiftFileDTO>?>, ValueTask<List<ShiftFileDTO>?>>? CompanyLogoImageResolver { get; set; }
    public Func<LookupOptionResolverModel<PartLocationNameResolverModel>, ValueTask<string?>>? PartLocationNameResolver { get; set; }
    public Func<LookupOptionResolverModel<long?>, ValueTask<(long? countryID, string countryName)?>>? CountryFromBranchIDResolver { get; set; }
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? CountryNameResolver { get; set; }
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? RegionNameResolver { get; set; }
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? CompanyNameResolver { get; set; }
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? CompanyBranchNameResolver { get; set; }
    public Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? CompanyLogoResolver { get; set; }
    public Func<LookupOptionResolverModel<PartLookupPriceResoulverModel>, ValueTask<(decimal? distributorPurchasePrice, IEnumerable<PartPriceDTO> prices)>>? PartLookupPriceResolver { get; set; }
    public Func<LookupOptionResolverModel<IEnumerable<StockPartDTO>>, ValueTask<IEnumerable<StockPartDTO>>>? PartLookupStocksResolver { get; set; }
    public bool IncludeInactivatedFreeServiceItems { get; set; }
    public bool PerVehicleEligibilitySupport { get; set; }
    public bool WarrantyStartDateDefaultsToInvoiceDate { get; set; } = true;
    public string SigningSecreteKey { get; set; }
    public TimeSpan SignatureValidityDuration { get; set; }
    //public string ServiceActivationPreClaimVoucherPrintingURL { get; set; }
    //public string VehicleInspectionPreClaimVoucherPrintingURL { get; set; }

    public Func<LookupOptionResolverModel<(string VehicleInspectionID, string ServiceItemID)>, ValueTask<string?>>? VehicleInspectionPreClaimVoucherPrintingURLResolver { get; set; }
    public Func<LookupOptionResolverModel<(string ServiceActivationID, string ServiceItemID)>, ValueTask<string?>>? ServiceActivationPreClaimVoucherPrintingURLResolver { get; set; }

    public List<VehicleItemWarning> StandardItemClaimWarnings { get; set; }

    public int? DistributorStockPartLookupQuantityThreshold { get; set; }
    public bool ShowPartLookupStockQauntity { get; set; } = false;
    public bool EnableManufacturerLookup { get; set; }
    public bool CatalogPartShouldComeFromStock { get; set; }
    public bool LookupBrokerStock { get; set; }
    public StorageSources VehicleLookupStorageSource { get; set; }
    public string VehicleLookupDuckDBConnection { get; set; }
}

public class PartLocationNameResolverModel
{
    public string PartNumber { get; internal set; }
    public string ItemType { get; internal set; }
    public string LocationID { get; internal set; }

    public PartLocationNameResolverModel(string partNumber, string itemType, string locationID)
    {
        this.PartNumber = partNumber;
        this.ItemType = itemType;
        this.LocationID = locationID;
    }
}

public class PartLookupPriceResoulverModel
{
    public decimal? DistributorPurchasePrice { get; set; }
    public IEnumerable<PartPriceDTO> Prices { get; set; }
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

public class LookupOptionResolverModel<T>
{
    public IServiceProvider Services { get; internal set; }
    public T? Value { get; set; }
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

public enum DepartmentType
{
    Sales,
    Service,
}

public enum PartLookupSource
{
    ReCaptcha,
    AzureFunctionKey,
    AppCheck,
    Anonymous,
    Token,
    Internal,
}