using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
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
        PartLookupSource? source = null)
    {
        var data = await partLookupCosmosService.GetAggregatePartAsync(partNumber);

        if (data == null)
            return null;

        var cosmosPartCatalog = data.PartCatalog.FirstOrDefault();

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
        foreach(var countryPrice in cosmosPartCatalog?.CountryPrices)
        {
            string? countryName = null;
            if(options?.CountryNameResolver is not null)
            {
                countryName = await options.CountryNameResolver(new LookupOptionResolverModel<string>
                {
                    Services = services,
                    Value = countryPrice.CountryIntegrationID,
                    Language = language,
                });
            }

            foreach (var price in countryPrice.RegionPrices)
            {
                string regionName = null;
                if(options?.RegionNameResolver is not null)
                {
                    regionName = await options.RegionNameResolver(new LookupOptionResolverModel<string>
                    {
                        Services = services,
                        Value = price.RegionIntegrationID,
                        Language = language,
                    });
                }

                prices.Add(new PartPriceDTO
                {
                    CountryIntegrationID = countryPrice.CountryIntegrationID,
                    CountryName = countryName,
                    RegionIntegrationID = price.RegionIntegrationID,
                    RegionName = regionName,
                    FOB = new(price.FOB),
                    Price = new(price.Price),
                    WarrantyPrice = new(price.WarrantyPrice)
                });
            }
        }

        IEnumerable<PartPriceDTO> resolvedPrices = new List<PartPriceDTO>();
        if (options.PartLookupPriceResolver is not null)
            resolvedPrices = await options.PartLookupPriceResolver(new(new(prices, source), language, services));

        var result = new PartLookupDTO
        {
            PartNumber = partNumber,
            PartDescription = data.StockParts.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.PartDescription))?.PartDescription ?? cosmosPartCatalog?.PartName,
            LocalDescription = cosmosPartCatalog?.LocalDescription,
            Group = cosmosPartCatalog?.ProductGroup,
            BinCode = null,
            CubicMeasure = cosmosPartCatalog?.CubicMeasure,
            dimension1 = cosmosPartCatalog?.Dimension1,
            dimension2 = cosmosPartCatalog?.Dimension2,
            dimension3 = cosmosPartCatalog?.Dimension3,
            GrossWeight = cosmosPartCatalog?.GrossWeight,
            HSCode = cosmosPartCatalog?.HSCode,
            NetWeight = cosmosPartCatalog?.NetWeight,
            Origin = cosmosPartCatalog?.Origin,
            PNC = cosmosPartCatalog?.PNC,
            PNCLocalName = cosmosPartCatalog?.PNCLocalDescription,
            UZHSCode = cosmosPartCatalog?.LocalHSCode,            
            StockParts = stockParts,
            Prices = resolvedPrices,
            SupersededTo = cosmosPartCatalog?.SupersededTo?.Select(x=> x.PartNumber)
        };

        var logId = await logCosmosService.LogPartLookupAsync(logInfo, result, distributorStockLookupQuantity);

        result.LogId = logId;

        return result;
    }
}
