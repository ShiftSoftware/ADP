using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class DuckDBVehicleLoockupStorageService(DuckDB.NET.Data.DuckDBConnection connection)
    : IVehicleLoockupStorageService
{
    #region Aggregated Company Data

    public async Task<CompanyDataAggregateModel> GetAggregatedCompanyData(string vin)
    {
        vin = vin?.Trim()?.ToUpper();

        var aggregate = new CompanyDataAggregateModel
        {
            VIN = vin,
        };

        aggregate.VehicleEntries = await GetVehicleEntriesAsync(vin);
        aggregate.InitialOfficialVINs = await GetInitialOfficialVINsAsync(vin);
        aggregate.SSCAffectedVINs = await GetSSCAffectedVINsAsync(vin);
        aggregate.WarrantyClaims = await GetWarrantyClaimsAsync(vin);
        aggregate.ItemClaims = await GetItemClaimsAsync(vin);
        aggregate.PaidServiceInvoices = await GetPaidServiceInvoicesAsync(vin);
        aggregate.ExtendedWarrantyEntries = await GetExtendedWarrantiesAsync(vin);
        aggregate.PaintThicknessInspections = await GetPaintThicknessInspectionsAsync(vin);
        aggregate.FreeServiceItemDateShifts = await GetFreeServiceItemDateShiftsAsync(vin);
        aggregate.FreeServiceItemExcludedVINs = await GetFreeServiceItemExcludedVINsAsync(vin);
        aggregate.WarrantyDateShifts = await GetWarrantyDateShiftsAsync(vin);

        return aggregate;
    }

    public async Task<IEnumerable<CompanyDataAggregateModel>> GetAggregatedCompanyData(IEnumerable<string> vins, IEnumerable<string> itemTypes)
    {
        if (vins == null || !vins.Any())
            return Enumerable.Empty<CompanyDataAggregateModel>();

        vins = vins.Select(x => x?.Trim()?.ToUpper());

        var results = new List<CompanyDataAggregateModel>();

        foreach (var vin in vins)
        {
            var aggregate = await GetAggregatedCompanyData(vin);
            results.Add(aggregate);
        }

        return results;
    }

    #endregion

    #region Vehicle Entry

    private async Task<List<VehicleEntryModel>> GetVehicleEntriesAsync(string vin)
    {
        var models = await ExecuteQueryAsync<VehicleEntryModel>(
            $"SELECT * FROM VehicleEntry WHERE VIN = '{EscapeSql(vin)}'");

        foreach (var model in models)
        {
            if (model.CompanyHashID is not null)
                model.CompanyID = ShiftEntityHashIdService.Decode(model.CompanyHashID, new ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter());

            if (model.BranchHashID is not null)
                model.BranchID = ShiftEntityHashIdService.Decode(model.BranchHashID, new ShiftSoftware.ShiftEntity.Model.HashIds.CompanyBranchHashIdConverter());

            if (model.RegionHashID is not null)
                model.RegionID = ShiftEntityHashIdService.Decode(model.RegionHashID, new ShiftSoftware.ShiftEntity.Model.HashIds.RegionHashIdConverter());

            if (model.BrandHashID is not null)
                model.BrandID = ShiftEntityHashIdService.Decode(model.BrandHashID, new ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter());
        }

        return models;
    }

    #endregion

    #region Initial Official VIN

    private async Task<List<InitialOfficialVINModel>> GetInitialOfficialVINsAsync(string vin)
    {
        return await ExecuteQueryAsync<InitialOfficialVINModel>(
            $"SELECT * FROM InitialOfficialVIN WHERE VIN = '{EscapeSql(vin)}'");
    }

    #endregion

    #region SSC Affected VIN

    private async Task<List<SSCAffectedVINModel>> GetSSCAffectedVINsAsync(string vin)
    {
        return await ExecuteQueryAsync<SSCAffectedVINModel>(
            $"SELECT * FROM SSCAffectedVIN WHERE VIN = '{EscapeSql(vin)}'");
    }

    #endregion

    #region Warranty Claim

    private async Task<List<WarrantyClaimModel>> GetWarrantyClaimsAsync(string vin)
    {
        return await ExecuteQueryAsync<WarrantyClaimModel>(
            $"SELECT * FROM WarrantyClaim WHERE VIN = '{EscapeSql(vin)}' AND IsDeleted = false");
    }

    #endregion

    #region Item Claim

    private async Task<List<ItemClaimModel>> GetItemClaimsAsync(string vin)
    {
        return await ExecuteQueryAsync<ItemClaimModel>(
            $"SELECT * FROM ItemClaim WHERE VIN = '{EscapeSql(vin)}' AND IsDeleted = false");
    }

    #endregion

    #region Paid Service Invoice

    private async Task<List<PaidServiceInvoiceModel>> GetPaidServiceInvoicesAsync(string vin)
    {
        return await ExecuteQueryAsync<PaidServiceInvoiceModel>(
            $"SELECT * FROM PaidServiceInvoice WHERE VIN = '{EscapeSql(vin)}' AND IsDeleted = false");
    }

    #endregion

    #region Extended Warranty

    private async Task<List<ExtendedWarrantyModel>> GetExtendedWarrantiesAsync(string vin)
    {
        return await ExecuteQueryAsync<ExtendedWarrantyModel>(
            $"SELECT * FROM ExtendedWarranty WHERE VIN = '{EscapeSql(vin)}' AND IsDeleted = false AND IsActive = true");
    }

    #endregion

    #region Paint Thickness Inspection

    private async Task<List<PaintThicknessInspectionModel>> GetPaintThicknessInspectionsAsync(string vin)
    {
        return await ExecuteQueryAsync<PaintThicknessInspectionModel>(
            $"SELECT * FROM PaintThicknessInspection WHERE VIN = '{EscapeSql(vin)}'");
    }

    #endregion

    #region Free Service Item Date Shift

    private async Task<List<FreeServiceItemDateShiftModel>> GetFreeServiceItemDateShiftsAsync(string vin)
    {
        return await ExecuteQueryAsync<FreeServiceItemDateShiftModel>(
            $"SELECT * FROM VehicleFreeServiceShiftDate WHERE VIN = '{EscapeSql(vin)}' AND IsDeleted = false");
    }

    #endregion

    #region Free Service Item Excluded VIN

    private async Task<List<FreeServiceItemExcludedVINModel>> GetFreeServiceItemExcludedVINsAsync(string vin)
    {
        return await ExecuteQueryAsync<FreeServiceItemExcludedVINModel>(
            $"SELECT * FROM VehicleFreeServiceItemExcludedVIN WHERE VIN = '{EscapeSql(vin)}' AND IsDeleted = false");
    }

    #endregion

    #region Warranty Date Shift

    private async Task<List<WarrantyDateShiftModel>> GetWarrantyDateShiftsAsync(string vin)
    {
        return await ExecuteQueryAsync<WarrantyDateShiftModel>(
            $"SELECT * FROM VehicleWarrantyShiftDate WHERE VIN = '{EscapeSql(vin)}' AND IsDeleted = false");
    }

    #endregion

    #region Service Items

    public async Task<IEnumerable<ServiceItemModel>> GetServiceItemsAsync()
    {
        return await ExecuteQueryAsync<ServiceItemModel>("SELECT * FROM ServiceItem");
    }

    #endregion

    #region Vehicle Models

    public async Task<VehicleModelModel> GetVehicleModelsAsync(string variant, long? brand)
    {
        if (string.IsNullOrWhiteSpace(variant))
            return null;

        var items = await ExecuteQueryAsync<VehicleModelModel>(
            $"SELECT * FROM VehicleModel WHERE VariantCode = '{EscapeSql(variant)}' AND BrandID = {brand}");

        return items.FirstOrDefault();
    }

    public async Task<IEnumerable<VehicleModelModel>> GetAllVehicleModelsAsync()
    {
        return await ExecuteQueryAsync<VehicleModelModel>("SELECT * FROM VehicleModel");
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByKatashikiAsync(string katashiki)
    {
        return await ExecuteQueryAsync<VehicleModelModel>(
            $"SELECT * FROM VehicleModel WHERE Katashiki = '{EscapeSql(katashiki)}'");
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVariantAsync(string variant)
    {
        return await ExecuteQueryAsync<VehicleModelModel>(
            $"SELECT * FROM VehicleModel WHERE VariantCode = '{EscapeSql(variant)}'");
    }

    public async Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVinAsync(string vin)
    {
        var entries = await GetVehicleEntriesAsync(vin);
        var entry = entries.FirstOrDefault();

        if (entry == null || string.IsNullOrWhiteSpace(entry.VariantCode))
            return Enumerable.Empty<VehicleModelModel>();

        return await GetVehicleModelsByVariantAsync(entry.VariantCode);
    }

    #endregion

    #region Colors

    public async Task<ColorModel> GetExteriorColorsAsync(string colorCode, long? brand)
    {
        if (string.IsNullOrWhiteSpace(colorCode))
            return null;

        var items = await ExecuteQueryAsync<ColorModel>(
            $"SELECT * FROM ExteriorColor WHERE Code = '{EscapeSql(colorCode)}' AND BrandID = {brand}");

        return items.FirstOrDefault();
    }

    public async Task<ColorModel> GetInteriorColorsAsync(string trimCode, long? brand)
    {
        if (string.IsNullOrWhiteSpace(trimCode))
            return null;

        var items = await ExecuteQueryAsync<ColorModel>(
            $"SELECT * FROM InteriorColor WHERE Code = '{EscapeSql(trimCode)}' AND BrandID = {brand}");

        return items.FirstOrDefault();
    }

    #endregion

    #region Broker

    public async Task<BrokerModel> GetBrokerAsync(string accountNumber, long? companyID)
    {
        var items = await ExecuteQueryAsync<BrokerModel>(
            $"SELECT * FROM Broker WHERE CompanyID = {companyID} AND IsDeleted = false");

        return items.FirstOrDefault(x => x.AccountNumbers != null && x.AccountNumbers.Contains(accountNumber));
    }

    public async Task<BrokerModel> GetBrokerAsync(long id)
    {
        var items = await ExecuteQueryAsync<BrokerModel>(
            $"SELECT * FROM Broker WHERE id = '{id}' AND IsDeleted = false");

        return items.FirstOrDefault();
    }

    #endregion

    #region Broker Stock

    public async Task<IEnumerable<TBP_StockModel>> GetBrokerStockAsync(long? brandId, string vin)
    {
        var sql = $"SELECT * FROM BrokerStock WHERE VIN = '{EscapeSql(vin)}'";

        if (brandId is not null)
            sql += $" AND BrandID = {brandId}";

        return await ExecuteQueryAsync<TBP_StockModel>(sql);
    }

    #endregion

    #region Customer

    public async Task<CustomerModel> GetCustomerAsync(string customerID, long? companyID)
    {
        if (string.IsNullOrWhiteSpace(customerID))
            return null;

        var items = await ExecuteQueryAsync<CustomerModel>(
            $"SELECT * FROM Customer WHERE CustomerID = '{EscapeSql(customerID)}' AND CompanyID = {companyID}");

        return items.FirstOrDefault();
    }

    #endregion

    #region Generic DuckDB Query Helper

    private async Task<List<T>> ExecuteQueryAsync<T>(string sql) where T : new()
    {
        var results = new List<T>();

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = await cmd.ExecuteReaderAsync();

            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
                columnMap[reader.GetName(i)] = i;

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToArray();

            while (await reader.ReadAsync())
            {
                var model = new T();

                foreach (var prop in properties)
                {
                    if (!columnMap.TryGetValue(prop.Name, out var ordinal))
                        continue;
                    if (reader.IsDBNull(ordinal))
                        continue;

                    try
                    {
                        var value = ReadPropertyValue(reader, ordinal, prop.PropertyType);
                        if (value != null)
                            prop.SetValue(model, value);
                    }
                    catch
                    {
                    }
                }

                results.Add(model);
            }
        }
        catch
        {
        }

        return results;
    }

    private static object ReadPropertyValue(DbDataReader reader, int ordinal, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(string))
            return reader.GetValue(ordinal)?.ToString();

        if (underlyingType == typeof(bool))
            return reader.GetBoolean(ordinal);

        if (underlyingType == typeof(int))
            return Convert.ToInt32(reader.GetValue(ordinal));

        if (underlyingType == typeof(long))
            return Convert.ToInt64(reader.GetValue(ordinal));

        if (underlyingType == typeof(decimal))
            return Convert.ToDecimal(reader.GetValue(ordinal));

        if (underlyingType == typeof(double))
            return Convert.ToDouble(reader.GetValue(ordinal));

        if (underlyingType == typeof(float))
            return Convert.ToSingle(reader.GetValue(ordinal));

        if (underlyingType == typeof(DateTime))
            return reader.GetDateTime(ordinal);

        if (underlyingType == typeof(DateTimeOffset))
        {
            var val = reader.GetValue(ordinal);
            if (val is DateTimeOffset dto) return dto;
            if (val is DateTime dt) return new DateTimeOffset(dt);
            return DateTimeOffset.Parse(val.ToString());
        }

        if (underlyingType == typeof(Guid))
        {
            var val = reader.GetValue(ordinal);
            if (val is Guid g) return g;
            return Guid.Parse(val.ToString());
        }

        if (underlyingType.IsEnum)
            return Enum.ToObject(underlyingType, Convert.ToInt32(reader.GetValue(ordinal)));

        if (IsComplexType(underlyingType))
        {
            var jsonString = reader.GetString(ordinal);
            return JsonSerializer.Deserialize(jsonString, targetType);
        }

        return reader.GetValue(ordinal);
    }

    private static bool IsComplexType(Type type)
    {
        return (type.IsClass && type != typeof(string) && type != typeof(byte[]))
            || type.IsInterface
            || (type.IsValueType && !type.IsPrimitive && !type.IsEnum && type.Namespace != "System");
    }

    private static string EscapeSql(string value)
    {
        return value?.Replace("'", "''");
    }

    #endregion
}