using ShiftSoftware.ADP.Menus.Sync.Replication;
using ShiftSoftware.ShiftEntity.Model.Replication;

using System.Reflection;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// Offline guards on the replication status reader — the check that answers "did the backfill finish?".
///
/// Its failure mode is the quiet kind: a table left out of the reader is simply never counted, so the
/// report says "up to date" while that table has never reached Cosmos at all. Pure reflection and
/// arithmetic here; no database, so these always run.
/// </summary>
public class MenuReplicationStatusTests
{
    /// <summary>
    /// Every entity that opts into replication must be counted. <c>IShiftEntityReplication</c> is the
    /// opt-in — the trigger and the sweep both constrain on it, so an entity carrying it is an entity
    /// expected in Cosmos, and one that carries it without appearing here is invisible to the check.
    /// </summary>
    [Fact]
    public void CountsEveryEntityThatOptsIntoReplication()
    {
        // Anchored on an ENTITY type: the entities live in ADP.Menus.Data while the replication flows
        // (and this status reader) live in ADP.Menus.Sync, and it is the entities assembly that holds
        // everything opting into replication.
        var replicatedEntities = typeof(Data.Entities.MenuVariant).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && typeof(IShiftEntityReplication).IsAssignableFrom(type))
            .ToList();

        Assert.NotEmpty(replicatedEntities);

        Assert.Equal(
            replicatedEntities.Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal),
            MenuReplicationStatus.ReplicatedEntityTypes.Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal));
    }

    /// <summary>The ten registrations of the plan's §17 table, counted once each.</summary>
    [Fact]
    public void CoversTheTenReplicatedTablesWithoutDuplicates()
    {
        Assert.Equal(10, MenuReplicationStatus.ReplicatedEntityTypes.Count);
        Assert.Equal(10, MenuReplicationStatus.ReplicatedEntityTypes.Distinct().Count());
    }

    /// <summary>
    /// The reader must not silently report on tables it cannot query. Both bookkeeping columns are
    /// resolved by name through <c>EF.Property</c>, so a framework rename would surface as a runtime
    /// query failure rather than a compile error if the names were ever typed as literals.
    /// </summary>
    [Fact]
    public void EveryCountedEntityCarriesBothBookkeepingColumns()
    {
        foreach (var entityType in MenuReplicationStatus.ReplicatedEntityTypes)
        {
            Assert.NotNull(entityType.GetProperty(
                nameof(IShiftEntityReplication.LastReplicationStamp),
                BindingFlags.Public | BindingFlags.Instance));

            Assert.NotNull(entityType.GetProperty(
                nameof(IShiftEntityReplication.LastReplicationDate),
                BindingFlags.Public | BindingFlags.Instance));
        }
    }

    /// <summary>
    /// The arithmetic a caller reads: pending rolls up across tables, in-sync is the complement, and
    /// "up to date" means zero pending anywhere — not zero in the table you happened to look at.
    /// </summary>
    [Fact]
    public void ReportRollsUpAcrossTables()
    {
        var report = new MenuReplicationStatusReport(
        [
            new MenuReplicationTableStatus("A", Total: 10, NeverReplicated: 0, Pending: 0),
            new MenuReplicationTableStatus("B", Total: 5, NeverReplicated: 2, Pending: 3),
        ]);

        Assert.Equal(15, report.Total);
        Assert.Equal(2, report.NeverReplicated);
        Assert.Equal(3, report.Pending);
        Assert.False(report.IsUpToDate);

        Assert.Equal(10, report.Tables[0].InSync);
        Assert.Equal(2, report.Tables[1].InSync);
    }

    /// <summary>A catalogue with nothing outstanding is what a finished backfill looks like.</summary>
    [Fact]
    public void ReportIsUpToDateOnlyWhenNothingIsPending()
    {
        var report = new MenuReplicationStatusReport(
            [new MenuReplicationTableStatus("A", Total: 10, NeverReplicated: 0, Pending: 0)]);

        Assert.True(report.IsUpToDate);
    }
}
