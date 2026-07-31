using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.CosmosDbReplication;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.Replication;

namespace ShiftSoftware.ADP.Menus.Data.Replication;

/// <summary>
/// Loads what a Cosmos projection needs beyond the row the trigger hands it.
///
/// The trigger supplies whatever EF happened to have tracked when the row was saved — which for a child
/// row is usually just the row itself, with every navigation null. Since the projections denormalize
/// heavily (a period needs its variant's menu for the partition key AND the interval's own code; an
/// item needs its parts, its replacement item and every interval group that item serves), they would
/// otherwise produce documents with holes, silently.
///
/// Two mechanisms live here:
///
///  • <see cref="With{TDbContext,TEntity}"/> builds <c>SetUpReplication</c>'s third argument: an async
///    pre-processor that REPLACES the entity everything downstream sees. It is the only async seam in
///    the pipeline — the mapper itself is synchronous — and it avoids having to add a repository (with
///    the easily-missed <c>IShiftEntityPrepareForReplicationAsync</c> re-declaration) for tables that
///    have none.
///
///  • <see cref="VariantMasterData{TDbContext}"/> resolves the two master rows a variant document
///    embeds but has no navigation to. It runs inside the mapper, so it is synchronous — see its
///    remarks.
/// </summary>
public static class MenuReplicationReload
{
    /// <summary>
    /// Builds the reload hook for one entity type.
    ///
    /// Query filters are ignored on purpose: a soft-deleted row must still be re-read so its document
    /// can be upserted with <c>IsDeleted = true</c>. If the row is gone entirely (a hard delete) the
    /// original entity is returned untouched — it still carries the <c>LastReplicationStamp</c> the
    /// pipeline needs to locate and remove the document.
    /// </summary>
    public static Func<EntityWrapper<TEntity>, ValueTask<TEntity>> With<TDbContext, TEntity>(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> include)
        where TDbContext : ShiftDbContext
        where TEntity : ShiftEntity<TEntity>, IShiftEntityReplication
    {
        return async wrapper =>
        {
            var database = wrapper.Services.GetRequiredService<TDbContext>();

            var reloaded = await include(database.Set<TEntity>().IgnoreQueryFilters())
                .FirstOrDefaultAsync(row => row.ID == wrapper.Entity.ID);

            return reloaded ?? wrapper.Entity;
        };
    }

    // ---- per-table include graphs --------------------------------------------------------------
    // Each must cover exactly what the matching MenuCosmosMappers.Map reads, and each mirrors the
    // export's own query (MenuController.GenerateLinesAsync) so the two paths see the same graph.
    // Keep them in step: a missing include does not throw, it produces a document with holes.

    /// <summary>Menu (partition key) + vehicle model (brand, model name) + the embedded country rates.</summary>
    public static Func<EntityWrapper<MenuVariant>, ValueTask<MenuVariant>> Variant<TDbContext>()
        where TDbContext : ShiftDbContext =>
        With<TDbContext, MenuVariant>(query => query
            .Include(variant => variant.Menu).ThenInclude(menu => menu.VehicleModel)
            .Include(variant => variant.LabourRates));

    /// <summary>Parent chain for the partition key, plus the interval the document denormalizes.</summary>
    public static Func<EntityWrapper<MenuPeriodicAvailability>, ValueTask<MenuPeriodicAvailability>> Period<TDbContext>()
        where TDbContext : ShiftDbContext =>
        With<TDbContext, MenuPeriodicAvailability>(query => query
            .Include(period => period.MenuVariant).ThenInclude(variant => variant.Menu)
            .Include(period => period.ServiceInterval));

    /// <summary>
    /// Parent chain for the partition key, plus the interval group AND its interval membership — the
    /// membership is what decides which periodic line this labour detail supplies.
    /// </summary>
    public static Func<EntityWrapper<MenuLabourDetails>, ValueTask<MenuLabourDetails>> Labour<TDbContext>()
        where TDbContext : ShiftDbContext =>
        With<TDbContext, MenuLabourDetails>(query => query
            .Include(labour => labour.MenuVariant).ThenInclude(variant => variant.Menu)
            .Include(labour => labour.ServiceIntervalGroup).ThenInclude(group => group.ServiceIntervals));

