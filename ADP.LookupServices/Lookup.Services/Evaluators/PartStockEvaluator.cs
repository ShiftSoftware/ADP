using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

internal class PartStockEvaluator
{
    private readonly PartAggregateCosmosModel partDataAggregate;
    private readonly LookupOptions options;
    private readonly IServiceProvider services;

    public PartStockEvaluator(PartAggregateCosmosModel partDataAggregate, LookupOptions options, IServiceProvider services)
    {
        this.partDataAggregate = partDataAggregate;
        this.options = options;
        this.services = services;
    }

    public async Task<IEnumerable<StockPartDTO>> Evaluate(int? distributorStockLookupQuantity, string language = "en")
    {
        List<StockPartDTO> stockParts = new();

        foreach (var item in partDataAggregate.StockParts)
        {
            var stock = new StockPartDTO
            {
                QuantityLookUpResult =
                                        distributorStockLookupQuantity is null ? Enums.QuantityLookUpResults.LookupIsSkipped :
                                        distributorStockLookupQuantity >= options?.DistributorStockPartLookupQuantityThreshold.GetValueOrDefault() || distributorStockLookupQuantity == 0 ? Enums.QuantityLookUpResults.QuantityNotWithinLookupThreshold :
                                        item.AvailableQuantity <= 0 ? Enums.QuantityLookUpResults.NotAvailable :
                                        item.AvailableQuantity >= distributorStockLookupQuantity ? Enums.QuantityLookUpResults.Available :
                                        Enums.QuantityLookUpResults.PartiallyAvailable,
                LocationID = item.Location,
                AvailableQuantity = (options?.ShowPartLookupStockQauntity ?? false) ? item.AvailableQuantity : null,
            };

            if (options?.PartLocationNameResolver is not null)
                stock.LocationName = await options.PartLocationNameResolver(
                    new(new(item.PartNumber, item.ItemType, item.Location), language, services));

            stockParts.Add(stock);
        }

        if (options?.PartLookupStocksResolver is not null)
            stockParts = (await options.PartLookupStocksResolver(new(stockParts, language, services))).ToList();

        return stockParts;
    }
}
