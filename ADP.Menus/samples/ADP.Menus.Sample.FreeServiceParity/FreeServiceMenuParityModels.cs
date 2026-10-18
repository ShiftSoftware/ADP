using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;

namespace ShiftSoftware.ADP.Menus.Sample.FreeServiceParity;

/// <summary>
/// How one free service item resolved against its model's FREE menu.
///
/// <para>The service-items system was filled BY HAND from the exported menu, and the menu lookup exists
/// to make that manual step unnecessary — so the identity that says "this entitlement is that menu
/// service" is the MENU CODE: the item's <c>PackageCode</c> was transcribed from the very <c>Code</c>
/// the menu generator produces. The audit is ONE-WAY and compares exactly two things: the item's code
/// must be a line of the model's free menu (the variants flagged free), and that line's service
/// interval must be the item's mileage. Menu lines no free item points at are never counted against
/// parity.</para>
/// </summary>
public enum FreeServiceParityMatchResult
{
    /// <summary>The item's menu code is a free menu line, and that line's interval is the item's mileage.</summary>
    Matched = 0,

    /// <summary>The item's menu code is a free menu line, but no line with that code carries the item's mileage.</summary>
    MileageMismatch = 1,

    /// <summary>The free service item carries NO menu code at all, so it cannot be looked up.</summary>
    FreeItemWithoutMenuCode = 2,

    /// <summary>The free service item carries a menu code, but the free menu generated no line with that code.</summary>
    FreeItemCodeUnmatched = 3,

    /// <summary>The item carries a code, but the VIN has no free menu lines to look it up in — see the row's menu status.</summary>
    NoFreeMenu = 4,
}

/// <summary>One VIN's overall verdict.</summary>
public enum FreeServiceParityVinOutcome
{
    /// <summary>Every free item found a free menu line by code, at the item's mileage.</summary>
    Match = 0,

    /// <summary>At least one free item has no code, a code the free menu did not generate, or a mileage the line does not carry.</summary>
    Mismatch = 1,

    /// <summary>The model has a menu, but none of its variants is flagged free — there is nothing to compare against.</summary>
    NoFreeMenu = 2,

    /// <summary>The VIN has free items but no menu is authored under its derived basic model code.</summary>
    MenuNotFound = 3,

    /// <summary>The VIN has free items but the menu subsystem could not be consulted.</summary>
    MenuUnavailable = 4,

    /// <summary>No menu lookup registered (should not appear in this audit).</summary>
    MenuNotRegistered = 5,

    /// <summary>The VIN has free items but no Katashiki to derive a model code from.</summary>
    NoBasicModelCode = 6,
}

/// <summary>
/// One detail row — one free service item's resolution. A row whose code found a free menu line carries
/// that line; the others carry only the item's columns.
/// </summary>
public class FreeServiceParityRowModel
{
    public string VIN { get; set; } = string.Empty;
    public string BasicModelCode { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public VehicleServiceMenuStatus? MenuStatus { get; set; }
    public FreeServiceParityMatchResult MatchResult { get; set; }

    // ---- free service item side (vehicle lookup ServiceItems, TypeEnum == Free, unconditional) ----
    public string ServiceItemId { get; set; } = string.Empty;
    public string ServiceItemName { get; set; } = string.Empty;
    public string ItemMenuCode { get; set; } = string.Empty;
    public long? ItemMaximumMileage { get; set; }
    public string ItemStatus { get; set; } = string.Empty;
    public VehcileServiceItemStatuses? ItemStatusEnum { get; set; }
    public bool? ItemClaimable { get; set; }
    public DateTime? ItemActivatedAt { get; set; }
    public DateTime? ItemExpiresAt { get; set; }
    public DateTimeOffset? ItemClaimDate { get; set; }

    // ---- the free menu line the item's code found (vehicle lookup ServiceMenu, FreeOnly) ----
    public long? MenuVariantId { get; set; }
    public string MenuVariantName { get; set; } = string.Empty;
    public string MenuLineKey { get; set; } = string.Empty;
    public string MenuLineCode { get; set; } = string.Empty;
    public string MenuLabourCode { get; set; } = string.Empty;
    public string MenuDescription { get; set; } = string.Empty;
    public ServiceMenuLineType? MenuLineType { get; set; }
    public bool? MenuIsStandalone { get; set; }
    public int? MenuIntervalKm { get; set; }
}

/// <summary>One VIN's roll-up.</summary>
public class FreeServiceParityVinSummaryModel
{
    public string VIN { get; set; } = string.Empty;
    public string BasicModelCode { get; set; } = string.Empty;
    public VehicleServiceMenuStatus? MenuStatus { get; set; }
    public FreeServiceParityVinOutcome Outcome { get; set; }

    /// <summary>Unconditional free items compared — conditional ones are set aside before anything else.</summary>
    public int FreeServiceItemCount { get; set; }

    /// <summary>How many of them are still <c>Pending</c> — at least one, or the VIN would have been skipped.</summary>
    public int PendingFreeServiceItemCount { get; set; }

