using ShiftSoftware.ADP.Lookup.Services.Aggregate;
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
        Task<CompanyDataAggregateCosmosModel> GetAggregatedCompanyData(string vin);
        Task<CompanyDataAggregateCosmosModel> GetAggregatedCompanyData(IEnumerable<string> vins, IEnumerable<string> itemTypes);
        //Task<IEnumerable<StockPartModel>> GetStockItemsAsync(IEnumerable<string> partNumbers);
        Task<ColorModel> GetExteriorColorsAsync(string colorCode, Brands? brand);
        Task<VehicleModelModel> GetVehicleModelsAsync(string variant, Brands? brand);
        Task<ColorModel> GetInteriorColorsAsync(string trimCode, Brands? brand);
        Task SaveChangesAsync();
        void UpdateVSDataModel(VehicleBase item, VehicleModelModel model);
        void UpdateVSDataColor(VehicleBase item, ColorModel color);
        void UpdateVSDataTrim(VehicleBase item, ColorModel trim);
        Task<IEnumerable<ServiceItemModel>> GetServiceItemsAsync();
        Task<IEnumerable<VehicleModelModel>> GetAllVehicleModelsAsync();
        Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByKatashikiAsync(string katashiki);
        Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVariantAsync(string variant);
        Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVinAsync(string vin);
    }
}