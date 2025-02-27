using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class IdentityCosmosService : IIdentityCosmosService
{
    private readonly CosmosClient client;

    public IdentityCosmosService(CosmosClient client)
    {
        this.client = client;
    }

    public async Task<IEnumerable<CompanyModel>> GetCompaniesAsync()
    {
        var container = client.GetContainer("Identity", "Companies");

        var queryable = container.GetItemLinqQueryable<CompanyModel>(true);

        var iterator = queryable.ToFeedIterator();
        var items = new List<CompanyModel>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            items.AddRange(response);
        }

        return items;
    }

    public async Task<IEnumerable<CompanyBranchModel>> GetCompanyBranchesAsync()
    {
        var container = client.GetContainer("Identity", "CompanyBranches");

        var queryable = container.GetItemLinqQueryable<CompanyBranchModel>(true)
            .Where(x => x.ItemType == CompanyBranchContainerItemTypes.Branch);

        var iterator = queryable.ToFeedIterator();
        var items = new List<CompanyBranchModel>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            items.AddRange(response);
        }

        return items;
    }
}