using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class DuckDBVehicleLoockupStorageService (DuckDB.NET.Data.DuckDBConnection connection)
    : IVehicleLoockupStorageService
{
    public async Task<CompanyDataAggregateModel> GetAggregatedCompanyData(string vin)
    {
        var aggregate = new CompanyDataAggregateModel
        {
            VIN = vin,
        };

        var sql = @$"
            SELECT 
                id, VIN, VariantCode, InvoiceDate, InvoiceTotal, ModelCode, Katashiki,
                SaleType, AccountNumber, CustomerAccountNumber, CustomerID,
                ExteriorColorCode, InteriorColorCode, InvoiceNumber, PostDate, Location,
                CompanyHashID, BranchHashID, RegionHashID, BrandHashID,
                ItemStatus, InvoiceStatus, ExtendedPrice, SoldQuantity,
                CompanyIntegrationID, BranchIntegrationID, SourceFile
            FROM VehicleEntry
            WHERE VIN = '{vin}'
        ";

        using var cmd = connection.CreateCommand();

        cmd.CommandText = sql;

        var models = new List<VehicleEntryModel>();

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var model = new VehicleEntryModel
            {
                id = reader.GetString(0),
                VIN = reader.GetString(1),
                VariantCode = reader.IsDBNull(2) ? null : reader.GetString(2),
                InvoiceDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                InvoiceTotal = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                ModelCode = reader.IsDBNull(5) ? null : reader.GetString(5),
                Katashiki = reader.IsDBNull(6) ? null : reader.GetString(6),
                SaleType = reader.IsDBNull(7) ? null : reader.GetString(7),
                AccountNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                CustomerAccountNumber = reader.IsDBNull(9) ? null : reader.GetString(9),
                CustomerID = reader.IsDBNull(10) ? null : reader.GetString(10),
                ExteriorColorCode = reader.IsDBNull(11) ? null : reader.GetString(11),
                InteriorColorCode = reader.IsDBNull(12) ? null : reader.GetString(12),
                InvoiceNumber = reader.IsDBNull(13) ? null : reader.GetString(13),
                PostDate = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
                Location = reader.IsDBNull(15) ? null : reader.GetString(15),
                CompanyHashID = reader.IsDBNull(16) ? null : reader.GetString(16),
                BranchHashID = reader.IsDBNull(17) ? null : reader.GetString(17),
                RegionHashID = reader.IsDBNull(18) ? null : reader.GetString(18),
                BrandHashID = reader.IsDBNull(19) ? null : reader.GetString(19),
                ItemStatus = reader.IsDBNull(20) ? null : reader.GetString(20),
                InvoiceStatus = reader.IsDBNull(21) ? null : reader.GetString(21),
                ExtendedPrice = reader.IsDBNull(22) ? null : reader.GetDecimal(22),
                SoldQuantity = reader.IsDBNull(23) ? null : reader.GetDecimal(23),
                CompanyIntegrationID = reader.IsDBNull(24) ? null : reader.GetString(24),
                BranchIntegrationID = reader.IsDBNull(25) ? null : reader.GetString(25)
            };

            if (model.CompanyHashID is not null)
                model.CompanyID = ShiftEntityHashIdService.Decode(model.CompanyHashID, new ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter());

            if (model.BranchHashID is not null)
                model.BranchID = ShiftEntityHashIdService.Decode(model.BranchHashID, new ShiftSoftware.ShiftEntity.Model.HashIds.CompanyBranchHashIdConverter());

            if (model.RegionHashID is not null)
                model.RegionID = ShiftEntityHashIdService.Decode(model.RegionHashID, new ShiftSoftware.ShiftEntity.Model.HashIds.RegionHashIdConverter());

            if (model.BrandHashID is not null)
                model.BrandID = ShiftEntityHashIdService.Decode(model.BrandHashID, new ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter());

            aggregate.VehicleEntries.Add(model);
        }

        return aggregate;
    }

    public async Task<IEnumerable<CompanyDataAggregateModel>> GetAggregatedCompanyData(IEnumerable<string> vins, IEnumerable<string> itemTypes)
    {
        return [];
    }

    public async Task<IEnumerable<VehicleModelModel>> GetAllVehicleModelsAsync()
    {
        return [];
    }

    public async Task<BrokerModel> GetBrokerAsync(string accountNumber, long? companyID)
    {
        return new();
    }

    public async Task<BrokerModel> GetBrokerAsync(long id)
    {
        return new();
    }

    public async Task<IEnumerable<TBP_StockModel>> GetBrokerStockAsync(long? brandId, string vin)
    {
        return [];
    }

    public async Task<ColorModel> GetExteriorColorsAsync(string colorCode, long? brand)
    {
        return new();
    }

    public async Task<ColorModel> GetInteriorColorsAsync(string trimCode, long? brand)
    {
        return new();
    }

    public async Task<IEnumerable<ServiceItemModel>> GetServiceItemsAsync()
    {
        return [];
    }

    public async Task<VehicleModelModel> GetVehicleModelsAsync(string variant, long? brand)
    {
        return new();
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByKatashikiAsync(string katashiki)
    {
        return [];
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVariantAsync(string variant)
    {
        return [];
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVinAsync(string vin)
    {
        return [];
    }

    public async Task<CustomerModel> GetCustomerAsync(string customerID, long? companyID)
    {
        return null;
    }
}