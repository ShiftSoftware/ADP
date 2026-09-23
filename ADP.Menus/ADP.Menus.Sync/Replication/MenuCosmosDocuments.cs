using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ShiftMapper;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Data.Mappers;
using ShiftSoftware.ADP.Models.Service.Cosmos;

namespace ShiftSoftware.ADP.Menus.Sync.Replication;

/// <summary>
/// Every Cosmos document the menu replication writes, through ShiftMapper: every field is written by a map in
/// <see cref="MenuReplicationMapper"/>, and what is here only resolves the mapper and puts mapped documents together.
/// Both replication paths — the save trigger (<c>AddMenuReplications</c>) and the catch-up sweep
/// (<c>MenuCatchUpReplicationExtensions</c>) — come through it, and neither needs the host to register anything.
///
/// The file has two halves, and the split matters:
///
///  • <see cref="Variant"/>, <see cref="Period"/>, <see cref="Labour"/> and <see cref="Item"/> — the ServiceMenus
///    documents, each of which embeds master rows. The rest of each document is its map, and every embedded copy is
///    the master's own map, written by the same code its fan-out uses. Some of those masters are not a same-named
///    member at all — a variant has no navigation to its labour-rate and brand mappings (the caller resolves them),
///    a menu item reaches its replacement item through the RIVM link — and a master that was not loaded is embedded
///    as null, never mapped from nothing.
///
///  • <c>ApplyTo</c> — the <c>UpdateReference</c> fan-outs. Each takes an EXISTING menu document, replaces the
///    slice this master entity owns with the master's map, and returns it. Everything else on the document is left
///    alone, because the master row is not the authority on it. That is not an entity → document map, which is why
///    the pipeline's merge door takes a delegate here.
/// </summary>
public static class MenuCosmosDocuments
{
    // ---- the mapper --------------------------------------------------------------------------------

    /// <summary>
    /// The mapper the replication writes through — resolved here so no host has to register one. The maps
    /// (<c>MenuReplicationMapper</c>) are declared in ADP.Menus.Data, which makes them part of that assembly's
    /// generated mapper: the one <c>AddMenuApiServices</c> (and <c>AddServiceMenuDuckDBSync</c>) already registers.
    /// When the scope's mapper carries them, it is used; a host that registered none — a catch-up-only Functions
    /// app, say — gets ADP.Menus.Data's generated mapper built outside a container. The documents are the same maps
    /// either way. Resolve once per unit of work and use the result on one thread at a time, as a scope would.
    /// </summary>
    public static IMapper ResolveMapper(IServiceProvider services) =>
        Covering(services.GetService<IMapper>());

    /// <summary>
    /// The same, reached through the host's menus DbContext — for the catch-up sweep, whose extension methods are
    /// handed the context rather than the scope. Resolved once per pass, before any row is read.
    /// </summary>
    public static IMapper ResolveMapper(DbContext database)
    {
        IMapper? mapper;

        try { mapper = database.GetService<IMapper>(); }
        catch (InvalidOperationException) { mapper = null; }

        return Covering(mapper);
    }

    /// <summary>
    /// The scope's mapper when it carries the menu maps, otherwise ADP.Menus.Data's generated mapper built outside a
    /// container — a NEW one each time, never a shared one. A ShiftMapper mapper fills its customization store on the
    /// first map that reads it, and two threads making that first map together corrupt it; a container avoids that by
    /// handing each scope its own, and this does the same. (What is costly — compiling the maps' expressions — is
    /// cached per mapper class by ShiftMapper, so a fresh instance does not pay it again.)
    /// </summary>
    private static IMapper Covering(IMapper? mapper) =>
        mapper is not null && mapper.CanMap(typeof(MenuItem), typeof(MenuItemCosmosModel))
            ? mapper
            : ShiftMapper.Mapper.Create(typeof(MenuReplicationMapper).Assembly);

    // ---- documents that embed what they cannot navigate to -----------------------------------------

    /// <param name="labourRateMapping">
    /// The live mapping for the variant's (brand, primary labour rate) pair, or null when there is none. Null is
    /// meaningful: the generator throws on a missing pair (open item O1), so the reader must leave the dictionary
    /// entry out rather than invent a code.
    /// </param>
    /// <param name="brandMapping">The live mapping for the variant's brand, or null when unmapped.</param>
    public static MenuVariantCosmosModel Variant(
        IMapper mapper,
        MenuVariant variant,
        LabourRateMapping? labourRateMapping,
        BrandMapping? brandMapping)
    {
        var document = mapper.Map<MenuVariant, MenuVariantCosmosModel>(variant);

        document.LabourRateMapping = labourRateMapping is null ? null : Map(mapper, labourRateMapping);
        document.BrandMapping = brandMapping is null ? null : Map(mapper, brandMapping);

        return document;
    }

