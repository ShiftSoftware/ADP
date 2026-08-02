using Microsoft.EntityFrameworkCore;

using ShiftSoftware.ADP.Menus.Data.Entities;

namespace ShiftSoftware.ADP.Menus.Data.DataServices;

/// <summary>
/// The DMS export's query graph, with soft-deleted rows filtered out AT THE DATABASE.
///
/// <para>Lives here rather than in the controller so it sits beside
/// <see cref="EfToGenerationAggregator"/>, whose <c>LivePeriods</c> / <c>LiveLabours</c> /
/// <c>LiveItems</c> / <c>LiveIntervalGroupLinks</c> these predicates mirror one for one. They are meant
/// to be read side by side; the same reason <c>MenuReplicationIncludes</c> exists for the replication
/// paths.</para>
///
/// <para><b>The database filter is the primary one; the adapter's is the authority.</b> That is not
/// belt-and-braces for its own sake — the two do different amounts of work:</para>
///
/// <list type="bullet">
/// <item>Only the database can avoid LOADING a deleted row, which is the whole point of filtering
/// here.</item>
/// <item>Only the adapter can express <b>"keep the item, drop its deleted standalone group"</b>. EF
/// filtered includes work on collection navigations only, and
/// <c>ReplacementItem.StandaloneReplacementItemGroup</c> is a reference — there is no way to load the
/// item while nulling its group. So that one rule is unavoidably adapter-side.</item>
/// <item>The agreement test that keeps the export and the vehicle lookup producing identical menu codes
/// (<c>ReplicateThenRead_AgreesWithTheExport_WhenARowIsSoftDeleted</c>) builds its graph in memory and
/// never runs this query. Move the rule here ALONE and that test stops guarding the export — it would
/// stay green while the two paths diverged.</item>
/// </list>
///
/// <para>Because the adapter re-applies the same rule, these predicates can only ever remove rows it
/// would remove anyway. They cannot change the export's output — only how much of it travels.</para>
///
/// <para><b>EF constraint to know before editing:</b> <c>Items</c> is included three times (a chain has
/// to restart from <c>Include</c> to branch), and EF Core throws at query time if the same navigation is
/// included more than once with DIFFERENT filters. The three predicates below must stay character-for-
/// character identical. <c>MenuExportIncludesTests</c> compiles the query to SQL offline, which is what
/// catches it if they drift.</para>
/// </summary>
public static class MenuExportIncludes
{
    /// <summary>
    /// Applies the export's include graph to an already-filtered variant query.
    ///
    /// The caller still owns the ROOT filters — <c>!variant.IsDeleted</c>, <c>!variant.Menu.IsDeleted</c>
    /// and the brand selection — because the brand list is a per-run choice, not part of this shape.
    /// </summary>
    public static IQueryable<MenuVariant> Apply(IQueryable<MenuVariant> query) =>
        query
            .Include(x => x.Items.Where(item => !item.IsDeleted && item.ReplacementItemVehicleModel != null && !item.ReplacementItemVehicleModel.IsDeleted && (item.ReplacementItemVehicleModel.ReplacementItem == null || !item.ReplacementItemVehicleModel.ReplacementItem.IsDeleted)))
                .ThenInclude(x => x.Parts.Where(part => !part.IsDeleted))
                .ThenInclude(x => x.CountryPrices.Where(price => !price.IsDeleted))

            .Include(x => x.Items.Where(item => !item.IsDeleted && item.ReplacementItemVehicleModel != null && !item.ReplacementItemVehicleModel.IsDeleted && (item.ReplacementItemVehicleModel.ReplacementItem == null || !item.ReplacementItemVehicleModel.ReplacementItem.IsDeleted)))
                .ThenInclude(x => x.ReplacementItemVehicleModel!.ReplacementItem)
                .ThenInclude(x => x.ReplacementItemServiceIntervalGroups.Where(link => !link.IsDeleted && !link.ServiceIntervalGroup.IsDeleted))
                .ThenInclude(x => x.ServiceIntervalGroup)
                .ThenInclude(x => x.ServiceIntervals.Where(interval => !interval.IsDeleted))

            // The standalone group is a REFERENCE navigation, so it cannot be filtered here. A deleted
            // one is loaded and then nulled by EfToGenerationAggregator, which is what turns the item
            // into an ungrouped standalone line instead of dropping it.
            .Include(x => x.Items.Where(item => !item.IsDeleted && item.ReplacementItemVehicleModel != null && !item.ReplacementItemVehicleModel.IsDeleted && (item.ReplacementItemVehicleModel.ReplacementItem == null || !item.ReplacementItemVehicleModel.ReplacementItem.IsDeleted)))
                .ThenInclude(x => x.ReplacementItemVehicleModel!.ReplacementItem)
                .ThenInclude(x => x.StandaloneReplacementItemGroup)

            .Include(x => x.PeriodicAvailabilities.Where(period => !period.IsDeleted && !period.ServiceInterval.IsDeleted))
                .ThenInclude(x => x.ServiceInterval)

            .Include(x => x.LabourDetails.Where(labour => !labour.IsDeleted && !labour.ServiceIntervalGroup.IsDeleted))
                .ThenInclude(x => x.ServiceIntervalGroup)
                .ThenInclude(x => x.ServiceIntervals.Where(interval => !interval.IsDeleted))

            .Include(x => x.LabourRates.Where(rate => !rate.IsDeleted))

            .Include(x => x.Menu).ThenInclude(x => x.VehicleModel)

            .AsSplitQuery();
}
