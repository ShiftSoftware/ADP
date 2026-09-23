using ShiftMapper;
using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourDetails;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourRate;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVariant;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVersion;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;
using ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Mapping;
using ShiftSoftware.ShiftEntity.Model.Dtos;

using MenuEntity = global::ShiftSoftware.ADP.Menus.Data.Entities.Menu;

namespace ShiftSoftware.ADP.Menus.Data.Mappers;

// SM0018 is reported for the three AfterMaps below: a map running a hook cannot be projected. All three are on
// WRITE maps (DTO → entity), which are only ever run in memory onto a tracked row, never projected.
#pragma warning disable SM0018

/// <summary>
/// The ONE place the menu module customizes its CRUD maps. Every repository triple's four maps (entity ↔ view,
/// entity → list, entity → entity) are declared automatically from the repository's type arguments, nested
/// children included, and most need nothing. The pairs written here are the ones convention cannot do; each
/// <c>CreateMap</c> REPLACES the automatic map for that pair (SM0047, informational) and the other pairs of the
/// triple stay automatic. A repository says nothing about what a member maps from. The few maps the API
/// serves outside a repository triple (the menu form's vehicle model, the stock endpoints' unit prices) are
/// declared here too, and reached through <c>IMapper</c>.
/// <para>
/// Two things a <c>CreateMap</c> does not inherit from the automatic map it replaces, so both are written: the
/// write map is declared as the REVERSE of the view map (a DTO is a subset of its entity, so the entity-only
/// members it leaves untouched are the quiet SM0006 rather than one SM0001 each), and flattening is off
/// (<see cref="ConfigureDefaults"/>), as it is on the automatic maps.
/// </para>
/// <para>
/// Three traps shape the maps below, all of them silent:
/// </para>
/// <list type="bullet">
///   <item>Child collections do NOT drop soft-deleted rows on their own. Every filter the view needs is written.</item>
///   <item>A child DTO whose <c>ID</c> means another entity's id would get the link row's own key by name. Those
///     collections are spelled out rather than left to the child map.</item>
///   <item>A write map writes every member the DTO carries. The members a repository or a reconciliation hook
///     owns are ignored here, one line each.</item>
/// </list>
/// </summary>
public class MenuMapper : ShiftMapperBase
{
    public MenuMapper()
    {
        // The framework's rules (the ShiftEntitySelectDTO convention, the members it owns such as ID, the
        // string → long foreign-key rule) reach a map through the repository markers only in a project that
        // closes ShiftRepository<,,,>. These maps are ALSO generated into every project that references this
        // assembly (the API, the sync, a host) — most of which close none — so the class carries the pack itself.
        AddConversions<ShiftEntityConversions>();

        AddMenuMaps();
        AddMenuVariantMaps();
        AddReplacementItemMaps();
        AddVehicleModelMaps();

        // ────────────────────────────────────────────────────────────────────────────────────────────────────
        // MenuVersion — LIST: the grid's display column.
        // ────────────────────────────────────────────────────────────────────────────────────────────────────
        CreateMap<MenuVersion, MenuVersionListDTO>()
            .ForMember(d => d.Text, opt => opt.MapFrom(e => e.Version.ToString() + " - " + e.VersionDateTime.ToString()));

        // ────────────────────────────────────────────────────────────────────────────────────────────────────
        // ServiceInterval — LIST: the flattened group name reaches through a navigation. (The view's
        // ServiceIntervalGroup select needs nothing: the select convention builds it from the foreign key.)
        // ────────────────────────────────────────────────────────────────────────────────────────────────────
        CreateMap<ServiceInterval, ServiceIntervalListDTO>()
            .ForMember(d => d.ServiceIntervalGroupName, opt => opt.MapFrom(e => e.ServiceIntervalGroup.Name));

        // ────────────────────────────────────────────────────────────────────────────────────────────────────
        // Part prices — a selling unit of a part's retail price, as the stock endpoints return it.
        // ────────────────────────────────────────────────────────────────────────────────────────────────────
        CreateMap<MenuPartUnitPrice, StockUnitPriceDTO>();
    }

