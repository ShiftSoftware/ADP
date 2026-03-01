using CsvHelper;
using CsvHelper.Configuration;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class DuckDBVehicleReportService(
    DuckDB.NET.Data.DuckDBConnection connection,
    VehicleLookupService vehicleLookupService) : IVehicleReportService
{
    private const int LookupBatchSize = 500;

    public async Task<IEnumerable<string>> GetDistinctVinsAsync(int? count = null)
    {
        if (count.HasValue && count.Value <= 0)
            return Enumerable.Empty<string>();

        var sql = new StringBuilder(@"
            SELECT DISTINCT UPPER(TRIM(VIN)) AS VIN
            FROM VehicleEntry
            WHERE VIN IS NOT NULL AND TRIM(VIN) <> ''
            ORDER BY VIN");

        if (count.HasValue)
            sql.Append($"\nLIMIT {count.Value}");

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql.ToString();

        var vins = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (reader.IsDBNull(0))
                continue;

            var vin = reader.GetString(0)?.Trim()?.ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(vin))
                vins.Add(vin);
        }

        return vins;
    }

    public async Task<IEnumerable<VehicleServiceItemReportModel>> GetVehicleServiceItemsReportAsync(
        IEnumerable<string> vins = null,
        int? distinctVinCount = null)
    {
        var normalizedVins = vins?
            .Select(NormalizeVin)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList()
            ?? (await GetDistinctVinsAsync(distinctVinCount)).ToList();

        if (normalizedVins.Count == 0)
            return Enumerable.Empty<VehicleServiceItemReportModel>();

        var bestItemsByServiceIdByVin = new Dictionary<string, Dictionary<string, VehicleServiceItemDTO>>(StringComparer.Ordinal);
        var batchCount = (normalizedVins.Count + LookupBatchSize - 1) / LookupBatchSize;

        for (var batchIndex = 0; batchIndex < batchCount; batchIndex++)
        {
            var batch = normalizedVins
                .Skip(batchIndex * LookupBatchSize)
                .Take(LookupBatchSize)
                .ToList();

            var lookups = await vehicleLookupService.LookupAsync(batch);

            foreach (var lookup in lookups)
            {
                if (string.IsNullOrWhiteSpace(lookup?.VIN))
                    continue;

                var vinKey = NormalizeVin(lookup.VIN);
                if (!bestItemsByServiceIdByVin.ContainsKey(vinKey))
                    bestItemsByServiceIdByVin[vinKey] = BuildBestItemsByServiceId(lookup.ServiceItems);
            }
        }

        var rows = new List<VehicleServiceItemReportModel>();

        foreach (var vin in normalizedVins)
        {
            bestItemsByServiceIdByVin.TryGetValue(vin, out var bestItemsByServiceId);
            bestItemsByServiceId ??= new Dictionary<string, VehicleServiceItemDTO>(StringComparer.Ordinal);

            foreach (var item in bestItemsByServiceId.Values.OrderBy(x => x.ServiceItemID, ServiceItemIdComparer))
            {
                rows.Add(CreateRow(vin, item));
            }
        }

        return rows;
    }

    public async Task<int> ExportVehicleServiceItemsReportToCsvAsync(
        string fileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("CSV output file path is required.", nameof(fileFullPath));

        var rows = (await GetVehicleServiceItemsReportAsync(vins, distinctVinCount)).ToList();

        var outputDirectory = Path.GetDirectoryName(fileFullPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        using var writer = new StreamWriter(fileFullPath, false);
        using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csvWriter.Context.RegisterClassMap<VehicleServiceItemReportModelCsvMap>();
        await csvWriter.WriteRecordsAsync(rows);

        return rows.Count;
    }

    private static VehicleServiceItemReportModel CreateRow(string vin, VehicleServiceItemDTO item)
    {
        return new VehicleServiceItemReportModel
        {
            VIN = vin ?? string.Empty,
            ServiceItemId = item?.ServiceItemID?.Trim() ?? string.Empty,
            ServiceItemName = item?.Name ?? string.Empty,
            GroupName = item?.Group?.Name ?? string.Empty,
            GroupTabOrder = item?.Group?.TabOrder,
            GroupIsDefault = item?.Group?.IsDefault,
            GroupIsSequential = item?.Group?.IsSequential,
            Status = item?.Status ?? string.Empty,
            StatusEnum = item?.StatusEnum,
            Type = item?.Type ?? string.Empty,
            TypeEnum = item?.TypeEnum,
            Price = item?.Cost,
            MenuCode = item?.PackageCode ?? string.Empty,
            ActivatedAt = item?.ActivatedAt == default ? null : item.ActivatedAt,
            ExpiresAt = item?.ExpiresAt,
            ClaimDate = item?.ClaimDate,
            CampaignId = item?.CampaignID,
            CampaignUniqueReference = item?.CampaignUniqueReference ?? string.Empty,
            ModelCostId = item?.ModelCostID,
            PaidServiceInvoiceLineId = item?.PaidServiceInvoiceLineID ?? string.Empty,
            CompanyName = item?.CompanyName ?? string.Empty,
            InvoiceNumber = item?.InvoiceNumber ?? string.Empty,
            JobNumber = item?.JobNumber ?? string.Empty,
            MaximumMileage = item?.MaximumMileage,
            Claimable = item?.Claimable,
            ClaimingMethod = item?.ClaimingMethodEnum,
            VehicleInspectionId = item?.VehicleInspectionID ?? string.Empty,
            VehicleInspectionTypeId = item?.VehicleInspectionTypeID ?? string.Empty
        };
    }

    private static string NormalizeVin(string vin)
    {
        return vin?.Trim()?.ToUpperInvariant();
    }

    private static Dictionary<string, VehicleServiceItemDTO> BuildBestItemsByServiceId(IEnumerable<VehicleServiceItemDTO> items)
    {
        return (items ?? Enumerable.Empty<VehicleServiceItemDTO>())
            .Where(x => !string.IsNullOrWhiteSpace(x.ServiceItemID))
            .GroupBy(x => x.ServiceItemID.Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g
                .OrderByDescending(x => x.ClaimDate ?? DateTimeOffset.MinValue)
                .ThenByDescending(x => x.ActivatedAt)
                .ThenByDescending(x => x.ExpiresAt ?? DateTime.MinValue)
                .First(), StringComparer.Ordinal);
    }

    private static readonly IComparer<string> ServiceItemIdComparer = Comparer<string>.Create((left, right) =>
    {
        if (long.TryParse(left, NumberStyles.Integer, CultureInfo.InvariantCulture, out var leftValue)
            && long.TryParse(right, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rightValue))
        {
            return leftValue.CompareTo(rightValue);
        }

        return string.Compare(left, right, StringComparison.Ordinal);
    });

    private sealed class VehicleServiceItemReportModelCsvMap : ClassMap<VehicleServiceItemReportModel>
    {
        public VehicleServiceItemReportModelCsvMap()
        {
            Map(x => x.VIN).Index(0);
            Map(x => x.ServiceItemId).Index(1);
            Map(x => x.ServiceItemName).Index(2);
            Map(x => x.GroupName).Index(3);
            Map(x => x.GroupTabOrder).Index(4);
            Map(x => x.GroupIsDefault).Index(5);
            Map(x => x.GroupIsSequential).Index(6);
            Map(x => x.Status).Index(7);
            Map(x => x.StatusEnum).Index(8);
            Map(x => x.Type).Index(9);
            Map(x => x.TypeEnum).Index(10);
            Map(x => x.Price).Index(11);
            Map(x => x.MenuCode).Index(12);
            Map(x => x.ActivatedAt).Index(13);
            Map(x => x.ExpiresAt).Index(14);
            Map(x => x.ClaimDate).Index(15);
            Map(x => x.CampaignId).Index(16);
            Map(x => x.CampaignUniqueReference).Index(17);
            Map(x => x.ModelCostId).Index(18);
            Map(x => x.PaidServiceInvoiceLineId).Index(19);
            Map(x => x.CompanyName).Index(20);
            Map(x => x.InvoiceNumber).Index(21);
            Map(x => x.JobNumber).Index(22);
            Map(x => x.MaximumMileage).Index(23);
            Map(x => x.Claimable).Index(24);
            Map(x => x.ClaimingMethod).Index(25);
            Map(x => x.VehicleInspectionId).Index(26);
            Map(x => x.VehicleInspectionTypeId).Index(27);
        }
    }
}
