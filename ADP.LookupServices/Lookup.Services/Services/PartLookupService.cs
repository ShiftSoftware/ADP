﻿using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
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

        List<StockPartDTO> stockParts = new();

        foreach (var item in data.StockParts)
        {
            var stock = new StockPartDTO
            {
                QuantityLookUpResult =
                                        distributorStockLookupQuantity is null ? Enums.QuantityLookUpResults.LookupIsSkipped :
                                        distributorStockLookupQuantity >= 10 || distributorStockLookupQuantity == 0 ? Enums.QuantityLookUpResults.QuantityNotWithinLookupThreshold :
                                        item.Quantity <= 0 ? Enums.QuantityLookUpResults.NotAvailable :
                                        item.Quantity >= distributorStockLookupQuantity ? Enums.QuantityLookUpResults.Available :
                                        Enums.QuantityLookUpResults.PartiallyAvailable,
                LocationID = item.Location,
            };

            if (options?.PartLocationNameResolver is not null)
                stock.LocationName = await options.PartLocationNameResolver(
                    new(new(item.PartNumber, item.ItemType, item.Location), language, services));

            stockParts.Add(stock);
        }

        if(options?.PartLookupStocksResolver is not null)
            stockParts = (await options.PartLookupStocksResolver(new(stockParts, language, services))).ToList();

        List<PartPriceDTO> prices = new();

        if (cosmosPartCatalog?.CountryData is not null)
        {
            foreach (var countryPrice in cosmosPartCatalog?.CountryData)
            {
                string? countryName = null;
                if (options?.CountryNameResolver is not null)
                {
                    countryName = await options.CountryNameResolver(new LookupOptionResolverModel<string>
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
                        regionName = await options.RegionNameResolver(new LookupOptionResolverModel<string>
                        {
                            Services = services,
                            Value = price.RegionID,
                            Language = language,
                        });
                    }

                    prices.Add(new PartPriceDTO
                    {
                        CountryID = countryPrice.CountryID,
                        CountryName = countryName,
                        RegionID = price.RegionID,
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
            DeadStock = null,
            LogId = null,
            StockParts = stockParts,
            Prices = resolvedPrices,
            SupersededTo = cosmosPartCatalog?.SupersededTo?.Select(x=> x.PartNumber)
        };

        if (!skipLogging)
        {
            var logId = await logCosmosService.LogPartLookupAsync(logInfo, result, distributorStockLookupQuantity);

            result.LogId = logId;
        }

        return result;
    }
}