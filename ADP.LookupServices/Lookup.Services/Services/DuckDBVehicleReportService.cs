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
    private const int QueryVinChunkSize = 500;

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

    public async Task<IEnumerable<VehicleSscReportModel>> GetVehicleSscReportAsync(
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
            return Enumerable.Empty<VehicleSscReportModel>();

        var rows = new List<VehicleSscReportModel>();

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
                var vin = NormalizeVin(lookup?.VIN);
                if (string.IsNullOrWhiteSpace(vin))
                    continue;

                foreach (var ssc in lookup.SSC ?? Enumerable.Empty<SscDTO>())
                {
                    rows.Add(CreateSscRow(vin, ssc));
                }
            }
        }

        return rows;
    }

    public async Task<int> ExportVehicleSscReportToCsvAsync(
        string fileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("CSV output file path is required.", nameof(fileFullPath));

        var rows = (await GetVehicleSscReportAsync(vins, distinctVinCount)).ToList();

        var outputDirectory = Path.GetDirectoryName(fileFullPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        using var writer = new StreamWriter(fileFullPath, false);
        using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csvWriter.Context.RegisterClassMap<VehicleSscReportModelCsvMap>();
        await csvWriter.WriteRecordsAsync(rows);

        return rows.Count;
    }

    public async Task<IEnumerable<VehicleServiceHistoryLaborReportModel>> GetVehicleServiceHistoryLaborReportAsync(
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
            return Enumerable.Empty<VehicleServiceHistoryLaborReportModel>();

        var rows = new List<VehicleServiceHistoryLaborReportModel>();

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
                var vin = NormalizeVin(lookup?.VIN);
                if (string.IsNullOrWhiteSpace(vin))
                    continue;

                foreach (var serviceHistoryEntry in lookup.ServiceHistory ?? Enumerable.Empty<VehicleServiceHistoryDTO>())
                {
                    foreach (var labor in serviceHistoryEntry?.LaborLines ?? Enumerable.Empty<VehicleLaborDTO>())
                    {
                        rows.Add(CreateServiceHistoryLaborRow(vin, serviceHistoryEntry, labor));
                    }
                }
            }
        }

        return rows;
    }

    public async Task<int> ExportVehicleServiceHistoryLaborReportToCsvAsync(
        string fileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("CSV output file path is required.", nameof(fileFullPath));

        var rows = (await GetVehicleServiceHistoryLaborReportAsync(vins, distinctVinCount)).ToList();

        var outputDirectory = Path.GetDirectoryName(fileFullPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        using var writer = new StreamWriter(fileFullPath, false);
        using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csvWriter.Context.RegisterClassMap<VehicleServiceHistoryLaborReportModelCsvMap>();
        await csvWriter.WriteRecordsAsync(rows);

        return rows.Count;
    }

    public async Task<IEnumerable<VehicleServiceHistoryPartReportModel>> GetVehicleServiceHistoryPartReportAsync(
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
            return Enumerable.Empty<VehicleServiceHistoryPartReportModel>();

        var rows = new List<VehicleServiceHistoryPartReportModel>();

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
                var vin = NormalizeVin(lookup?.VIN);
                if (string.IsNullOrWhiteSpace(vin))
                    continue;

                foreach (var serviceHistoryEntry in lookup.ServiceHistory ?? Enumerable.Empty<VehicleServiceHistoryDTO>())
                {
                    foreach (var part in serviceHistoryEntry?.PartLines ?? Enumerable.Empty<VehiclePartDTO>())
                    {
                        rows.Add(CreateServiceHistoryPartRow(vin, serviceHistoryEntry, part));
                    }
                }
            }
        }

        return rows;
    }

    public async Task<int> ExportVehicleServiceHistoryPartReportToCsvAsync(
        string fileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("CSV output file path is required.", nameof(fileFullPath));

        var rows = (await GetVehicleServiceHistoryPartReportAsync(vins, distinctVinCount)).ToList();

        var outputDirectory = Path.GetDirectoryName(fileFullPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        using var writer = new StreamWriter(fileFullPath, false);
        using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csvWriter.Context.RegisterClassMap<VehicleServiceHistoryPartReportModelCsvMap>();
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

    private static VehicleSscReportModel CreateSscRow(string vin, SscDTO ssc)
    {
        var labors = (ssc?.Labors ?? Enumerable.Empty<SSCLaborDTO>()).Take(3).ToList();
        var parts = (ssc?.Parts ?? Enumerable.Empty<SSCPartDTO>()).Take(3).ToList();

        var labor1 = labors.Count > 0 ? labors[0] : null;
        var labor2 = labors.Count > 1 ? labors[1] : null;
        var labor3 = labors.Count > 2 ? labors[2] : null;

        var part1 = parts.Count > 0 ? parts[0] : null;
        var part2 = parts.Count > 1 ? parts[1] : null;
        var part3 = parts.Count > 2 ? parts[2] : null;

        return new VehicleSscReportModel
        {
            VIN = vin ?? string.Empty,
            SSCCode = ssc?.SSCCode ?? string.Empty,
            Description = ssc?.Description ?? string.Empty,
            Repaired = ssc?.Repaired ?? false,
            RepairDate = ssc?.RepairDate,

            LaborCode1 = labor1?.LaborCode ?? string.Empty,
            LaborDescription1 = labor1?.LaborDescription ?? string.Empty,
            LaborAllowedTime1 = labor1?.AllowedTime,

            LaborCode2 = labor2?.LaborCode ?? string.Empty,
            LaborDescription2 = labor2?.LaborDescription ?? string.Empty,
            LaborAllowedTime2 = labor2?.AllowedTime,

            LaborCode3 = labor3?.LaborCode ?? string.Empty,
            LaborDescription3 = labor3?.LaborDescription ?? string.Empty,
            LaborAllowedTime3 = labor3?.AllowedTime,

            PartNumber1 = part1?.PartNumber ?? string.Empty,
            PartDescription1 = part1?.PartDescription ?? string.Empty,
            PartIsAvailable1 = part1?.IsAvailable,

            PartNumber2 = part2?.PartNumber ?? string.Empty,
            PartDescription2 = part2?.PartDescription ?? string.Empty,
            PartIsAvailable2 = part2?.IsAvailable,

            PartNumber3 = part3?.PartNumber ?? string.Empty,
            PartDescription3 = part3?.PartDescription ?? string.Empty,
            PartIsAvailable3 = part3?.IsAvailable,
        };
    }

    private static VehicleServiceHistoryLaborReportModel CreateServiceHistoryLaborRow(string vin, VehicleServiceHistoryDTO serviceHistoryEntry, VehicleLaborDTO labor)
    {
        return new VehicleServiceHistoryLaborReportModel
        {
            VIN = vin ?? string.Empty,
            ServiceType = serviceHistoryEntry?.ServiceType ?? string.Empty,
            ServiceDate = serviceHistoryEntry?.ServiceDate,
            Mileage = serviceHistoryEntry?.Mileage,
            CompanyName = serviceHistoryEntry?.CompanyName ?? string.Empty,
            BranchName = serviceHistoryEntry?.BranchName ?? string.Empty,
            AccountNumber = serviceHistoryEntry?.AccountNumber ?? string.Empty,
            InvoiceNumber = serviceHistoryEntry?.InvoiceNumber ?? string.Empty,
            ParentInvoiceNumber = serviceHistoryEntry?.ParentInvoiceNumber ?? string.Empty,
            JobNumber = serviceHistoryEntry?.JobNumber ?? string.Empty,

            LaborCode = labor?.LaborCode ?? string.Empty,
            LaborPackageCode = labor?.PackageCode ?? string.Empty,
            LaborServiceCode = labor?.ServiceCode ?? string.Empty,
            LaborServiceDescription = labor?.ServiceDescription ?? string.Empty,
        };
    }

    private static VehicleServiceHistoryPartReportModel CreateServiceHistoryPartRow(string vin, VehicleServiceHistoryDTO serviceHistoryEntry, VehiclePartDTO part)
    {
        return new VehicleServiceHistoryPartReportModel
        {
            VIN = vin ?? string.Empty,
            ServiceType = serviceHistoryEntry?.ServiceType ?? string.Empty,
            ServiceDate = serviceHistoryEntry?.ServiceDate,
            Mileage = serviceHistoryEntry?.Mileage,
            CompanyName = serviceHistoryEntry?.CompanyName ?? string.Empty,
            BranchName = serviceHistoryEntry?.BranchName ?? string.Empty,
            AccountNumber = serviceHistoryEntry?.AccountNumber ?? string.Empty,
            InvoiceNumber = serviceHistoryEntry?.InvoiceNumber ?? string.Empty,
            ParentInvoiceNumber = serviceHistoryEntry?.ParentInvoiceNumber ?? string.Empty,
            JobNumber = serviceHistoryEntry?.JobNumber ?? string.Empty,

            PartNumber = part?.PartNumber ?? string.Empty,
            PartQty = part?.QTY,
            PartPackageCode = part?.PackageCode ?? string.Empty,
            PartDescription = part?.PartDescription ?? string.Empty,
        };
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

    private sealed class VehicleSscReportModelCsvMap : ClassMap<VehicleSscReportModel>
    {
        public VehicleSscReportModelCsvMap()
        {
            Map(x => x.VIN).Index(0);
            Map(x => x.SSCCode).Index(1);
            Map(x => x.Description).Index(2);
            Map(x => x.Repaired).Index(3);
            Map(x => x.RepairDate).Index(4);

            Map(x => x.LaborCode1).Index(5);
            Map(x => x.LaborDescription1).Index(6);
            Map(x => x.LaborAllowedTime1).Index(7);

            Map(x => x.LaborCode2).Index(8);
            Map(x => x.LaborDescription2).Index(9);
            Map(x => x.LaborAllowedTime2).Index(10);

            Map(x => x.LaborCode3).Index(11);
            Map(x => x.LaborDescription3).Index(12);
            Map(x => x.LaborAllowedTime3).Index(13);

            Map(x => x.PartNumber1).Index(14);
            Map(x => x.PartDescription1).Index(15);
            Map(x => x.PartIsAvailable1).Index(16);

            Map(x => x.PartNumber2).Index(17);
            Map(x => x.PartDescription2).Index(18);
            Map(x => x.PartIsAvailable2).Index(19);

            Map(x => x.PartNumber3).Index(20);
            Map(x => x.PartDescription3).Index(21);
            Map(x => x.PartIsAvailable3).Index(22);
        }
    }

    private sealed class VehicleServiceHistoryLaborReportModelCsvMap : ClassMap<VehicleServiceHistoryLaborReportModel>
    {
        public VehicleServiceHistoryLaborReportModelCsvMap()
        {
            Map(x => x.VIN).Index(0);
            Map(x => x.ServiceType).Index(1);
            Map(x => x.ServiceDate).Index(2);
            Map(x => x.Mileage).Index(3);
            Map(x => x.CompanyName).Index(4);
            Map(x => x.BranchName).Index(5);
            Map(x => x.AccountNumber).Index(6);
            Map(x => x.InvoiceNumber).Index(7);
            Map(x => x.ParentInvoiceNumber).Index(8);
            Map(x => x.JobNumber).Index(9);

            Map(x => x.LaborCode).Index(10);
            Map(x => x.LaborPackageCode).Index(11);
            Map(x => x.LaborServiceCode).Index(12);
            Map(x => x.LaborServiceDescription).Index(13);
        }
    }

    private sealed class VehicleServiceHistoryPartReportModelCsvMap : ClassMap<VehicleServiceHistoryPartReportModel>
    {
        public VehicleServiceHistoryPartReportModelCsvMap()
        {
            Map(x => x.VIN).Index(0);
            Map(x => x.ServiceType).Index(1);
            Map(x => x.ServiceDate).Index(2);
            Map(x => x.Mileage).Index(3);
            Map(x => x.CompanyName).Index(4);
            Map(x => x.BranchName).Index(5);
            Map(x => x.AccountNumber).Index(6);
            Map(x => x.InvoiceNumber).Index(7);
            Map(x => x.ParentInvoiceNumber).Index(8);
            Map(x => x.JobNumber).Index(9);

            Map(x => x.PartNumber).Index(10);
            Map(x => x.PartQty).Index(11);
            Map(x => x.PartPackageCode).Index(12);
            Map(x => x.PartDescription).Index(13);
        }
    }
}
