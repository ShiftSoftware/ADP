using ShiftSoftware.ADP.Models.Aggregate;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services
{
    public interface IVehicleLoockupCosmosService
    {
        Task<BrokerModel> GetBrokerAsync(string accountNumber, string companyIntegrationID);
        Task<BrokerModel> GetBrokerAsync(long id);
        Task<DealerDataAggregateCosmosModel> GetAggregatedDealerData(string vin);
        Task<DealerDataAggregateCosmosModel> GetAggregatedDealerData(IEnumerable<string> vins, IEnumerable<string> itemTypes);
        Task<IEnumerable<PartStockModel>> GetStockItemsAsync(IEnumerable<string> partNumbers);
        Task<VTColorCosmosModel> GetVTColorAsync(string colorCode, Brands? brand);
        Task<VTModelRecordsCosmosModel> GetVTModelAsync(string variant, Brands? brand);
        Task<VTTrimCosmosModel> GetVTTrimAsync(string trimCode, Brands? brand);
        Task SaveChangesAsync();
        void UpdateVSDataColor(VSDataCosmosModel item, VTColorCosmosModel color);
        void UpdateVSDataTrim(VSDataCosmosModel item, VTTrimCosmosModel trim);
        Task<IEnumerable<ServiceItemModel>> GetRedeemableItemsAsync(Brands brand);
        void UpdateVSDataModel(VSDataCosmosModel item, VTModelRecordsCosmosModel model);
        Task<IEnumerable<VTModelRecordsCosmosModel>> GetAllVTModelsAsync();
        Task<IEnumerable<VTModelRecordsCosmosModel>> GetVTModelsByKatashikiAsync(string katashiki);
        Task<IEnumerable<VTModelRecordsCosmosModel>> GetVTModelsByVariantAsync(string variant);
        Task<IEnumerable<VTModelRecordsCosmosModel>> GetVTModelsByVinAsync(string vin);
    }
}