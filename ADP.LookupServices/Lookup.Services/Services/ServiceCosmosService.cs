using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using ShiftSoftware.ADP.Models.Service.CosmosModels;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class ServiceCosmosService
{
    private readonly CosmosClient client;

    public ServiceCosmosService(CosmosClient client)
    {
        this.client = client;
    }

    internal async Task<IEnumerable<FlatRateCosmosModel>> GetFlatRatesAsycn(string vds, string? wmi)
    {
        var container = client.GetContainer("Services", "FlatRate");
        var query = container.GetItemLinqQueryable<FlatRateCosmosModel>(true)
            .Where(x => x.VDS == vds.ToUpper());

        if(!string.IsNullOrWhiteSpace(wmi))
            query = query.Where(x => x.WMI == (wmi == null ? null : wmi.ToUpper()) || x.WMI == null || x.WMI == "");

        var iterator = query.ToFeedIterator();

        List<FlatRateCosmosModel> items = new();

        while (iterator.HasMoreResults)
            items.AddRange(await iterator.ReadNextAsync());

        return items;
    }

    internal async Task<FlatRateCosmosModel?> GetFlatRateAsync(string laborCode, string vds, string? wmi)
    {
        var container = client.GetContainer("Services", "FlatRate");

        try
        {
            var pb = new PartitionKeyBuilder();
            pb.Add(vds.ToUpper()).Add(wmi?.ToUpper());

            var response = await container.ReadItemAsync<FlatRateCosmosModel?>(laborCode, pb.Build());

            if ((int?)response?.StatusCode >= 200 && (int?)response?.StatusCode < 300)
                return response.Resource;
            else
                return null;
        }
        catch (System.Exception)
        {
            return null;
        }
    }
}
