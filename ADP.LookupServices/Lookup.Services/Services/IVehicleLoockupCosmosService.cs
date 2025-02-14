using System.Collections.Generic;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Models.DealerData.CosmosModels;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.FranchiseData.CosmosModels;
using ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;
using ShiftSoftware.ADP.Models.Stock.CosmosModels;

namespace ShiftSoftware.ADP.Lookup.Services.Services
{
    public interface IVehicleLoockupCosmosService
    {
        Task<BrokerCosmosModel> GetBrokerAsync(string accountNumber, string dealerId);
        Task<BrokerCosmosModel> GetBrokerAsync(long id);
        Task<DealerDataAggregateCosmosModel> GetAggregatedDealerData(string vin);
        Task<DealerDataAggregateCosmosModel> GetAggregatedDealerData(IEnumerable<string> vins, IEnumerable<string> itemTypes);
        Task<IEnumerable<TIQStockCosmosModel>> GetStockItemsAsync(IEnumerable<string> partNumbers);
        Task<VTColorCosmosModel> GetVTColorAsync(string colorCode, Franchises? brand);
        Task<VTModelRecordsCosmosModel> GetVTModelAsync(string variant, Franchises? brand);
        Task<VTTrimCosmosModel> GetVTTrimAsync(string trimCode, Franchises? brand);
        Task SaveChangesAsync();
        void UpdateVSDataColor(VSDataCosmosModel item, VTColorCosmosModel color);
        void UpdateVSDataTrim(VSDataCosmosModel item, VTTrimCosmosModel trim);
        Task<IEnumerable<ToyotaLoyaltyProgramRedeemableItemCosmosModel>> GetRedeemableItemsAsync(Franchises brand);
        void UpdateVSDataModel(VSDataCosmosModel item, VTModelRecordsCosmosModel model);
        Task<IEnumerable<VTModelRecordsCosmosModel>> GetAllVTModelsAsync();
        Task<IEnumerable<VTModelRecordsCosmosModel>> GetVTModelsByKatashikiAsync(string katashiki);
        Task<IEnumerable<VTModelRecordsCosmosModel>> GetVTModelsByVariantAsync(string variant);
        Task<IEnumerable<VTModelRecordsCosmosModel>> GetVTModelsByVinAsync(string vin);
    }
}