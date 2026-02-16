using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

internal class PartPriceEvaluator
{
    private readonly PartAggregateCosmosModel partDataAggregate;
    private readonly LookupOptions options;
    private readonly IServiceProvider services;

    public PartPriceEvaluator(PartAggregateCosmosModel partDataAggregate, LookupOptions options, IServiceProvider services)
    {
        this.partDataAggregate = partDataAggregate;
        this.options = options;
        this.services = services;
    }

    public async Task<(decimal? distributorPurchasePrice, IEnumerable<PartPriceDTO> prices)> Evaluate(PartLookupSource? source, string language = "en")
    {
        List<PartPriceDTO> prices = new();
        var cosmosPartCatalog = partDataAggregate.CatalogParts.FirstOrDefault();

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

        if (options?.PartLookupPriceResolver is not null)
        {
            var resolvedResult = await options.PartLookupPriceResolver(new(new(cosmosPartCatalog?.DistributorPurchasePrice, prices, source), language, services));
            return resolvedResult;
        }

        return (null, []);
    }
}
