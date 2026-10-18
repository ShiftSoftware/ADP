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
/// <c>Include = true</c> and <c>FreeFilter = FreeOnly</c>, so only the variants flagged free are
/// generated — and the free service items and the menu being compared come out of the same
/// <c>VehicleLookupDTO</c>, produced by the same pipeline the deployment serves.
///
/// <para><b>Scope, in the order it is applied.</b>
/// (1) <b>Invoice date</b> — when a date is given, a VIN whose sale invoice date is before it is
/// skipped; a VIN with no invoice date stays in. (2) <b>Conditional items</b> — free items whose
/// catalog entry carries eligibility conditions are set aside: they are offered by rule, not
/// transcribed from the menu. (3) <b>Pending</b> — a VIN enters the comparison only when at least one
/// of its remaining free items is <c>Pending</c>; one whose entitlements are all processed, expired or
/// cancelled, or that carries none, is skipped whole. On a VIN that IS in scope every unconditional
/// free item is compared, whatever its own status.</para>
///
/// <para><b>One direction, two properties.</b> Each free service item looks for a free menu line with
/// its MENU CODE — the item's <c>PackageCode</c> is a hand transcription of the generated <c>Code</c> —
/// and that line's service interval must be the item's MAXIMUM MILEAGE. Nothing else is compared.
/// Lines are not consumed (a catalog line can answer any number of entitlements), and free menu lines
/// no item points at are never counted against parity.</para>
/// </summary>
public class FreeServiceMenuParityAuditor(
    VehicleLookupService vehicleLookupService,
    IVehicleReportService vehicleReportService,
    IVehicleLookupStorageService vehicleLookupStorageService)
{
    /// <summary>
    /// Runs the audit over <paramref name="vins"/> (or every distinct VIN in the store when null,
    /// capped by <paramref name="distinctVinCount"/>), streaming detail rows to
    /// <paramref name="csvPath"/> and returning the totals and per-VIN summaries — for the VINs in
    /// scope only; the rest survive as the report's skipped counts.
    /// </summary>
    /// <param name="invoiceDateFrom">
    /// Only VINs invoiced on or after this date (by date, time ignored), or never invoiced, are compared.
    /// Null compares every VIN.
    /// </param>
    public async Task<FreeServiceParityReportModel> ExportToCsvAsync(
        string csvPath,
        IEnumerable<string>? vins = null,
        int? distinctVinCount = null,
        int batchSize = 1000,
        VehicleLookupRequestOptions? requestOptions = null,
        DateTime? invoiceDateFrom = null)
    {
        var allVins = vins?
            .Select(NormalizeVin)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToList()
            ?? (await vehicleReportService.GetDistinctVinsAsync(distinctVinCount)).ToList();

        var report = new FreeServiceParityReportModel { RequestedVinCount = allVins.Count };

        var outputDirectory = Path.GetDirectoryName(csvPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        using var writer = new StreamWriter(csvPath, false);
        using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csvWriter.Context.RegisterClassMap<FreeServiceParityRowModelCsvMap>();

        // The header goes out even when nothing is compared, so an empty run still opens as a sheet.
        csvWriter.WriteHeader<FreeServiceParityRowModel>();
        await csvWriter.NextRecordAsync();

        if (allVins.Count == 0)
            return report;

        var conditionalServiceItemIds = await LoadConditionalServiceItemIdsAsync();
        var effectiveOptions = BuildMenuLookupOptions(requestOptions);

        for (var offset = 0; offset < allVins.Count; offset += batchSize)
        {
            var batch = allVins.GetRange(offset, Math.Min(batchSize, allVins.Count - offset));
            var lookups = await vehicleLookupService.LookupAsync(batch, effectiveOptions);

            var rows = new List<FreeServiceParityRowModel>();
            Accumulate(report, lookups, rows, invoiceDateFrom?.Date, conditionalServiceItemIds);

            await csvWriter.WriteRecordsAsync(rows);
            await writer.FlushAsync();
        }

        return report;
    }

    /// <summary>
    /// The ids (as the lookup reports them — the catalog's <c>IntegrationID</c>) of every catalog
    /// service item that carries eligibility conditions.
    /// </summary>
    private async Task<HashSet<string>> LoadConditionalServiceItemIdsAsync()
    {
        var catalog = await vehicleLookupStorageService.GetServiceItemsAsync(useCache: true);

        return catalog
            .Where(x => x.EligibilityConditions?.Any() == true && !string.IsNullOrWhiteSpace(x.IntegrationID))
            .Select(x => x.IntegrationID.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The options the audit's lookups run under: the caller's language, broker-stock preference and
    /// menu country / transfer rate are honoured, the menu section is forced on — the FREE variants
    /// only. A fresh instance so the caller's options are never mutated.
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
                FreeFilter = ServiceMenuFreeFilter.FreeOnly,
                CountryID = source?.ServiceMenuOptions?.CountryID,
                TransferRate = source?.ServiceMenuOptions?.TransferRate,
            },
        };
    }

    private static void Accumulate(
        FreeServiceParityReportModel report,
        IEnumerable<VehicleLookupDTO> lookups,
        List<FreeServiceParityRowModel> rowSink,
        DateTime? invoiceDateFrom,
        HashSet<string> conditionalServiceItemIds)
    {
        foreach (var lookup in lookups ?? Enumerable.Empty<VehicleLookupDTO>())
        {
            var vin = NormalizeVin(lookup?.VIN);
            if (string.IsNullOrWhiteSpace(vin) || lookup is null)
                continue;

            // (1) The invoice-date gate. A VIN with no invoice date has not been sold through the
            // path that stamps one, which says nothing about its age — it stays in.
            var invoiceDate = lookup.SaleInformation?.InvoiceDate;

            if (invoiceDateFrom is not null && invoiceDate is not null && invoiceDate.Value.Date < invoiceDateFrom.Value)
            {
                report.SkippedVinCount++;
                report.SkippedVinsInvoicedBeforeDate++;
                continue;
            }

            // (2) Conditional items are set aside before the pending gate, so a VIN whose only
            // pending entitlement is conditional has nothing transcribed to audit.
            var allFreeItems = CollectFreeItems(lookup.ServiceItems);
            var freeItems = allFreeItems.Where(x => !IsConditional(x, conditionalServiceItemIds)).ToList();
            report.ExcludedConditionalFreeItems += allFreeItems.Count - freeItems.Count;

            // (3) The pending gate. Nothing pending means the vehicle's entitlements are spent
            // history, and the migration is not asked to reproduce them. Skip it before a single row
            // is written, so the CSV and every number in the summary describe the same population.
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
                ? lookup.ServiceMenu?.Services ?? new List<VehicleServiceMenuLineDTO>()
                : new List<VehicleServiceMenuLineDTO>();

            var basicModelCode = lookup.BasicModelCode ?? lookup.ServiceMenu?.BasicModelCode ?? string.Empty;

            var summary = new FreeServiceParityVinSummaryModel
            {
                VIN = vin!,
                BasicModelCode = basicModelCode,
                MenuStatus = menuStatus,
                FreeServiceItemCount = freeItems.Count,
                PendingFreeServiceItemCount = pendingFreeItemCount,
                FreeMenuLineCount = menuLines.Count,
            };

            FreeServiceParityRowModel Row(FreeServiceParityMatchResult result, VehicleServiceItemDTO item, VehicleServiceMenuLineDTO? line) =>
                CreateRow(vin!, basicModelCode, invoiceDate, menuStatus, result, item, line);

            foreach (var item in freeItems)
            {
                var itemCode = item.PackageCode?.Trim();

                if (string.IsNullOrEmpty(itemCode))
                {
                    summary.ItemsWithoutMenuCodeCount++;
                    rowSink.Add(Row(FreeServiceParityMatchResult.FreeItemWithoutMenuCode, item, null));
                    continue;
                }

                if (menuLines.Count == 0)
                {
                    summary.ItemsWithoutFreeMenuCount++;
                    rowSink.Add(Row(FreeServiceParityMatchResult.NoFreeMenu, item, null));
                    continue;
                }

                // Not consumed: a menu line is a catalog entry, and any number of entitlements may
                // legitimately point at it.
                var sameCode = menuLines
                    .Where(x => string.Equals(x.Code?.Trim(), itemCode, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (sameCode.Count == 0)
                {
                    summary.ItemsCodeUnmatchedCount++;
                    rowSink.Add(Row(FreeServiceParityMatchResult.FreeItemCodeUnmatched, item, null));
                    continue;
                }

                // One code can be generated by more than one free variant; the pair holds when ANY of
                // them carries the item's mileage. Otherwise the first is shown beside the item.
                var line = sameCode.FirstOrDefault(x => MileageAgrees(item, x));

                if (line is not null)
                {
                    summary.MatchedCount++;
                    rowSink.Add(Row(FreeServiceParityMatchResult.Matched, item, line));
                }
                else
                {
                    summary.MileageMismatchCount++;
                    rowSink.Add(Row(FreeServiceParityMatchResult.MileageMismatch, item, sameCode[0]));
                }
            }

            summary.Outcome = ResolveOutcome(menuStatus, summary);

            report.VinCount++;
            report.TotalFreeServiceItems += summary.FreeServiceItemCount;
            report.TotalPendingFreeServiceItems += summary.PendingFreeServiceItemCount;
            report.TotalFreeMenuLines += summary.FreeMenuLineCount;
            report.TotalMatched += summary.MatchedCount;
            report.TotalMileageMismatch += summary.MileageMismatchCount;
            report.TotalItemsWithoutMenuCode += summary.ItemsWithoutMenuCodeCount;
            report.TotalItemsCodeUnmatched += summary.ItemsCodeUnmatchedCount;
            report.TotalItemsWithoutFreeMenu += summary.ItemsWithoutFreeMenuCount;
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

    private static bool IsConditional(VehicleServiceItemDTO item, HashSet<string> conditionalServiceItemIds) =>
        !string.IsNullOrWhiteSpace(item.ServiceItemID) && conditionalServiceItemIds.Contains(item.ServiceItemID.Trim());

    /// <summary>The item's maximum mileage against the line's service interval; two absent values agree.</summary>
    private static bool MileageAgrees(VehicleServiceItemDTO item, VehicleServiceMenuLineDTO line) =>
        item.MaximumMileage == line.ServiceIntervalValueInMeter;

    private static FreeServiceParityVinOutcome ResolveOutcome(
        VehicleServiceMenuStatus? menuStatus,
        FreeServiceParityVinSummaryModel summary)
    {
        // Every VIN reaching here carries at least one pending free item — the scope gates dropped the
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

        // Found, but the free filter left nothing: the model's menu has no variant flagged free.
        if (summary.FreeMenuLineCount == 0)
            return FreeServiceParityVinOutcome.NoFreeMenu;

        return summary.MatchedCount == summary.FreeServiceItemCount
            ? FreeServiceParityVinOutcome.Match
            : FreeServiceParityVinOutcome.Mismatch;
    }

    private static FreeServiceParityRowModel CreateRow(
        string vin,
        string basicModelCode,
        DateTime? invoiceDate,
        VehicleServiceMenuStatus? menuStatus,
        FreeServiceParityMatchResult result,
        VehicleServiceItemDTO item,
        VehicleServiceMenuLineDTO? line)
    {
        return new FreeServiceParityRowModel
        {
            VIN = vin,
            BasicModelCode = basicModelCode,
            InvoiceDate = invoiceDate,
            MenuStatus = menuStatus,
            MatchResult = result,

            ServiceItemId = item.ServiceItemID?.Trim() ?? string.Empty,
            ServiceItemName = item.Name ?? string.Empty,
            ItemMenuCode = item.PackageCode ?? string.Empty,
            ItemMaximumMileage = item.MaximumMileage,
            ItemStatus = item.Status ?? string.Empty,
            ItemStatusEnum = item.StatusEnum,
            ItemClaimable = item.Claimable,
            ItemActivatedAt = item.ActivatedAt == default ? null : item.ActivatedAt,
            ItemExpiresAt = item.ExpiresAt,
            ItemClaimDate = item.ClaimDate,

            MenuVariantId = line?.VariantID,
            MenuVariantName = line?.VariantName ?? string.Empty,
            MenuLineKey = line?.LineKey ?? string.Empty,
            MenuLineCode = line?.Code ?? string.Empty,
            MenuLabourCode = line?.LabourCode ?? string.Empty,
            MenuDescription = line?.Description ?? string.Empty,
            MenuLineType = line?.LineType,
            MenuIsStandalone = line?.IsStandalone,
            MenuIntervalKm = line?.ServiceIntervalValueInMeter,
        };
    }

    private static string? NormalizeVin(string? vin) => vin?.Trim()?.ToUpperInvariant();

    /// <summary>
    /// Column order is for HUMAN reading: after the row's identity and verdict, the two compared pairs
    /// sit side by side — the item's value immediately left of the menu's (code | code,
    /// mileage | interval) — so a scan across two adjacent cells IS the comparison. The single-sided
    /// context columns follow, item's then menu's.
    /// </summary>
    private sealed class FreeServiceParityRowModelCsvMap : ClassMap<FreeServiceParityRowModel>
    {
        public FreeServiceParityRowModelCsvMap()
        {
            Map(x => x.VIN).Index(0);
            Map(x => x.BasicModelCode).Index(1);
            Map(x => x.InvoiceDate).Index(2).TypeConverterOption.Format("yyyy-MM-dd");
            Map(x => x.MenuStatus).Index(3);
            Map(x => x.MatchResult).Index(4);

            // ---- the compared pairs, side by side: item | menu ----
            Map(x => x.ItemMenuCode).Index(5);
            Map(x => x.MenuLineCode).Index(6);
            Map(x => x.ItemMaximumMileage).Index(7);
            Map(x => x.MenuIntervalKm).Index(8);

            // ---- item-only context ----
            Map(x => x.ServiceItemId).Index(9);
            Map(x => x.ServiceItemName).Index(10);
            Map(x => x.ItemStatus).Index(11);
            Map(x => x.ItemStatusEnum).Index(12);
            Map(x => x.ItemClaimable).Index(13);
            Map(x => x.ItemActivatedAt).Index(14);
            Map(x => x.ItemExpiresAt).Index(15);
            Map(x => x.ItemClaimDate).Index(16);

            // ---- menu-only context ----
            Map(x => x.MenuDescription).Index(17);
            Map(x => x.MenuVariantId).Index(18);
            Map(x => x.MenuVariantName).Index(19);
            Map(x => x.MenuLineKey).Index(20);
            Map(x => x.MenuLabourCode).Index(21);
            Map(x => x.MenuLineType).Index(22);
            Map(x => x.MenuIsStandalone).Index(23);
        }
    }
}
