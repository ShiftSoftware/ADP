using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services
{
    public interface IVehicleLoockupCosmosService
    {
        Task<BrokerModel> GetBrokerAsync(string accountNumber, long? companyID);
        Task<BrokerModel> GetBrokerAsync(long id);
        Task<CompanyDataAggregateCosmosModel> GetAggregatedCompanyData(string vin);
        Task<IEnumerable<CompanyDataAggregateCosmosModel>> GetAggregatedCompanyData(IEnumerable<string> vins, IEnumerable<string> itemTypes);
        //Task<IEnumerable<StockPartModel>> GetStockItemsAsync(IEnumerable<string> partNumbers);
        Task<ColorModel> GetExteriorColorsAsync(string colorCode, long? brand);
        Task<VehicleModelModel> GetVehicleModelsAsync(string variant, long? brand);
        Task<ColorModel> GetInteriorColorsAsync(string trimCode, long? brand);
        Task<IEnumerable<ServiceItemModel>> GetServiceItemsAsync();
        Task<IEnumerable<VehicleModelModel>> GetAllVehicleModelsAsync();
        Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByKatashikiAsync(string katashiki);
        Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVariantAsync(string variant);
        Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVinAsync(string vin);
        Task<IEnumerable<TBP_StockModel>> GetBrokerStockAsync(long? brandId, string vin);
    }
}