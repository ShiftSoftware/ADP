using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Part;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class PartLookupService
{
    private readonly PartLookupCosmosService partLookupCosmosService;
    private readonly IServiceProvider services;
    private readonly ILogCosmosService logCosmosService;
    private readonly LookupOptions options;

    public PartLookupService(
        PartLookupCosmosService partLookupCosmosService,
        IServiceProvider services,
        ILogCosmosService logCosmosService,
        LookupOptions options)
    {
        this.partLookupCosmosService = partLookupCosmosService;
        this.services = services;
        this.logCosmosService = logCosmosService;
        this.options = options;
    }

    public async Task<PartLookupDTO> LookupAsync(
        string partNumber,
        int? distributorStockLookupQuantity = null,
        PartLookupLogInfo? logInfo = null,
        string language = "en",
        PartLookupSource? source = null,
        bool skipLogging = false)
    {
        partNumber = partNumber?.Replace("-", "");

        var data = await partLookupCosmosService.GetAggregatePartAsync(partNumber);

        if (data == null)
            return null;

        var cosmosPartCatalog = data.CatalogParts.FirstOrDefault();

        List<PartPriceDTO> prices = new();

        if (cosmosPartCatalog?.CountryData is not null)
        {
            foreach (var countryPrice in cosmosPartCatalog?.CountryData)
            {
                string? countryName = null;
                if (options?.CountryNameResolver is not null)
                {
                    countryName = await options.CountryNameResolver(new LookupOptionResolverModel<long?>
                    {
                        Services = services,
                        Value = countryPrice.CountryID,
                        Language = language,
                    });
                }

                foreach (var price in countryPrice.RegionPrices)
                {
                    string regionName = null;
                    if (options?.RegionNameResolver is not null)
                    {
                        regionName = await options.RegionNameResolver(new LookupOptionResolverModel<long?>
                        {
                            Services = services,
                            Value = price.RegionID,
                            Language = language,
                        });
                    }

                    prices.Add(new PartPriceDTO
                    {
                        CountryID = countryPrice.CountryID.ToString(),
                        CountryName = countryName,
                        RegionID = price.RegionID.ToString(),
                        RegionName = regionName,
                        PurchasePrice = new(price.PurchasePrice),
                        RetailPrice = new(price.RetailPrice),
                        WarrantyPrice = new(price.WarrantyPrice)
                    });
                }
            }
        }

        IEnumerable<PartPriceDTO> resolvedPrices = new List<PartPriceDTO>();
        decimal? distributorPurchasePrice = null;


        if (options.PartLookupPriceResolver is not null)
        {
            var resolvedResult = await options.PartLookupPriceResolver(new(new(cosmosPartCatalog?.DistributorPurchasePrice, prices, source), language, services));

            distributorPurchasePrice = resolvedResult.distributorPurchasePrice;
            resolvedPrices = resolvedResult.prices;
        }

        var result = new PartLookupDTO
        {
            PartNumber = partNumber,
            PartDescription = cosmosPartCatalog?.PartName,
            LocalDescription = cosmosPartCatalog?.LocalDescription,
            ProductGroup = cosmosPartCatalog?.ProductGroup,
            BinType = cosmosPartCatalog?.BinType,
            CubicMeasure = cosmosPartCatalog?.CubicMeasure,
            Length = cosmosPartCatalog?.Length is not null && cosmosPartCatalog?.Length > 0 ? cosmosPartCatalog?.Length : cosmosPartCatalog?.Dimension1,
            Width = cosmosPartCatalog?.Width is not null && cosmosPartCatalog?.Width > 0 ? cosmosPartCatalog?.Width : cosmosPartCatalog?.Dimension2,
            Height = cosmosPartCatalog?.Height is not null && cosmosPartCatalog?.Height > 0 ? cosmosPartCatalog?.Height : cosmosPartCatalog?.Dimension3,
            GrossWeight = cosmosPartCatalog?.GrossWeight,
            HSCode = cosmosPartCatalog?.HSCode,
            NetWeight = cosmosPartCatalog?.NetWeight,
            Origin = cosmosPartCatalog?.Origin,
            PNC = cosmosPartCatalog?.PNC,
            DistributorPurchasePrice = distributorPurchasePrice,
            HSCodes = null,
            LogId = null,
            Prices = resolvedPrices,
            SupersededTo = cosmosPartCatalog?.SupersededTo?.Select(x=> x.PartNumber),
            SupersededFrom = cosmosPartCatalog?.SupersededFrom?.Select(x=> x.PartNumber),
            ShowManufacturerPartLookup = CalculateShowManufacturerPartLookup(distributorStockLookupQuantity, data.StockParts?.Sum(x=> x.AvailableQuantity)??0, distributorStockLookupQuantity >= 10),
            DeadStock = await new PartDeadStockEvaluator(data, options, services).Evaluate(language),
            StockParts = await new PartStockEvaluator(data, options, services).Evaluate(distributorStockLookupQuantity, language),
        };

        if (!skipLogging)
        {
            var logId = await logCosmosService.LogPartLookupAsync(logInfo, result, distributorStockLookupQuantity);

            result.LogId = logId;
        }

        return result;
    }

    public async Task<string> ManufacturerPartLookupRequestAsync(ManufacturerPartLookupRequestDTO dto, long? userId, string? userEmail, long? companyId = null, long? companyBranchId = null)
    {
        var id = Guid.NewGuid().ToString();

        var model = new ManufacturerPartLookupModel
        {
            PartNumber = dto.PartNumber?.Trim(),
            Quantity = dto.Quantity,
            BranchID = companyBranchId,
            CompanyID = companyId,
            id = id,
            LogId = dto.LogId,
            OrderType = dto.OrderType,
            UserID = userId,
            Status = ManufacturerPartLookupStatus.Pending,
            UserEmail = userEmail,
        };

        await partLookupCosmosService.InsertManufacturerPartLookupAsync(model);

        return id;
    }

    public async Task UpdateManufacturerPartLookupStatusAsync(string id, string partNumber, ManufacturerPartLookupStatus status, IEnumerable<KeyValuePair<string, string>>? lookupResult = null)
    {
        await partLookupCosmosService.UpdateManufacturerPartLookupStatusAsync(id, partNumber, status, lookupResult);
    }

    public async Task<IEnumerable<ManufacturerPartLookupResponseDTO>> GetManufacturerPartLookupsByStatusAsync(ManufacturerPartLookupStatus status)
    {
        return (await partLookupCosmosService.GetManufacturerPartLookupsByStatusAsync(status))
            .Select(x => new ManufacturerPartLookupResponseDTO
            {
                id = x.id,
                PartNumber = x.PartNumber,
                OrderType = x.OrderType,
                Status = x.Status,
                Quantity= x.Quantity
            });
    }

    public async Task<ManufacturerPartLookupResponseDTO?> GetManufacturerPartLookupByStatusAsync(ManufacturerPartLookupStatus status)
    {
        var model = await partLookupCosmosService.GetManufacturerPartLookupByStatusAsync(status);
        if (model is null) return null;

        return new ManufacturerPartLookupResponseDTO
        {
            id = model.id,
            PartNumber = model.PartNumber,
            OrderType = model.OrderType,
            Status = model.Status,
            Quantity = model.Quantity
        };
    }

    public async Task<ManufacturerPartLookupResponseDTO?> GetManufacturerPartLookupAsync(string id, string partNumber)
    {
        var model = await partLookupCosmosService.GetManufacturerPartLookupAsync(id, partNumber);
        if (model is null) return null;

        return new ManufacturerPartLookupResponseDTO
        {
            id = model.id,
            PartNumber = model.PartNumber,
            OrderType = model.OrderType,
            Status = model.Status,
            Quantity = model.Quantity,
            ManufacturerResult = model.ManufacturerResult.Select(x => new KeyValuePairDTO
            {
                Key = x.Key,
                Value = x.Value
            }),
        };
    }

    private bool CalculateShowManufacturerPartLookup(int? requestedQuantity, decimal availableQuantity, bool exceedsThreshold)
    {
        if (exceedsThreshold)
            return false;

        if (requestedQuantity is null)
            return false;

        if(requestedQuantity > availableQuantity)
            return true;

        return false;
    }
}