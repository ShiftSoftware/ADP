using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services;

public class LookupOptions
{
    public Func<LookupOptionResolverModel<Dictionary<string,string>>, ValueTask<string?>>? ServiceItemImageUrlResolver { get; set; }
    public Func<LookupOptionResolverModel<string>,ValueTask<string?>>? PaintThickneesImageUrlResolver { get; set; }
    public Func<LookupOptionResolverModel<string>, ValueTask<string?>>? AccessoryImageUrlResolver { get; set; }
    public Func<LookupOptionResolverModel<List<ShiftFileDTO>?>, ValueTask<List<ShiftFileDTO>?>>? CompanyLogoImageResolver { get; set; }
    public Func<LookupOptionResolverModel<PartLocationNameResolverModel>, ValueTask<string?>>? PartLocationNameResolver { get; set; }
    public Func<LookupOptionResolverModel<(string companyIntegrationID, string companyBranchIntegrationID)>, ValueTask<(string countryIntegrationID, string countryName)?>>? CountryFromBranchIDResolver { get; set; }
    public Func<LookupOptionResolverModel<string>, ValueTask<string?>>? CountryNameResolver { get; set; }
    public Func<LookupOptionResolverModel<string>, ValueTask<string?>>? RegionNameResolver { get; set; }
    public Func<LookupOptionResolverModel<string>, ValueTask<string?>>? CompanyNameResolver { get; set; }
    public Func<LookupOptionResolverModel<CompanyBranchNameResolverModel>, ValueTask<string?>>? CompanyBranchNameResolver { get; set; }
    public Func<LookupOptionResolverModel<string>, ValueTask<string?>>? CompanyLogoResolver { get; set; }
    public Func<LookupOptionResolverModel<PartLookupPriceResoulverModel>, ValueTask<IEnumerable<PartPriceDTO>>>? PartLookupPriceResolver { get; set; }
    public Func<LookupOptionResolverModel<IEnumerable<StockPartDTO>>, ValueTask<IEnumerable<StockPartDTO>>>? PartLookupStocksResolver { get; set; }
}

public class CompanyBranchNameResolverModel
{
    public string CompanyIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
    public DepartmentType? DepartmentType { get; set; }

    public CompanyBranchNameResolverModel(string companyIntegrationID, string branchIntegrationID, DepartmentType? departmentType)
    {
        this.CompanyIntegrationID = companyIntegrationID;
        this.BranchIntegrationID = branchIntegrationID;
        this.DepartmentType = departmentType;
    }
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
    public IEnumerable<PartPriceDTO> Prices { get; set; }
    public PartLookupSource? Source { get; set; }

    public PartLookupPriceResoulverModel()
    {
        
    }

    public PartLookupPriceResoulverModel(IEnumerable<PartPriceDTO> prices, PartLookupSource? source)
    {
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