    /// <summary>
    /// The widest graph: parent chain for the partition key, the embedded parts and prices, and the
    /// whole replacement-item slice — the item itself, its standalone group, and every interval group
    /// it serves with that group's own interval membership.
    /// </summary>
    public static Func<EntityWrapper<MenuItem>, ValueTask<MenuItem>> Item<TDbContext>()
        where TDbContext : ShiftDbContext =>
        With<TDbContext, MenuItem>(query => query
            .Include(item => item.MenuVariant).ThenInclude(variant => variant.Menu)
            .Include(item => item.Parts).ThenInclude(part => part.CountryPrices)
            .Include(item => item.ReplacementItemVehicleModel!).ThenInclude(link => link.ReplacementItem)
                .ThenInclude(replacementItem => replacementItem.StandaloneReplacementItemGroup)
            .Include(item => item.ReplacementItemVehicleModel!).ThenInclude(link => link.ReplacementItem)
                .ThenInclude(replacementItem => replacementItem.ReplacementItemServiceIntervalGroups)
                .ThenInclude(link => link.ServiceIntervalGroup).ThenInclude(group => group.ServiceIntervals)
            .AsSplitQuery());

    /// <summary>Interval membership — what lets generation match an interval to this group's labour.</summary>
    public static Func<EntityWrapper<ServiceIntervalGroup>, ValueTask<ServiceIntervalGroup>> IntervalGroup<TDbContext>()
        where TDbContext : ShiftDbContext =>
        With<TDbContext, ServiceIntervalGroup>(query => query
            .Include(group => group.ServiceIntervals));

    /// <summary>
    /// The standalone group and the interval groups (with their membership) a replacement item serves.
    /// Both travel with the item because its fan-out refreshes the menu item's whole replacement-item
    /// slice, not just its scalar fields.
    /// </summary>
    public static Func<EntityWrapper<Entities.ReplacementItem>, ValueTask<Entities.ReplacementItem>> ReplacementItem<TDbContext>()
        where TDbContext : ShiftDbContext =>
        With<TDbContext, Entities.ReplacementItem>(query => query
            .Include(replacementItem => replacementItem.StandaloneReplacementItemGroup)
            .Include(replacementItem => replacementItem.ReplacementItemServiceIntervalGroups)
                .ThenInclude(link => link.ServiceIntervalGroup).ThenInclude(group => group.ServiceIntervals)
            .AsSplitQuery());

    // ServiceInterval, StandaloneReplacementItemGroup, LabourRateMapping and BrandMapping project
    // scalars only, so they need no reload — the tracked entity the trigger supplies is complete.

    // ---- master data a variant embeds but cannot navigate to -------------------------------------

    /// <summary>
    /// The two master rows a variant document embeds: the labour-rate mapping for its (brand, primary
    /// labour rate) pair, and its brand's mapping. Either may be null; null is meaningful, and the
    /// document records it as such (see <see cref="MenuCosmosMappers"/>).
    /// </summary>
    public readonly record struct MenuVariantMasterData(
        LabourRateMapping? LabourRateMapping,
        BrandMapping? BrandMapping);

    /// <summary>
    /// Resolves <see cref="MenuVariantMasterData"/> for a variant.
    ///
    /// SYNCHRONOUS, deliberately: it is called from the <c>Replicate</c> mapper, which the framework
    /// defines as a synchronous delegate, and there is no navigation from a variant to either mapping
    /// so the async reload hook cannot carry them. Replication runs fire-and-forget on a background
    /// task with no synchronization context, so a blocking query here cannot deadlock a request.
    ///
    /// Only LIVE mappings count, matching the catalogues the export builds
    /// (<c>MenuController</c> filters both with <c>!IsDeleted</c>). Ordered by id so a catalogue that
    /// somehow holds duplicates resolves the same way every time instead of at the database's whim —
    /// the export would throw on such a duplicate, and quietly picking a different row on each save is
    /// the one outcome worse than either.
    /// </summary>
    public static MenuVariantMasterData VariantMasterData<TDbContext>(EntityWrapper<MenuVariant> wrapper)
        where TDbContext : ShiftDbContext
    {
        var database = wrapper.Services.GetRequiredService<TDbContext>();

        var brandId = wrapper.Entity.Menu?.VehicleModel?.BrandID;
        var labourRate = wrapper.Entity.LabourRate;

        var labourRateMapping = database.Set<LabourRateMapping>()
            .AsNoTracking()
            .Where(mapping => !mapping.IsDeleted && mapping.BrandID == brandId && mapping.LabourRate == labourRate)
            .OrderBy(mapping => mapping.ID)
            .FirstOrDefault();

        var brandMapping = database.Set<BrandMapping>()
            .AsNoTracking()
            .Where(mapping => !mapping.IsDeleted && mapping.BrandID == brandId)
            .OrderBy(mapping => mapping.ID)
            .FirstOrDefault();

        return new MenuVariantMasterData(labourRateMapping, brandMapping);
    }
}