    /// <summary>How many lines the free menu generated — context, never counted against parity.</summary>
    public int FreeMenuLineCount { get; set; }

    public int MatchedCount { get; set; }
    public int MileageMismatchCount { get; set; }
    public int ItemsWithoutMenuCodeCount { get; set; }
    public int ItemsCodeUnmatchedCount { get; set; }
    public int ItemsWithoutFreeMenuCount { get; set; }
}

/// <summary>
/// The whole run: per-VIN summaries plus fleet totals, over the VINs IN SCOPE (invoiced on or after the
/// run's invoice date, or never invoiced, and carrying at least one pending unconditional free item);
/// the skipped counts are the only trace the rest leave. Detail rows are streamed to the CSV, never
/// held here, so a full-population run stays memory-bounded.
/// </summary>
public class FreeServiceParityReportModel
{
    public int RequestedVinCount { get; set; }

    /// <summary>VINs actually compared.</summary>
    public int VinCount { get; set; }

    /// <summary>VINs skipped whole (split by the three counts below).</summary>
    public int SkippedVinCount { get; set; }

    /// <summary>Of the skipped: VINs whose sale invoice date is before the run's invoice-date filter.</summary>
    public int SkippedVinsInvoicedBeforeDate { get; set; }

    /// <summary>Of the skipped: VINs carrying no unconditional free service item at all.</summary>
    public int SkippedVinsWithoutFreeItems { get; set; }

    /// <summary>Of the skipped: VINs whose unconditional free items are all processed, expired or cancelled.</summary>
    public int SkippedVinsWithoutPendingFreeItems { get; set; }

    /// <summary>Unconditional free service items sitting on VINs skipped for having nothing pending.</summary>
    public int SkippedFreeServiceItems { get; set; }

    /// <summary>
    /// Conditional free items (catalog items with eligibility conditions) set aside on every VIN inside
    /// the invoice-date window — they are offered by rule, not transcribed from the menu.
    /// </summary>
    public int ExcludedConditionalFreeItems { get; set; }

    public int TotalFreeServiceItems { get; set; }

    /// <summary>Of the compared items, how many are the still-pending ones that put their VIN in scope.</summary>
    public int TotalPendingFreeServiceItems { get; set; }

    /// <summary>Free menu lines generated across the compared VINs — context, never counted against parity.</summary>
    public int TotalFreeMenuLines { get; set; }

    public int TotalMatched { get; set; }
    public int TotalMileageMismatch { get; set; }
    public int TotalItemsWithoutMenuCode { get; set; }
    public int TotalItemsCodeUnmatched { get; set; }
    public int TotalItemsWithoutFreeMenu { get; set; }

    public Dictionary<FreeServiceParityVinOutcome, int> OutcomeCounts { get; } = new();

    public List<FreeServiceParityVinSummaryModel> VinSummaries { get; } = new();
}

/// <summary>
/// Everything one run of the audit needs. Only <see cref="DuckDb"/> is required; the rest defaults to
/// "every VIN, English codes, no invoice-date filter".
/// </summary>
public class FreeServiceParityRunOptions
{
    /// <summary>A DuckDB file path or connection string. It is always opened read-only.</summary>
    public string DuckDb { get; set; } = string.Empty;

    /// <summary>
    /// The deployment's identity hash-id salt, when the store encodes company/branch/region/brand ids as
    /// hash ids. A deployment secret — never stored in this repo.
    /// </summary>
    public string? HashSalt { get; set; }

    public int HashMinLength { get; set; } = 5;

    /// <summary>
    /// Only VINs whose sale invoice date is on or after this date — or that have no invoice date — are
    /// compared. Null (the default) compares every VIN.
    /// </summary>
    public DateTime? InvoiceDateFrom { get; set; }

    /// <summary>An explicit VIN list; null means every distinct VIN in the store.</summary>
    public IReadOnlyList<string>? Vins { get; set; }

    /// <summary>Caps the run at the first N VINs (of <see cref="Vins"/>, or of the store).</summary>
    public int? Limit { get; set; }

    public int BatchSize { get; set; } = 1000;

    /// <summary>The language menu codes are generated in — a code transcribed from another language's export will not match.</summary>
    public string Language { get; set; } = "en";

    public long? CountryId { get; set; }
    public bool IgnoreBrokerStock { get; set; }
    public bool LookupBrokerStock { get; set; } = true;

    /// <summary>Where the two CSVs are written; created when missing.</summary>
    public string OutputDirectory { get; set; } = string.Empty;
}

/// <summary>What a run produced.</summary>
public class FreeServiceParityRunResult
{
    public required FreeServiceParityReportModel Report { get; init; }
    public required string DetailsCsvPath { get; init; }
    public required string SummaryCsvPath { get; init; }
    public required string ConnectionString { get; init; }
    public required TimeSpan Elapsed { get; init; }
}

/// <summary>One line of the summary CSV — <c>Section,Metric,Value</c>, so it opens as a two-column sheet.</summary>
public record FreeServiceParitySummaryRow(string Section, string Metric, string Value);
