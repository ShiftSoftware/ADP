using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;

namespace ShiftSoftware.ADP.Menus.Sample.FreeServiceParity;

/// <summary>
/// How one free service item resolved against its model's menu.
///
/// <para>The service-items system was filled BY HAND from the exported menu, and the menu lookup exists
/// to make that manual step unnecessary — so the identity that says "this entitlement is that menu
/// service" is the MENU CODE: the item's <c>PackageCode</c> was transcribed from the very <c>Code</c>
/// the menu generator produces. The audit is ONE-WAY: each free service item looks for its match among
/// ALL the menu's generated lines (every variant — the free-of-charge flag is not authored yet, so it
/// selects nothing). Menu lines no free item points at are expected — the menu also prices paid work —
/// and are never counted against parity.</para>
/// </summary>
public enum FreeServiceParityMatchResult
{
    /// <summary>The item's menu code found its menu line and every compared property agrees.</summary>
    Matched = 0,

    /// <summary>The item's menu code found its menu line — the identity holds — but some compared property differs (see the row's differences).</summary>
    MatchedWithDifferences = 1,

    /// <summary>The free service item carries NO menu code at all, so it cannot be looked up.</summary>
    FreeItemWithoutMenuCode = 2,

    /// <summary>The free service item carries a menu code, but the menu generated no line with that code.</summary>
    FreeItemCodeUnmatched = 3,
}

/// <summary>One VIN's overall verdict.</summary>
public enum FreeServiceParityVinOutcome
{
    /// <summary>Every free item found its menu line by code, and every compared property agrees.</summary>
    Match = 0,

    /// <summary>Every free item found its menu line by code — the identity the migration needs — with property differences to review.</summary>
    MatchWithDifferences = 1,

    /// <summary>At least one free item has no menu code, or a code the menu did not generate.</summary>
    Mismatch = 2,

    /// <summary>The VIN carries no free service items — nothing to look up.</summary>
    NoFreeItems = 3,

    /// <summary>The VIN has free items but no menu is authored under its derived basic model code.</summary>
    MenuNotFound = 4,

    /// <summary>The VIN has free items but the menu subsystem could not be consulted.</summary>
    MenuUnavailable = 5,

    /// <summary>No menu lookup registered (should not appear in this audit).</summary>
    MenuNotRegistered = 6,

    /// <summary>The VIN has free items but no Katashiki to derive a model code from.</summary>
    NoBasicModelCode = 7,
}

/// <summary>
/// One detail row — one free service item's resolution. A matched row carries the menu line it found
/// and its property differences; an unmatched row carries only the item's columns.
/// </summary>
public class FreeServiceParityRowModel
{
    public string VIN { get; set; } = string.Empty;
    public string BasicModelCode { get; set; } = string.Empty;
    public VehicleServiceMenuStatus? MenuStatus { get; set; }
    public FreeServiceParityMatchResult MatchResult { get; set; }

    /// <summary>The secondary comparison, human-readable: "Mileage: 10000 != 15000 | Price: 25 != 30". Empty when everything agrees.</summary>
    public string Differences { get; set; } = string.Empty;

    // ---- free service item side (vehicle lookup ServiceItems, TypeEnum == Free) ----
    public string ServiceItemId { get; set; } = string.Empty;
    public string ServiceItemName { get; set; } = string.Empty;
    public string ItemMenuCode { get; set; } = string.Empty;
    public long? ItemMaximumMileage { get; set; }
    public decimal? ItemCost { get; set; }
    public string ItemStatus { get; set; } = string.Empty;
    public VehcileServiceItemStatuses? ItemStatusEnum { get; set; }
    public bool? ItemClaimable { get; set; }
    public DateTime? ItemActivatedAt { get; set; }
    public DateTime? ItemExpiresAt { get; set; }
    public DateTimeOffset? ItemClaimDate { get; set; }

    // ---- the menu line the item's code found (vehicle lookup ServiceMenu, ALL variants) ----
    public long? MenuVariantId { get; set; }
    public string MenuVariantName { get; set; } = string.Empty;
    public bool? MenuVariantIsFree { get; set; }
    public string MenuLineKey { get; set; } = string.Empty;
    public string MenuLineCode { get; set; } = string.Empty;
    public string MenuLabourCode { get; set; } = string.Empty;
    public string MenuDescription { get; set; } = string.Empty;
    public ServiceMenuLineType? MenuLineType { get; set; }
    public bool? MenuIsStandalone { get; set; }
    public int? MenuIntervalKm { get; set; }
    public decimal? MenuTotalPrice { get; set; }
}

/// <summary>One VIN's roll-up.</summary>
public class FreeServiceParityVinSummaryModel
{
    public string VIN { get; set; } = string.Empty;
    public string BasicModelCode { get; set; } = string.Empty;
    public VehicleServiceMenuStatus? MenuStatus { get; set; }
    public FreeServiceParityVinOutcome Outcome { get; set; }
    public int FreeServiceItemCount { get; set; }

    /// <summary>How many lines the whole menu generated — context, never counted against parity.</summary>
    public int MenuLineCount { get; set; }

    public int MatchedCount { get; set; }
    public int MatchedWithDifferencesCount { get; set; }
    public int ItemsWithoutMenuCodeCount { get; set; }
    public int ItemsCodeUnmatchedCount { get; set; }
}

/// <summary>
/// The whole run: per-VIN summaries plus fleet totals. The CSV export streams the detail rows to the
/// file and leaves <see cref="Rows"/> empty so a full-population run stays memory-bounded.
/// </summary>
public class FreeServiceParityReportModel
{
    public int RequestedVinCount { get; set; }
    public int VinCount { get; set; }

    public int TotalFreeServiceItems { get; set; }

    /// <summary>Menu lines generated across all answered VINs — context, never counted against parity.</summary>
    public int TotalMenuLines { get; set; }

    public int TotalMatched { get; set; }
    public int TotalMatchedWithDifferences { get; set; }
    public int TotalItemsWithoutMenuCode { get; set; }
    public int TotalItemsCodeUnmatched { get; set; }

    public Dictionary<FreeServiceParityVinOutcome, int> OutcomeCounts { get; } = new();

    public List<FreeServiceParityVinSummaryModel> VinSummaries { get; } = new();

    public List<FreeServiceParityRowModel> Rows { get; } = new();
}
