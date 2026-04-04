using ShiftSoftware.ADP.Lookup.Services.Aggregate;
    using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services
{
    /// <summary>
    /// The main storage abstraction for vehicle lookup data.
    /// Provides methods to retrieve aggregated vehicle data, broker stock, colors, vehicle models, service items, and customers.
    /// Implemented by CosmosDB and DuckDB storage services.
    /// </summary>
    public interface IVehicleLoockupStorageService
    {
        /// <summary>Retrieves a broker by account number and company ID.</summary>
        Task<BrokerModel> GetBrokerAsync(string accountNumber, long? companyID);
        /// <summary>Retrieves a broker by its ID.</summary>
        Task<BrokerModel> GetBrokerAsync(long id);
        /// <summary>Retrieves the full aggregated company data for a single VIN.</summary>
        Task<CompanyDataAggregateModel> GetAggregatedCompanyData(string vin);
        /// <summary>Retrieves aggregated company data for multiple VINs, filtered by item types.</summary>
        Task<IEnumerable<CompanyDataAggregateModel>> GetAggregatedCompanyData(IEnumerable<string> vins, IEnumerable<string> itemTypes);
        /// <summary>Retrieves aggregated company data for multiple VINs in bulk lookup mode.</summary>
        Task<IEnumerable<CompanyDataAggregateModel>> GetAggregatedCompanyDataForBulkLookupAsync(IEnumerable<string> vins);
        /// <summary>Retrieves the exterior color by color code and brand.</summary>
        Task<ColorModel> GetExteriorColorsAsync(string colorCode, long? brand);
        /// <summary>Retrieves the vehicle model by variant code and brand.</summary>
        Task<VehicleModelModel> GetVehicleModelsAsync(string variant, long? brand);
        /// <summary>Retrieves the interior color by trim code and brand.</summary>
        Task<ColorModel> GetInteriorColorsAsync(string trimCode, long? brand);
        /// <summary>Retrieves all service item definitions, optionally from cache.</summary>
        Task<IEnumerable<ServiceItemModel>> GetServiceItemsAsync(bool useCache = true);
        /// <summary>Retrieves all vehicle model definitions.</summary>
        Task<IEnumerable<VehicleModelModel>> GetAllVehicleModelsAsync();
        /// <summary>Retrieves vehicle models matching a Katashiki.</summary>
        Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByKatashikiAsync(string katashiki);
        /// <summary>Retrieves vehicle models matching a variant code.</summary>
        Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVariantAsync(string variant);
        /// <summary>Retrieves vehicle models matching a VIN (by extracted variant).</summary>
        Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVinAsync(string vin);
        /// <summary>Retrieves broker stock records for a VIN under a specific brand.</summary>
        Task<IEnumerable<TBP_StockModel>> GetBrokerStockAsync(long? brandId, string vin);
        /// <summary>Retrieves a customer record by customer ID and company ID.</summary>
        Task<CustomerModel> GetCustomerAsync(string customerID, long? companyID);
    }
}
