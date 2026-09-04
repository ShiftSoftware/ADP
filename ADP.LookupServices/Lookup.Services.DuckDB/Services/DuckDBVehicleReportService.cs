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
    global::DuckDB.NET.Data.DuckDBConnection connection,
    VehicleLookupService vehicleLookupService) : IVehicleReportService
{
    private const int LookupBatchSize = 1_000_000;

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
        int? distinctVinCount = null,
        VehicleLookupRequestOptions requestOptions = null)
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
        var freeServiceItemStartDateByVin = new Dictionary<string, DateTime?>(StringComparer.Ordinal);
        var batchCount = (normalizedVins.Count + LookupBatchSize - 1) / LookupBatchSize;

        for (var batchIndex = 0; batchIndex < batchCount; batchIndex++)
        {
            var batch = normalizedVins
                .Skip(batchIndex * LookupBatchSize)
                .Take(LookupBatchSize)
                .ToList();

            var lookups = requestOptions is null
                ? await vehicleLookupService.LookupAsync(batch)
                : await vehicleLookupService.LookupAsync(batch, requestOptions);

            foreach (var lookup in lookups)
            {
                if (string.IsNullOrWhiteSpace(lookup?.VIN))
                    continue;

                var vinKey = NormalizeVin(lookup.VIN);
                if (!bestItemsByServiceIdByVin.ContainsKey(vinKey))
                    bestItemsByServiceIdByVin[vinKey] = BuildBestItemsByServiceId(lookup.ServiceItems);

                if (!freeServiceItemStartDateByVin.ContainsKey(vinKey))
                    freeServiceItemStartDateByVin[vinKey] = lookup.Warranty?.FreeServiceStartDate;
            }
        }

        var rows = new List<VehicleServiceItemReportModel>();

        foreach (var vin in normalizedVins)
        {
            bestItemsByServiceIdByVin.TryGetValue(vin, out var bestItemsByServiceId);
            bestItemsByServiceId ??= new Dictionary<string, VehicleServiceItemDTO>(StringComparer.Ordinal);
            freeServiceItemStartDateByVin.TryGetValue(vin, out var freeServiceItemStartDate);

            foreach (var item in bestItemsByServiceId.Values.OrderBy(x => x.ServiceItemID, ServiceItemIdComparer))
            {
                rows.Add(CreateRow(vin, item, freeServiceItemStartDate));
            }
        }

        return rows;
    }

    public async Task<int> ExportVehicleServiceItemsReportToCsvAsync(
        string fileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null,
        int batchSize = 1000,
        VehicleLookupRequestOptions requestOptions = null)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("CSV output file path is required.", nameof(fileFullPath));

        var allVins = await ResolveVinsAsync(vins, distinctVinCount);
        if (allVins.Count == 0) return 0;

        return await StreamCsvAsync<VehicleServiceItemReportModel, VehicleServiceItemReportModelCsvMap>(fileFullPath, allVins, batchSize, TransformServiceItemLookups, requestOptions);
    }

    public async Task<int> ExportVehicleServiceItemsReportToParquetAsync(
        string fileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null,
        int batchSize = 1000,
        VehicleLookupRequestOptions requestOptions = null)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("Parquet output file path is required.", nameof(fileFullPath));

        var allVins = await ResolveVinsAsync(vins, distinctVinCount);
        if (allVins.Count == 0) return 0;

        return await StreamParquetAsync(fileFullPath, allVins, batchSize, TransformServiceItemLookups, requestOptions);
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
        int? distinctVinCount = null,
        int batchSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("CSV output file path is required.", nameof(fileFullPath));

        var allVins = await ResolveVinsAsync(vins, distinctVinCount);
        if (allVins.Count == 0) return 0;

        return await StreamCsvAsync<VehicleSscReportModel, VehicleSscReportModelCsvMap>(fileFullPath, allVins, batchSize, TransformSscLookups);
    }

    public async Task<int> ExportVehicleSscReportToParquetAsync(
        string fileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null,
        int batchSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("Parquet output file path is required.", nameof(fileFullPath));

        var allVins = await ResolveVinsAsync(vins, distinctVinCount);
        if (allVins.Count == 0) return 0;

        return await StreamParquetAsync(fileFullPath, allVins, batchSize, TransformSscLookups);
    }

    public async Task<IEnumerable<VehicleLookupTopLevelReportModel>> GetVehicleLookupTopLevelReportAsync(
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
            return Enumerable.Empty<VehicleLookupTopLevelReportModel>();

        var rows = new List<VehicleLookupTopLevelReportModel>();
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

                rows.Add(CreateVehicleLookupTopLevelRow(vin, lookup));
            }
        }

        return rows;
    }

    public async Task<int> ExportVehicleLookupTopLevelReportToCsvAsync(
        string fileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null,
        int batchSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("CSV output file path is required.", nameof(fileFullPath));

        var allVins = await ResolveVinsAsync(vins, distinctVinCount);
        if (allVins.Count == 0) return 0;

        return await StreamCsvAsync<VehicleLookupTopLevelReportModel, VehicleLookupTopLevelReportModelCsvMap>(fileFullPath, allVins, batchSize, TransformTopLevelLookups);
    }

    public async Task<int> ExportVehicleLookupTopLevelReportToParquetAsync(
        string fileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null,
        int batchSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("Parquet output file path is required.", nameof(fileFullPath));

        var allVins = await ResolveVinsAsync(vins, distinctVinCount);
        if (allVins.Count == 0) return 0;

        return await StreamParquetAsync(fileFullPath, allVins, batchSize, TransformTopLevelLookups);
    }

    public async Task<(List<VehicleServiceHistoryLaborReportModel> LaborReports, List<VehicleServiceHistoryPartReportModel> PartReports)> GetVehicleServiceHistoryReportsAsync(
        IEnumerable<string> vins = null,
        int? distinctVinCount = null)
    {
        return await BuildServiceHistoryReportsAsync(vins, distinctVinCount);
    }

    public async Task<(int LaborRowCount, int PartRowCount)> ExportVehicleServiceHistoryReportsToCsvAsync(
        string laborFileFullPath,
        string partFileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null,
        int batchSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(laborFileFullPath))
            throw new ArgumentException("Labor CSV output file path is required.", nameof(laborFileFullPath));

        if (string.IsNullOrWhiteSpace(partFileFullPath))
            throw new ArgumentException("Part CSV output file path is required.", nameof(partFileFullPath));

        var allVins = await ResolveVinsAsync(vins, distinctVinCount);
        if (allVins.Count == 0) return (0, 0);

        var laborCount = await StreamCsvAsync<VehicleServiceHistoryLaborReportModel, VehicleServiceHistoryLaborReportModelCsvMap>(laborFileFullPath, allVins, batchSize, TransformServiceHistoryLaborLookups);
        var partCount = await StreamCsvAsync<VehicleServiceHistoryPartReportModel, VehicleServiceHistoryPartReportModelCsvMap>(partFileFullPath, allVins, batchSize, TransformServiceHistoryPartLookups);

        return (laborCount, partCount);
    }

    public async Task<(int LaborRowCount, int PartRowCount)> ExportVehicleServiceHistoryReportsToParquetAsync(
        string laborFileFullPath,
        string partFileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null,
        int batchSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(laborFileFullPath))
            throw new ArgumentException("Labor Parquet output file path is required.", nameof(laborFileFullPath));

        if (string.IsNullOrWhiteSpace(partFileFullPath))
            throw new ArgumentException("Part Parquet output file path is required.", nameof(partFileFullPath));

        var allVins = await ResolveVinsAsync(vins, distinctVinCount);
        if (allVins.Count == 0) return (0, 0);

        var laborCount = await StreamParquetAsync(laborFileFullPath, allVins, batchSize, TransformServiceHistoryLaborLookups);
        var partCount = await StreamParquetAsync(partFileFullPath, allVins, batchSize, TransformServiceHistoryPartLookups);

        return (laborCount, partCount);
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
        int? distinctVinCount = null,
        int batchSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("CSV output file path is required.", nameof(fileFullPath));

        var allVins = await ResolveVinsAsync(vins, distinctVinCount);
        if (allVins.Count == 0) return 0;

        return await StreamCsvAsync<VehicleServiceHistoryLaborReportModel, VehicleServiceHistoryLaborReportModelCsvMap>(fileFullPath, allVins, batchSize, TransformServiceHistoryLaborLookups);
    }

    public async Task<int> ExportVehicleServiceHistoryLaborReportToParquetAsync(
        string fileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null,
        int batchSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("Parquet output file path is required.", nameof(fileFullPath));

        var allVins = await ResolveVinsAsync(vins, distinctVinCount);
        if (allVins.Count == 0) return 0;

        return await StreamParquetAsync(fileFullPath, allVins, batchSize, TransformServiceHistoryLaborLookups);
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
        int? distinctVinCount = null,
        int batchSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("CSV output file path is required.", nameof(fileFullPath));

        var allVins = await ResolveVinsAsync(vins, distinctVinCount);
        if (allVins.Count == 0) return 0;

        return await StreamCsvAsync<VehicleServiceHistoryPartReportModel, VehicleServiceHistoryPartReportModelCsvMap>(fileFullPath, allVins, batchSize, TransformServiceHistoryPartLookups);
    }

    public async Task<int> ExportVehicleServiceHistoryPartReportToParquetAsync(
        string fileFullPath,
        IEnumerable<string> vins = null,
        int? distinctVinCount = null,
        int batchSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("Parquet output file path is required.", nameof(fileFullPath));

        var allVins = await ResolveVinsAsync(vins, distinctVinCount);
        if (allVins.Count == 0) return 0;

        return await StreamParquetAsync(fileFullPath, allVins, batchSize, TransformServiceHistoryPartLookups);
    }

    private async Task<(List<VehicleServiceHistoryLaborReportModel> LaborReports, List<VehicleServiceHistoryPartReportModel> PartReports)> BuildServiceHistoryReportsAsync(
        IEnumerable<string> vins,
        int? distinctVinCount)
    {
        var normalizedVins = vins?
            .Select(NormalizeVin)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList()
            ?? (await GetDistinctVinsAsync(distinctVinCount)).ToList();

        var laborReports = new List<VehicleServiceHistoryLaborReportModel>();
        var partReports = new List<VehicleServiceHistoryPartReportModel>();

        if (normalizedVins.Count == 0)
            return (laborReports, partReports);

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
                        laborReports.Add(CreateServiceHistoryLaborRow(vin, serviceHistoryEntry, labor));
                    }

                    foreach (var part in serviceHistoryEntry?.PartLines ?? Enumerable.Empty<VehiclePartDTO>())
                    {
                        partReports.Add(CreateServiceHistoryPartRow(vin, serviceHistoryEntry, part));
                    }
                }
            }
        }

        return (laborReports, partReports);
    }

    #region Streaming Export Helpers

    private async Task<List<string>> ResolveVinsAsync(IEnumerable<string> vins, int? distinctVinCount)
    {
        return (vins?
            .Select(NormalizeVin)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList())
            ?? (await GetDistinctVinsAsync(distinctVinCount)).ToList();
    }

    private static IEnumerable<List<string>> Chunk(List<string> items, int size)
    {
        for (var i = 0; i < items.Count; i += size)
            yield return items.GetRange(i, Math.Min(size, items.Count - i));
    }

    private static void EnsureDirectoryExists(string fileFullPath)
    {
        var outputDirectory = Path.GetDirectoryName(fileFullPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);
    }

    private async Task<int> StreamCsvAsync<TModel, TMap>(
        string fileFullPath,
        List<string> allVins,
        int batchSize,
        Func<List<string>, IEnumerable<VehicleLookupDTO>, List<TModel>> transform,
        VehicleLookupRequestOptions requestOptions = null)
        where TMap : ClassMap<TModel>, new()
    {
        EnsureDirectoryExists(fileFullPath);
        using var writer = new StreamWriter(fileFullPath, false);
        using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csvWriter.Context.RegisterClassMap<TMap>();

        int totalRows = 0;

        foreach (var vinChunk in Chunk(allVins, batchSize))
        {
            var lookups = requestOptions is null
                ? await vehicleLookupService.LookupAsync(vinChunk)
                : await vehicleLookupService.LookupAsync(vinChunk, requestOptions);
            var rows = transform(vinChunk, lookups);
            await csvWriter.WriteRecordsAsync(rows);
            await writer.FlushAsync();
            totalRows += rows.Count;
        }

        return totalRows;
    }

    private async Task<int> StreamParquetAsync<TModel>(
        string fileFullPath,
        List<string> allVins,
        int batchSize,
        Func<List<string>, IEnumerable<VehicleLookupDTO>, List<TModel>> transform,
        VehicleLookupRequestOptions requestOptions = null)
    {
        var file = new ParquetReportFile<TModel>(fileFullPath);

        foreach (var vinChunk in Chunk(allVins, batchSize))
        {
            var lookups = requestOptions is null
                ? await vehicleLookupService.LookupAsync(vinChunk)
                : await vehicleLookupService.LookupAsync(vinChunk, requestOptions);
            await file.AppendAsync(transform(vinChunk, lookups));
        }

        return (int)file.RowCount;
    }

    #endregion

    #region Transform Functions

    private static List<VehicleServiceItemReportModel> TransformServiceItemLookups(List<string> vinChunk, IEnumerable<VehicleLookupDTO> lookups)
    {
        var lookupByVin = FirstLookupByVin(lookups);
        var rows = new List<VehicleServiceItemReportModel>();
        foreach (var vin in vinChunk)
            if (lookupByVin.TryGetValue(vin, out var lookup))
                rows.AddRange(VehicleReportRows.ServiceItems(vin, lookup));
        return rows;
    }

    /// <summary>The first lookup returned for each VIN; a VIN the lookup did not answer is absent.</summary>
    private static Dictionary<string, VehicleLookupDTO> FirstLookupByVin(IEnumerable<VehicleLookupDTO> lookups)
    {
        var lookupByVin = new Dictionary<string, VehicleLookupDTO>(StringComparer.Ordinal);
        foreach (var lookup in lookups ?? Enumerable.Empty<VehicleLookupDTO>())
        {
            if (string.IsNullOrWhiteSpace(lookup?.VIN)) continue;
            var vinKey = NormalizeVin(lookup.VIN);
            if (!lookupByVin.ContainsKey(vinKey))
                lookupByVin[vinKey] = lookup;
        }
        return lookupByVin;
    }

    private static List<VehicleSscReportModel> TransformSscLookups(List<string> vinChunk, IEnumerable<VehicleLookupDTO> lookups)
    {
        var rows = new List<VehicleSscReportModel>();
        foreach (var lookup in lookups)
        {
            var vin = NormalizeVin(lookup?.VIN);
            if (string.IsNullOrWhiteSpace(vin)) continue;
            foreach (var ssc in lookup.SSC ?? Enumerable.Empty<SscDTO>())
                rows.Add(CreateSscRow(vin, ssc));
        }
        return rows;
    }

    private static List<VehicleLookupTopLevelReportModel> TransformTopLevelLookups(List<string> vinChunk, IEnumerable<VehicleLookupDTO> lookups)
    {
        var rows = new List<VehicleLookupTopLevelReportModel>();
        foreach (var lookup in lookups)
        {
            var vin = NormalizeVin(lookup?.VIN);
            if (string.IsNullOrWhiteSpace(vin)) continue;
            rows.Add(CreateVehicleLookupTopLevelRow(vin, lookup));
        }
        return rows;
    }

    private static List<VehicleServiceHistoryLaborReportModel> TransformServiceHistoryLaborLookups(List<string> vinChunk, IEnumerable<VehicleLookupDTO> lookups)
    {
        var rows = new List<VehicleServiceHistoryLaborReportModel>();
        foreach (var lookup in lookups)
        {
            var vin = NormalizeVin(lookup?.VIN);
            if (string.IsNullOrWhiteSpace(vin)) continue;
            foreach (var entry in lookup.ServiceHistory ?? Enumerable.Empty<VehicleServiceHistoryDTO>())
                foreach (var labor in entry?.LaborLines ?? Enumerable.Empty<VehicleLaborDTO>())
                    rows.Add(CreateServiceHistoryLaborRow(vin, entry, labor));
        }
        return rows;
    }

    private static List<VehicleServiceHistoryPartReportModel> TransformServiceHistoryPartLookups(List<string> vinChunk, IEnumerable<VehicleLookupDTO> lookups)
    {
        var rows = new List<VehicleServiceHistoryPartReportModel>();
        foreach (var lookup in lookups)
        {
            var vin = NormalizeVin(lookup?.VIN);
            if (string.IsNullOrWhiteSpace(vin)) continue;
            foreach (var entry in lookup.ServiceHistory ?? Enumerable.Empty<VehicleServiceHistoryDTO>())
                foreach (var part in entry?.PartLines ?? Enumerable.Empty<VehiclePartDTO>())
                    rows.Add(CreateServiceHistoryPartRow(vin, entry, part));
        }
        return rows;
    }

    #endregion

    private static async Task WriteCsvAsync<TModel>(string fileFullPath, IEnumerable<TModel> rows, ClassMap<TModel> classMap)
    {
        var outputDirectory = Path.GetDirectoryName(fileFullPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        using var writer = new StreamWriter(fileFullPath, false);
        using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csvWriter.Context.RegisterClassMap(classMap);
        await csvWriter.WriteRecordsAsync(rows);
    }

    private static async Task WriteParquetAsync<TModel>(string fileFullPath, IEnumerable<TModel> rows)
    {
        var file = new ParquetReportFile<TModel>(fileFullPath);
        await file.AppendAsync(rows?.ToList() ?? new List<TModel>());
        await file.CompleteAsync();
    }

    private static VehicleServiceItemReportModel CreateRow(string vin, VehicleServiceItemDTO item, DateTime? freeServiceItemStartDate) =>
        VehicleReportRows.ServiceItem(vin, item, freeServiceItemStartDate);

    private static string NormalizeVin(string vin) => VehicleReportRows.NormalizeVin(vin);

    private static VehicleSscReportModel CreateSscRow(string vin, SscDTO ssc) => VehicleReportRows.Ssc(vin, ssc);

    private static VehicleServiceHistoryLaborReportModel CreateServiceHistoryLaborRow(string vin, VehicleServiceHistoryDTO serviceHistoryEntry, VehicleLaborDTO labor) =>
        VehicleReportRows.ServiceHistoryLabor(vin, serviceHistoryEntry, labor);

    private static VehicleServiceHistoryPartReportModel CreateServiceHistoryPartRow(string vin, VehicleServiceHistoryDTO serviceHistoryEntry, VehiclePartDTO part) =>
        VehicleReportRows.ServiceHistoryPart(vin, serviceHistoryEntry, part);

    private static VehicleLookupTopLevelReportModel CreateVehicleLookupTopLevelRow(string vin, VehicleLookupDTO lookup) =>
        VehicleReportRows.TopLevel(vin, lookup);

    /// <summary>The service-items report's deduplication; lives in <see cref="VehicleReportRows"/>, kept here for callers that found it here.</summary>
    public static Dictionary<string, VehicleServiceItemDTO> BuildBestItemsByServiceId(IEnumerable<VehicleServiceItemDTO> items) =>
        VehicleReportRows.BuildBestItemsByServiceId(items);

    /// <summary>The report's service-item ordering; lives in <see cref="VehicleReportRows"/>, kept here for callers that found it here.</summary>
    public static readonly IComparer<string> ServiceItemIdComparer = VehicleReportRows.ServiceItemIdComparer;

    private sealed class VehicleServiceItemReportModelCsvMap : ClassMap<VehicleServiceItemReportModel>
    {
        public VehicleServiceItemReportModelCsvMap()
        {
            Map(x => x.VIN).Index(0);
            Map(x => x.FreeServiceItemStartDate).Index(1);
            Map(x => x.ServiceItemId).Index(2);
            Map(x => x.ServiceItemName).Index(3);
            Map(x => x.GroupName).Index(4);
            Map(x => x.GroupTabOrder).Index(5);
            Map(x => x.GroupIsDefault).Index(6);
            Map(x => x.GroupIsSequential).Index(7);
            Map(x => x.Status).Index(8);
            Map(x => x.StatusEnum).Index(9);
            Map(x => x.Type).Index(10);
            Map(x => x.TypeEnum).Index(11);
            Map(x => x.Price).Index(12);
            Map(x => x.MenuCode).Index(13);
            Map(x => x.ActivatedAt).Index(14);
            Map(x => x.ExpiresAt).Index(15);
            Map(x => x.ClaimDate).Index(16);
            Map(x => x.CampaignId).Index(17);
            Map(x => x.CampaignUniqueReference).Index(18);
            Map(x => x.ModelCostId).Index(19);
            Map(x => x.PaidServiceInvoiceLineId).Index(20);
            Map(x => x.CompanyName).Index(21);
            Map(x => x.InvoiceNumber).Index(22);
            Map(x => x.JobNumber).Index(23);
            Map(x => x.MaximumMileage).Index(24);
            Map(x => x.Claimable).Index(25);
            Map(x => x.ClaimingMethod).Index(26);
            Map(x => x.VehicleInspectionId).Index(27);
            Map(x => x.VehicleInspectionTypeId).Index(28);
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

    private sealed class VehicleLookupTopLevelReportModelCsvMap : ClassMap<VehicleLookupTopLevelReportModel>
    {
        public VehicleLookupTopLevelReportModelCsvMap()
        {
            Map(x => x.VIN).Index(0);
            Map(x => x.IsAuthorized).Index(1);
            Map(x => x.NextServiceDate).Index(2);
            Map(x => x.SSCLogId).Index(3);
            Map(x => x.BasicModelCode).Index(4);

            Map(x => x.IdentifiersVin).Index(5);
            Map(x => x.IdentifiersVariant).Index(6);
            Map(x => x.IdentifiersKatashiki).Index(7);
            Map(x => x.IdentifiersColor).Index(8);
            Map(x => x.IdentifiersTrim).Index(9);
            Map(x => x.IdentifiersBrandId).Index(10);

            Map(x => x.SaleCountryId).Index(11);
            Map(x => x.SaleCountryName).Index(12);
            Map(x => x.SaleCompanyId).Index(13);
            Map(x => x.SaleCompanyName).Index(14);
            Map(x => x.SaleBranchId).Index(15);
            Map(x => x.SaleBranchName).Index(16);
            Map(x => x.SaleCustomerAccountNumber).Index(17);
            Map(x => x.SaleCustomerId).Index(18);
            Map(x => x.SaleInvoiceDate).Index(19);
            Map(x => x.SaleWarrantyActivationDate).Index(20);
            Map(x => x.SaleInvoiceNumber).Index(21);
            Map(x => x.SaleRegionId).Index(22);

            Map(x => x.SaleBrokerId).Index(23);
            Map(x => x.SaleBrokerName).Index(24);
            Map(x => x.SaleBrokerInvoiceNumber).Index(25);
            Map(x => x.SaleBrokerInvoiceDate).Index(26);

            Map(x => x.SaleEndCustomerId).Index(27);
            Map(x => x.SaleEndCustomerName).Index(28);
            Map(x => x.SaleEndCustomerPhone).Index(29);
            Map(x => x.SaleEndCustomerIdNumber).Index(30);

            Map(x => x.WarrantyHasActiveWarranty).Index(31);
            Map(x => x.WarrantyStartDate).Index(32);
            Map(x => x.WarrantyEndDate).Index(33);
            Map(x => x.WarrantyActivationIsRequired).Index(34);
            Map(x => x.WarrantyHasExtendedWarranty).Index(35);
            Map(x => x.WarrantyExtendedStartDate).Index(36);
            Map(x => x.WarrantyExtendedEndDate).Index(37);
            Map(x => x.WarrantyFreeServiceStartDate).Index(38);

            Map(x => x.VariantInfoModelCode).Index(39);
            Map(x => x.VariantInfoSfx).Index(40);
            Map(x => x.VariantInfoModelYear).Index(41);

            Map(x => x.VehicleSpecModelCode).Index(42);
            Map(x => x.VehicleSpecModelYear).Index(43);
            Map(x => x.VehicleSpecProductionDate).Index(44);
            Map(x => x.VehicleSpecModelDescription).Index(45);
            Map(x => x.VehicleSpecVariantDescription).Index(46);
            Map(x => x.VehicleSpecClass).Index(47);
            Map(x => x.VehicleSpecBodyType).Index(48);
            Map(x => x.VehicleSpecEngine).Index(49);
            Map(x => x.VehicleSpecCylinders).Index(50);
            Map(x => x.VehicleSpecLightHeavyType).Index(51);
            Map(x => x.VehicleSpecDoors).Index(52);
            Map(x => x.VehicleSpecFuel).Index(53);
            Map(x => x.VehicleSpecTransmission).Index(54);
            Map(x => x.VehicleSpecSide).Index(55);
            Map(x => x.VehicleSpecEngineType).Index(56);
            Map(x => x.VehicleSpecTankCap).Index(57);
            Map(x => x.VehicleSpecStyle).Index(58);
            Map(x => x.VehicleSpecFuelLiter).Index(59);
            Map(x => x.VehicleSpecExteriorColor).Index(60);
            Map(x => x.VehicleSpecInteriorColor).Index(61);
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
