using System.Text.Json;
using System.Globalization;
using System.Linq.Expressions;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// A mapped Cosmos document: the coordinates (<see cref="Id"/> + hierarchical
/// <see cref="PartitionKey"/>) and the body properties. The coordinates ARE the
/// <c>CosmosKeyPolicy</c> — they must reproduce the incumbent writer's document coordinates
/// exactly (the Cosmos contract wins; no re-key ever).
/// </summary>
public sealed class CosmosDocument
{
    public required string Id { get; init; }

    /// <summary>
    /// Hierarchical partition-key values, in level order (string, number, bool, or null —
    /// a null level is written as an EXPLICIT null partition-key value, matching writers
    /// that use <c>AddNullValue</c>; it is not <c>PartitionKey.None</c>).
    /// </summary>
    public required IReadOnlyList<object?> PartitionKey { get; init; }

    /// <summary>Document properties. <c>id</c> is set from <see cref="Id"/> automatically.</summary>
    public required IDictionary<string, object?> Body { get; init; }

    /// <summary>
    /// Creates a document body from a typed Cosmos DTO. Property names and value types are
    /// therefore compiler-checked at the mapping site; serialization is performed once into
    /// the engine's dictionary contract. The DTO's <c>id</c> member, when present, is removed
    /// because <see cref="Id"/> is the authoritative coordinate and the transport adds it.
    /// </summary>
    public static CosmosDocument FromModel<TModel>(
        string id,
        IReadOnlyList<object?> partitionKey,
        TModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var element = JsonSerializer.SerializeToElement(model);
        if (element.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("A Cosmos document model must serialize to a JSON object.", nameof(model));

        var body = element.EnumerateObject()
            .Where(property => !property.NameEquals("id"))
            .ToDictionary(
                property => property.Name,
                property => (object?)property.Value.Clone(),
                StringComparer.Ordinal);

        return new CosmosDocument
        {
            Id = id,
            PartitionKey = partitionKey,
            Body = body,
        };
    }
}

/// <summary>
/// One replication family over a snapshot table: which rows belong to it (predicate — one
/// source may fan out to several families, e.g. an order-line table splitting into labor and
/// part lines) and how a row maps to its Cosmos document. Client repos own these mappings;
/// the engine owns everything else.
/// </summary>
public sealed class CosmosFamilyMapping
{
    /// <summary>Family name — used in the replication stamp, recon ops, and diagnostics. Stable.</summary>
    public required string Family { get; init; }

    public required string Database { get; init; }
    public required string Container { get; init; }

    /// <summary>Which live rows belong to this family. Null = every row. Rows matching no family are marked replication-excluded.</summary>
    public Func<DirtyRow, bool>? Predicate { get; init; }

    /// <summary>
    /// Maps one snapshot row to its document. Must be deterministic and total for rows the
    /// predicate accepts. Exactly one of this or <see cref="Grouping"/> is configured.
    /// </summary>
    public Func<DirtyRow, CosmosDocument>? Map { get; init; }

    /// <summary>
    /// Typed many-source-rows-to-one-document projection. Hawta batches dirty work by its
    /// declared group key, fetches every affected group in one set-based DuckDB query, and
    /// runs the typed reducer once per group.
    /// </summary>
    public CosmosGroupProjection? Grouping { get; init; }
}

/// <summary>
/// Non-generic engine view of a typed group projection. Client mappings normally instantiate
/// <see cref="CosmosGroupProjection{TRow,TGroupKey,TProjection}"/> directly.
/// </summary>
public abstract class CosmosGroupProjection
{
    internal abstract SnapshotTableDefinition Table { get; }
    internal abstract string GroupColumn { get; }
    internal abstract string OrderColumn { get; }
    internal abstract string StorageKey(DirtyRow row);
    internal abstract CosmosDocument? Project(IReadOnlyList<DirtyRow> orderedLiveRows);
}

/// <summary>
/// Compile-time-oriented group mapping: a typed source row declares the direct group and
/// order properties, a typed reducer builds one canonical projection, and a typed mapper
/// turns that projection into the destination document. Returning null from the reducer
/// excludes the group (for example, a blank external key).
/// </summary>
public sealed class CosmosGroupProjection<TRow, TGroupKey, TProjection> : CosmosGroupProjection
    where TRow : new()
    where TGroupKey : notnull
    where TProjection : class
{
    private readonly SnapshotTableDefinition<TRow> table;
    private readonly Func<TRow, TGroupKey> groupKey;
    private readonly Func<TGroupKey, IReadOnlyList<TRow>, TProjection?> reduce;
    private readonly Func<TGroupKey, TProjection, CosmosDocument> map;

    public CosmosGroupProjection(
        SnapshotTableDefinition<TRow> table,
        Expression<Func<TRow, TGroupKey>> groupKey,
        Expression<Func<TRow, long?>> orderBy,
        Func<TGroupKey, IReadOnlyList<TRow>, TProjection?> reduce,
        Func<TGroupKey, TProjection, CosmosDocument> map)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(groupKey);
        ArgumentNullException.ThrowIfNull(orderBy);
        ArgumentNullException.ThrowIfNull(reduce);
        ArgumentNullException.ThrowIfNull(map);

        this.table = table;
        this.groupKey = groupKey.Compile();
        this.reduce = reduce;
        this.map = map;
        GroupKeyColumn = table.Column(groupKey);
        OrderByColumn = table.Column(orderBy);
    }

    /// <summary>The compiler-checked source property used as the aggregate key.</summary>
    public string GroupKeyColumn { get; }

    /// <summary>The compiler-checked source property preserving source encounter order.</summary>
    public string OrderByColumn { get; }

    /// <summary>Projects one already-ordered typed group; useful for adapter-level contract tests.</summary>
    public CosmosDocument? Project(TGroupKey key, IReadOnlyList<TRow> orderedRows)
    {
        var projection = reduce(key, orderedRows);
        return projection is null ? null : map(key, projection);
    }

    internal override SnapshotTableDefinition Table => table;
    internal override string GroupColumn => GroupKeyColumn;
    internal override string OrderColumn => OrderByColumn;

    internal override string StorageKey(DirtyRow row)
    {
        var value = groupKey(table.Read(row));
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    internal override CosmosDocument? Project(IReadOnlyList<DirtyRow> orderedLiveRows)
    {
        if (orderedLiveRows.Count == 0)
            return null;

        var typed = orderedLiveRows.Select(table.Read).ToList();
        var key = groupKey(typed[0]);
        return Project(key, typed);
    }
}
