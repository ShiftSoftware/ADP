using CsvHelper;
using CsvHelper.Configuration;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System.Globalization;

namespace ShiftSoftware.ADP.Menus.Sample.FreeServiceParity;

/// <summary>
/// The audit itself. Each batch is one REAL bulk vehicle lookup — the service menu attached with
/// <c>Include = true</c> and <c>FreeFilter = All</c>, so EVERY variant's generated lines are on the
/// table — and the free service items and the menu being compared come out of the same
/// <c>VehicleLookupDTO</c>, produced by the same pipeline the deployment serves.
///
/// <para><b>Scope: what is still on the table.</b> A VIN enters the comparison only when at least one
/// of its FREE service items is <c>Pending</c>. A vehicle whose free entitlements have all been
/// processed, expired or cancelled — and one carrying no free items at all — is skipped whole: no
/// detail rows, no share of any total. Those are history, transcribed against older menu exports and
/// often without a menu code, and nothing the menu side has to reproduce. On a VIN that IS in scope,
/// EVERY free item is compared whatever its own status — its pending siblings say the record is
/// current, so its spent ones are still evidence about the same transcription.</para>
///
/// <para><b>One direction, one key.</b> Each FREE service item looks for its match among the model's
/// generated menu lines by MENU CODE — the item's <c>PackageCode</c> is a hand transcription of the
/// generated <c>Code</c>, and that equality is the parity being audited. The free-of-charge flag is
/// not consulted (it is not authored yet), lines are not consumed (a catalog line can answer any
/// number of entitlements), and menu lines no item points at are expected — the menu also prices paid
/// work — so they are never counted against parity. After a code matches, the secondary properties
/// (mileage, description, price) are compared and any disagreement is reported on the row, but a
/// differing property never breaks the match.</para>
/// </summary>
public class FreeServiceMenuParityAuditor(
    VehicleLookupService vehicleLookupService,
    IVehicleReportService vehicleReportService)
{
    /// <summary>
    /// Runs the audit over <paramref name="vins"/> (or every distinct VIN in the store when null,
    /// capped by <paramref name="distinctVinCount"/>), streaming detail rows to
    /// <paramref name="csvPath"/> and returning the totals and per-VIN summaries — for the VINs in
    /// scope only; the ones with nothing pending survive as the report's skipped counts.
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

        var effectiveOptions = BuildMenuLookupOptions(requestOptions);

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
    /// The options the audit's lookups run under: the caller's language, broker-stock preference and
    /// menu country / transfer rate are honoured, the menu section is forced on — with the WHOLE menu,
    /// every variant, because the item's code must be findable wherever it was transcribed from.
    /// A fresh instance so the caller's options are never mutated.
    /// </summary>
    private static VehicleLookupRequestOptions BuildMenuLookupOptions(VehicleLookupRequestOptions? source)
    {
        return new VehicleLookupRequestOptions
        {
            LanguageCode = source?.LanguageCode ?? "en",
            IgnoreBrokerStock = source?.IgnoreBrokerStock ?? false,
            RequestingCompanyID = source?.RequestingCompanyID,
            ServiceMenuOptions = new VehicleServiceMenuRequestOptions
            {
                Include = true,
                FreeFilter = ServiceMenuFreeFilter.All,
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

            // The scope gate. Nothing pending — every free item processed, expired or cancelled, or
            // no free item at all — means the vehicle's entitlements are spent history, and the
            // migration is not asked to reproduce them. Skip it before a single row is written, so
            // the CSV and every number in the report describe the same live population.
            var pendingFreeItemCount = freeItems.Count(x => x.StatusEnum == VehcileServiceItemStatuses.Pending);

            if (pendingFreeItemCount == 0)
            {
                report.SkippedVinCount++;
                report.SkippedFreeServiceItems += freeItems.Count;

                if (freeItems.Count == 0)
                    report.SkippedVinsWithoutFreeItems++;
                else
                    report.SkippedVinsWithoutPendingFreeItems++;

                continue;
            }

            var menuStatus = lookup.ServiceMenu?.Status;

            var menuLines = menuStatus == VehicleServiceMenuStatus.Found
                ? lookup.ServiceMenu.Services ?? new List<VehicleServiceMenuLineDTO>()
                : new List<VehicleServiceMenuLineDTO>();

            var basicModelCode = lookup.BasicModelCode ?? lookup.ServiceMenu?.BasicModelCode ?? string.Empty;

            var summary = new FreeServiceParityVinSummaryModel
            {
                VIN = vin!,
                BasicModelCode = basicModelCode,
                MenuStatus = menuStatus,
                FreeServiceItemCount = freeItems.Count,
                PendingFreeServiceItemCount = pendingFreeItemCount,
                MenuLineCount = menuLines.Count,
            };

            foreach (var item in freeItems)
            {
                var itemCode = item.PackageCode?.Trim();

                if (string.IsNullOrEmpty(itemCode))
                {
                    summary.ItemsWithoutMenuCodeCount++;
                    rowSink.Add(CreateRow(vin!, basicModelCode, menuStatus, FreeServiceParityMatchResult.FreeItemWithoutMenuCode, string.Empty, item, null));
                    continue;
                }

                // Not consumed: a menu line is a catalog entry, and any number of entitlements may
                // legitimately point at it.
                var line = menuLines.FirstOrDefault(x => string.Equals(x.Code?.Trim(), itemCode, StringComparison.OrdinalIgnoreCase));

                if (line is null)
                {
                    summary.ItemsCodeUnmatchedCount++;
                    rowSink.Add(CreateRow(vin!, basicModelCode, menuStatus, FreeServiceParityMatchResult.FreeItemCodeUnmatched, string.Empty, item, null));
                    continue;
                }

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

            summary.Outcome = ResolveOutcome(menuStatus, summary);

            report.VinCount++;
            report.TotalFreeServiceItems += summary.FreeServiceItemCount;
            report.TotalPendingFreeServiceItems += summary.PendingFreeServiceItemCount;
            report.TotalMenuLines += summary.MenuLineCount;
            report.TotalMatched += summary.MatchedCount;
            report.TotalMatchedWithDifferences += summary.MatchedWithDifferencesCount;
            report.TotalItemsWithoutMenuCode += summary.ItemsWithoutMenuCodeCount;
            report.TotalItemsCodeUnmatched += summary.ItemsCodeUnmatchedCount;
            report.OutcomeCounts[summary.Outcome] = report.OutcomeCounts.TryGetValue(summary.Outcome, out var count) ? count + 1 : 1;
            report.VinSummaries.Add(summary);
        }
    }

    /// <summary>
    /// The VIN's free service items in the service-items report's own shape — the shared
    /// <see cref="DuckDBVehicleReportService.BuildBestItemsByServiceId"/> dedup (best row per
    /// <c>ServiceItemID</c>) — plus, appended, free items that carry no id at all, because "cannot be
    /// deduplicated" must not become "silently excluded from the audit".
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
        // Every VIN reaching here carries at least one pending free item — the scope gate dropped the
        // rest — so there is always something to look up.
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

        if (summary.ItemsWithoutMenuCodeCount > 0 || summary.ItemsCodeUnmatchedCount > 0)
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
        VehicleServiceItemDTO item,
        VehicleServiceMenuLineDTO? line)
    {
        return new FreeServiceParityRowModel
        {
            VIN = vin,
            BasicModelCode = basicModelCode,
            MenuStatus = menuStatus,
            MatchResult = result,
            Differences = differences,

            ServiceItemId = item.ServiceItemID?.Trim() ?? string.Empty,
            ServiceItemName = item.Name ?? string.Empty,
            ItemMenuCode = item.PackageCode ?? string.Empty,
            ItemMaximumMileage = item.MaximumMileage,
            ItemCost = item.Cost,
            ItemStatus = item.Status ?? string.Empty,
            ItemStatusEnum = item.StatusEnum,
            ItemClaimable = item.Claimable,
            ItemActivatedAt = item.ActivatedAt == default ? null : item.ActivatedAt,
            ItemExpiresAt = item.ExpiresAt,
            ItemClaimDate = item.ClaimDate,

            MenuVariantId = line?.VariantID,
            MenuVariantName = line?.VariantName ?? string.Empty,
            MenuVariantIsFree = line?.IsFree,
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

    /// <summary>
    /// Column order is for HUMAN reading: after the row's identity and verdict, every comparable pair
    /// sits side by side — the item's value immediately left of the menu's value it was compared to
    /// (code | code, mileage | interval, name | description, cost | price) — so a scan across two
    /// adjacent cells IS the comparison. The single-sided context columns follow, item's then menu's.
    /// </summary>
    private sealed class FreeServiceParityRowModelCsvMap : ClassMap<FreeServiceParityRowModel>
    {
        public FreeServiceParityRowModelCsvMap()
        {
            Map(x => x.VIN).Index(0);
            Map(x => x.BasicModelCode).Index(1);
            Map(x => x.MenuStatus).Index(2);
            Map(x => x.MatchResult).Index(3);
            Map(x => x.Differences).Index(4);

            // ---- the compared pairs, side by side: item | menu ----
            Map(x => x.ItemMenuCode).Index(5);
            Map(x => x.MenuLineCode).Index(6);
            Map(x => x.ItemMaximumMileage).Index(7);
            Map(x => x.MenuIntervalKm).Index(8);
            Map(x => x.ServiceItemName).Index(9);
            Map(x => x.MenuDescription).Index(10);
            Map(x => x.ItemCost).Index(11);
            Map(x => x.MenuTotalPrice).Index(12);

            // ---- item-only context ----
            Map(x => x.ServiceItemId).Index(13);
            Map(x => x.ItemStatus).Index(14);
            Map(x => x.ItemStatusEnum).Index(15);
            Map(x => x.ItemClaimable).Index(16);
            Map(x => x.ItemActivatedAt).Index(17);
            Map(x => x.ItemExpiresAt).Index(18);
            Map(x => x.ItemClaimDate).Index(19);

            // ---- menu-only context ----
            Map(x => x.MenuVariantId).Index(20);
            Map(x => x.MenuVariantName).Index(21);
            Map(x => x.MenuVariantIsFree).Index(22);
            Map(x => x.MenuLineKey).Index(23);
            Map(x => x.MenuLabourCode).Index(24);
            Map(x => x.MenuLineType).Index(25);
            Map(x => x.MenuIsStandalone).Index(26);
        }
    }
}
