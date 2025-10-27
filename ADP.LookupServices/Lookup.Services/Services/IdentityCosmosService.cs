using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using ShiftSoftware.ShiftEntity.Model.Replication;
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
        var container = client.GetContainer(
            IdentityDatabaseAndContainerNames.DatabaseName,
            IdentityDatabaseAndContainerNames.CompanyContainerName
        );

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
        var container = client.GetContainer(
            IdentityDatabaseAndContainerNames.DatabaseName,
            IdentityDatabaseAndContainerNames.CompanyBranchContainerName
        );

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


    private Dictionary<long?, RegionModel> regions = new();
    private Dictionary<long?, CountryModel> countries = new();
    private Dictionary<long?, CompanyModel> companies = new();
    private Dictionary<long?, CompanyBranchModel> companiesBranch = new();

    public async Task<RegionModel> GetRegionAsync(long? id)
    {
        // Get region form catch if exists
        if (regions.TryGetValue(id, out var region))
            return region;

        var container = client.GetContainer(
            IdentityDatabaseAndContainerNames.DatabaseName,
            IdentityDatabaseAndContainerNames.CountryContainerName
        );

        var query = container.GetItemLinqQueryable<RegionModel>(true)
            .Where(x => x.RegionID == id && x.ItemType == CountryContainerItemTypes.Region);

        var iterator = query.ToFeedIterator();

        // Store the region into catch
        var result = (await iterator.ReadNextAsync()).FirstOrDefault();
        regions[id] = result;

        return result;
    }

    public async Task<CountryModel> GetCountryAsync(long? id)
    {
        // Get the country from catch if exists
        if (countries.TryGetValue(id, out var country))
            return country;

        var container = client.GetContainer(
            IdentityDatabaseAndContainerNames.DatabaseName,
            IdentityDatabaseAndContainerNames.CountryContainerName
        );

        var query = container.GetItemLinqQueryable<CountryModel>(true)
            .Where(x => x.CountryID == id && x.ItemType == CountryContainerItemTypes.Country);

        var iterator = query.ToFeedIterator();

        // Store the country into catch
        var result = (await iterator.ReadNextAsync()).FirstOrDefault();
        countries[id] = result;

        return result;
    }

    public async Task<CompanyModel> GetCompanyAsync(long? id)
    {
        if (id is null)
            return null;

        // Get the branch from the catch if exists
        if (companies.TryGetValue(id, out var company))
            return company;

        var container = client.GetContainer(
            IdentityDatabaseAndContainerNames.DatabaseName,
            IdentityDatabaseAndContainerNames.CompanyContainerName
        );

        var response = await container.ReadItemAsync<CompanyModel>(id!.ToString(), new PartitionKey(id.ToString()));

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            return null;

        var result = response.Resource;
        companies[id] = result;

        return result;
    }

    public async Task<CompanyBranchModel> GetCompanyBranchAsync(long? id)
    {
        if (id is null)
            return null;

        // Get the branch from the catch if exists
        if (companiesBranch.TryGetValue(id, out var branch))
            return branch;

        var container = client.GetContainer(
            IdentityDatabaseAndContainerNames.DatabaseName,
            IdentityDatabaseAndContainerNames.CompanyBranchContainerName
        );

        var pb = new PartitionKeyBuilder().Add(id.ToString()).Add(CompanyBranchContainerItemTypes.Branch);
        var response = await container.ReadItemAsync<CompanyBranchModel>(id!.ToString(), pb.Build());

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            return null;

        var result = response.Resource;
        companiesBranch[id] = result;

        return result;
    }
}