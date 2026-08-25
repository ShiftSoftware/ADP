using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ShiftEntity.Core;
using Entities = ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ShiftEntity.CosmosDbReplication;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.Replication;

namespace ShiftSoftware.ADP.Menus.Sync.Replication;

/// <summary>
/// Loads what a Cosmos projection needs beyond the row the save trigger hands it.
///
/// The trigger supplies whatever EF happened to have tracked when the row was saved — which for a child
/// row is usually just the row itself, with every navigation null. Since the projections denormalize
/// heavily (a period needs its variant's menu for the partition key AND the interval's own code; an
/// item needs its parts, its replacement item and every interval group that item serves), they would
/// otherwise produce documents with holes, silently.
///
/// The include graphs themselves live in <see cref="MenuReplicationIncludes"/>, shared with the
/// catch-up sweep so the two paths cannot load different graphs.
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

    // ---- per-table reload hooks ------------------------------------------------------------------
    // Each is the matching MenuReplicationIncludes shaper, wrapped in the reload above.

    public static Func<EntityWrapper<MenuVariant>, ValueTask<MenuVariant>> Variant<TDbContext>()
        where TDbContext : ShiftDbContext =>
        With<TDbContext, MenuVariant>(MenuReplicationIncludes.Variant);

    public static Func<EntityWrapper<MenuPeriodicAvailability>, ValueTask<MenuPeriodicAvailability>> Period<TDbContext>()
        where TDbContext : ShiftDbContext =>
        With<TDbContext, MenuPeriodicAvailability>(MenuReplicationIncludes.Period);

    public static Func<EntityWrapper<MenuLabourDetails>, ValueTask<MenuLabourDetails>> Labour<TDbContext>()
        where TDbContext : ShiftDbContext =>
        With<TDbContext, MenuLabourDetails>(MenuReplicationIncludes.Labour);

    public static Func<EntityWrapper<MenuItem>, ValueTask<MenuItem>> Item<TDbContext>()
        where TDbContext : ShiftDbContext =>
        With<TDbContext, MenuItem>(MenuReplicationIncludes.Item);

    public static Func<EntityWrapper<ServiceIntervalGroup>, ValueTask<ServiceIntervalGroup>> IntervalGroup<TDbContext>()
        where TDbContext : ShiftDbContext =>
        With<TDbContext, ServiceIntervalGroup>(MenuReplicationIncludes.IntervalGroup);

    public static Func<EntityWrapper<Entities.ReplacementItem>, ValueTask<Entities.ReplacementItem>> ReplacementItem<TDbContext>()
        where TDbContext : ShiftDbContext =>
        With<TDbContext, Entities.ReplacementItem>(MenuReplicationIncludes.ReplacementItem);

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
    /// Resolves <see cref="MenuVariantMasterData"/> for a variant, straight from the database.
    ///
    /// SYNCHRONOUS, deliberately: it is called from the <c>Replicate</c> mapper, which the framework
    /// defines as a synchronous delegate, and there is no navigation from a variant to either mapping
    /// so the async reload hook cannot carry them. Replication runs fire-and-forget on a background
    /// task with no synchronization context, so a blocking query here cannot deadlock a request.
    ///
    /// The catch-up sweep resolves the same thing against pre-loaded catalogues rather than one query
    /// per variant — both go through <see cref="SelectLabourRateMapping"/> and
    /// <see cref="SelectBrandMapping"/> so the selection rule itself is shared.
    /// </summary>
    public static MenuVariantMasterData VariantMasterData<TDbContext>(EntityWrapper<MenuVariant> wrapper)
        where TDbContext : ShiftDbContext
    {
        var database = wrapper.Services.GetRequiredService<TDbContext>();

        var brandId = wrapper.Entity.Menu?.VehicleModel?.BrandID;

        return new MenuVariantMasterData(
            SelectLabourRateMapping(database.Set<LabourRateMapping>().AsNoTracking(), brandId, wrapper.Entity.LabourRate),
            SelectBrandMapping(database.Set<BrandMapping>().AsNoTracking(), brandId));
    }

    /// <summary>
    /// The mapping a variant's labour code resolves through, or null when the pair is unmapped — which
    /// is meaningful: generation throws on a missing pair (open item O1), so a null must be recorded
    /// rather than papered over.
    ///
    /// Only LIVE mappings count, matching the catalogue the export builds (<c>MenuController</c>
    /// filters with <c>!IsDeleted</c>). Ordered by id so a catalogue that somehow holds duplicates
    /// resolves the same way every time instead of at the database's whim — the export would throw on
    /// such a duplicate, and quietly picking a different row on each run is the one outcome worse than
    /// either.
    /// </summary>
    /// <param name="candidates">
    /// The catalogue to search. An <see cref="IQueryable"/> so the trigger can pass a DbSet and have
    /// this run as one server-side query, while the sweep passes a pre-loaded list.
    /// </param>
    public static LabourRateMapping? SelectLabourRateMapping(
        IQueryable<LabourRateMapping> candidates, long? brandId, decimal labourRate) =>
        candidates
            .Where(mapping => !mapping.IsDeleted && mapping.BrandID == brandId && mapping.LabourRate == labourRate)
            .OrderBy(mapping => mapping.ID)
            .FirstOrDefault();

    /// <summary>
    /// The brand's mapping, or null when the brand is unmapped — valid, and the generator falls back to
    /// the "Z" abbreviation. Same live-only, deterministic-order rules as
    /// <see cref="SelectLabourRateMapping"/>.
    /// </summary>
    public static BrandMapping? SelectBrandMapping(IQueryable<BrandMapping> candidates, long? brandId) =>
        candidates
            .Where(mapping => !mapping.IsDeleted && mapping.BrandID == brandId)
            .OrderBy(mapping => mapping.ID)
            .FirstOrDefault();
}
