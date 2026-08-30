using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;

namespace ShiftSoftware.ADP.Menus.Sample.FreeServiceParity;

/// <summary>
/// How one row of the parity report resolved.
///
/// <para>The service-items system was filled BY HAND from the exported menu, and the menu lookup exists
/// to make that manual step unnecessary — so the identity that says "these are the same service" is the
/// MENU CODE both sides carry: the service item's <c>PackageCode</c> was transcribed from the very
/// <c>Code</c> the menu generator produces. Matching is therefore by menu code alone; the other
/// properties are compared afterwards on matched pairs and reported, but they are secondary.</para>
/// </summary>
public enum FreeServiceParityMatchResult
{
    /// <summary>Menu codes match and every compared property agrees.</summary>
    Matched = 0,

    /// <summary>Menu codes match — the identity holds — but some compared property differs (see the row's differences).</summary>
    MatchedWithDifferences = 1,

    /// <summary>The free service item carries NO menu code at all, so it cannot be matched to anything.</summary>
    FreeItemWithoutMenuCode = 2,

    /// <summary>The free service item carries a menu code, but no free menu line generated that code.</summary>
    FreeItemCodeUnmatched = 3,

    /// <summary>A free menu line whose code no free service item carries.</summary>
    MenuLineUnmatched = 4,
}

/// <summary>One VIN's overall verdict.</summary>
public enum FreeServiceParityVinOutcome
{
    /// <summary>Every free item and every free menu line matched by code, and every compared property agrees.</summary>
    Match = 0,

    /// <summary>Fully matched by code — the identity the migration needs — with property differences to review.</summary>
    MatchWithDifferences = 1,

    /// <summary>At least one side has an entry no menu code could pair.</summary>
    Mismatch = 2,

    /// <summary>Menu found, but the VIN has no free items and the menu no free lines — nothing to compare.</summary>
    NothingFree = 3,

    /// <summary>No menu is authored under the VIN's derived basic model code.</summary>
    MenuNotFound = 4,

    /// <summary>The menu subsystem could not be consulted.</summary>
    MenuUnavailable = 5,

    /// <summary>No menu lookup registered (should not appear in this audit).</summary>
    MenuNotRegistered = 6,

    /// <summary>No Katashiki to derive a model code from — including VINs the store holds nothing about.</summary>
    NoBasicModelCode = 7,
}

/// <summary>
/// One detail row: a matched pair carries both sides and its property differences; an unmatched entry
/// carries only its own side's columns.
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

    // ---- free menu line side (vehicle lookup ServiceMenu, FreeFilter = FreeOnly) ----
    public long? MenuVariantId { get; set; }
    public string MenuVariantName { get; set; } = string.Empty;
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
    public int FreeMenuLineCount { get; set; }
    public int MatchedCount { get; set; }
    public int MatchedWithDifferencesCount { get; set; }
    public int ItemsWithoutMenuCodeCount { get; set; }
    public int ItemsCodeUnmatchedCount { get; set; }
    public int MenuLinesUnmatchedCount { get; set; }
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
    public int TotalFreeMenuLines { get; set; }
    public int TotalMatched { get; set; }
    public int TotalMatchedWithDifferences { get; set; }
    public int TotalItemsWithoutMenuCode { get; set; }
    public int TotalItemsCodeUnmatched { get; set; }
    public int TotalMenuLinesUnmatched { get; set; }

    public Dictionary<FreeServiceParityVinOutcome, int> OutcomeCounts { get; } = new();

    public List<FreeServiceParityVinSummaryModel> VinSummaries { get; } = new();

    public List<FreeServiceParityRowModel> Rows { get; } = new();
}
