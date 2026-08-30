using CsvHelper;
using CsvHelper.Configuration;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System.Globalization;

namespace ShiftSoftware.ADP.Menus.Sample.FreeServiceParity;

/// <summary>
/// The comparison itself. Each batch is one REAL bulk vehicle lookup — the service menu attached with
/// <c>Include = true</c> and <c>FreeFilter = FreeOnly</c>, forced — so the free service items and the
/// free menu lines being compared come out of the same <c>VehicleLookupDTO</c>, produced by the same
/// pipeline the deployment serves.
///
/// <para><b>Matching is by menu code, and only by menu code.</b> The service-items system was filled
/// by hand from the exported menu, so the item's <c>PackageCode</c> and the generated line's
/// <c>Code</c> are transcriptions of the same identity — that equality IS the parity being audited.
/// After a code matches, the secondary properties (mileage, description, price) are compared and any
/// disagreement is reported on the row, but a differing property never breaks the match.</para>
/// </summary>
public class FreeServiceMenuParityAuditor(
    VehicleLookupService vehicleLookupService,
    IVehicleReportService vehicleReportService)
{
    /// <summary>
    /// Runs the comparison over <paramref name="vins"/> (or every distinct VIN in the store when null,
    /// capped by <paramref name="distinctVinCount"/>), streaming detail rows to
    /// <paramref name="csvPath"/> and returning the totals and per-VIN summaries.
    /// </summary>
    public async Task<FreeServiceParityReportModel> ExportToCsvAsync(
        string csvPath,
        IEnumerable<string>? vins = null,
        int? distinctVinCount = null,
        int batchSize = 1000,
        VehicleLookupRequestOptions? requestOptions = null)
    {
        var allVins = vins?
            .Select(NormalizeVin)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToList()
            ?? (await vehicleReportService.GetDistinctVinsAsync(distinctVinCount)).ToList();

        var report = new FreeServiceParityReportModel { RequestedVinCount = allVins.Count };

        if (allVins.Count == 0)
            return report;

        var effectiveOptions = BuildFreeMenuLookupOptions(requestOptions);

        var outputDirectory = Path.GetDirectoryName(csvPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        using var writer = new StreamWriter(csvPath, false);
        using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csvWriter.Context.RegisterClassMap<FreeServiceParityRowModelCsvMap>();

        for (var offset = 0; offset < allVins.Count; offset += batchSize)
        {
            var batch = allVins.GetRange(offset, Math.Min(batchSize, allVins.Count - offset));
            var lookups = await vehicleLookupService.LookupAsync(batch, effectiveOptions);

            var rows = new List<FreeServiceParityRowModel>();
            Accumulate(report, lookups, rows);

            await csvWriter.WriteRecordsAsync(rows);
            await writer.FlushAsync();
        }

        return report;
    }

    /// <summary>
    /// The options the parity lookups run under: the caller's language, broker-stock preference and
    /// menu country / transfer rate are honoured, the menu section itself is forced on with
    /// <c>FreeOnly</c>. A fresh instance so the caller's options are never mutated.
    /// </summary>
    private static VehicleLookupRequestOptions BuildFreeMenuLookupOptions(VehicleLookupRequestOptions? source)
    {
        return new VehicleLookupRequestOptions
        {
            LanguageCode = source?.LanguageCode ?? "en",
            IgnoreBrokerStock = source?.IgnoreBrokerStock ?? false,
            RequestingCompanyID = source?.RequestingCompanyID,
            ServiceMenuOptions = new VehicleServiceMenuRequestOptions
            {
                Include = true,
                FreeFilter = ServiceMenuFreeFilter.FreeOnly,
                CountryID = source?.ServiceMenuOptions?.CountryID,
                TransferRate = source?.ServiceMenuOptions?.TransferRate,
            },
        };
    }

    private static void Accumulate(
        FreeServiceParityReportModel report,
        IEnumerable<VehicleLookupDTO> lookups,
        List<FreeServiceParityRowModel> rowSink)
    {
        foreach (var lookup in lookups ?? Enumerable.Empty<VehicleLookupDTO>())
        {
            var vin = NormalizeVin(lookup?.VIN);
            if (string.IsNullOrWhiteSpace(vin) || lookup is null)
                continue;

            var freeItems = CollectFreeItems(lookup.ServiceItems);

            var menuStatus = lookup.ServiceMenu?.Status;

            var freeLines = menuStatus == VehicleServiceMenuStatus.Found
                ? (lookup.ServiceMenu.Services ?? new List<VehicleServiceMenuLineDTO>()).Where(x => x.IsFree).ToList()
                : new List<VehicleServiceMenuLineDTO>();

            var basicModelCode = lookup.BasicModelCode ?? lookup.ServiceMenu?.BasicModelCode ?? string.Empty;

            var summary = new FreeServiceParityVinSummaryModel
            {
                VIN = vin!,
                BasicModelCode = basicModelCode,
                MenuStatus = menuStatus,
                FreeServiceItemCount = freeItems.Count,
                FreeMenuLineCount = freeLines.Count,
            };

            var lineTaken = new bool[freeLines.Count];

            foreach (var item in freeItems)
            {
                var itemCode = item.PackageCode?.Trim();

                if (string.IsNullOrEmpty(itemCode))
                {
                    summary.ItemsWithoutMenuCodeCount++;
                    rowSink.Add(CreateRow(vin!, basicModelCode, menuStatus, FreeServiceParityMatchResult.FreeItemWithoutMenuCode, string.Empty, item, null));
                    continue;
                }

                var lineIndex = FindLineByCode(freeLines, lineTaken, itemCode);

                if (lineIndex < 0)
                {
                    summary.ItemsCodeUnmatchedCount++;
                    rowSink.Add(CreateRow(vin!, basicModelCode, menuStatus, FreeServiceParityMatchResult.FreeItemCodeUnmatched, string.Empty, item, null));
                    continue;
                }

                lineTaken[lineIndex] = true;
                var line = freeLines[lineIndex];
                var differences = DescribePropertyDifferences(item, line);

                if (differences.Length == 0)
                {
                    summary.MatchedCount++;
                    rowSink.Add(CreateRow(vin!, basicModelCode, menuStatus, FreeServiceParityMatchResult.Matched, string.Empty, item, line));
                }
                else
                {
                    summary.MatchedWithDifferencesCount++;
                    rowSink.Add(CreateRow(vin!, basicModelCode, menuStatus, FreeServiceParityMatchResult.MatchedWithDifferences, differences, item, line));
                }
            }

            for (var lineIndex = 0; lineIndex < freeLines.Count; lineIndex++)
            {
                if (lineTaken[lineIndex])
                    continue;

                summary.MenuLinesUnmatchedCount++;
                rowSink.Add(CreateRow(vin!, basicModelCode, menuStatus, FreeServiceParityMatchResult.MenuLineUnmatched, string.Empty, null, freeLines[lineIndex]));
            }

            summary.Outcome = ResolveOutcome(menuStatus, summary);

            report.VinCount++;
            report.TotalFreeServiceItems += summary.FreeServiceItemCount;
            report.TotalFreeMenuLines += summary.FreeMenuLineCount;
            report.TotalMatched += summary.MatchedCount;
            report.TotalMatchedWithDifferences += summary.MatchedWithDifferencesCount;
            report.TotalItemsWithoutMenuCode += summary.ItemsWithoutMenuCodeCount;
            report.TotalItemsCodeUnmatched += summary.ItemsCodeUnmatchedCount;
            report.TotalMenuLinesUnmatched += summary.MenuLinesUnmatchedCount;
            report.OutcomeCounts[summary.Outcome] = report.OutcomeCounts.TryGetValue(summary.Outcome, out var count) ? count + 1 : 1;
            report.VinSummaries.Add(summary);
        }
    }

    /// <summary>
    /// The VIN's free service items in the service-items report's own shape — the shared
    /// <see cref="DuckDBVehicleReportService.BuildBestItemsByServiceId"/> dedup (best row per
    /// <c>ServiceItemID</c>) — plus, appended, free items that carry no id at all, because "cannot be
    /// deduplicated" must not become "silently excluded from the comparison".
    /// </summary>
    private static List<VehicleServiceItemDTO> CollectFreeItems(IEnumerable<VehicleServiceItemDTO>? serviceItems)
    {
        var freeItems = (serviceItems ?? Enumerable.Empty<VehicleServiceItemDTO>())
            .Where(x => x?.TypeEnum == VehcileServiceItemTypes.Free)
            .ToList();

        var collected = DuckDBVehicleReportService.BuildBestItemsByServiceId(freeItems)
            .Values
            .OrderBy(x => x.ServiceItemID, DuckDBVehicleReportService.ServiceItemIdComparer)
            .ToList();

        collected.AddRange(freeItems.Where(x => string.IsNullOrWhiteSpace(x.ServiceItemID)));

        return collected;
    }

    private static int FindLineByCode(List<VehicleServiceMenuLineDTO> lines, bool[] taken, string itemCode)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (taken[i])
                continue;

            if (string.Equals(lines[i].Code?.Trim(), itemCode, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// The secondary comparison on a code-matched pair. Menu codes carry the identity; these carry the
    /// content — reported, never match-breaking. Cost is only compared when the item carries one.
    /// </summary>
    private static string DescribePropertyDifferences(VehicleServiceItemDTO item, VehicleServiceMenuLineDTO line)
    {
        var differences = new List<string>();

        if (item.MaximumMileage is not null && line.ServiceIntervalValueInMeter is not null
            && item.MaximumMileage.Value != line.ServiceIntervalValueInMeter.Value)
        {
            differences.Add($"Mileage: {item.MaximumMileage.Value} != {line.ServiceIntervalValueInMeter.Value}");
        }
        else if ((item.MaximumMileage is null) != (line.ServiceIntervalValueInMeter is null))
        {
            differences.Add($"Mileage: {(object?)item.MaximumMileage ?? "none"} != {(object?)line.ServiceIntervalValueInMeter ?? "none"}");
        }

        var itemName = item.Name?.Trim();
        var lineDescription = line.Description?.Trim();
        if (!string.IsNullOrEmpty(itemName) && !string.IsNullOrEmpty(lineDescription)
            && !string.Equals(itemName, lineDescription, StringComparison.OrdinalIgnoreCase))
        {
            differences.Add($"Description: '{itemName}' != '{lineDescription}'");
        }

        if (item.Cost is not null && item.Cost.Value != line.TotalPrice)
            differences.Add($"Price: {item.Cost.Value.ToString(CultureInfo.InvariantCulture)} != {line.TotalPrice.ToString(CultureInfo.InvariantCulture)}");

        return string.Join(" | ", differences);
    }

    private static FreeServiceParityVinOutcome ResolveOutcome(
        VehicleServiceMenuStatus? menuStatus,
        FreeServiceParityVinSummaryModel summary)
    {
        switch (menuStatus)
        {
            case VehicleServiceMenuStatus.NoBasicModelCode:
                return FreeServiceParityVinOutcome.NoBasicModelCode;
            case VehicleServiceMenuStatus.NotRegistered:
                return FreeServiceParityVinOutcome.MenuNotRegistered;
            case VehicleServiceMenuStatus.NotFound:
                return FreeServiceParityVinOutcome.MenuNotFound;
            case VehicleServiceMenuStatus.Unavailable:
            case null:
                return FreeServiceParityVinOutcome.MenuUnavailable;
        }

        if (summary.FreeServiceItemCount == 0 && summary.FreeMenuLineCount == 0)
            return FreeServiceParityVinOutcome.NothingFree;

        var fullyMatchedByCode =
            summary.ItemsWithoutMenuCodeCount == 0
            && summary.ItemsCodeUnmatchedCount == 0
            && summary.MenuLinesUnmatchedCount == 0;

        if (!fullyMatchedByCode)
            return FreeServiceParityVinOutcome.Mismatch;

        return summary.MatchedWithDifferencesCount == 0
            ? FreeServiceParityVinOutcome.Match
            : FreeServiceParityVinOutcome.MatchWithDifferences;
    }

    private static FreeServiceParityRowModel CreateRow(
        string vin,
        string basicModelCode,
        VehicleServiceMenuStatus? menuStatus,
        FreeServiceParityMatchResult result,
        string differences,
        VehicleServiceItemDTO? item,
        VehicleServiceMenuLineDTO? line)
    {
        return new FreeServiceParityRowModel
        {
            VIN = vin,
            BasicModelCode = basicModelCode,
            MenuStatus = menuStatus,
            MatchResult = result,
            Differences = differences,

            ServiceItemId = item?.ServiceItemID?.Trim() ?? string.Empty,
            ServiceItemName = item?.Name ?? string.Empty,
            ItemMenuCode = item?.PackageCode ?? string.Empty,
            ItemMaximumMileage = item?.MaximumMileage,
            ItemCost = item?.Cost,
            ItemStatus = item?.Status ?? string.Empty,
            ItemStatusEnum = item?.StatusEnum,
            ItemClaimable = item?.Claimable,
            ItemActivatedAt = item is null || item.ActivatedAt == default ? null : item.ActivatedAt,
            ItemExpiresAt = item?.ExpiresAt,
            ItemClaimDate = item?.ClaimDate,

            MenuVariantId = line?.VariantID,
            MenuVariantName = line?.VariantName ?? string.Empty,
            MenuLineKey = line?.LineKey ?? string.Empty,
            MenuLineCode = line?.Code ?? string.Empty,
            MenuLabourCode = line?.LabourCode ?? string.Empty,
            MenuDescription = line?.Description ?? string.Empty,
            MenuLineType = line?.LineType,
            MenuIsStandalone = line?.IsStandalone,
            MenuIntervalKm = line?.ServiceIntervalValueInMeter,
            MenuTotalPrice = line?.TotalPrice,
        };
    }

    private static string? NormalizeVin(string? vin) => vin?.Trim()?.ToUpperInvariant();

    private sealed class FreeServiceParityRowModelCsvMap : ClassMap<FreeServiceParityRowModel>
    {
        public FreeServiceParityRowModelCsvMap()
        {
            Map(x => x.VIN).Index(0);
            Map(x => x.BasicModelCode).Index(1);
            Map(x => x.MenuStatus).Index(2);
            Map(x => x.MatchResult).Index(3);
            Map(x => x.Differences).Index(4);

            Map(x => x.ServiceItemId).Index(5);
            Map(x => x.ServiceItemName).Index(6);
            Map(x => x.ItemMenuCode).Index(7);
            Map(x => x.ItemMaximumMileage).Index(8);
            Map(x => x.ItemCost).Index(9);
            Map(x => x.ItemStatus).Index(10);
            Map(x => x.ItemStatusEnum).Index(11);
            Map(x => x.ItemClaimable).Index(12);
            Map(x => x.ItemActivatedAt).Index(13);
            Map(x => x.ItemExpiresAt).Index(14);
            Map(x => x.ItemClaimDate).Index(15);

            Map(x => x.MenuVariantId).Index(16);
            Map(x => x.MenuVariantName).Index(17);
            Map(x => x.MenuLineKey).Index(18);
            Map(x => x.MenuLineCode).Index(19);
            Map(x => x.MenuLabourCode).Index(20);
            Map(x => x.MenuDescription).Index(21);
            Map(x => x.MenuLineType).Index(22);
            Map(x => x.MenuIsStandalone).Index(23);
            Map(x => x.MenuIntervalKm).Index(24);
            Map(x => x.MenuTotalPrice).Index(25);
        }
    }
}
