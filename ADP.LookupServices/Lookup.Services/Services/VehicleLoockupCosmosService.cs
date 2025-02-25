using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json.Linq;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Aggregate;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.LookupCosmosModels;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services
{
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

            var container = client.GetContainer("DealerData", "DealerData");
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

            var container = client.GetContainer("DealerData", "DealerData");

            var query = new QueryDefinition($"SELECT * FROM c WHERE ARRAY_CONTAINS(@vins, c.VIN) AND ARRAY_CONTAINS(@itemTypes, c.ItemType)")
                .WithParameter("@vins", vins)
                .WithParameter("@itemTypes", itemTypes);

            var iterator = container.GetItemQueryIterator<dynamic>(query);

            var items = new List<dynamic>();

            while (iterator.HasMoreResults)
                items.AddRange(await iterator.ReadNextAsync());

            return items;
        }

        public async Task<DealerDataAggregateCosmosModel> GetAggregatedDealerData(string vin)
        {
            var items = await GetLookupItems(vin);

            return ConvertDynamicListItemsToDealerData(items);
        }

        public async Task<DealerDataAggregateCosmosModel> GetAggregatedDealerData(IEnumerable<string> vins, IEnumerable<string> itemTypes)
        {
            var items = await GetLookupItems(vins, itemTypes);

            return ConvertDynamicListItemsToDealerData(items);
        }

        internal static DealerDataAggregateCosmosModel ConvertDynamicListItemsToDealerData(List<dynamic> items)
        {
            var dealerData = new DealerDataAggregateCosmosModel();

            dealerData.VehicleEntries = items.Where(x => x.ItemType.ToString() == ModelTypes.VehicleEntry)
                .Select(x => ((JObject)x).ToObject<VehicleEntryModel>()).ToList();

            dealerData.InitialOfficialVINs = items.Where(x => x.ItemType.ToString() == ModelTypes.InitialOfficialVIN)
                .Select(x => ((JObject)x).ToObject<InitialOfficialVINModel>()).ToList();

            dealerData.Invoices = items.Where(x => x.ItemType.ToString() == ModelTypes.Invoice)
                .Select(x => ((JObject)x).ToObject<InvoiceModel>()).ToList();

            dealerData.LaborLines = items.Where(x => x.ItemType.ToString() == ModelTypes.InvoiceLaborLine)
                .Select(x => ((JObject)x).ToObject<InvoiceLaborLineModel>()).ToList();

            dealerData.PartLines = items.Where(x => x.ItemType.ToString() == ModelTypes.InvoicePartLine)
                .Select(x => ((JObject)x).ToObject<InvoicePartLineModel>()).ToList();

            dealerData.SSCAffectedVINs = items.Where(x => x.ItemType.ToString() == ModelTypes.SSCAffectedVIN)
                .Select(x => ((JObject)x).ToObject<SSCAffectedVINModel>()).ToList();

            dealerData.WarrantyClaims = items.Where(x => x.ItemType.ToString() == ModelTypes.WarrantyClaim)
                .Select(x => ((JObject)x).ToObject<WarrantyClaimModel>())
                .Where(x => !(x?.IsDeleted ?? false)).ToList();

            dealerData.BrokerInitialVehicles = items.Where(x => x.ItemType.ToString() == ModelTypes.BrokerInitialVehicle)
                .Select(x => ((JObject)x).ToObject<BrokerInitialVehicleModel>())
                .Where(x => !(x?.Deleted ?? false)).ToList();

            dealerData.BrokerInvoices = items.Where(x => x.ItemType.ToString() == ModelTypes.BrokerInvoice)
                .Select(x => ((JObject)x).ToObject<BrokerInvoiceModel>())
                .Where(x => !(x?.IsDeleted ?? false)).ToList();

            dealerData.PaidServiceInvoices = items.Where(x => x.ItemType.ToString() == ModelTypes.PaidServiceInvoice)
                .Select(x => ((JObject)x).ToObject<PaidServiceInvoiceModel>())
                .Where(x => !(x?.IsDeleted ?? false)).ToList();

            dealerData.ServiceItemClaimLines = items.Where(x => x.ItemType.ToString() == ModelTypes.ServiceItemClaimLine)
                .Select(x => ((JObject)x).ToObject<ServiceItemClaimLineModel>()).ToList();

            dealerData.ServiceItemExcludedVINs = items.Where(x => x.ItemType.ToString() == ModelTypes.FreeServiceItemExcludedVIN)
                .Select(x => ((JObject)x).ToObject<FreeServiceItemExcludedVINModel>()).ToList();

            dealerData.FreeServiceItemDateShifts = items.Where(x => x.ItemType.ToString() == ModelTypes.FreeServiceItemDateShift)
                .Select(x => ((JObject)x).ToObject<FreeServiceItemDateShiftModel>()).ToList();

            dealerData.PaintThicknessInspections = items.Where(x => x.ItemType.ToString() == ModelTypes.PaintThicknessInspection)
                .Select(x => ((JObject)x).ToObject<PaintThicknessInspectionModel>()).FirstOrDefault();

            dealerData.WarrantyDateShifts = items.Where(x => x.ItemType.ToString() == ModelTypes.WarrantyDateShift)
                .Select(x => ((JObject)x).ToObject<WarrantyDateShiftCosmosModel>()).ToList();

            dealerData.Accessories = items.Where(x => x.ItemType.ToString() == ModelTypes.VehicleAccessory)
                .Select(x => ((JObject)x).ToObject<VehicleAccessoryModel>()).ToList();

            return dealerData;
        }

        public async Task<VehicleModelModel> GetVTModelAsync(string variant, Brands? brand)
        {
            var container = client.GetContainer("DealerData", "VTModels");

            var query = new QueryDefinition("SELECT top 1 * FROM c WHERE c.Variant_Code = @variant AND c.Brand = @brand");
            query.WithParameter("@variant", variant);
            query.WithParameter("@brand", brand);

            var iterator = container.GetItemQueryIterator<VehicleModelModel>(query);
            var items = new List<VehicleModelModel>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                items.AddRange(response);
            }

            return items.FirstOrDefault();
        }

        public async Task<ExteriorColorModel> GetVTColorAsync(string colorCode, Brands? brand)
        {
            var container = client.GetContainer("DealerData", "VTColors");

            var query = new QueryDefinition("SELECT top 1 * FROM c WHERE c.Color_Code = @colorCode AND c.Brand = @brand");
            query.WithParameter("@colorCode", colorCode);
            query.WithParameter("@brand", brand);

            var iterator = container.GetItemQueryIterator<ExteriorColorModel>(query);
            var items = new List<ExteriorColorModel>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                items.AddRange(response);
            }

            return items.FirstOrDefault();
        }

        public async Task<InteriorColorModel> GetVTTrimAsync(string trimCode, Brands? brand)
        {
            var container = client.GetContainer("DealerData", "VTTrims");

            var query = new QueryDefinition("SELECT top 1 * FROM c WHERE c.Trim_Code = @trimCode AND c.Brand = @brand");
            query.WithParameter("@trimCode", trimCode);
            query.WithParameter("@brand", brand);

            var iterator = container.GetItemQueryIterator<InteriorColorModel>(query);
            var items = new List<InteriorColorModel>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                items.AddRange(response);
            }

            return items.FirstOrDefault();
        }

        public async Task<IEnumerable<StockPartModel>> GetStockItemsAsync(IEnumerable<string> partNumbers)
        {
            if (partNumbers is null || !partNumbers.Any())
                return null;

            var container = client.GetContainer("DealerData", "Stock");

            var queryable = container.GetItemLinqQueryable<StockPartModel>(true)
                .Where(x => partNumbers.Contains(x.PartNumber));

            var iterator = queryable.ToFeedIterator();
            var items = new List<StockPartModel>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                items.AddRange(response);
            }

            return items;
        }

        public async Task<BrokerModel> GetBrokerAsync(string accountNumber, string companyIntegrationID)
        {
            var container = client.GetContainer("DealerData", "Broker");

            var queryable = container.GetItemLinqQueryable<BrokerModel>(true)
                .Where(x => !x.IsDeleted && x.AccountNumbers.Contains(accountNumber) && x.CompanyIntegrationID == companyIntegrationID);

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
            var container = client.GetContainer("DealerData", "Broker");

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

        public void UpdateVSDataColor(VehicleEntryModel item, ExteriorColorModel color)
        {
            var contaner = client.GetContainer("DealerData", "DealerData");

            var pb = new PartitionKeyBuilder();
            pb.Add(item.VIN).Add("VS");

            tasks.Add(
                contaner.PatchItemAsync<ExteriorColorModel>(item.id, pb.Build(),
                    new List<PatchOperation>
                    {
                    PatchOperation.Set("/VTColor", color)
                    })
            );
        }

        public void UpdateVSDataTrim(VehicleEntryModel item, InteriorColorModel trim)
        {
            var contaner = client.GetContainer("DealerData", "DealerData");

            var pb = new PartitionKeyBuilder();
            pb.Add(item.VIN).Add("VS");

            tasks.Add(
                contaner.PatchItemAsync<InteriorColorModel>(item.id, pb.Build(),
                    new List<PatchOperation>
                    {
                        PatchOperation.Set("/VTTrim", trim)
                    })
            );
        }

        public void UpdateVSDataModel(VehicleEntryModel item, VehicleModelModel model)
        {
            var contaner = client.GetContainer("DealerData", "DealerData");

            var pb = new PartitionKeyBuilder();
            pb.Add(item.VIN).Add("VS");

            tasks.Add(
                contaner.PatchItemAsync<VehicleModelModel>(item.id, pb.Build(),
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

            var container = client.GetContainer("DealerData", "ServiceItems");

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

        public async Task<IEnumerable<VehicleModelModel>> GetAllVTModelsAsync()
        {
            var container = client.GetContainer("DealerData", "VTModels");

            var queryable = container.GetItemLinqQueryable<VehicleModelModel>(true);

            var iterator = queryable.ToFeedIterator();

            var items = new List<VehicleModelModel>();

            while (iterator.HasMoreResults)
                items.AddRange(await iterator.ReadNextAsync());

            return items;
        }

        public async Task<IEnumerable<VehicleModelModel>> GetVTModelsByKatashikiAsync(string katashiki)
        {
            var container = client.GetContainer("DealerData", "VTModels");

            var queryable = container.GetItemLinqQueryable<VehicleModelModel>(true)
                .Where(x => x.Katashiki == katashiki);

            var iterator = queryable.ToFeedIterator();

            var items = new List<VehicleModelModel>();

            while (iterator.HasMoreResults)
                items.AddRange(await iterator.ReadNextAsync());

            return items;
        }

        public async Task<IEnumerable<VehicleModelModel>> GetVTModelsByVariantAsync(string variant)
        {
            var container = client.GetContainer("DealerData", "VTModels");

            var queryable = container.GetItemLinqQueryable<VehicleModelModel>(true)
                .Where(x => x.VariantCode == variant);

            var iterator = queryable.ToFeedIterator();

            var items = new List<VehicleModelModel>();

            while (iterator.HasMoreResults)
                items.AddRange(await iterator.ReadNextAsync());

            return items;
        }

        public async Task<IEnumerable<VehicleModelModel>> GetVTModelsByVinAsync(string vin)
        {
            var dealerDataContainer = client.GetContainer("DealerData", "DealerData");

            var vsQuery = dealerDataContainer.GetItemLinqQueryable<VehicleEntryModel>(true)
                .Where(x => x.ItemType == new VehicleEntryModel().ItemType)
                .Where(x => x.VIN == vin);

            var vsIterator = vsQuery.ToFeedIterator();
            var vs = new VehicleEntryModel();

            if (vsIterator.HasMoreResults)
                vs = (await vsIterator.ReadNextAsync()).FirstOrDefault();

            if (vs.VehicleModel is not null)
                return new List<VehicleModelModel> { vs.VehicleModel };

            return await GetVTModelsByVariantAsync(vs.VariantCode);
        }
    }
}