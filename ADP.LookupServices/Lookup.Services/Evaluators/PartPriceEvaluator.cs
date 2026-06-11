using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Models.Part;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

internal class PartPriceEvaluator
{
    /// <summary>
    /// The unit name used for the default retail unit price when the source data
    /// does not already provide a default entry.
    /// </summary>
    private const string DefaultUnitName = "each";

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
                        RetailPrice = new(price.RetailPrice)
                        {
                            UnitPrices = BuildRetailUnitPrices(price.RetailUnitPrices, price.RetailPrice)
                        },
                        WarrantyPrice = new(price.WarrantyPrice)
                    });
                }
            }
        }

        if (options.PartLookupPriceResolver is not null)
        {
            var resolvedResult = await options.PartLookupPriceResolver(new(new(cosmosPartCatalog?.DistributorPurchasePrice, prices, source), language, services));
            return resolvedResult;
        }

        return (null, []);
    }

    /// <summary>
    /// Maps the per-unit retail prices from the catalog into DTOs and guarantees a single default entry.
    /// When the source contains no default: if an "each" unit already exists it is promoted to the default;
    /// otherwise a default entry named "each" is appended using the region's retail price. Unit-name
    /// uniqueness and the single-default rule are enforced when the source is assigned to
    /// <see cref="ShiftSoftware.ADP.Models.Part.RegionPriceModel.RetailUnitPrices"/>.
    /// </summary>
    private static List<PartUnitPriceDTO> BuildRetailUnitPrices(IEnumerable<PartUnitPriceModel> source, decimal? retailPrice)
    {
        var unitPrices = new List<PartUnitPriceDTO>();

        if (source is not null)
        {
            foreach (var unit in source)
            {
                unitPrices.Add(new PartUnitPriceDTO
                {
                    UnitName = unit.UnitName,
                    Price = unit.Price,
                    IsDefault = unit.IsDefault,
                });
            }
        }

        // Ensure a default exists.
        if (!unitPrices.Any(x => x.IsDefault))
        {
            // If an "each" unit already exists, promote it to the default instead of adding a duplicate;
            // otherwise fall back to the region retail price under a new "each" unit.
            var existingEach = unitPrices.FirstOrDefault(x => string.Equals(x.UnitName, DefaultUnitName, StringComparison.OrdinalIgnoreCase));
            if (existingEach is not null)
                existingEach.IsDefault = true;
            else
                unitPrices.Add(new PartUnitPriceDTO
                {
                    UnitName = DefaultUnitName,
                    Price = retailPrice,
                    IsDefault = true,
                });
        }

        return unitPrices;
    }
}