    /// <summary>A periodic availability, with its service interval embedded.</summary>
    public static MenuPeriodCosmosModel Period(IMapper mapper, MenuPeriodicAvailability period)
    {
        var document = mapper.Map<MenuPeriodicAvailability, MenuPeriodCosmosModel>(period);

        document.ServiceInterval = period.ServiceInterval is null ? null : Map(mapper, period.ServiceInterval);

        return document;
    }

    /// <summary>A labour detail, with its service-interval group (and that group's membership) embedded.</summary>
    public static MenuLabourCosmosModel Labour(IMapper mapper, MenuLabourDetails labour)
    {
        var document = mapper.Map<MenuLabourDetails, MenuLabourCosmosModel>(labour);

        document.ServiceIntervalGroup = labour.ServiceIntervalGroup is null ? null : Map(mapper, labour.ServiceIntervalGroup);

        return document;
    }

    /// <summary>
    /// A menu item, with its replacement-item slice written the same way the replacement item's own fan-out
    /// (<see cref="ApplyTo(IMapper, ReplacementItem, MenuItemCosmosModel)"/>) refreshes it.
    /// </summary>
    public static MenuItemCosmosModel Item(IMapper mapper, MenuItem item)
    {
        var document = mapper.Map<MenuItem, MenuItemCosmosModel>(item);

        WriteReplacementItemSlice(mapper, item.ReplacementItemVehicleModel?.ReplacementItem, document);

        return document;
    }

    // ---- UpdateReference fan-outs ------------------------------------------------------------------
    // Each replaces only the slice its master entity owns. They run on ChangeType.Modified ONLY, which is correct
    // for an edit and is the reason a freshly INSERTED master row reaches existing documents only when those
    // documents are themselves re-saved (or pushed by a backfill).

    /// <summary>Refreshes the interval embedded on a periodic-availability document.</summary>
    public static MenuPeriodCosmosModel ApplyTo(IMapper mapper, ServiceInterval interval, MenuPeriodCosmosModel document)
    {
        document.ServiceInterval = Map(mapper, interval);
        return document;
    }

    /// <summary>Refreshes the interval group embedded on a labour-detail document.</summary>
    public static MenuLabourCosmosModel ApplyTo(IMapper mapper, ServiceIntervalGroup group, MenuLabourCosmosModel document)
    {
        document.ServiceIntervalGroup = Map(mapper, group);
        return document;
    }

    /// <summary>
    /// Refreshes this group's entry in a menu item's denormalized group list, leaving the item's other groups
    /// untouched. The entry is matched by id and replaced in place so the list keeps its order; a document that
    /// somehow lacks the entry gains it, which is the self-healing outcome.
    /// </summary>
    public static MenuItemCosmosModel ApplyTo(IMapper mapper, ServiceIntervalGroup group, MenuItemCosmosModel document)
    {
        document.ServiceIntervalGroups ??= [];

        var replacement = Map(mapper, group);
        var index = document.ServiceIntervalGroups
            .FindIndex(existing => existing.ServiceIntervalGroupID == group.ID);

        if (index >= 0)
            document.ServiceIntervalGroups[index] = replacement;
        else
            document.ServiceIntervalGroups.Add(replacement);

        return document;
    }

    /// <summary>
    /// Refreshes a menu item's whole replacement-item slice: the item itself, the standalone group it points at,
    /// and the interval groups it serves.
    ///
    /// All three move together on purpose — a replacement item can be re-pointed at a different standalone group
    /// or re-linked to different interval groups, and refreshing only its scalar fields would leave the document
    /// describing the old ones.
    /// </summary>
    public static MenuItemCosmosModel ApplyTo(IMapper mapper, ReplacementItem replacementItem, MenuItemCosmosModel document)
    {
        WriteReplacementItemSlice(mapper, replacementItem, document);
        return document;
    }

    /// <summary>Refreshes the standalone group embedded on a menu-item document.</summary>
    public static MenuItemCosmosModel ApplyTo(IMapper mapper, StandaloneReplacementItemGroup group, MenuItemCosmosModel document)
    {
        document.StandaloneGroup = Map(mapper, group);
        return document;
    }

