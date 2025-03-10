using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json.Linq;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class VehicleLoockupCosmosService : IVehicleLoockupCosmosService
{
    private readonly CosmosClient client;
    private readonly List<Task> tasks = new List<Task>();

    public VehicleLoockupCosmosService(CosmosClient client)
    {
        this.client = client;
    }

    private async Task<List<dynamic>> GetLookupItems(string vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
            return new();

        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.Vehicles
        );

        var query = new QueryDefinition("SELECT * FROM c WHERE c.VIN = @vin");
        query.WithParameter("@vin", vin?.Trim().ToUpper());

        var iterator = container.GetItemQueryIterator<dynamic>(query);

        var items = new List<dynamic>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            items.AddRange(response);
        }

        return items;
    }

    private async Task<List<dynamic>> GetLookupItems(IEnumerable<string> vins, IEnumerable<string> itemTypes)
    {
        if (vins == null || !vins.Any())
            return new List<dynamic>();

        if (vins.Count() > 100)
            throw new Exception("Can't lookup more than 100 VINs at a time.");

        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.Vehicles
        );

        var query = new QueryDefinition($"SELECT * FROM c WHERE ARRAY_CONTAINS(@vins, c.VIN) AND ARRAY_CONTAINS(@itemTypes, c.ItemType)")
            .WithParameter("@vins", vins)
            .WithParameter("@itemTypes", itemTypes);

        var iterator = container.GetItemQueryIterator<dynamic>(query);

        var items = new List<dynamic>();

        while (iterator.HasMoreResults)
            items.AddRange(await iterator.ReadNextAsync());

        return items;
    }

    public async Task<CompanyDataAggregateCosmosModel> GetAggregatedCompanyData(string vin)
    {
        var items = await GetLookupItems(vin);

        return ConvertDynamicListItemsToCompanyData(items);
    }

    public async Task<CompanyDataAggregateCosmosModel> GetAggregatedCompanyData(IEnumerable<string> vins, IEnumerable<string> itemTypes)
    {
        var items = await GetLookupItems(vins, itemTypes);

        return ConvertDynamicListItemsToCompanyData(items);
    }

    internal static CompanyDataAggregateCosmosModel ConvertDynamicListItemsToCompanyData(List<dynamic> items)
    {
        var companyData = new CompanyDataAggregateCosmosModel();

        companyData.VehicleEntries = items.Where(x => x.ItemType.ToString() == ModelTypes.VehicleEntry)
            .Select(x => ((JObject)x).ToObject<VehicleEntryModel>()).ToList();

        companyData.InitialOfficialVINs = items.Where(x => x.ItemType.ToString() == ModelTypes.InitialOfficialVIN)
            .Select(x => ((JObject)x).ToObject<InitialOfficialVINModel>()).ToList();

        companyData.Invoices = items.Where(x => x.ItemType.ToString() == ModelTypes.Invoice)
            .Select(x => ((JObject)x).ToObject<InvoiceModel>()).ToList();

        companyData.LaborLines = items.Where(x => x.ItemType.ToString() == ModelTypes.InvoiceLaborLine)
            .Select(x => ((JObject)x).ToObject<InvoiceLaborLineModel>()).ToList();

        companyData.PartLines = items.Where(x => x.ItemType.ToString() == ModelTypes.InvoicePartLine)
            .Select(x => ((JObject)x).ToObject<InvoicePartLineModel>()).ToList();

        companyData.SSCAffectedVINs = items.Where(x => x.ItemType.ToString() == ModelTypes.SSCAffectedVIN)
            .Select(x => ((JObject)x).ToObject<SSCAffectedVINModel>()).ToList();

        companyData.WarrantyClaims = items.Where(x => x.ItemType.ToString() == ModelTypes.WarrantyClaim)
            .Select(x => ((JObject)x).ToObject<WarrantyClaimModel>())
            .Where(x => !(x?.IsDeleted ?? false)).ToList();

        companyData.BrokerInitialVehicles = items.Where(x => x.ItemType.ToString() == ModelTypes.BrokerInitialVehicle)
            .Select(x => ((JObject)x).ToObject<BrokerInitialVehicleModel>())
            .Where(x => !(x?.Deleted ?? false)).ToList();

        companyData.BrokerInvoices = items.Where(x => x.ItemType.ToString() == ModelTypes.BrokerInvoice)
            .Select(x => ((JObject)x).ToObject<BrokerInvoiceModel>())
            .Where(x => !(x?.IsDeleted ?? false)).ToList();

        companyData.PaidServiceInvoices = items.Where(x => x.ItemType.ToString() == ModelTypes.PaidServiceInvoice)
            .Select(x => ((JObject)x).ToObject<PaidServiceInvoiceModel>())
            .Where(x => !(x?.IsDeleted ?? false)).ToList();

        companyData.ServiceItemClaimLines = items.Where(x => x.ItemType.ToString() == ModelTypes.ServiceItemClaimLine)
            .Select(x => ((JObject)x).ToObject<ServiceItemClaimLineModel>()).ToList();

        companyData.FreeServiceItemExcludedVINs = items.Where(x => x.ItemType.ToString() == ModelTypes.FreeServiceItemExcludedVIN)
            .Select(x => ((JObject)x).ToObject<FreeServiceItemExcludedVINModel>()).ToList();

        companyData.FreeServiceItemDateShifts = items.Where(x => x.ItemType.ToString() == ModelTypes.FreeServiceItemDateShift)
            .Select(x => ((JObject)x).ToObject<FreeServiceItemDateShiftModel>()).ToList();

        companyData.PaintThicknessInspections = items.Where(x => x.ItemType.ToString() == ModelTypes.PaintThicknessInspection)
            .Select(x => ((JObject)x).ToObject<PaintThicknessInspectionModel>()).FirstOrDefault();

        companyData.WarrantyDateShifts = items.Where(x => x.ItemType.ToString() == ModelTypes.WarrantyDateShift)
            .Select(x => ((JObject)x).ToObject<WarrantyDateShiftModel>()).ToList();

        companyData.Accessories = items.Where(x => x.ItemType.ToString() == ModelTypes.VehicleAccessory)
            .Select(x => ((JObject)x).ToObject<VehicleAccessoryModel>()).ToList();

        return companyData;
    }

    public async Task<VehicleModelModel> GetVehicleModelsAsync(string variant, Brands? brand)
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.VehicleModels
        );

        var pb = new PartitionKeyBuilder();
        pb.Add(variant).Add((int)brand);
        var response = await container.ReadItemAsync<VehicleModelModel>(variant, pb.Build());

        if(response.StatusCode == System.Net.HttpStatusCode.OK)
            return response.Resource;

        return null;
    }

    public async Task<ColorModel> GetExteriorColorsAsync(string colorCode, Brands? brand)
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.ExteriorColors
        );

        var pb = new PartitionKeyBuilder();
        pb.Add(colorCode).Add((int)brand);
        var response = await container.ReadItemAsync<ColorModel>(colorCode, pb.Build());

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
            return response.Resource;

        return null;
    }

    public async Task<ColorModel> GetInteriorColorsAsync(string trimCode, Brands? brand)
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.InteriorColors
        );

        var pb = new PartitionKeyBuilder();
        pb.Add(trimCode).Add((int)brand);
        var response = await container.ReadItemAsync<ColorModel>(trimCode, pb.Build());

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
            return response.Resource;

        return null;
    }

    public async Task<BrokerModel> GetBrokerAsync(string accountNumber, string companyID)
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.Brokers
        );

        var queryable = container.GetItemLinqQueryable<BrokerModel>(true)
            .Where(x => !x.IsDeleted && x.AccountNumbers.Contains(accountNumber) && x.id == companyID);

        var iterator = queryable.ToFeedIterator();
        var items = new List<BrokerModel>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            items.AddRange(response);
        }

        return items.FirstOrDefault();
    }

    public async Task<BrokerModel> GetBrokerAsync(long id)
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.Brokers
        );

        var queryable = container.GetItemLinqQueryable<BrokerModel>(true)
            .Where(x => !x.IsDeleted && x.id == id.ToString());

        var iterator = queryable.ToFeedIterator();
        var items = new List<BrokerModel>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            items.AddRange(response);
        }

        return items.FirstOrDefault();
    }

    public void UpdateVSDataColor(VehicleEntryModel item, ColorModel color)
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.Vehicles
        );

        var pb = new PartitionKeyBuilder();
        pb.Add(item.VIN).Add("VS");

        tasks.Add(
            container.PatchItemAsync<ColorModel>(item.id, pb.Build(),
                new List<PatchOperation>
                {
                PatchOperation.Set("/VTColor", color)
                })
        );
    }

    public void UpdateVSDataTrim(VehicleEntryModel item, ColorModel trim)
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.Vehicles
        );

        var pb = new PartitionKeyBuilder();
        pb.Add(item.VIN).Add("VS");

        tasks.Add(
            container.PatchItemAsync<ColorModel>(item.id, pb.Build(),
                new List<PatchOperation>
                {
                    PatchOperation.Set("/VTTrim", trim)
                })
        );
    }

    public void UpdateVSDataModel(VehicleEntryModel item, VehicleModelModel model)
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.Vehicles
        );

        var pb = new PartitionKeyBuilder();
        pb.Add(item.VIN).Add("VS");

        tasks.Add(
            container.PatchItemAsync<VehicleModelModel>(item.id, pb.Build(),
                new List<PatchOperation>
                {
                PatchOperation.Set("/VTModel", model)
                })
        );
    }

    public async Task SaveChangesAsync()
    {
        await Task.WhenAll(tasks);
    }

    public async Task<IEnumerable<ServiceItemModel>> GetServiceItemsAsync(Brands brand)
    {
        //if (invoiceDate is null)
        //    return null;


        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.ServiceItems
        );

        var queryable = container.GetItemLinqQueryable<ServiceItemModel>(true)
            //.Where(x => !x.Deleted.HasValue || x.Deleted == false)
            //.Where(x => invoiceDate >= x.PublishDate && invoiceDate <= x.ExpireDate)
            //.Where(x => x.ModelCosts.Count() == 0
            //    || x.ModelCosts.Any(a =>
            //    (katashiki.StartsWith(a.Katashiki) && a.Katashiki.Trim() != string.Empty)
            //    || (variant.StartsWith(a.Variant) && a.Variant.Trim() != string.Empty)))
            //.Where(x => !x.ArchivedVersion.HasValue || x.ArchivedVersion == false || x.ArchivedVersion == null)
            .Where(x => x.Brands.Any(a => a == brand));

        var iterator = queryable.ToFeedIterator();
        var items = new List<ServiceItemModel>();

        while (iterator.HasMoreResults)
            items.AddRange(await iterator.ReadNextAsync());

        return items;
    }

    public async Task<IEnumerable<VehicleModelModel>> GetAllVehicleModelsAsync()
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.VehicleModels
        );

        var queryable = container.GetItemLinqQueryable<VehicleModelModel>(true);

        var iterator = queryable.ToFeedIterator();

        var items = new List<VehicleModelModel>();

        while (iterator.HasMoreResults)
            items.AddRange(await iterator.ReadNextAsync());

        return items;
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByKatashikiAsync(string katashiki)
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.VehicleModels
        );

        var queryable = container.GetItemLinqQueryable<VehicleModelModel>(true)
            .Where(x => x.Katashiki == katashiki);

        var iterator = queryable.ToFeedIterator();

        var items = new List<VehicleModelModel>();

        while (iterator.HasMoreResults)
            items.AddRange(await iterator.ReadNextAsync());

        return items;
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVariantAsync(string variant)
    {
        var container = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.VehicleModels
        );

        var queryable = container.GetItemLinqQueryable<VehicleModelModel>(true)
            .Where(x => x.VariantCode == variant);

        var iterator = queryable.ToFeedIterator();

        var items = new List<VehicleModelModel>();

        while (iterator.HasMoreResults)
            items.AddRange(await iterator.ReadNextAsync());

        return items;
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVinAsync(string vin)
    {
        var companyDataContainer = client.GetContainer(
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Databases.CompanyData,
            ShiftSoftware.ADP.Models.Constants.NoSQLConstants.Containers.Vehicles
        );

        var vsQuery = companyDataContainer.GetItemLinqQueryable<VehicleEntryModel>(true)
            .Where(x => x.ItemType == new VehicleEntryModel().ItemType)
            .Where(x => x.VIN == vin);

        var vsIterator = vsQuery.ToFeedIterator();
        var vs = new VehicleEntryModel();

        if (vsIterator.HasMoreResults)
            vs = (await vsIterator.ReadNextAsync()).FirstOrDefault();

        if (vs.VehicleModel is not null)
            return new List<VehicleModelModel> { vs.VehicleModel };

        return await GetVehicleModelsByVariantAsync(vs.VariantCode);
    }
}