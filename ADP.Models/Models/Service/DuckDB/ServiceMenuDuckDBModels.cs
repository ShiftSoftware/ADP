using System;

namespace ShiftSoftware.ADP.Models.Service.DuckDB;

// The NORMALIZED DuckDB layout of the menu catalog — one table per source SQL table, related by ids,
// written by the menu DuckDB sync (ADP.Menus.Sync) and read by the DuckDB menu lookup
// (ShiftSoftware.ADP.Lookup.Services.DuckDB).
//
// This deliberately differs from the Cosmos layout (ServiceMenuCosmosModels.cs). Cosmos cannot join,
// so its documents EMBED copies of the reference data (intervals, groups, replacement items,
// mappings) and replication runs UpdateReference fan-outs to keep the copies fresh. DuckDB joins, so
// none of that is needed here: reference data lives once in its own table, the reader joins it at
// query time, and a reference edit is ONE row update caught by that table's own watermark — there are
// no embedded copies to go stale and no fan-outs at all.
//
// Two conventions:
//  • Every row carries the source row's ID (the primary key), IsDeleted where the source soft
//    deletes, and LastSaveDate — the sync's incremental watermark: each table pulls source rows with
//    LastSaveDate at or past the destination's MAX(LastSaveDate). Because the layout is normalized, a
//    row changes only when ITS source row changes, so per-table watermarks are consistent by
//    construction (nothing flattened from a parent can go stale).
//  • Columns are the fields the menu lookup needs — these are lookup tables, not archives. The lookup
//    entry point is Menu.BasicModelCode; everything else is reached by id, the same way the vehicle
//    DuckDB tables are entered by VIN.

/// <summary>The DuckDB table names — shared by the sync (writer) and the lookup (reader).</summary>
public static class ServiceMenuDuckDBTables
{
    public const string Menu = "Menu";

    /// <summary>Prefixed: the vehicle lookup's own VehicleModel table may share the database file.</summary>
    public const string VehicleModel = "MenuVehicleModel";

    public const string MenuVariant = "MenuVariant";
    public const string MenuVariantLabourRate = "MenuVariantLabourRate";
    public const string MenuPeriodicAvailability = "MenuPeriodicAvailability";
    public const string MenuLabourDetails = "MenuLabourDetails";
    public const string MenuItem = "MenuItem";
    public const string MenuItemPart = "MenuItemPart";
    public const string MenuItemPartCountryPrice = "MenuItemPartCountryPrice";
    public const string ServiceInterval = "ServiceInterval";
    public const string ServiceIntervalGroup = "ServiceIntervalGroup";
    public const string ReplacementItem = "ReplacementItem";
    public const string ReplacementItemServiceIntervalGroup = "ReplacementItemServiceIntervalGroup";
    public const string ReplacementItemVehicleModel = "ReplacementItemVehicleModel";
    public const string StandaloneReplacementItemGroup = "StandaloneReplacementItemGroup";
    public const string LabourRateMapping = "LabourRateMapping";
    public const string BrandMapping = "BrandMapping";
}

/// <summary>
/// What every menu DuckDB row shares: the source row's id as the primary key. Lets the sync's
/// per-table plumbing (keyed upserts, prune-by-id) stay generic.
/// </summary>
public interface IServiceMenuDuckDBRow
{
    long ID { get; set; }
}

/// <summary>The lookup's entry point: basic model code → menu, then everything by id.</summary>
public class MenuDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public string BasicModelCode { get; set; }
    public long? VehicleModelID { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

/// <summary>The menus module's own vehicle model — supplies the variant's brand and model name.</summary>
public class MenuVehicleModelDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public string Name { get; set; }
    public long? BrandID { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class MenuVariantDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public long MenuID { get; set; }
    public string Name { get; set; }
    public string MenuPrefix { get; set; }
    public string MenuPostfix { get; set; }
    public string StandaloneMenuPrefix { get; set; }
    public string StandaloneMenuPostfix { get; set; }
    public decimal LabourRate { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public bool IsFree { get; set; }
    public bool HasStandaloneItems { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class MenuVariantLabourRateDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public long MenuVariantID { get; set; }
    public long CountryID { get; set; }
    public decimal LabourRate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class MenuPeriodicAvailabilityDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public long MenuVariantID { get; set; }
    public long ServiceIntervalID { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class MenuLabourDetailsDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public long MenuVariantID { get; set; }
    public long ServiceIntervalGroupID { get; set; }
    public decimal AllowedTime { get; set; }
    public decimal Consumable { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class MenuItemDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public long MenuVariantID { get; set; }
    public long? ReplacementItemVehicleModelID { get; set; }
    public decimal StandaloneAllowedTime { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class MenuItemPartDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public long MenuItemID { get; set; }
    public int SortOrder { get; set; }
    public string PartNumber { get; set; }
    public decimal? PeriodicQuantity { get; set; }
    public decimal? StandaloneQuantity { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class MenuItemPartCountryPriceDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public long MenuItemPartID { get; set; }
    public long CountryID { get; set; }

    /// <summary>Dealer cost.</summary>
    public decimal? PartPrice { get; set; }

    /// <summary>Retail price.</summary>
    public decimal PartFinalPrice { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class ServiceIntervalDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public int ValueInMeter { get; set; }

    /// <summary>Also the group membership: a group's intervals are the rows carrying its id here.</summary>
    public long ServiceIntervalGroupID { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class ServiceIntervalGroupDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public string LabourCode { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class ReplacementItemDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public string FriendlyName { get; set; }
    public string StandaloneOperationCode { get; set; }
    public string StandaloneLabourCode { get; set; }
    public long? StandaloneReplacementItemGroupID { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

/// <summary>
/// The replacement-item ↔ interval-group link rows, kept as rows (Cosmos flattens them to live-only
/// id lists). Their own watermark means a link edit lands on its own — the §17 caveat that a link
/// edit only reaches Cosmos with the next save of its parent item does not exist here.
/// </summary>
public class ReplacementItemServiceIntervalGroupDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public long ReplacementItemID { get; set; }
    public long ServiceIntervalGroupID { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

/// <summary>The item's replacement-item LINK row — supplies HasReplacementItem / ReplacementItemDeleted.</summary>
public class ReplacementItemVehicleModelDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public long ReplacementItemID { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class StandaloneReplacementItemGroupDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public string Name { get; set; }
    public string MenuCode { get; set; }
    public string LabourCode { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class LabourRateMappingDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public long? BrandID { get; set; }
    public decimal LabourRate { get; set; }
    public string Code { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}

public class BrandMappingDuckDBModel : IServiceMenuDuckDBRow
{
    public long ID { get; set; }
    public long? BrandID { get; set; }
    public string Code { get; set; }
    public string BrandAbbreviation { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}