    /// <summary>Refreshes the labour-rate mapping embedded on a variant document.</summary>
    public static MenuVariantCosmosModel ApplyTo(IMapper mapper, LabourRateMapping mapping, MenuVariantCosmosModel document)
    {
        document.LabourRateMapping = Map(mapper, mapping);
        return document;
    }

    /// <summary>Refreshes the brand mapping embedded on a variant document.</summary>
    public static MenuVariantCosmosModel ApplyTo(IMapper mapper, BrandMapping mapping, MenuVariantCosmosModel document)
    {
        document.BrandMapping = Map(mapper, mapping);
        return document;
    }

    // ---- shared -------------------------------------------------------------------------------------

    private static void WriteReplacementItemSlice(IMapper mapper, ReplacementItem? replacementItem, MenuItemCosmosModel document)
    {
        document.ReplacementItem = replacementItem is null ? null : Map(mapper, replacementItem);
        document.ServiceIntervalGroups = MapIntervalGroups(mapper, replacementItem);
        document.StandaloneGroup = replacementItem?.StandaloneReplacementItemGroup is null
            ? null
            : Map(mapper, replacementItem.StandaloneReplacementItemGroup);
    }

    /// <summary>
    /// The interval groups a replacement item serves, each with its labour code and FULL interval membership. The
    /// membership has to travel with the item: generation asks "does group G contain interval I" for every group the
    /// item serves, including groups the variant has no labour detail for, so it cannot be recovered from the
    /// sibling labour documents.
    ///
    /// <para><b>Only LIVE links, and this is the one place the replication filters a soft delete — it has to.</b>
    /// Everywhere else the projection carries the flag and the reader decides — but a link row has no document and no
    /// flag anywhere in the document shape: it contributes only its group (here) and its group id (to
    /// <c>ReplacementItemCosmosModel.ServiceIntervalGroupIDs</c>, which the replacement item's map filters the same
    /// way). So a deleted link that is projected is indistinguishable from a live one, and the lookup would keep
    /// pricing parts onto periodic lines the export has stopped pricing.</para>
    ///
    /// <para>Filtering here rather than widening the shape is deliberate: the flat id list is what makes the
    /// interval-group fan-out an <c>ARRAY_CONTAINS</c> instead of a scan (§17), and a deleted link's group genuinely
    /// should no longer find this document.</para>
    ///
    /// <para>Mirrors <c>EfToGenerationAggregator.LiveIntervalGroupLinks</c>, minus the group's own flag — that one
    /// stays a read-time decision, because a group can be deleted long after the item was last replicated. <b>Note
    /// the usual caveat (§17): editing a link does not re-replicate its parent replacement item, so this takes effect
    /// on the next save of that item or on a catch-up sweep.</b></para>
    /// </summary>
    private static List<ServiceIntervalGroupCosmosModel> MapIntervalGroups(IMapper mapper, ReplacementItem? replacementItem) =>
        (replacementItem?.ReplacementItemServiceIntervalGroups ?? [])
            .Where(link => !link.IsDeleted && link.ServiceIntervalGroup is not null)
            .Select(link => Map(mapper, link.ServiceIntervalGroup))
            .ToList();

    // ---- the master documents: one map each ----------------------------------------------------------
    // Their own containers' documents, and the copies the menu documents embed. Every replication call maps
    // through these rather than leaving the pipeline to find a mapper, so a host never has to register one.

    public static ServiceIntervalCosmosModel Map(IMapper mapper, ServiceInterval interval) =>
        mapper.Map<ServiceInterval, ServiceIntervalCosmosModel>(interval);

    public static ServiceIntervalGroupCosmosModel Map(IMapper mapper, ServiceIntervalGroup group) =>
        mapper.Map<ServiceIntervalGroup, ServiceIntervalGroupCosmosModel>(group);

    public static ReplacementItemCosmosModel Map(IMapper mapper, ReplacementItem replacementItem) =>
        mapper.Map<ReplacementItem, ReplacementItemCosmosModel>(replacementItem);

    public static StandaloneReplacementItemGroupCosmosModel Map(IMapper mapper, StandaloneReplacementItemGroup group) =>
        mapper.Map<StandaloneReplacementItemGroup, StandaloneReplacementItemGroupCosmosModel>(group);

    public static LabourRateMappingCosmosModel Map(IMapper mapper, LabourRateMapping mapping) =>
        mapper.Map<LabourRateMapping, LabourRateMappingCosmosModel>(mapping);

    public static BrandMappingCosmosModel Map(IMapper mapper, BrandMapping mapping) =>
        mapper.Map<BrandMapping, BrandMappingCosmosModel>(mapping);
}
