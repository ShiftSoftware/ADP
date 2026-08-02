using Microsoft.EntityFrameworkCore;

using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication;

namespace ShiftSoftware.ADP.Menus.Data.Replication;

/// <summary>One replicated table's standing against Cosmos.</summary>
/// <param name="Table">The entity type name — the table, as the replication registrations name it.</param>
/// <param name="Total">Rows in the table, soft-deleted ones included (they replicate too).</param>
/// <param name="NeverReplicated">
/// Rows that have never reached Cosmos: no replication stamp at all. A subset of
/// <paramref name="Pending"/>.
/// </param>
/// <param name="Pending">Rows whose current version is not the one in Cosmos — never replicated, or edited since.</param>
public sealed record MenuReplicationTableStatus(string Table, int Total, int NeverReplicated, int Pending)
{
    /// <summary>Rows whose current version is the one in Cosmos.</summary>
    public int InSync => Total - Pending;
}

/// <summary>Every replicated table's standing, and whether the catalogue as a whole is caught up.</summary>
/// <param name="Tables">One entry per table, in the order <c>ReplicateAllAsync</c> sweeps them.</param>
public sealed record MenuReplicationStatusReport(IReadOnlyList<MenuReplicationTableStatus> Tables)
{
    /// <summary>Rows across every replicated table.</summary>
    public int Total => Tables.Sum(table => table.Total);

    /// <summary>Rows that have never reached Cosmos.</summary>
    public int NeverReplicated => Tables.Sum(table => table.NeverReplicated);

    /// <summary>Rows whose current version is not the one in Cosmos.</summary>
    public int Pending => Tables.Sum(table => table.Pending);

    /// <summary>True when every row's current version is in Cosmos — what a finished backfill looks like.</summary>
    public bool IsUpToDate => Pending == 0;
}

/// <summary>
/// Reads how far the menu catalog's Cosmos replication has actually got — the answer to "did the
/// backfill finish?", which nothing else in the pipeline will tell you.
///
/// <para><b>Why this has to exist.</b> Both replication paths are quiet about partial failure. The save
/// trigger is fire-and-forget, so a failed row is a log line at most. The catch-up sweep catches a
/// failed row, marks it unsuccessful and moves on — leaving its stamp null and logging nothing — so a
/// sweep that wrote 1060 of 1188 documents returns exactly like one that wrote all of them. (It is
/// idempotent and self-healing across runs, which is what makes that survivable: the next run picks up
/// precisely the rows that failed. But a single sweep is not a guarantee, and this is how you see the
/// difference.)</para>
///
/// <para><b>The reading.</b> <c>LastReplicationDate</c> is a watermark, not a timestamp: the pipeline
/// copies the replicated row version's <c>LastSaveDate</c> into it, so equality means "in sync", a later
/// save moves the row behind its watermark, and null means never replicated. Counting rows that fail
/// that equality is therefore the same question the sweep's own dirty-only pass asks — this reports what
/// <c>updateAll: false</c> would pick up.
///
/// <c>NeverReplicated</c> is reported separately because it is the sharper signal: it counts rows with
/// no stamp at all, which after a full sweep means the write for that row FAILED rather than simply
/// being outrun by a later edit.</para>
///
/// <para>One caveat worth knowing: this compares SQL against SQL. It proves the pipeline believes every
/// row is replicated, not that the documents are correct — a stale document produced by an edit that
/// bypassed its owning row (open item O2's system-wide parts-price update) leaves the row clean and
/// invisible here. That case is what <c>updateAll: true</c> is for.</para>
/// </summary>
public static class MenuReplicationStatus
{
    /// <summary>
    /// The ten replicated tables, in the order <c>MenuCatchUpReplicationExtensions.ReplicateAllAsync</c>
    /// sweeps them: the model-partition documents first, then the master tables.
    /// </summary>
    private static readonly IReadOnlyList<TableReader> Readers =
    [
        // The ServiceMenus container.
        For<MenuVariant>(),
        For<MenuPeriodicAvailability>(),
        For<MenuLabourDetails>(),
        For<MenuItem>(),

        // A container each, plus their fan-outs onto the documents above.
        For<ServiceInterval>(),
        For<ServiceIntervalGroup>(),
        For<Entities.ReplacementItem>(),
        For<StandaloneReplacementItemGroup>(),
        For<LabourRateMapping>(),
        For<BrandMapping>(),
    ];

    /// <summary>
    /// The entity types this reports on, in sweep order.
    ///
    /// Public so a test can assert it against the types that actually implement
    /// <see cref="IShiftEntityReplication"/>: a table added to the replication but forgotten here would
    /// otherwise never be counted, and the report would say "up to date" while that table had never
    /// reached Cosmos at all.
    /// </summary>
    public static IReadOnlyList<Type> ReplicatedEntityTypes { get; } =
        Readers.Select(reader => reader.EntityType).ToList();

    /// <summary>Counts every replicated table. Two <c>COUNT</c>s per table, no rows materialised.</summary>
    /// <param name="database">
    /// A context that maps the menu tables the SAME way the writing host does — see
    /// <c>MenuReplicationDB</c> in the sample Functions host for why a second context is only safe when
    /// its model agrees.
    /// </param>
    /// <param name="cancellationToken">Cancellation.</param>
    public static async Task<MenuReplicationStatusReport> ReadAsync(
        DbContext database,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);

        var tables = new List<MenuReplicationTableStatus>(Readers.Count);

        foreach (var reader in Readers)
            tables.Add(await reader.Read(database, cancellationToken));

        return new MenuReplicationStatusReport(tables);
    }

    private static TableReader For<TEntity>()
        where TEntity : class, IShiftEntityAudit, IShiftEntityReplication =>
        new(typeof(TEntity), ReadTableAsync<TEntity>);

    private static async Task<MenuReplicationTableStatus> ReadTableAsync<TEntity>(
        DbContext database,
        CancellationToken cancellationToken)
        where TEntity : class, IShiftEntityAudit, IShiftEntityReplication
    {
        var rows = database.Set<TEntity>().AsNoTracking();

        // EF.Property rather than a plain member access: both bookkeeping columns are declared on
        // interfaces the entity implements, and only this form is certain to translate for an open
        // generic. The names come from nameof, so a framework rename breaks the build, not the query.
        var total = await rows.CountAsync(cancellationToken);

        var neverReplicated = await rows.CountAsync(
            row => EF.Property<string?>(row, nameof(IShiftEntityReplication.LastReplicationStamp)) == null,
            cancellationToken);

        var pending = await rows.CountAsync(
            row => EF.Property<DateTimeOffset?>(row, nameof(IShiftEntityReplication.LastReplicationDate)) == null
                || EF.Property<DateTimeOffset?>(row, nameof(IShiftEntityReplication.LastReplicationDate))
                    != EF.Property<DateTimeOffset>(row, nameof(IShiftEntityAudit.LastSaveDate)),
            cancellationToken);

        return new MenuReplicationTableStatus(typeof(TEntity).Name, total, neverReplicated, pending);
    }

    private sealed record TableReader(
        Type EntityType,
        Func<DbContext, CancellationToken, Task<MenuReplicationTableStatus>> Read);
}
