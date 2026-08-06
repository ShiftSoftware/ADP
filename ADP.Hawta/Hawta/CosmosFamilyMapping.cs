using System.Text.Json;

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

    /// <summary>Maps a snapshot row to its document. Must be deterministic and total for rows the predicate accepts.</summary>
    public required Func<DirtyRow, CosmosDocument> Map { get; init; }
}
