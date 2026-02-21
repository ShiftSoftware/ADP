using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class DuckDBVehicleLoockupStorageService(DuckDB.NET.Data.DuckDBConnection connection)
    : IVehicleLoockupStorageService
{
    private const int AggregateVinChunkSize = 500;
    private static readonly TimeSpan ServiceItemCacheTtl = TimeSpan.FromMinutes(5);

    private readonly object serviceItemsCacheLock = new();
    private List<ServiceItemModel>? serviceItemsCache;
    private DateTimeOffset serviceItemsCacheExpiresAt = DateTimeOffset.MinValue;

    private readonly ConcurrentDictionary<(string Variant, long? Brand), VehicleModelModel> vehicleModelCache = new();
    private readonly ConcurrentDictionary<(string ColorCode, long? Brand), ColorModel> exteriorColorCache = new();
    private readonly ConcurrentDictionary<(string TrimCode, long? Brand), ColorModel> interiorColorCache = new();
    private readonly ConcurrentDictionary<(string CustomerId, long? CompanyId), CustomerModel> customerCache = new();
    private readonly ConcurrentDictionary<(long? BrandId, string Vin), List<TBP_StockModel>> brokerStockCache = new();

    #region Aggregated Company Data

    public async Task<CompanyDataAggregateModel> GetAggregatedCompanyData(string vin)
    {
        vin = NormalizeVin(vin);

        var aggregate = new CompanyDataAggregateModel
        {
            VIN = vin,
        };

        if (string.IsNullOrWhiteSpace(vin))
            return aggregate;

        var aggregateList = await GetAggregatedCompanyData([vin], itemTypes: null);
        return aggregateList.FirstOrDefault() ?? aggregate;
    }

    public async Task<IEnumerable<CompanyDataAggregateModel>> GetAggregatedCompanyData(IEnumerable<string> vins, IEnumerable<string> itemTypes)
    {
        if (vins is null)
            return Enumerable.Empty<CompanyDataAggregateModel>();

        var normalizedInputOrder = vins
            .Select(NormalizeVin)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (normalizedInputOrder.Count == 0)
            return Enumerable.Empty<CompanyDataAggregateModel>();

        var normalizedDistinctVins = normalizedInputOrder
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var timer = Stopwatch.StartNew();
        var aggregateMap = normalizedDistinctVins.ToDictionary(
            vinKey => vinKey,
            vinKey => new CompanyDataAggregateModel { VIN = vinKey },
            StringComparer.Ordinal);

        foreach (var vinChunk in Chunk(normalizedDistinctVins, AggregateVinChunkSize))
        {
            var inClause = BuildVinInClause(vinChunk);

            AssignItemsByVin(await GetVehicleEntriesByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.VehicleEntries = items);
            AssignItemsByVin(await GetInitialOfficialVINsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.InitialOfficialVINs = items);
            AssignItemsByVin(await GetSSCAffectedVINsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.SSCAffectedVINs = items);
            AssignItemsByVin(await GetWarrantyClaimsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.WarrantyClaims = items);
            AssignItemsByVin(await GetItemClaimsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.ItemClaims = items);
            AssignItemsByVin(await GetPaidServiceInvoicesByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.PaidServiceInvoices = items);
            AssignItemsByVin(await GetExtendedWarrantiesByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.ExtendedWarrantyEntries = items);
            AssignItemsByVin(await GetPaintThicknessInspectionsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.PaintThicknessInspections = items);
            AssignItemsByVin(await GetFreeServiceItemDateShiftsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.FreeServiceItemDateShifts = items);
            AssignItemsByVin(await GetFreeServiceItemExcludedVINsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.FreeServiceItemExcludedVINs = items);
            AssignItemsByVin(await GetWarrantyDateShiftsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.WarrantyDateShifts = items);
        }

        timer.Stop();
        Debug.WriteLine($"[DuckDBVehicleLookup] Batched aggregate fetch completed for {normalizedDistinctVins.Count} VIN(s) in {timer.ElapsedMilliseconds} ms.");

        return normalizedInputOrder
            .Distinct(StringComparer.Ordinal)
            .Select(vinKey => aggregateMap[vinKey])
            .ToList();
    }

    public async Task<IEnumerable<CompanyDataAggregateModel>> GetAggregatedCompanyDataForBulkLookupAsync(IEnumerable<string> vins)
    {
        if (vins is null)
            return Enumerable.Empty<CompanyDataAggregateModel>();

        var normalizedInputOrder = vins
            .Select(NormalizeVin)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (normalizedInputOrder.Count == 0)
            return Enumerable.Empty<CompanyDataAggregateModel>();

        var normalizedDistinctVins = normalizedInputOrder
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var timer = Stopwatch.StartNew();
        var aggregateMap = normalizedDistinctVins.ToDictionary(
            vinKey => vinKey,
            vinKey => new CompanyDataAggregateModel { VIN = vinKey },
            StringComparer.Ordinal);

        var inClause = BuildVinInClause(normalizedDistinctVins);

        AssignItemsByVin(await GetVehicleEntriesByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.VehicleEntries = items);
        AssignItemsByVin(await GetInitialOfficialVINsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.InitialOfficialVINs = items);
        AssignItemsByVin(await GetSSCAffectedVINsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.SSCAffectedVINs = items);
        AssignItemsByVin(await GetWarrantyClaimsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.WarrantyClaims = items);
        AssignItemsByVin(await GetItemClaimsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.ItemClaims = items);
        AssignItemsByVin(await GetPaidServiceInvoicesByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.PaidServiceInvoices = items);
        AssignItemsByVin(await GetExtendedWarrantiesByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.ExtendedWarrantyEntries = items);
        AssignItemsByVin(await GetPaintThicknessInspectionsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.PaintThicknessInspections = items);
        AssignItemsByVin(await GetFreeServiceItemDateShiftsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.FreeServiceItemDateShifts = items);
        AssignItemsByVin(await GetFreeServiceItemExcludedVINsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.FreeServiceItemExcludedVINs = items);
        AssignItemsByVin(await GetWarrantyDateShiftsByVinsAsync(inClause), aggregateMap, x => x.VIN, (a, items) => a.WarrantyDateShifts = items);

        timer.Stop();
        Debug.WriteLine($"[DuckDBVehicleLookup] Bulk-prefetch aggregate fetch completed for {normalizedDistinctVins.Count} VIN(s) in {timer.ElapsedMilliseconds} ms.");

        return normalizedInputOrder
            .Distinct(StringComparer.Ordinal)
            .Select(vinKey => aggregateMap[vinKey])
            .ToList();
    }

    #endregion

    #region Vehicle Entry

    private async Task<List<VehicleEntryModel>> GetVehicleEntriesAsync(string vin)
    {
        var inClause = BuildVinInClause([vin]);
        return await GetVehicleEntriesByVinsAsync(inClause);
    }

    private async Task<List<VehicleEntryModel>> GetVehicleEntriesByVinsAsync(string vinInClause)
    {
        var models = await ExecuteQueryAsync<VehicleEntryModel>(
            $"SELECT * FROM VehicleEntry WHERE VIN IN ({vinInClause})");

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
        var inClause = BuildVinInClause([vin]);
        return await GetInitialOfficialVINsByVinsAsync(inClause);
    }

    private async Task<List<InitialOfficialVINModel>> GetInitialOfficialVINsByVinsAsync(string vinInClause)
    {
        return await ExecuteQueryAsync<InitialOfficialVINModel>(
            $"SELECT * FROM InitialOfficialVIN WHERE VIN IN ({vinInClause})");
    }

    #endregion

    #region SSC Affected VIN

    private async Task<List<SSCAffectedVINModel>> GetSSCAffectedVINsAsync(string vin)
    {
        var inClause = BuildVinInClause([vin]);
        return await GetSSCAffectedVINsByVinsAsync(inClause);
    }

    private async Task<List<SSCAffectedVINModel>> GetSSCAffectedVINsByVinsAsync(string vinInClause)
    {
        return await ExecuteQueryAsync<SSCAffectedVINModel>(
            $"SELECT * FROM SSCAffectedVIN WHERE VIN IN ({vinInClause})");
    }

    #endregion

    #region Warranty Claim

    private async Task<List<WarrantyClaimModel>> GetWarrantyClaimsAsync(string vin)
    {
        var inClause = BuildVinInClause([vin]);
        return await GetWarrantyClaimsByVinsAsync(inClause);
    }

    private async Task<List<WarrantyClaimModel>> GetWarrantyClaimsByVinsAsync(string vinInClause)
    {
        return await ExecuteQueryAsync<WarrantyClaimModel>(
            $"SELECT * FROM WarrantyClaim WHERE VIN IN ({vinInClause}) AND IsDeleted = false");
    }

    #endregion

    #region Item Claim

    private async Task<List<ItemClaimModel>> GetItemClaimsAsync(string vin)
    {
        var inClause = BuildVinInClause([vin]);
        return await GetItemClaimsByVinsAsync(inClause);
    }

    private async Task<List<ItemClaimModel>> GetItemClaimsByVinsAsync(string vinInClause)
    {
        return await ExecuteQueryAsync<ItemClaimModel>(
            $"SELECT * FROM ItemClaim WHERE VIN IN ({vinInClause}) AND IsDeleted = false");
    }

    #endregion

    #region Paid Service Invoice

    private async Task<List<PaidServiceInvoiceModel>> GetPaidServiceInvoicesAsync(string vin)
    {
        var inClause = BuildVinInClause([vin]);
        return await GetPaidServiceInvoicesByVinsAsync(inClause);
    }

    private async Task<List<PaidServiceInvoiceModel>> GetPaidServiceInvoicesByVinsAsync(string vinInClause)
    {
        return await ExecuteQueryAsync<PaidServiceInvoiceModel>(
            $"SELECT * FROM PaidServiceInvoice WHERE VIN IN ({vinInClause}) AND IsDeleted = false");
    }

    #endregion

    #region Extended Warranty

    private async Task<List<ExtendedWarrantyModel>> GetExtendedWarrantiesAsync(string vin)
    {
        var inClause = BuildVinInClause([vin]);
        return await GetExtendedWarrantiesByVinsAsync(inClause);
    }

    private async Task<List<ExtendedWarrantyModel>> GetExtendedWarrantiesByVinsAsync(string vinInClause)
    {
        return await ExecuteQueryAsync<ExtendedWarrantyModel>(
            $"SELECT * FROM ExtendedWarranty WHERE VIN IN ({vinInClause}) AND IsDeleted = false AND IsActive = true");
    }

    #endregion

    #region Paint Thickness Inspection

    private async Task<List<PaintThicknessInspectionModel>> GetPaintThicknessInspectionsAsync(string vin)
    {
        var inClause = BuildVinInClause([vin]);
        return await GetPaintThicknessInspectionsByVinsAsync(inClause);
    }

    private async Task<List<PaintThicknessInspectionModel>> GetPaintThicknessInspectionsByVinsAsync(string vinInClause)
    {
        return await ExecuteQueryAsync<PaintThicknessInspectionModel>(
            $"SELECT * FROM PaintThicknessInspection WHERE VIN IN ({vinInClause})");
    }

    #endregion

    #region Free Service Item Date Shift

    private async Task<List<FreeServiceItemDateShiftModel>> GetFreeServiceItemDateShiftsAsync(string vin)
    {
        var inClause = BuildVinInClause([vin]);
        return await GetFreeServiceItemDateShiftsByVinsAsync(inClause);
    }

    private async Task<List<FreeServiceItemDateShiftModel>> GetFreeServiceItemDateShiftsByVinsAsync(string vinInClause)
    {
        return await ExecuteQueryAsync<FreeServiceItemDateShiftModel>(
            $"SELECT * FROM VehicleFreeServiceShiftDate WHERE VIN IN ({vinInClause}) AND IsDeleted = false");
    }

    #endregion

    #region Free Service Item Excluded VIN

    private async Task<List<FreeServiceItemExcludedVINModel>> GetFreeServiceItemExcludedVINsAsync(string vin)
    {
        var inClause = BuildVinInClause([vin]);
        return await GetFreeServiceItemExcludedVINsByVinsAsync(inClause);
    }

    private async Task<List<FreeServiceItemExcludedVINModel>> GetFreeServiceItemExcludedVINsByVinsAsync(string vinInClause)
    {
        return await ExecuteQueryAsync<FreeServiceItemExcludedVINModel>(
            $"SELECT * FROM VehicleFreeServiceItemExcludedVIN WHERE VIN IN ({vinInClause}) AND IsDeleted = false");
    }

    #endregion

    #region Warranty Date Shift

    private async Task<List<WarrantyDateShiftModel>> GetWarrantyDateShiftsAsync(string vin)
    {
        var inClause = BuildVinInClause([vin]);
        return await GetWarrantyDateShiftsByVinsAsync(inClause);
    }

    private async Task<List<WarrantyDateShiftModel>> GetWarrantyDateShiftsByVinsAsync(string vinInClause)
    {
        return await ExecuteQueryAsync<WarrantyDateShiftModel>(
            $"SELECT * FROM VehicleWarrantyShiftDate WHERE VIN IN ({vinInClause}) AND IsDeleted = false");
    }

    #endregion

    #region Service Items

    public async Task<IEnumerable<ServiceItemModel>> GetServiceItemsAsync(bool useCache = true)
    {
        if (!useCache)
            return await ExecuteQueryAsync<ServiceItemModel>("SELECT * FROM ServiceItem");

        lock (serviceItemsCacheLock)
        {
            if (serviceItemsCache is not null && DateTimeOffset.UtcNow < serviceItemsCacheExpiresAt)
            {
                Debug.WriteLine("[DuckDBVehicleLookup] ServiceItem cache hit.");
                return serviceItemsCache;
            }
        }

        var timer = Stopwatch.StartNew();
        var loaded = await ExecuteQueryAsync<ServiceItemModel>("SELECT * FROM ServiceItem");
        timer.Stop();

        lock (serviceItemsCacheLock)
        {
            serviceItemsCache = loaded;
            serviceItemsCacheExpiresAt = DateTimeOffset.UtcNow.Add(ServiceItemCacheTtl);
        }

        Debug.WriteLine($"[DuckDBVehicleLookup] ServiceItem cache miss, loaded {loaded.Count} rows in {timer.ElapsedMilliseconds} ms.");

        return loaded;
    }

    #endregion

    #region Vehicle Models

    public async Task<VehicleModelModel> GetVehicleModelsAsync(string variant, long? brand)
    {
        if (string.IsNullOrWhiteSpace(variant))
            return null;

        variant = variant.Trim();
        var cacheKey = (variant, brand);

        if (vehicleModelCache.TryGetValue(cacheKey, out var cachedModel))
            return cachedModel;

        var items = await ExecuteQueryAsync<VehicleModelModel>(
            $"SELECT * FROM VehicleModel WHERE VariantCode = '{EscapeSql(variant)}' AND BrandID = {brand}");

        var first = items.FirstOrDefault();
        if (first is not null)
            vehicleModelCache[cacheKey] = first;

        return first;
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

        colorCode = colorCode.Trim();
        var cacheKey = (colorCode, brand);

        if (exteriorColorCache.TryGetValue(cacheKey, out var cachedColor))
            return cachedColor;

        var items = await ExecuteQueryAsync<ColorModel>(
            $"SELECT * FROM ExteriorColor WHERE Code = '{EscapeSql(colorCode)}' AND BrandID = {brand}");

        var first = items.FirstOrDefault();
        if (first is not null)
            exteriorColorCache[cacheKey] = first;

        return first;
    }

    public async Task<ColorModel> GetInteriorColorsAsync(string trimCode, long? brand)
    {
        if (string.IsNullOrWhiteSpace(trimCode))
            return null;

        trimCode = trimCode.Trim();
        var cacheKey = (trimCode, brand);

        if (interiorColorCache.TryGetValue(cacheKey, out var cachedColor))
            return cachedColor;

        var items = await ExecuteQueryAsync<ColorModel>(
            $"SELECT * FROM InteriorColor WHERE Code = '{EscapeSql(trimCode)}' AND BrandID = {brand}");

        var first = items.FirstOrDefault();
        if (first is not null)
            interiorColorCache[cacheKey] = first;

        return first;
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
        vin = NormalizeVin(vin);
        var cacheKey = (brandId, vin);

        if (brokerStockCache.TryGetValue(cacheKey, out var cachedStock))
            return cachedStock;

        var sql = $"SELECT * FROM BrokerStock WHERE VIN = '{EscapeSql(vin)}'";

        if (brandId is not null)
            sql += $" AND BrandID = {brandId}";

        var result = await ExecuteQueryAsync<TBP_StockModel>(sql);
        brokerStockCache[cacheKey] = result;

        return result;
    }

    #endregion

    #region Customer

    public async Task<CustomerModel> GetCustomerAsync(string customerID, long? companyID)
    {
        if (string.IsNullOrWhiteSpace(customerID))
            return null;

        customerID = customerID.Trim();
        var cacheKey = (customerID, companyID);

        if (customerCache.TryGetValue(cacheKey, out var cachedCustomer))
            return cachedCustomer;

        var items = await ExecuteQueryAsync<CustomerModel>(
            $"SELECT * FROM Customer WHERE CustomerID = '{EscapeSql(customerID)}' AND CompanyID = {companyID}");

        var first = items.FirstOrDefault();
        if (first is not null)
            customerCache[cacheKey] = first;

        return first;
    }

    #endregion

    #region Batch Helpers

    private static string NormalizeVin(string vin)
    {
        return vin?.Trim()?.ToUpper();
    }

    private static IEnumerable<List<string>> Chunk(IReadOnlyList<string> items, int size)
    {
        for (var i = 0; i < items.Count; i += size)
            yield return items.Skip(i).Take(size).ToList();
    }

    private static string BuildVinInClause(IEnumerable<string> vins)
    {
        return string.Join(
            ",",
            vins
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => $"'{EscapeSql(v)}'"));
    }

    private static void AssignItemsByVin<T>(
        IEnumerable<T> items,
        Dictionary<string, CompanyDataAggregateModel> aggregateMap,
        Func<T, string> vinSelector,
        Action<CompanyDataAggregateModel, List<T>> assigner)
    {
        var grouped = items
            .Where(x => x is not null)
            .GroupBy(x => NormalizeVin(vinSelector(x)), StringComparer.Ordinal);

        foreach (var group in grouped)
        {
            if (string.IsNullOrWhiteSpace(group.Key))
                continue;

            if (!aggregateMap.TryGetValue(group.Key, out var aggregate))
                continue;

            assigner(aggregate, group.ToList());
        }
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
