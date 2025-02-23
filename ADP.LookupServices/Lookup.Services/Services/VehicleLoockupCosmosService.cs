using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json.Linq;
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

            dealerData.VSData = items.Where(x => x.ItemType.ToString().ToLower() == "VS".ToLower())
                .Select(x => ((JObject)x).ToObject<VSDataCosmosModel>()).ToList();

            dealerData.TIQOfficialVIN = items.Where(x => x.ItemType.ToString().ToLower() == "TIQOfficialVIN".ToLower())
                .Select(x => ((JObject)x).ToObject<InitialOfficialVINModel>()).ToList();

            dealerData.CPU = items.Where(x => x.ItemType.ToString().ToLower() == "CPU".ToLower())
                .Select(x => ((JObject)x).ToObject<InvoiceModel>()).ToList();

            dealerData.SOLabor = items.Where(x => x.ItemType.ToString().ToLower() == "Labor".ToLower())
                .Select(x => ((JObject)x).ToObject<InvoiceLaborLineModel>()).ToList();

            dealerData.SOPart = items.Where(x => x.ItemType.ToString().ToLower() == "Part".ToLower())
                .Select(x => ((JObject)x).ToObject<InvoicePartLineModel>()).ToList();

            dealerData.TiqSSCAffectedVin = items.Where(x => x.ItemType.ToString().ToLower() == "SSCAffectedVin".ToLower())
                .Select(x => ((JObject)x).ToObject<SSCAffectedVINModel>()).ToList();

            dealerData.ToyotaWarrantyClaim = items.Where(x => x.ItemType.ToString().ToLower() == "ToyotaWarrantyClaim".ToLower())
                .Select(x => ((JObject)x).ToObject<WarrantyClaimModel>())
                .Where(x => !(x?.IsDeleted ?? false)).ToList();

            dealerData.BrokerInitialVehicle = items.Where(x => x.ItemType.ToString().ToLower() == "BrokerInitialVehicle".ToLower())
                .Select(x => ((JObject)x).ToObject<BrokerInitialVehicleModel>())
                .Where(x => !(x?.Deleted ?? false)).ToList();

            dealerData.BrokerInvoice = items.Where(x => x.ItemType.ToString().ToLower() == "BrokerInvoice".ToLower())
                .Select(x => ((JObject)x).ToObject<BrokerInvoiceModel>())
                .Where(x => !(x?.IsDeleted ?? false)).ToList();

            dealerData.TLPPackageInvoice = items.Where(x => x.ItemType.ToString().ToLower() == "TLPPackageInvoice".ToLower())
                .Select(x => ((JObject)x).ToObject<PaidServiceInvoiceModel>())
                .Where(x => !(x?.IsDeleted ?? false)).ToList();

            dealerData.ToyotaLoyaltyProgramTransactionLine = items.Where(x => x.ItemType.ToString().ToLower() == "ToyotaLoyaltyProgramTransactionLine".ToLower())
                .Select(x => ((JObject)x).ToObject<ToyotaLoyaltyProgramTransactionLineCosmosModel>()).ToList();

            dealerData.VehicleFreeServiceExcludedVIN = items.Where(x => x.ItemType.ToString().ToLower() == "VehicleFreeServiceExcludedVINs".ToLower())
                .Select(x => ((JObject)x).ToObject<VehicleFreeServiceExcludedVINsCosmosModel>()).ToList();

            dealerData.VehicleFreeServiceInvoiceDateShiftVIN = items.Where(x => x.ItemType.ToString().ToLower() == "VehicleFreeServiceInvoiceDateShiftVINs".ToLower())
                .Select(x => ((JObject)x).ToObject<VehicleFreeServiceInvoiceDateShiftVINsCosmosModel>()).ToList();

            dealerData.PaintThicknessVehicle = items.Where(x => x.ItemType.ToString().ToLower() == new PaintThicknessVehicleModel().ItemType.ToLower())
                .Select(x => ((JObject)x).ToObject<PaintThicknessVehicleModel>()).FirstOrDefault();

            dealerData.WarrantyShiftDate = items.Where(x => x.ItemType.ToString().ToLower() == new WarrantyDateShiftCosmosModel().ItemType.ToLower())
                .Select(x => ((JObject)x).ToObject<WarrantyDateShiftCosmosModel>()).ToList();

            dealerData.Accessories = items.Where(x => x.ItemType.ToString().ToLower() == new AccessoryModel().ItemType.ToLower())
                .Select(x => ((JObject)x).ToObject<AccessoryModel>()).ToList();

            return dealerData;
        }

        public async Task<VTModelRecordsCosmosModel> GetVTModelAsync(string variant, Brands? brand)
        {
            var container = client.GetContainer("DealerData", "VTModels");

            var query = new QueryDefinition("SELECT top 1 * FROM c WHERE c.Variant_Code = @variant AND c.Brand = @brand");
            query.WithParameter("@variant", variant);
            query.WithParameter("@brand", brand);

            var iterator = container.GetItemQueryIterator<VTModelRecordsCosmosModel>(query);
            var items = new List<VTModelRecordsCosmosModel>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                items.AddRange(response);
            }

            return items.FirstOrDefault();
        }

        public async Task<VTColorCosmosModel> GetVTColorAsync(string colorCode, Brands? brand)
        {
            var container = client.GetContainer("DealerData", "VTColors");

            var query = new QueryDefinition("SELECT top 1 * FROM c WHERE c.Color_Code = @colorCode AND c.Brand = @brand");
            query.WithParameter("@colorCode", colorCode);
            query.WithParameter("@brand", brand);

            var iterator = container.GetItemQueryIterator<VTColorCosmosModel>(query);
            var items = new List<VTColorCosmosModel>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                items.AddRange(response);
            }

            return items.FirstOrDefault();
        }

        public async Task<VTTrimCosmosModel> GetVTTrimAsync(string trimCode, Brands? brand)
        {
            var container = client.GetContainer("DealerData", "VTTrims");

            var query = new QueryDefinition("SELECT top 1 * FROM c WHERE c.Trim_Code = @trimCode AND c.Brand = @brand");
            query.WithParameter("@trimCode", trimCode);
            query.WithParameter("@brand", brand);

            var iterator = container.GetItemQueryIterator<VTTrimCosmosModel>(query);
            var items = new List<VTTrimCosmosModel>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                items.AddRange(response);
            }

            return items.FirstOrDefault();
        }

        public async Task<IEnumerable<PartStockModel>> GetStockItemsAsync(IEnumerable<string> partNumbers)
        {
            if (partNumbers is null || !partNumbers.Any())
                return null;

            var container = client.GetContainer("DealerData", "Stock");

            var queryable = container.GetItemLinqQueryable<PartStockModel>(true)
                .Where(x => partNumbers.Contains(x.PartNumber));

            var iterator = queryable.ToFeedIterator();
            var items = new List<PartStockModel>();

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

        public void UpdateVSDataColor(VSDataCosmosModel item, VTColorCosmosModel color)
        {
            var contaner = client.GetContainer("DealerData", "DealerData");

            var pb = new PartitionKeyBuilder();
            pb.Add(item.VIN).Add("VS");

            tasks.Add(
                contaner.PatchItemAsync<VTColorCosmosModel>(item.id, pb.Build(),
                    new List<PatchOperation>
                    {
                    PatchOperation.Set("/VTColor", color)
                    })
            );
        }

        public void UpdateVSDataTrim(VSDataCosmosModel item, VTTrimCosmosModel trim)
        {
            var contaner = client.GetContainer("DealerData", "DealerData");

            var pb = new PartitionKeyBuilder();
            pb.Add(item.VIN).Add("VS");

            tasks.Add(
                contaner.PatchItemAsync<VTTrimCosmosModel>(item.id, pb.Build(),
                    new List<PatchOperation>
                    {
                        PatchOperation.Set("/VTTrim", trim)
                    })
            );
        }

        public void UpdateVSDataModel(VSDataCosmosModel item, VTModelRecordsCosmosModel model)
        {
            var contaner = client.GetContainer("DealerData", "DealerData");

            var pb = new PartitionKeyBuilder();
            pb.Add(item.VIN).Add("VS");

            tasks.Add(
                contaner.PatchItemAsync<VTModelRecordsCosmosModel>(item.id, pb.Build(),
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

        public async Task<IEnumerable<ServiceItemModel>> GetRedeemableItemsAsync(Brands brand)
        {
            //if (invoiceDate is null)
            //    return null;

            var container = client.GetContainer("DealerData", "ToyotaLoyaltyProgramRedeemableItem");

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

        public async Task<IEnumerable<VTModelRecordsCosmosModel>> GetAllVTModelsAsync()
        {
            var container = client.GetContainer("DealerData", "VTModels");

            var queryable = container.GetItemLinqQueryable<VTModelRecordsCosmosModel>(true);

            var iterator = queryable.ToFeedIterator();

            var items = new List<VTModelRecordsCosmosModel>();

            while (iterator.HasMoreResults)
                items.AddRange(await iterator.ReadNextAsync());

            return items;
        }

        public async Task<IEnumerable<VTModelRecordsCosmosModel>> GetVTModelsByKatashikiAsync(string katashiki)
        {
            var container = client.GetContainer("DealerData", "VTModels");

            var queryable = container.GetItemLinqQueryable<VTModelRecordsCosmosModel>(true)
                .Where(x => x.Katashiki == katashiki);

            var iterator = queryable.ToFeedIterator();

            var items = new List<VTModelRecordsCosmosModel>();

            while (iterator.HasMoreResults)
                items.AddRange(await iterator.ReadNextAsync());

            return items;
        }

        public async Task<IEnumerable<VTModelRecordsCosmosModel>> GetVTModelsByVariantAsync(string variant)
        {
            var container = client.GetContainer("DealerData", "VTModels");

            var queryable = container.GetItemLinqQueryable<VTModelRecordsCosmosModel>(true)
                .Where(x => x.Variant_Code == variant);

            var iterator = queryable.ToFeedIterator();

            var items = new List<VTModelRecordsCosmosModel>();

            while (iterator.HasMoreResults)
                items.AddRange(await iterator.ReadNextAsync());

            return items;
        }

        public async Task<IEnumerable<VTModelRecordsCosmosModel>> GetVTModelsByVinAsync(string vin)
        {
            var dealerDataContainer = client.GetContainer("DealerData", "DealerData");

            var vsQuery = dealerDataContainer.GetItemLinqQueryable<VSDataCosmosModel>(true)
                .Where(x => x.ItemType == new VSDataCosmosModel().ItemType)
                .Where(x => x.VIN == vin);

            var vsIterator = vsQuery.ToFeedIterator();
            var vs = new VSDataCosmosModel();

            if (vsIterator.HasMoreResults)
                vs = (await vsIterator.ReadNextAsync()).FirstOrDefault();

            if (vs.VTModel is not null)
                return new List<VTModelRecordsCosmosModel> { vs.VTModel };

            return await GetVTModelsByVariantAsync(vs.VariantCode);
        }
    }
}