    /// <summary>
    /// The automatic maps these replace do not flatten; the maps written here keep that, so replacing one changes
    /// nothing but the members written.
    /// </summary>
    protected override void ConfigureDefaults(MapOptions options) => options.Flattening = false;

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Menu
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddMenuMaps()
    {
        // VIEW is the automatic one: VehicleModel is a select built from the foreign key, its Text from the
        // included navigation. WRITE: BrandID is REPOSITORY-owned — UpsertAsync derives it from the selected
        // vehicle model just before calling base. Written from the request body instead (MenuDTO.BrandID is a
        // string the client controls), a caller could pin a menu to a brand its vehicle model does not belong to.
        CreateMap<MenuEntity, MenuDTO>()
            .ReverseMap()
            .ForMember(e => e.BrandID, opt => opt.Ignore());

        // LIST: both reach through a navigation. (The DTO member spelling is a long-standing typo kept as-is —
        // renaming it would break clients.)
        CreateMap<MenuEntity, MenuListDTO>()
            .ForMember(d => d.VehilceModel, opt => opt.MapFrom(e => e.VehicleModel != null ? e.VehicleModel.Name : string.Empty))
            .ForMember(d => d.VariantsCount, opt => opt.MapFrom(e => e.Variants.Count(v => !v.IsDeleted)));
    }

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // MenuVariant
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddMenuVariantMaps()
    {
        // VIEW. LabourRates and LabourDetails are left to the automatic child maps, which get them right.
        //
        //   PeriodicAvailabilities — spelled out. ServiceIntervalIDSelectorDTO.ID is a SERVICE INTERVAL id; the
        //     child map would fill it from the link row's own primary key.
        //
        //   Items — spelled out, down to the country prices, because both levels are FILTERED and a child map
        //     cannot be handed a filtered source (the collection would reach it whole). Soft-deleted items are
        //     excluded, and so are items whose backing RIVM has been unticked on the vehicle model: those rows
        //     still exist but no longer belong to the menu. Soft-deleted parts are excluded too, and the rest
        //     kept in SortOrder; country prices are NOT filtered by IsDeleted — they never were. Left unset, as
        //     they always were: NotStoredInMenuItemsTable (set by the UI), the part-level PartPrice /
        //     PartPriceMarginPercentage / PartFinalPrice and the country price's UnitPrices (all filled by the
        //     pricing refresh rather than read from the entity).
        //
        // WRITE (the reverse). All four child collections are tracked rows with required foreign keys back to
        // the variant, so a write map that replaced them with new objects would sever every existing row
        // (SM0049). They are ignored and reconciled in the AfterMap instead. MenuID needs nothing: the
        // framework's string → long rule converts the hash-id string to the foreign key.
        CreateMap<MenuVariant, MenuVariantDTO>()
            .ForMember(d => d.PeriodicAvailabilities, opt => opt.MapFrom(e => e.PeriodicAvailabilities
                .Select(s => new ServiceIntervalIDSelectorDTO { ID = s.ServiceIntervalID.ToString() })
                .ToList()))
            .ForMember(d => d.Items, opt => opt.MapFrom(e => e.Items
                .Where(mi => !mi.IsDeleted && (mi.ReplacementItemVehicleModel == null || !mi.ReplacementItemVehicleModel.IsDeleted))
                .Select(mi => new MenuItemDTO
                {
                    ReplacementItemVehicleModelID = mi.ReplacementItemVehicleModelID,
                    StandaloneAllowedTime = mi.StandaloneAllowedTime,

                    // Flattened across RIVM → ReplacementItem → its standalone group.
                    ReplacementItem = mi.ReplacementItemVehicleModel == null || mi.ReplacementItemVehicleModel.ReplacementItem == null
                        ? null!
                        : new MenuItemReplacementItemDTO
                        {
                            ID = mi.ReplacementItemVehicleModel.ReplacementItem.ID.ToString(),
                            Name = mi.ReplacementItemVehicleModel.ReplacementItem.Name,
                            Type = mi.ReplacementItemVehicleModel.ReplacementItem.Type,
                            AllowMultiplePartNumbers = mi.ReplacementItemVehicleModel.ReplacementItem.AllowMultiplePartNumbers,
                            StandaloneAllowedTime = mi.ReplacementItemVehicleModel.StandaloneAllowedTime,
                            DefaultPartPriceMarginPercentage = mi.ReplacementItemVehicleModel.DefaultPartPriceMarginPercentage,
                            StandaloneReplacementItemGroup = mi.ReplacementItemVehicleModel.ReplacementItem.StandaloneReplacementItemGroup == null
                                ? null
                                : new ShiftEntitySelectDTO
                                {
                                    Value = mi.ReplacementItemVehicleModel.ReplacementItem.StandaloneReplacementItemGroup.ID.ToString(),
                                    Text = mi.ReplacementItemVehicleModel.ReplacementItem.StandaloneReplacementItemGroup.Name
                                }
                        },

                    // Per-MenuItem staleness: only highlight when the RIVM is pending AND this particular
                    // MenuItem's LastPropagatedAt is older than RIVM.PendingSince. After bulk-form propagation or
                    // a MenuForm save, LastPropagatedAt is bumped → the highlight clears for that row even if
                    // other variants are still stale.
                    BackingItemHasPendingPropagation = mi.ReplacementItemVehicleModel != null
                        && mi.ReplacementItemVehicleModel.HasPendingPropagation
                        && mi.ReplacementItemVehicleModel.PendingSince.HasValue
                        && (!mi.LastPropagatedAt.HasValue
                            || mi.LastPropagatedAt.Value < mi.ReplacementItemVehicleModel.PendingSince.Value),

                    // The part ID is what the save-time diff in ApplyMenuItem matches on.
                    Parts = mi.Parts
                        .Where(p => !p.IsDeleted)
                        .OrderBy(p => p.SortOrder)
                        .Select(p => new MenuItemPartDTO
                        {
                            ID = p.ID,
                            PartNumber = p.PartNumber,
                            PeriodicQuantity = p.PeriodicQuantity,
                            StandaloneQuantity = p.StandaloneQuantity,
                            CountryPrices = p.CountryPrices
                                .Select(cp => new PartPriceByCountryDTO
                                {
                                    CountryID = cp.CountryID,
                                    PartPrice = cp.PartPrice,
                                    PartPriceMarginPercentage = cp.PartPriceMarginPercentage,
                                    PartFinalPrice = cp.PartFinalPrice,
                                    SelectedUnitName = cp.SelectedUnitName
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList()))
            .ReverseMap()
            .ForMember(e => e.PeriodicAvailabilities, opt => opt.Ignore())
            .ForMember(e => e.LabourDetails, opt => opt.Ignore())
            .ForMember(e => e.Items, opt => opt.Ignore())
            .ForMember(e => e.LabourRates, opt => opt.Ignore())
            .AfterMap((dto, entity) => ReconcileVariantChildren(dto, entity));

        // No entity source; the form fills it client-side.
        CreateMap<MenuLabourDetails, LabourDetailsDTO>()
            .ForMember(d => d.Name, opt => opt.Ignore());
    }

    /// <summary>
    /// The variant's four child collections, reconciled against the tracked rows rather than replaced.
    /// <para>
    /// Rows the DTO dropped are removed from the collection here; the repository's UpsertAsync has already
    /// deleted them from the context (DeleteChildrenDroppedByTheDto), which is what keeps the required foreign
    /// key from turning into a conceptual null. Menu items are never removed — see the comment on them below.
    /// </para>
    /// </summary>
    private static void ReconcileVariantChildren(MenuVariantDTO dto, MenuVariant entity)
    {
        entity.LabourDetails ??= [];
        var itemsToRemove = entity.LabourDetails
            .Where(existing => !dto.LabourDetails.Any(r => r.ServiceIntervalGroupID == existing.ServiceIntervalGroupID.ToString()))
            .ToList();
        foreach (var item in itemsToRemove)
            entity.LabourDetails.Remove(item);

        foreach (var item in dto.LabourDetails)
        {
            var existingItem = entity.LabourDetails
                .FirstOrDefault(r => r.ServiceIntervalGroupID.ToString() == item.ServiceIntervalGroupID);
            if (existingItem != null)
            {
                existingItem.AllowedTime = item.AllowedTime.GetValueOrDefault();
                existingItem.Consumable = item.Consumable.GetValueOrDefault();
            }
            else
                entity.LabourDetails.Add(new MenuLabourDetails
                {
                    ServiceIntervalGroupID = item.ServiceIntervalGroupID.ToLong(),
                    AllowedTime = item.AllowedTime.GetValueOrDefault(),
                    Consumable = item.Consumable.GetValueOrDefault()
                });
        }

        entity.PeriodicAvailabilities ??= [];
        var serviceIntervalItemsToRemove = entity.PeriodicAvailabilities
            .Where(existing => !dto.PeriodicAvailabilities.Any(r => r.ID == existing.ServiceIntervalID.ToString()))
            .ToList();
        foreach (var item in serviceIntervalItemsToRemove)
            entity.PeriodicAvailabilities.Remove(item);

        foreach (var item in dto.PeriodicAvailabilities)
        {
            var existingItem = entity.PeriodicAvailabilities
                .FirstOrDefault(r => r.ServiceIntervalID.ToString() == item.ID);
            if (existingItem == null)
                entity.PeriodicAvailabilities.Add(new MenuPeriodicAvailability { ServiceIntervalID = item.ID.ToLong() });
        }

        entity.Items ??= [];
        // Skip already-soft-deleted items: they won't be in the DTO (they're filtered out of the view) and we
        // must not touch them here — removing them would sever a required non-nullable FK
        // (MenuItem.MenuVariantID) and throw.
        foreach (var item in dto.Items)
        {
            var existingItem = entity.Items
                .FirstOrDefault(r => !r.IsDeleted && r.ReplacementItemVehicleModelID == item.ReplacementItemVehicleModelID);
            if (existingItem != null)
                ApplyMenuItem(item, existingItem);
            else
            {
                var created = new MenuItem();
                ApplyMenuItem(item, created);
                entity.Items.Add(created);
            }
        }

        entity.LabourRates ??= [];
        var labourRatesToRemove = entity.LabourRates
            .Where(existing => !dto.LabourRates.Any(r => r.CountryID == existing.CountryID))
            .ToList();
        foreach (var item in labourRatesToRemove)
            entity.LabourRates.Remove(item);

        foreach (var item in dto.LabourRates)
        {
            var existingItem = entity.LabourRates
                .FirstOrDefault(r => r.CountryID == item.CountryID);
            if (existingItem is not null)
                existingItem.LabourRate = item.LabourRate.GetValueOrDefault();
            else
                entity.LabourRates.Add(new MenuVariantLabourRate
                {
                    CountryID = item.CountryID.GetValueOrDefault(),
                    LabourRate = item.LabourRate.GetValueOrDefault()
                });
        }
    }

    /// <summary>
    /// Writes one incoming <see cref="MenuItemDTO"/> onto a menu item — a fresh one when the variant has no row
    /// for that replacement item yet, otherwise the tracked one — together with the diff of its parts.
    /// </summary>
    private static void ApplyMenuItem(MenuItemDTO src, MenuItem dest)
    {
        dest.ReplacementItemVehicleModelID = src.ReplacementItemVehicleModelID;
        dest.StandaloneAllowedTime = src.StandaloneAllowedTime.GetValueOrDefault();

        // Stamp LastPropagatedAt on every MenuItem save (MenuForm path). Saving counts as the user confirming
        // the values against current vehicle-model defaults — same effect as propagating via the bulk dialog.
        // Clears the per-MenuItem pending highlight for this row.
        dest.LastPropagatedAt = DateTimeOffset.UtcNow;

        // Don't Clear()/Remove() on tracked Parts — MenuItemPart.MenuItemID is non-nullable and ShiftEntity
        // forces DeleteBehavior.Restrict, which turns severed FKs into a HandleConceptualNulls throw. Diff by ID
        // instead, and soft-delete rows missing from the incoming DTO.
        dest.Parts ??= [];

        var incomingPartIds = src.Parts
            .Where(p => p.ID.HasValue)
            .Select(p => p.ID!.Value)
            .ToHashSet();

        foreach (var existingPart in dest.Parts.Where(p => !p.IsDeleted && !incomingPartIds.Contains(p.ID)).ToList())
        {
            existingPart.IsDeleted = true;
            foreach (var cp in existingPart.CountryPrices?.Where(c => !c.IsDeleted) ?? [])
                cp.IsDeleted = true;
        }

        for (int i = 0; i < src.Parts.Count; i++)
        {
            var sourcePart = src.Parts[i];
            var sourceCountryPrices = sourcePart.CountryPrices ?? [];

            var existingPart = sourcePart.ID.HasValue
                ? dest.Parts.FirstOrDefault(p => !p.IsDeleted && p.ID == sourcePart.ID.Value)
                : null;

            if (existingPart != null)
            {
                existingPart.SortOrder = i;
                existingPart.PartNumber = sourcePart.PartNumber;
                existingPart.PeriodicQuantity = sourcePart.PeriodicQuantity;
                existingPart.StandaloneQuantity = sourcePart.StandaloneQuantity;

                existingPart.CountryPrices ??= new HashSet<MenuItemPartCountryPrice>();

                var incomingCountryIds = sourceCountryPrices
                    .Where(cp => cp.CountryID.HasValue)
                    .Select(cp => cp.CountryID!.Value)
                    .ToHashSet();

                foreach (var cp in existingPart.CountryPrices.Where(c => !c.IsDeleted && !incomingCountryIds.Contains(c.CountryID)).ToList())
                    cp.IsDeleted = true;

                foreach (var sourceCp in sourceCountryPrices)
                {
                    var existingCp = sourceCp.CountryID.HasValue
                        ? existingPart.CountryPrices.FirstOrDefault(c => !c.IsDeleted && c.CountryID == sourceCp.CountryID.Value)
                        : null;

                    if (existingCp != null)
                    {
                        existingCp.PartPrice = sourceCp.PartPrice;
                        existingCp.PartPriceMarginPercentage = sourceCp.PartPriceMarginPercentage;
                        existingCp.PartFinalPrice = sourceCp.PartFinalPrice.GetValueOrDefault();
                        existingCp.SelectedUnitName = sourceCp.SelectedUnitName;
                    }
                    else
                    {
                        existingPart.CountryPrices.Add(new MenuItemPartCountryPrice
                        {
                            CountryID = sourceCp.CountryID.GetValueOrDefault(),
                            PartPrice = sourceCp.PartPrice,
                            PartPriceMarginPercentage = sourceCp.PartPriceMarginPercentage,
                            PartFinalPrice = sourceCp.PartFinalPrice.GetValueOrDefault(),
                            SelectedUnitName = sourceCp.SelectedUnitName
                        });
                    }
                }
            }
            else
            {
                dest.Parts.Add(new MenuItemPart
                {
                    SortOrder = i,
                    PartNumber = sourcePart.PartNumber,
                    PeriodicQuantity = sourcePart.PeriodicQuantity,
                    StandaloneQuantity = sourcePart.StandaloneQuantity,
                    CountryPrices = sourceCountryPrices
                        .Select(cp => new MenuItemPartCountryPrice
                        {
                            CountryID = cp.CountryID.GetValueOrDefault(),
                            PartPrice = cp.PartPrice,
                            PartPriceMarginPercentage = cp.PartPriceMarginPercentage,
                            PartFinalPrice = cp.PartFinalPrice.GetValueOrDefault(),
                            SelectedUnitName = cp.SelectedUnitName
                        })
                        .ToList()
                });
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // ReplacementItem
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddReplacementItemMaps()
    {
        // VIEW: the M:N link rows become bare id selectors, soft-deleted links excluded. The DTO member name does
        // not match the link navigation, so nothing would fill it. StandaloneReplacementItemGroup needs nothing
        // in either direction: the select convention builds it from the foreign key, Text from the navigation.
        //
        // WRITE (the reverse): the link rows are reconciled, never replaced — ReplacementItemServiceIntervalGroups
        // carries a required non-nullable FK that ShiftEntity forces to Restrict, so severing one throws a
        // HandleConceptualNulls exception rather than deleting a row. The DTO's ServiceIntervalGroups matches no
        // entity member, so the map itself writes nothing to the links; the AfterMap does.
        CreateMap<ReplacementItem, ReplacementItemDTO>()
            .ForMember(d => d.ServiceIntervalGroups, opt => opt.MapFrom(e => e.ReplacementItemServiceIntervalGroups
                .Where(s => !s.IsDeleted)
                .Select(s => new ServiceIntervalGroupReplacaementItemDTO(s.ServiceIntervalGroupID.ToString()))
                .ToList()))
            .ReverseMap()
            .AfterMap((dto, entity) => ReconcileServiceIntervalGroups(dto, entity));
    }

    private static void ReconcileServiceIntervalGroups(ReplacementItemDTO dto, ReplacementItem entity)
    {
        entity.ReplacementItemServiceIntervalGroups ??= [];

        // 1. Soft-delete links missing from the source. Physically removing them would sever a required
        //    non-nullable FK (ShiftEntity forces Restrict) and throw a HandleConceptualNulls exception.
        //    Soft-deleted links are filtered out of the view.
        var serviceIntervalItemsToRemove = entity.ReplacementItemServiceIntervalGroups
            .Where(existing => !existing.IsDeleted
                && !dto.ServiceIntervalGroups.Any(r => r.ID == existing.ServiceIntervalGroupID.ToString()))
            .ToList();

        foreach (var item in serviceIntervalItemsToRemove)
            item.IsDeleted = true;

        // 2. Add new items, reviving a previously soft-deleted link when present (the unique index on
        //    ReplacementItemID+ServiceIntervalGroupID has no IsDeleted filter, so we must reuse the existing
        //    row, not insert a duplicate).
        foreach (var item in dto.ServiceIntervalGroups)
        {
            var existingItem = entity.ReplacementItemServiceIntervalGroups
                .FirstOrDefault(r => r.ServiceIntervalGroupID.ToString() == item.ID);
            if (existingItem == null)
                entity.ReplacementItemServiceIntervalGroups.Add(new ReplacementItemServiceIntervalGroup { ServiceIntervalGroupID = item.ID.ToLong() });
            else if (existingItem.IsDeleted)
                existingItem.IsDeleted = false;
        }
    }

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // VehicleModel
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddVehicleModelMaps()
    {
        // VIEW: the form's replacement-item rows are a flattening of RIVM + its ReplacementItem + its default
        // parts, with soft-deleted rows and parts excluded and the parts kept in SortOrder. Nothing about that is
        // convention-derivable, so it is spelled out. Brand needs nothing: VehicleModel has no Brand navigation,
        // so the select convention's selector carries Value only.
        //
        // WRITE (the reverse): both child collections are tracked rows with required foreign keys, so a write
        // map that replaced them with new objects would sever the existing ones (SM0049). They are reconciled in
        // the AfterMap instead, which also keeps an old asymmetry — labour details and rates are HARD removed,
        // replacement items are soft-deleted. The DTO's ReplacementItems matches no entity member, so the map
        // itself never writes the RIVM rows.
        CreateMap<VehicleModel, VehicleModelDTO>()
            .ForMember(d => d.ReplacementItems, opt => opt.MapFrom(e => e.ReplacementItemVehicleModels == null
                ? new List<VehicleModelDTOReplacementItem>()
                : e.ReplacementItemVehicleModels
                    .Where(s => !s.IsDeleted)
                    .Select(s => new VehicleModelDTOReplacementItem(s.ReplacementItemID.ToString())
                    {
                        Name = s.ReplacementItem != null ? s.ReplacementItem.Name : string.Empty,
                        Type = s.ReplacementItem != null ? s.ReplacementItem.Type : default,
                        AllowMultiplePartNumbers = s.ReplacementItem != null && s.ReplacementItem.AllowMultiplePartNumbers,
                        StandaloneAllowedTime = s.StandaloneAllowedTime,
                        DefaultPartPriceMarginPercentage = s.DefaultPartPriceMarginPercentage,
                        HasPendingPropagation = s.HasPendingPropagation,
                        PendingSince = s.PendingSince,
                        DefaultParts = (s.DefaultParts ?? new List<ReplacementItemVehicleModelPart>())
                            .Where(p => !p.IsDeleted)
                            .OrderBy(p => p.SortOrder)
                            .Select(p => new ReplacementItemDefaultPartDTO
                            {
                                ID = p.ID,
                                PartNumber = p.PartNumber,
                                DefaultPeriodicQuantity = p.DefaultPeriodicQuantity,
                                DefaultStandaloneQuantity = p.DefaultStandaloneQuantity
                            }).ToList(),
                        StandaloneReplacementItemGroup = s.ReplacementItem != null && s.ReplacementItem.StandaloneReplacementItemGroup != null
                            ? new ShiftEntitySelectDTO
                            {
                                Value = s.ReplacementItem.StandaloneReplacementItemGroup.ID.ToString(),
                                Text = s.ReplacementItem.StandaloneReplacementItemGroup.Name
                            }
                            : null
                    })
                    .ToList()))
            .ReverseMap()
            .ForMember(e => e.LabourDetails, opt => opt.Ignore())
            .ForMember(e => e.LabourRates, opt => opt.Ignore())
            .AfterMap((dto, entity) => ReconcileVehicleModelChildren(dto, entity));

        // No entity source; the form fills it client-side.
        CreateMap<VehicleModelLabourDetails, LabourDetailsDTO>()
            .ForMember(d => d.Name, opt => opt.Ignore());

        // What the menu form loads for its vehicle model (VehicleModelController.GetById): the model's live
        // replacement items with their live default parts in SortOrder, and its live country labour rates.
        // Both are filtered, so both are spelled out; LabourDetails is not, and nests through the map above.
        // BrandID (long? → string) and LabourRate need nothing.
        CreateMap<VehicleModel, VehicleModelMenuDTO>()
            .ForMember(d => d.VehicleModelID, opt => opt.MapFrom(e => e.ID.ToString()))
            .ForMember(d => d.VehicleModelName, opt => opt.MapFrom(e => e.Name))
            .ForMember(d => d.LabourRates, opt => opt.MapFrom(e => e.LabourRates
                .Where(x => !x.IsDeleted)
                .Select(x => new LabourRateByCountryDTO
                {
                    CountryID = x.CountryID,
                    LabourRate = x.LabourRate
                })
                .ToList()))
            .ForMember(d => d.ReplacementItems, opt => opt.MapFrom(e => e.ReplacementItemVehicleModels == null
                ? new List<MenuReplacementItemDTO>()
                : e.ReplacementItemVehicleModels
                    .Where(x => !x.IsDeleted)
                    .Select(x => new MenuReplacementItemDTO
                    {
                        ID = x.ReplacementItemID.ToString(),
                        ReplacementItemVehicleModelID = x.ID,
                        Name = x.ReplacementItem.Name,
                        Type = x.ReplacementItem.Type,
                        AllowMultiplePartNumbers = x.ReplacementItem.AllowMultiplePartNumbers,
                        StandaloneOperationCode = x.ReplacementItem.StandaloneOperationCode,
                        StandaloneLabourCode = x.ReplacementItem.StandaloneLabourCode,
                        StandaloneAllowedTime = x.StandaloneAllowedTime,
                        DefaultPartPriceMarginPercentage = x.DefaultPartPriceMarginPercentage,
                        DefaultParts = x.DefaultParts
                            .Where(p => !p.IsDeleted)
                            .OrderBy(p => p.SortOrder)
                            .Select(p => new ReplacementItemDefaultPartDTO
                            {
                                ID = p.ID,
                                PartNumber = p.PartNumber,
                                DefaultPeriodicQuantity = p.DefaultPeriodicQuantity,
                                DefaultStandaloneQuantity = p.DefaultStandaloneQuantity
                            })
                            .ToList(),
                        StandaloneReplacementItemGroup = x.ReplacementItem.StandaloneReplacementItemGroup == null
                            ? null
                            : new ShiftEntitySelectDTO
                            {
                                Value = x.ReplacementItem.StandaloneReplacementItemGroup.ID.ToString(),
                                Text = x.ReplacementItem.StandaloneReplacementItemGroup.Name
                            }
                    })
                    .ToList()));
    }

    private static void ReconcileVehicleModelChildren(VehicleModelDTO dto, VehicleModel entity)
    {
        // Handle ReplacementItemVehicleModels
        entity.ReplacementItemVehicleModels ??= [];

        // 1. Soft-delete items that are not in the source. Physically removing them would sever a required
        //    non-nullable FK (ShiftEntity forces Restrict). Soft-deleted RIVMs are filtered out of the view.
        var itemsToRemove = entity.ReplacementItemVehicleModels
            .Where(existing => !existing.IsDeleted && !dto.ReplacementItems.Any(r => r.ReplacementItemID == existing.ReplacementItemID.ToString()))
            .ToList();
        foreach (var item in itemsToRemove)
        {
            item.IsDeleted = true;
            foreach (var part in item.DefaultParts?.Where(p => !p.IsDeleted) ?? [])
                part.IsDeleted = true;
        }

        // 2. Update existing items or add new items
        foreach (var item in dto.ReplacementItems)
        {
            var existingItem = entity.ReplacementItemVehicleModels
                .FirstOrDefault(r => !r.IsDeleted && r.ReplacementItemID.ToString() == item.ReplacementItemID);
            if (existingItem != null)
            {
                existingItem.StandaloneAllowedTime = item.StandaloneAllowedTime ?? existingItem.StandaloneAllowedTime;
                existingItem.DefaultPartPriceMarginPercentage = item.DefaultPartPriceMarginPercentage ?? existingItem.DefaultPartPriceMarginPercentage;
                existingItem.DefaultParts ??= [];

                var incomingPartIds = item.DefaultParts
                    .Where(p => p.ID.HasValue)
                    .Select(p => p.ID!.Value)
                    .ToHashSet();

                // Soft-delete parts missing from the incoming list. Physically removing them would sever a
                // required non-nullable FK (ShiftEntity forces Restrict).
                foreach (var existingPart in existingItem.DefaultParts.Where(p => !p.IsDeleted && !incomingPartIds.Contains(p.ID)).ToList())
                    existingPart.IsDeleted = true;

                for (int i = 0; i < item.DefaultParts.Count; i++)
                {
                    var sourcePart = item.DefaultParts[i];
                    var existingPart = sourcePart.ID.HasValue
                        ? existingItem.DefaultParts.FirstOrDefault(p => !p.IsDeleted && p.ID == sourcePart.ID.Value)
                        : null;

                    if (existingPart != null)
                    {
                        existingPart.SortOrder = i;
                        existingPart.PartNumber = sourcePart.PartNumber;
                        existingPart.DefaultPeriodicQuantity = sourcePart.DefaultPeriodicQuantity;
                        existingPart.DefaultStandaloneQuantity = sourcePart.DefaultStandaloneQuantity;
                    }
                    else
                    {
                        existingItem.DefaultParts.Add(new ReplacementItemVehicleModelPart
                        {
                            SortOrder = i,
                            PartNumber = sourcePart.PartNumber,
                            DefaultPeriodicQuantity = sourcePart.DefaultPeriodicQuantity,
                            DefaultStandaloneQuantity = sourcePart.DefaultStandaloneQuantity
                        });
                    }
                }
            }
            else
            {
                // Written out rather than mapped, so the pending flags (HasPendingPropagation, PendingSince)
                // stay repository-owned.
                entity.ReplacementItemVehicleModels.Add(new ReplacementItemVehicleModel
                {
                    ReplacementItemID = item.ReplacementItemID.ToLong(),
                    StandaloneAllowedTime = item.StandaloneAllowedTime.GetValueOrDefault(),
                    DefaultPartPriceMarginPercentage = item.DefaultPartPriceMarginPercentage,
                    DefaultParts = item.DefaultParts
                        .Select((p, index) => new ReplacementItemVehicleModelPart
                        {
                            SortOrder = index,
                            PartNumber = p.PartNumber,
                            DefaultPeriodicQuantity = p.DefaultPeriodicQuantity,
                            DefaultStandaloneQuantity = p.DefaultStandaloneQuantity
                        }).ToList()
                });
            }
        }

        // Handle LabourDetails. These are hard-removed, not soft-deleted — unlike the RIVM rows above.
        entity.LabourDetails ??= [];

        var laborDetailItemsToRemove = entity.LabourDetails
            .Where(existing => !dto.LabourDetails.Any(r => r.ServiceIntervalGroupID == existing.ServiceIntervalGroupID.ToString()))
            .ToList();
        foreach (var item in laborDetailItemsToRemove)
            entity.LabourDetails.Remove(item);

        foreach (var item in dto.LabourDetails)
        {
            var existingItem = entity.LabourDetails
                .FirstOrDefault(r => r.ServiceIntervalGroupID.ToString() == item.ServiceIntervalGroupID);
            if (existingItem != null)
            {
                existingItem.AllowedTime = item.AllowedTime.GetValueOrDefault();
                existingItem.Consumable = item.Consumable.GetValueOrDefault();
            }
            else
                entity.LabourDetails.Add(new VehicleModelLabourDetails
                {
                    ServiceIntervalGroupID = item.ServiceIntervalGroupID.ToLong(),
                    AllowedTime = item.AllowedTime.GetValueOrDefault(),
                    Consumable = item.Consumable.GetValueOrDefault()
                });
        }

        // Handle LabourRates
        entity.LabourRates ??= [];
        var labourRatesToRemove = entity.LabourRates
            .Where(existing => !dto.LabourRates.Any(r => r.CountryID == existing.CountryID))
            .ToList();
        foreach (var item in labourRatesToRemove)
            entity.LabourRates.Remove(item);

        foreach (var item in dto.LabourRates)
        {
            var existingItem = entity.LabourRates
                .FirstOrDefault(r => r.CountryID == item.CountryID);
            if (existingItem is not null)
                existingItem.LabourRate = item.LabourRate.GetValueOrDefault();
            else
                entity.LabourRates.Add(new VehicleModelLabourRate
                {
                    CountryID = item.CountryID.GetValueOrDefault(),
                    LabourRate = item.LabourRate.GetValueOrDefault()
                });
        }
    }
}
#pragma warning restore SM0018
