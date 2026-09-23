using ShiftMapper;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Models.Service.DuckDB;

namespace ShiftSoftware.ADP.Menus.Data.Mappers;

/// <summary>
/// EF entity → DuckDB row maps, the normalized counterpart of <see cref="MenuReplicationMapper"/> — and
/// deliberately much smaller: every map is a plain copy of one row, because there is nothing to embed. Cosmos
/// cannot join, so its projections denormalize reference data into the menu documents; DuckDB joins, so each
/// table carries only its own fields plus the ids the reader joins on.
///
/// <para>Every row member has a same-named member on its entity, so none of these maps says anything beyond
/// the pair. A row member added without an entity counterpart is an SM0001 warning, not a silently empty column.</para>
///
/// The rules that DO hold here are the same ones the Cosmos projections follow:
///  • the row id is the source row's database id;
///  • <c>IsDeleted</c> is carried, never filtered — whether a soft-deleted row contributes to a menu
///    is decided once, by the generation layer, so the DMS export and both lookups cannot disagree;
///  • <c>LastSaveDate</c> is carried as the sync's incremental watermark.
///
/// <para>Part of this assembly's generated mapper, which <c>AddServiceMenuDuckDBSync</c> registers (as does
/// <c>AddMenuApiServices</c>). Keep the constructor parameterless: a dependency would make this assembly's generated
/// mapper — and every referencing project's — usable only from a container.</para>
/// </summary>
public class MenuDuckDBMapper : ShiftMapperBase
{
    public MenuDuckDBMapper()
    {
        CreateMap<Menu, MenuDuckDBModel>();
        CreateMap<VehicleModel, MenuVehicleModelDuckDBModel>();
        CreateMap<MenuVariant, MenuVariantDuckDBModel>();
        CreateMap<MenuVariantLabourRate, MenuVariantLabourRateDuckDBModel>();
        CreateMap<MenuPeriodicAvailability, MenuPeriodicAvailabilityDuckDBModel>();
        CreateMap<MenuLabourDetails, MenuLabourDetailsDuckDBModel>();
        CreateMap<MenuItem, MenuItemDuckDBModel>();
        CreateMap<MenuItemPart, MenuItemPartDuckDBModel>();
        CreateMap<MenuItemPartCountryPrice, MenuItemPartCountryPriceDuckDBModel>();
        CreateMap<ServiceInterval, ServiceIntervalDuckDBModel>();
        CreateMap<ServiceIntervalGroup, ServiceIntervalGroupDuckDBModel>();
        CreateMap<ReplacementItem, ReplacementItemDuckDBModel>();
        CreateMap<ReplacementItemServiceIntervalGroup, ReplacementItemServiceIntervalGroupDuckDBModel>();
        CreateMap<ReplacementItemVehicleModel, ReplacementItemVehicleModelDuckDBModel>();
        CreateMap<StandaloneReplacementItemGroup, StandaloneReplacementItemGroupDuckDBModel>();
        CreateMap<LabourRateMapping, LabourRateMappingDuckDBModel>();
        CreateMap<BrandMapping, BrandMappingDuckDBModel>();
    }

    /// <summary>A row is its own entity's columns; nothing is reached through a navigation by name.</summary>
    protected override void ConfigureDefaults(MapOptions options) => options.Flattening = false;
}
