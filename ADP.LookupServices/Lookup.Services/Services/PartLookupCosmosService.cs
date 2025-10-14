using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Models.Part;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class PartLookupCosmosService
{
    private readonly CosmosClient client;

    public PartLookupCosmosService(CosmosClient client)
    {
        this.client = client;
    }

    private async Task<List<dynamic>> GetLookupItemsAsync(string partNumber)
    {
        if (string.IsNullOrWhiteSpace(partNumber))
            return new();

        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.Parts
        );

        var query = new QueryDefinition("SELECT * FROM c WHERE c.PartNumber = @partNumber");
        query.WithParameter("@partNumber", partNumber.ToUpper());

        var iterator = container.GetItemQueryIterator<dynamic>(query);

        var items = new List<dynamic>();

        while (iterator.HasMoreResults)
            items.AddRange(await iterator.ReadNextAsync());

        return items;
    }

    public async Task<PartAggregateCosmosModel> GetAggregatePartAsync(string partNumber)
    {
        var items = await GetLookupItemsAsync(partNumber);

        if (!items.Any())
            return null;

        var result = new PartAggregateCosmosModel
        {
            StockParts = items.Where(x => x.ItemType.ToString() == new StockPartModel().ItemType)
                .Select(x => ((JObject)x).ToObject<StockPartModel>()).ToList(),
            CatalogParts = items.Where(x => x.ItemType.ToString() == new CatalogPartModel().ItemType)
                .Select(x => ((JObject)x).ToObject<CatalogPartModel>()).ToList(),
            CompanyDeadStockParts = items.Where(x => x.ItemType.ToString() == new CompanyDeadStockPartModel().ItemType)
                .Select(x => ((JObject)x).ToObject<CompanyDeadStockPartModel>()).ToList(),
        };

        return result;
    }
}