using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json.Linq;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Part;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    public async Task InsertManufacturerPartLookupAsync(ManufacturerPartLookupModel model)
    {
        var manufacturerLookupcontainer = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.Logs,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.ManufacturerPartLookupLogs
        );

        await manufacturerLookupcontainer.CreateItemAsync(model, new PartitionKey(model.PartNumber));

        try
        {
            if(model.LogId is not null)
            {
                // Update the log to store the manufacturer lookup
                var partLookupLogContainer = client.GetContainer(
                    ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.Logs,
                    ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.PartLookupLogs
                );

                await partLookupLogContainer.PatchItemAsync<PartLookupLogCosmosModel>(model.LogId, new PartitionKey(model.PartNumber), new[]
                {
                    PatchOperation.Set($"/{nameof(PartLookupLogCosmosModel.ManufacturerLookup)}", model),
                });
            }
        }
        catch (System.Exception)
        {
        }
    }

    public async Task UpdateManufacturerPartLookupStatusAsync(string id, string partNumber, ManufacturerPartLookupStatus botStatus, IEnumerable<KeyValuePair<string, string>>? lookupResult = null)
    {
        var manufacturerLookupcontainer = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.Logs,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.ManufacturerPartLookupLogs
        );

        // Patch bot status and lookup result
        await manufacturerLookupcontainer.PatchItemAsync<ManufacturerPartLookupModel>(id, new PartitionKey(partNumber), new[]
        {
            PatchOperation.Set($"/{nameof(ManufacturerPartLookupModel.Status)}", botStatus),
            PatchOperation.Set($"/{nameof(ManufacturerPartLookupModel.ManufacturerResult)}", lookupResult),
        });

        try
        {
            // Get the ManufacturerPartLookupModel
            var manufacturerPartLookupResponse = await manufacturerLookupcontainer.ReadItemAsync<ManufacturerPartLookupModel>(id, new PartitionKey(partNumber));

            if (manufacturerPartLookupResponse?.Resource?.LogId is not null)
            {
                // Update the log to store the updated manufacturer lookup
                var partLookupLogContainer = client.GetContainer(
                    ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.Logs,
                    ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.PartLookupLogs
                );

                await partLookupLogContainer.PatchItemAsync<PartLookupLogCosmosModel>(manufacturerPartLookupResponse.Resource.LogId, new PartitionKey(partNumber), new[]
                {
                    PatchOperation.Set($"/{nameof(PartLookupLogCosmosModel.ManufacturerLookup)}", manufacturerPartLookupResponse.Resource),
                });
            }
        }
        catch (System.Exception)
        {

        }
    }

    public async Task<IEnumerable<ManufacturerPartLookupModel>> GetManufacturerPartLookupsByStatusAsync(ManufacturerPartLookupStatus botStatus)
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.Logs,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.ManufacturerPartLookupLogs
        );

        var query = container.GetItemLinqQueryable<ManufacturerPartLookupModel>(true)
            .Where(x=> x.Status == botStatus)
            .OrderBy(x => x.CreateDate);

        var items = new List<ManufacturerPartLookupModel>();

        var iterator = query.ToFeedIterator();
        while (iterator.HasMoreResults)
            items.AddRange(await iterator.ReadNextAsync());

        return items;
    }

    public async Task<ManufacturerPartLookupModel> GetManufacturerPartLookupByStatusAsync(ManufacturerPartLookupStatus botStatus)
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.Logs,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.ManufacturerPartLookupLogs
        );

        var query = container.GetItemLinqQueryable<ManufacturerPartLookupModel>(true)
            .Where(x => x.Status == botStatus)
            .OrderBy(x => x.CreateDate)
            .Take(1);

        var iterator = query.ToFeedIterator();
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }

        return null;
    }

    public async Task<ManufacturerPartLookupModel> GetManufacturerPartLookupAsync(string id, string partNumber)
    {
        try
        {
            var container = client.GetContainer(
                ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.Logs,
                ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.ManufacturerPartLookupLogs
            );
            var response = await container.ReadItemAsync<ManufacturerPartLookupModel>(id, new PartitionKey(partNumber));
            return response.Resource;
        }
        catch (System.Exception)
        {
            return null;
        }
    }
}