using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lookup.Services.Functions.Services;

public class IdentityCosmosService
{
    private readonly CosmosClient client;
    private Dictionary<string, RegionModel?> regions = new();
    private Dictionary<string, CountryModel?> countries = new();
    private Dictionary<string, CompanyModel?> companies = new();
    private Dictionary<string, CompanyBranchModel?> companiesBranch = new();

    public IdentityCosmosService(CosmosClient client)
    {
        this.client = client;
    }

    public async Task<RegionModel?> GetRegionAsync(string id)
    {
        // Get region form catch if exists
        var region = regions.GetValueOrDefault(id);
        if (region is not null)
            return region;

        var container = client.GetContainer("Identity", "Countries");
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id AND c.ItemType = 'Region'")
            .WithParameter("@id", id);

        var iterator = container.GetItemQueryIterator<RegionModel>(query);

        // Store the region into catch
        var result = (await iterator.ReadNextAsync()).FirstOrDefault();
        regions.TryAdd(id, result);

        return result;
    }

    public async Task<CountryModel?> GetCountryAsync(string id)
    {
        // Get the country from catch if exists
        var country = countries.GetValueOrDefault(id);
        if (country is not null)
            return country;

        var container = client.GetContainer("Identity", "Countries");
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id AND c.ItemType = 'Country'")
            .WithParameter("@id", id);

        var iterator = container.GetItemQueryIterator<CountryModel>(query);

        // Store the country into catch
        var result = (await iterator.ReadNextAsync()).FirstOrDefault();
        countries.TryAdd(id, result);

        return result;
    }

    public async Task<CompanyModel?> GetCompanyAsync(string id)
    {
        // Get the branch from the catch if exists
        var company = companies.GetValueOrDefault(id);
        if (company is not null)
            return company;

        var container = client.GetContainer("Identity", "Companies");
        var query = container.GetItemLinqQueryable<CompanyModel>(true)
            .Where(x => x.id == id);

        var iterator = query.ToFeedIterator();
        var result = (await iterator.ReadNextAsync()).FirstOrDefault();
        companies.TryAdd(id, result);

        return result;
    }

    public async Task<CompanyBranchModel?> GetCompanyBranchAsync(string id)
    {
        // Get the branch from the catch if exists
        var branch = companiesBranch.GetValueOrDefault(id);
        if (branch is not null)
            return branch;

        var container = client.GetContainer("Identity", "CompanyBranches");
        var query = container.GetItemLinqQueryable<CompanyBranchModel>(true)
            .Where(x => x.id == id && x.ItemType == CompanyBranchContainerItemTypes.Branch);

        var iterator = query.ToFeedIterator();
        var result = (await iterator.ReadNextAsync()).FirstOrDefault();
        companiesBranch.TryAdd(id, result);

        return result;
    }
}
