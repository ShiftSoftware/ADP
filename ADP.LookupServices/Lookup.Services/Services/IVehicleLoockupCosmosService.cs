using ShiftSoftware.ADP.Models.Aggregate;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Part;
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
        Task<IEnumerable<StockPartModel>> GetStockItemsAsync(IEnumerable<string> partNumbers);
        Task<ColorModel> GetVTColorAsync(string colorCode, Brands? brand);
        Task<VehicleModelModel> GetVTModelAsync(string variant, Brands? brand);
        Task<ColorModel> GetVTTrimAsync(string trimCode, Brands? brand);
        Task SaveChangesAsync();
        void UpdateVSDataColor(VehicleEntryModel item, ColorModel color);
        void UpdateVSDataTrim(VehicleEntryModel item, ColorModel trim);
        Task<IEnumerable<ServiceItemModel>> GetServiceItemsAsync(Brands brand);
        void UpdateVSDataModel(VehicleEntryModel item, VehicleModelModel model);
        Task<IEnumerable<VehicleModelModel>> GetAllVTModelsAsync();
        Task<IEnumerable<VehicleModelModel>> GetVTModelsByKatashikiAsync(string katashiki);
        Task<IEnumerable<VehicleModelModel>> GetVTModelsByVariantAsync(string variant);
        Task<IEnumerable<VehicleModelModel>> GetVTModelsByVinAsync(string vin);
    }
}