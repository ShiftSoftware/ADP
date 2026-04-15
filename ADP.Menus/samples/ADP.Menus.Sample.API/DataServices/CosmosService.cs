using ShiftSoftware.ADP.Menus.Data.DataServices;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Constants;
using ShiftSoftware.ADP.Models.Part;

namespace ShiftSoftware.ADP.Menus.Sample.API.DataServices;

public class CosmosService : IMenuPartPriceService
{
    private readonly CosmosClient client;

    public CosmosService(CosmosClient client)
    {
        this.client = client;
    }

    public async ValueTask<MenuPartPrice?> StockByPartNumberAsync(string partNumber)
    {
        var container = client.GetContainer(
            NoSQLConstants.Databases.CompanyData,
            NoSQLConstants.Containers.Parts
        );

        var trimmed = partNumber.Trim().ToUpper();
        var stripped = trimmed.Replace("-", "");

        var result = await QuerySingleAsync(container, stripped);
        if (result is null && stripped != trimmed)
            result = await QuerySingleAsync(container, trimmed);

        if (result is null)
            return null;

        result.PartNumber = trimmed;
        return Map(result);
    }

    public async ValueTask<IEnumerable<MenuPartPrice>> StockByPartNumbersAsync(IEnumerable<string> partNumbers)
    {
        var container = client.GetContainer(
            NoSQLConstants.Databases.CompanyData,
            NoSQLConstants.Containers.Parts
        );

        // Preserve the trimmed original text per stripped key so results can be echoed back with it.
        var strippedToOriginal = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pn in partNumbers)
        {
            if (string.IsNullOrWhiteSpace(pn)) continue;
            var trimmed = pn.Trim().ToUpper();
            var stripped = trimmed.Replace("-", "");
            if (!strippedToOriginal.ContainsKey(stripped))
                strippedToOriginal[stripped] = trimmed;
        }

        if (strippedToOriginal.Count == 0)
            return [];

        var strippedKeys = strippedToOriginal.Keys.ToList();
        var firstPass = await QueryManyAsync(container, strippedKeys);

        var results = new List<MenuPartPrice>();
        var foundStripped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in firstPass)
        {
            if (string.IsNullOrWhiteSpace(item.PartNumber)) continue;
            if (strippedToOriginal.TryGetValue(item.PartNumber, out var original))
            {
                foundStripped.Add(item.PartNumber);
                item.PartNumber = original;
                results.Add(Map(item));
            }
        }

        var missingOriginals = strippedToOriginal
            .Where(kv => kv.Key != kv.Value && !foundStripped.Contains(kv.Key))
            .Select(kv => kv.Value)
            .ToList();

        if (missingOriginals.Count > 0)
        {
            var secondPass = await QueryManyAsync(container, missingOriginals);
            foreach (var item in secondPass)
            {
                if (string.IsNullOrWhiteSpace(item.PartNumber)) continue;
                // item.PartNumber already matches the original trimmed text used in the query.
                results.Add(Map(item));
            }
        }

        return results;
    }

    private static MenuPartPrice Map(CatalogPartModel source)
    {
        var countryPrices = new List<MenuPartCountryPrice>();
        if (source.CountryData is not null)
        {
            foreach (var country in source.CountryData)
            {
                countryPrices.Add(new MenuPartCountryPrice
                {
                    CountryID = country.CountryID,
                    Price = country.RegionPrices?.FirstOrDefault()?.RetailPrice,
                });
            }
        }

        return new MenuPartPrice
        {
            PartNumber = source.PartNumber ?? string.Empty,
            CountryPrices = countryPrices,
        };
    }

    private static async Task<CatalogPartModel?> QuerySingleAsync(Container container, string partNumber)
    {
        var query = container.GetItemLinqQueryable<CatalogPartModel>(true)
            .Where(x => x.ItemType == ModelTypes.CatalogPart)
            .Where(x => x.PartNumber == partNumber);

        var iterator = query.ToFeedIterator();
        return (await iterator.ReadNextAsync()).FirstOrDefault();
    }

    private static async Task<List<CatalogPartModel>> QueryManyAsync(Container container, List<string> partNumbers)
    {
        var query = container.GetItemLinqQueryable<CatalogPartModel>(true)
            .Where(x => x.ItemType == ModelTypes.CatalogPart)
            .Where(x => partNumbers.Contains(x.PartNumber));

        var iterator = query.ToFeedIterator();
        var result = new List<CatalogPartModel>();
        while (iterator.HasMoreResults)
            result.AddRange(await iterator.ReadNextAsync());
        return result;
    }
}
