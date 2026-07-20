using Microsoft.Azure.Cosmos;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Customer;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Constants;
using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

/// <summary>
/// Customer-360 lookups over the golden-customer projections the identity-resolution engine
/// maintains in Cosmos. Deliberately introduces NO extra document family: phone lookup is an
/// INDEXED query over the existing golden docs (<c>PhoneNumbers</c> is an array; the containers'
/// default indexing policy range-indexes array elements, so <c>ARRAY_CONTAINS</c> is an index
/// seek, not a scan — measured ~3 RU against a ~1M-doc corpus), and everything else is
/// point-reads. Golden docs live at the undefined-CompanyID partition level (an identity may
/// span dealers), so the phone query is cross-partition by construction.
/// </summary>
public class GoldenCustomerLookupService
{
    private readonly LookUpCosmosClient client;
    private readonly LookupOptions options;

    public GoldenCustomerLookupService(LookUpCosmosClient client, LookupOptions options)
    {
        this.client = client;
        this.options = options;
    }

    private Container Customers => client.GetContainer(
        NoSQLConstants.Databases.Customers,
        NoSQLConstants.Containers.Customers_Customers);

    /// <summary>
    /// Canonical stored digit form of a caller-supplied phone. Tenants differ (one strips its
    /// country code at ingest, another stores full E.164 digits) — configure
    /// <see cref="LookupOptions.GoldenCustomerPhoneNormalizer"/> to match the tenant's ingest
    /// rule; the default keeps digits only and strips a leading international "00".
    /// </summary>
    private string NormalizePhone(string phone)
    {
        if (options?.GoldenCustomerPhoneNormalizer is { } normalizer)
            return normalizer(phone) ?? string.Empty;
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.StartsWith("00") ? digits.Substring(2) : digits;
    }

    /// <summary>
    /// All golden customers carrying <paramref name="phone"/>, each with its linked vehicles.
    /// Several matches are legitimate (shared numbers); <paramref name="maxMatches"/> bounds the
    /// response against hot placeholder numbers (a dealer front-desk number can be attached to
    /// many identities).
    /// </summary>
    public async Task<List<GoldenCustomerLookupDTO>> LookupByPhoneAsync(string phone, int maxMatches = 10)
    {
        var p = NormalizePhone(phone);
        if (p.Length < 5)
            return new List<GoldenCustomerLookupDTO>();

        var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.ItemType = @itemType AND ARRAY_CONTAINS(c.PhoneNumbers, @phone)")
            .WithParameter("@itemType", (string)ModelTypes.GoldenCustomer)
            .WithParameter("@phone", p);

        var matches = new List<GoldenCustomerModel>();
        var iterator = Customers.GetItemQueryIterator<GoldenCustomerModel>(query,
            requestOptions: new QueryRequestOptions { MaxItemCount = maxMatches });
        while (iterator.HasMoreResults && matches.Count < maxMatches)
        {
            var page = await iterator.ReadNextAsync();
            matches.AddRange(page.Take(maxMatches - matches.Count));
        }

        var results = new List<GoldenCustomerLookupDTO>(matches.Count);
        foreach (var customer in matches)
            results.Add(new GoldenCustomerLookupDTO
            {
                Customer = customer,
                Vehicles = await GetVehicleLinksAsync(customer.GoldenCustomerID),
            });
        return results;
    }

    /// <summary>One golden customer by its stable id (point-read), with its linked vehicles; null when absent.</summary>
    public async Task<GoldenCustomerLookupDTO> LookupByGoldenCustomerIDAsync(string goldenCustomerID)
    {
        if (string.IsNullOrWhiteSpace(goldenCustomerID))
            return null;
        try
        {
            var pk = new PartitionKeyBuilder().AddNoneType().Add(goldenCustomerID).Add((string)ModelTypes.GoldenCustomer).Build();
            var response = await Customers.ReadItemAsync<GoldenCustomerModel>(goldenCustomerID, pk);
            return new GoldenCustomerLookupDTO
            {
                Customer = response.Resource,
                Vehicles = await GetVehicleLinksAsync(goldenCustomerID),
            };
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// One VIN's golden-ownership timeline (owners on sale-grade evidence with effective periods,
    /// service-contacts with observation windows); null when the VIN has no ownership doc. The
    /// current owner is the <c>owner</c> link whose <c>EffectiveTo</c> is null.
    /// </summary>
    public async Task<VehicleGoldenOwnershipModel> GetVehicleOwnershipAsync(string vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
            return null;
        vin = vin.Trim().ToUpperInvariant();
        try
        {
            var container = client.GetContainer(
                NoSQLConstants.Databases.CompanyData,
                NoSQLConstants.Containers.Vehicles);
            var pk = new PartitionKeyBuilder().Add(vin).Add((string)ModelTypes.VehicleGoldenOwnership).AddNoneType().Build();
            var response = await container.ReadItemAsync<VehicleGoldenOwnershipModel>(vin, pk);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<List<GoldenVehicleLinkModel>> GetVehicleLinksAsync(string goldenCustomerID)
    {
        try
        {
            var pk = new PartitionKeyBuilder().AddNoneType().Add(goldenCustomerID).Add((string)ModelTypes.GoldenCustomerVehicleLinks).Build();
            var response = await Customers.ReadItemAsync<GoldenCustomerVehicleLinksModel>(goldenCustomerID, pk);
            return response.Resource?.Links?.ToList() ?? new List<GoldenVehicleLinkModel>();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<GoldenVehicleLinkModel>();
        }
    }
}
