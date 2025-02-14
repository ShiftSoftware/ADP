using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Models.DTOs.TBP;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class TBPCosmosService
{
    private readonly CosmosClient client;

    public TBPCosmosService(CosmosClient client)
    {
        this.client = client;
    }

    public async Task<IEnumerable<BrokerVehicleStockDTO>> GetBrokerStockAsync(params long[] brokerIds)
    {
        return await GetBrokerStockAsync(brokerIds?.AsEnumerable());
    }

    public async Task<IEnumerable<BrokerVehicleStockDTO>> GetBrokerStockAsync(IEnumerable<long> brokerIds)
    {
        if (brokerIds?.Any() != true)
            return Array.Empty<BrokerVehicleStockDTO>();

        var container = client.GetContainer("TBP", "BrokerStock");
        var query = new QueryDefinition("SELECT c.Info FROM c WHERE ARRAY_CONTAINS(@brokerIds, c.BrokerId) AND c.ItemType = 'BrokerStock' AND c.Quantity > 0")
            .WithParameter("@brokerIds", brokerIds);

        var iterator = container.GetItemQueryIterator<dynamic>(query);
        var results = new List<BrokerVehicleStockDTO>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response.Select(x => ((JObject)x.Info).ToObject<BrokerVehicleStockDTO>()));
        }

        return results;
    }

    public async Task<BrokerVehicleStockDTO> GetBrokerStockAsync(long brokerId, string vin)
    {
        var container = client.GetContainer("TBP", "BrokerStock");

        try
        {
            var pb = new PartitionKeyBuilder();
            pb.Add(brokerId).Add(vin).Add("BrokerStock");
            var item = await container.ReadItemAsync<BrokerVehicleStockDTO>(brokerId.ToString(), pb.Build());

            if ((int)item.StatusCode < 200 || (int)item.StatusCode > 299)
                return null;

            return item.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
