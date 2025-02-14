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
    private Dictionary<KeyValuePair<string,string>, CompanyBranchModel?> companiesBranch = new();

    public IdentityCosmosService(CosmosClient client)
    {
        this.client = client;
    }

    public async Task<RegionModel?> GetRegionAsync(string regionIntegrationId)
    {
        // Get region form catch if exists
        var region = regions.GetValueOrDefault(regionIntegrationId);
        if (region is not null)
            return region;

        var container = client.GetContainer("Identity", "Regions");
        var query = new QueryDefinition("SELECT * FROM c WHERE c.IntegrationId = @regionIntegrationId")
            .WithParameter("@regionIntegrationId", regionIntegrationId);

        var iterator = container.GetItemQueryIterator<RegionModel>(query);


        // Store the region into catch
        var result = (await iterator.ReadNextAsync()).FirstOrDefault();
        regions.Add(regionIntegrationId, result);

        return result;
    }

    public async Task<RegionModel?> GetTCARegionAsync(string regionIntegrationId)
    {
        // Get region form catch if exists
        var region = regions.GetValueOrDefault(regionIntegrationId);
        if (region is not null)
            return region;

        var container = client.GetContainer("Identity", "Countries");
        var query = new QueryDefinition("SELECT * FROM c WHERE c.IntegrationId = @integrationId AND c.ItemType = 'Region'")
            .WithParameter("@integrationId", regionIntegrationId);

        var iterator = container.GetItemQueryIterator<RegionModel>(query);

        // Store the region into catch
        var result = (await iterator.ReadNextAsync()).FirstOrDefault();
        regions.Add(regionIntegrationId, result);

        return result;
    }

    public async Task<CountryModel?> GetTCACountryAsync(string countryIntegrationId)
    {
        // Get the country from catch if exists
        var country = countries.GetValueOrDefault(countryIntegrationId);
        if (country is not null) 
            return country;

        var container = client.GetContainer("Identity", "Countries");
        var query = new QueryDefinition("SELECT * FROM c WHERE c.IntegrationId = @integrationId AND c.ItemType = 'Country'")
            .WithParameter("@integrationId", countryIntegrationId);

        var iterator = container.GetItemQueryIterator<CountryModel>(query);

        // Store the country into catch
        var result = (await iterator.ReadNextAsync()).FirstOrDefault();
        countries.Add(countryIntegrationId, result);

        return result;
    }

    public async Task<CompanyModel?> GetCompanyAsync(string companyIntegrationId)
    {
        // Get the branch from the catch if exists
        var company = companies.GetValueOrDefault(companyIntegrationId);
        if(company is not null) 
            return company;

        var container = client.GetContainer("Identity", "Companies");
        var query = container.GetItemLinqQueryable<CompanyModel>(true)
            .Where(x => x.IntegrationId == companyIntegrationId);

        var iterator = query.ToFeedIterator<CompanyModel>();
        var result = (await iterator.ReadNextAsync()).FirstOrDefault();
        companies.Add(companyIntegrationId, result);

        return result;
    }

    public async Task<CompanyBranchModel?> GetCompanyBranchAsync(string companyIntegrationId, string branchIntegrationId)
    {
        // Get the branch from the catch if exists
        var branch = companiesBranch.GetValueOrDefault(new(companyIntegrationId, branchIntegrationId));
        if (branch is not null)
            return branch;

        var container = client.GetContainer("Identity", "CompanyBranches");
        var query = container.GetItemLinqQueryable<CompanyBranchModel>(true)
            .Where(x => x.IntegrationId == branchIntegrationId && x.Company.IntegrationId == companyIntegrationId);

        var iterator = query.ToFeedIterator<CompanyBranchModel>();
        var result = (await iterator.ReadNextAsync()).FirstOrDefault();
        companiesBranch.Add(new(companyIntegrationId, branchIntegrationId), result);

        return result;
    }
}
