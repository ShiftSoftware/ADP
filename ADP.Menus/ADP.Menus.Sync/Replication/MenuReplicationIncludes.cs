using Microsoft.EntityFrameworkCore;

using ShiftSoftware.ADP.Menus.Data.Entities;

namespace ShiftSoftware.ADP.Menus.Sync.Replication;

/// <summary>
/// The navigations each Cosmos projection reads, as query shapers.
///
/// There are two ways a row reaches Cosmos and they must load the SAME graph:
///
///  • the save trigger, which re-fetches one row through <see cref="MenuReplicationReload"/>;
///  • the catch-up service, which sweeps many rows through <c>MenuCatchUpReplicationExtensions</c>.
///
/// Both take their includes from here, because the failure mode of letting them drift is silent: a
/// navigation missing on one path does not throw, it produces a document with holes — an interval with
/// no code, an item with no replacement item, a period in the wrong partition because its variant's
/// menu was never loaded. Keeping one definition per entity makes "the two paths agree" structural.
///
/// Each shaper must cover exactly what the matching <see cref="MenuCosmosMappers"/> method reads, and
/// mirrors the export's own query (<c>MenuController.GenerateLinesAsync</c>) so the replicated
/// documents describe the same graph the DMS export folds.
/// </summary>
public static class MenuReplicationIncludes
{
    /// <summary>Menu (partition key) + vehicle model (brand, model name) + the embedded country rates.</summary>
    public static IQueryable<MenuVariant> Variant(IQueryable<MenuVariant> query) =>
        query
            .Include(variant => variant.Menu).ThenInclude(menu => menu.VehicleModel)
            .Include(variant => variant.LabourRates);

    /// <summary>Parent chain for the partition key, plus the interval the document denormalizes.</summary>
    public static IQueryable<MenuPeriodicAvailability> Period(IQueryable<MenuPeriodicAvailability> query) =>
        query
            .Include(period => period.MenuVariant).ThenInclude(variant => variant.Menu)
            .Include(period => period.ServiceInterval);

    /// <summary>
    /// Parent chain for the partition key, plus the interval group AND its interval membership — the
    /// membership is what decides which periodic line this labour detail supplies.
    /// </summary>
    public static IQueryable<MenuLabourDetails> Labour(IQueryable<MenuLabourDetails> query) =>
        query
            .Include(labour => labour.MenuVariant).ThenInclude(variant => variant.Menu)
            .Include(labour => labour.ServiceIntervalGroup).ThenInclude(group => group.ServiceIntervals);

    /// <summary>
    /// The widest graph: parent chain for the partition key, the embedded parts and prices, and the
    /// whole replacement-item slice — the item itself, its standalone group, and every interval group
    /// it serves with that group's own interval membership.
    /// </summary>
    public static IQueryable<MenuItem> Item(IQueryable<MenuItem> query) =>
        query
            .Include(item => item.MenuVariant).ThenInclude(variant => variant.Menu)
            .Include(item => item.Parts).ThenInclude(part => part.CountryPrices)
            .Include(item => item.ReplacementItemVehicleModel!).ThenInclude(link => link.ReplacementItem)
                .ThenInclude(replacementItem => replacementItem.StandaloneReplacementItemGroup)
            .Include(item => item.ReplacementItemVehicleModel!).ThenInclude(link => link.ReplacementItem)
                .ThenInclude(replacementItem => replacementItem.ReplacementItemServiceIntervalGroups)
                .ThenInclude(link => link.ServiceIntervalGroup).ThenInclude(group => group.ServiceIntervals)
            .AsSplitQuery();

    /// <summary>Interval membership — what lets generation match an interval to this group's labour.</summary>
    public static IQueryable<ServiceIntervalGroup> IntervalGroup(IQueryable<ServiceIntervalGroup> query) =>
        query.Include(group => group.ServiceIntervals);

    /// <summary>
    /// The standalone group and the interval groups (with their membership) a replacement item serves.
    /// Both travel with the item because its fan-out refreshes the menu item's whole replacement-item
    /// slice, not just its scalar fields.
    /// </summary>
    public static IQueryable<ReplacementItem> ReplacementItem(IQueryable<ReplacementItem> query) =>
        query
            .Include(replacementItem => replacementItem.StandaloneReplacementItemGroup)
            .Include(replacementItem => replacementItem.ReplacementItemServiceIntervalGroups)
                .ThenInclude(link => link.ServiceIntervalGroup).ThenInclude(group => group.ServiceIntervals)
            .AsSplitQuery();

    // ServiceInterval, StandaloneReplacementItemGroup, LabourRateMapping and BrandMapping project
    // scalars only, so they need no shaper on either path.
}
