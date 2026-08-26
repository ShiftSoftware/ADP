using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent.Configurations;

/// <summary>
/// One secondary index the DuckDB destination creates on its table, next to the PRIMARY KEY the
/// engine already indexes for its upserts.
///
/// <para>Indexes are the WRITER's declaration of how the table will be READ. The destination writes
/// batches and never queries by anything but the key, so nothing here speeds the sync up — it is
/// declared with the sync because the sync is what owns the schema: whoever reads the table
/// afterwards (a lookup service joining by a foreign id, a report filtering by a code) is the one
/// paying for a missing index, and the writer is the only place that can hand them one.</para>
///
/// <para>The columns are named the same way <c>PrimaryKey</c> is — a member expression over the
/// destination row, single (<c>row =&gt; row.MenuID</c>) or composite
/// (<c>row =&gt; new { row.MenuVariantID, row.CountryID }</c>) — so a renamed property is a compile
/// error rather than an index that silently stops matching. <see cref="SqlExpressions"/> is the
/// escape hatch for what a member expression cannot say.</para>
///
/// <para>Deliberately no <c>IsUnique</c>: a second UNIQUE/PRIMARY KEY constraint on the same table
/// makes DuckDB demand an explicit conflict target from the <c>INSERT OR REPLACE</c> the destination
/// upserts with, so declaring one here would break the very batches it was declared alongside.
/// Uniqueness belongs to <c>PrimaryKey</c>; this type is for read paths.</para>
/// </summary>
public class DuckDBIndexDefinition<TDestination>
    where TDestination : class
{
    /// <summary>
    /// The indexed columns as a member expression over the destination row — one property, or a
    /// composite via an anonymous type / array, in the order they should be indexed.
    /// </summary>
    public Expression<Func<TDestination, object>>? Columns { get; set; }

    /// <summary>
    /// Raw DuckDB index expressions, for what a member expression cannot reach — a function over a
    /// column (<c>lower("Code")</c>), an extraction from a JSON column. Emitted VERBATIM, so quote
    /// identifiers yourself; this is author-written SQL, never a value from the data being synced.
    /// Appended after <see cref="Columns"/> when both are set.
    /// </summary>
    public IReadOnlyList<string>? SqlExpressions { get; set; }

    /// <summary>
    /// The index name. Defaults to <c>IX_{table}_{columns}</c> — deterministic on purpose, because
    /// the destination creates indexes with <c>IF NOT EXISTS</c> on every run and a name that
    /// changed between runs would leave the old index behind. Set it explicitly when two indexes on
    /// one table would otherwise derive the same name.
    /// </summary>
    public string? Name { get; set; }
}
