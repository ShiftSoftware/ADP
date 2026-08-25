namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// The validated set of sources one agent runs — the replacement for attribute-scattered
/// worker classes: generic dispatch iterates this, and adding a source is one entry.
/// Validation happens once, at construction, so a mis-wired registry fails at startup
/// rather than mid-cycle.
/// </summary>
public sealed class SourceRegistry
{
    private readonly Dictionary<string, SnapshotSource> byKey;

    public SourceRegistry(IEnumerable<SnapshotSource> sources)
    {
        Sources = sources.ToList();

        if (Sources.Count == 0)
            throw new ArgumentException("A source registry needs at least one source.", nameof(sources));

        byKey = new Dictionary<string, SnapshotSource>(StringComparer.OrdinalIgnoreCase);
        var tablesByName = new Dictionary<string, SnapshotTableDefinition>(StringComparer.OrdinalIgnoreCase);
        var familiesByTable = new Dictionary<string, IReadOnlyList<CosmosFamilyMapping>?>(StringComparer.OrdinalIgnoreCase);
        var tables = new List<SnapshotTableDefinition>();

        foreach (var source in Sources)
        {
            if (string.IsNullOrWhiteSpace(source.Key))
                throw new ArgumentException("A source key must be non-blank.", nameof(sources));
            if (source.SourceScope is not null && string.IsNullOrWhiteSpace(source.SourceScope))
                throw new ArgumentException(
                    $"Source '{source.Key}': source scope must be non-blank when present; use null for unscoped.",
                    nameof(sources));

            if (!byKey.TryAdd(source.Key, source))
                throw new ArgumentException($"Duplicate source key '{source.Key}'.", nameof(sources));

            // Exactly one ingest delegate. None is a source that is scheduled and then does
            // nothing; more than one is a source whose behaviour depends on which branch the
            // dispatcher happens to prefer. Validated at construction, so the answer arrives at
            // startup rather than on the first cadence tick.
            //
            // THREE states, not two, and the widening had to land BEFORE any source changed form:
            // a source adopting Fetch sets neither of the old two, so against the old table every
            // migrated source would have failed at startup reading as "it has neither".
            var declared = new List<string>(3);
            if (source.HasSynchronousIngest) declared.Add(nameof(SnapshotSource.Ingest));
            if (source.IngestAsync is not null) declared.Add(nameof(SnapshotSource.IngestAsync));
            if (source.Fetch is not null) declared.Add(nameof(SnapshotSource.Fetch));

            if (declared.Count != 1)
            {
                throw new ArgumentException(
                    $"Source '{source.Key}': set exactly one of Ingest, IngestAsync or Fetch — it has " +
                    (declared.Count == 0 ? "none." : $"{declared.Count} ({string.Join(", ", declared)})."),
                    nameof(sources));
            }

            if (source.ConcurrencyGroup is not null && string.IsNullOrWhiteSpace(source.ConcurrencyGroup))
            {
                throw new ArgumentException(
                    $"Source '{source.Key}': concurrency group must be non-blank when present; use null to opt out.",
                    nameof(sources));
            }

            if (source.CosmosRead is { } cosmosRead
                && (string.IsNullOrWhiteSpace(cosmosRead.Database) || string.IsNullOrWhiteSpace(cosmosRead.Container)))
            {
                throw new ArgumentException(
                    $"Source '{source.Key}': a Cosmos read must name both a database and a container.",
                    nameof(sources));
            }

            if (source.Cadence <= TimeSpan.Zero)
                throw new ArgumentException($"Source '{source.Key}': cadence must be positive.", nameof(sources));

            if (source.ReplicationBatchSize <= 0)
                throw new ArgumentException(
                    $"Source '{source.Key}': replication batch size must be positive.",
                    nameof(sources));

            if (source.ReplicationMaxInFlightRows <= 0)
                throw new ArgumentException(
                    $"Source '{source.Key}': replication max in-flight rows must be positive.",
                    nameof(sources));

            if (tablesByName.TryGetValue(source.Table.Name, out var existingTable))
            {
                // Same table from several sources (per-dealer scopes): one definition
                // instance, or the sources WILL disagree about columns eventually.
                if (!ReferenceEquals(existingTable, source.Table))
                    throw new ArgumentException(
                        $"Source '{source.Key}': table '{source.Table.Name}' is declared by multiple sources " +
                        "with different SnapshotTableDefinition instances — share one instance.",
                        nameof(sources));

                // Same rule for the families list: the pump runs per TABLE, so two sources
                // publishing the same table with different mappings would be order-dependent.
                if (!ReferenceEquals(familiesByTable[source.Table.Name], source.Families))
                    throw new ArgumentException(
                        $"Source '{source.Key}': table '{source.Table.Name}' is declared by multiple sources " +
                        "with different Families lists — share one instance (mappings resolve per-scope " +
                        "values from DirtyRow.SourceScope).",
                        nameof(sources));

                // Pump gating is per table too — half-on would be order-dependent.
                var sibling = Sources.First(s =>
                    s.Table.Name.Equals(source.Table.Name, StringComparison.OrdinalIgnoreCase));
                if (sibling.ReplicationEnabled != source.ReplicationEnabled)
                    throw new ArgumentException(
                        $"Source '{source.Key}': table '{source.Table.Name}' has sources disagreeing on " +
                        "ReplicationEnabled — the pump runs per table; they must agree.",
                        nameof(sources));

                if (sibling.ReplicationBatchSize != source.ReplicationBatchSize)
                    throw new ArgumentException(
                        $"Source '{source.Key}': table '{source.Table.Name}' has sources disagreeing on " +
                        "ReplicationBatchSize — the pump runs per table; they must agree.",
                        nameof(sources));

                if (sibling.ReplicationMaxInFlightRows != source.ReplicationMaxInFlightRows)
                    throw new ArgumentException(
                        $"Source '{source.Key}': table '{source.Table.Name}' has sources disagreeing on " +
                        "ReplicationMaxInFlightRows — the pump runs per table; they must agree.",
                        nameof(sources));

                var scopeOwner = byKey.Values.FirstOrDefault(candidate =>
                    !ReferenceEquals(candidate, source)
                    && candidate.Table.Name.Equals(source.Table.Name, StringComparison.OrdinalIgnoreCase)
                    && NullableOrdinalIgnoreCaseComparer.Instance.Equals(
                        candidate.SourceScope, source.SourceScope));
                if (scopeOwner is not null)
                    throw new ArgumentException(
                        $"Sources '{scopeOwner.Key}' and '{source.Key}' both claim table " +
                        $"'{source.Table.Name}' scope '{source.SourceScope ?? "<null>"}'. " +
                        "Each table/scope must have exactly one source owner or unchanged rows will churn identity.",
                        nameof(sources));
            }
            else
            {
                tablesByName.Add(source.Table.Name, source.Table);
                familiesByTable.Add(source.Table.Name, source.Families);
                tables.Add(source.Table);
            }
        }

        Tables = tables;

        // Lazy-residency qualification — COMPUTED from the declared shape, never a flag a
        // host sets per table. A table can be deferred at cold start only when every source
        // feeding it can answer "unchanged" without reading data, which today means a
        // declarative file source with a change gate wired, and when no source pins it.
        // Disagreement resolves to Resident; nothing is refused — a mixed table simply keeps
        // today's behaviour. Evaluated over the declared shape regardless of Enabled: a dark
        // source never ticks, so it never forces a load, and its previously published rows
        // ride the deferred copy like everyone else's.
        foreach (var group in Sources.GroupBy(s => s.Table.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (group.All(s => s.FileIngestion is { ChangeGate: not null } && !s.PinResident))
                deferredCapableTables.Add(group.Key);

            if (group.Any(s => s is { Families.Count: > 0, ReplicationEnabled: true }))
                replicationEnabledFamilyTables.Add(group.Key);
        }
    }

    private readonly HashSet<string> deferredCapableTables = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> replicationEnabledFamilyTables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// True when every source feeding the table is a gate-wired file source and none pins it —
    /// the precondition for a cold start to leave the table's rows in the published copy.
    ///
    /// <para><b>Operational rule that cannot be machine-enforced, so it is stated here:</b>
    /// never remove a deferred-capable source from the configuration without first taking its
    /// table Resident (change the feed, or pin it, and let a publish commit resident rows). A
    /// table absent from the registry drops out of every new manifest, and once the older
    /// manifests age out of retention its parquet survives only as an unreferenced stray no
    /// rebuild can restore. For a resident table that is an inconvenience; for a deferred one
    /// it strands the only copy of its rows outside every restore path.</para>
    /// </summary>
    public bool IsTableDeferredCapable(string tableName) => deferredCapableTables.Contains(tableName);

    /// <summary>
    /// True when the table feeds a Cosmos family AND some source currently has replication
    /// enabled — the configuration under which a cold start must prove the committed copy owes
    /// the pump nothing (manifest <c>replicationPending</c> = 0) before it may defer the table.
    /// </summary>
    public bool TableHasReplicationEnabledCosmosFamily(string tableName) =>
        replicationEnabledFamilyTables.Contains(tableName);

    /// <summary>All sources, registry order (enabled or not).</summary>
    public IReadOnlyList<SnapshotSource> Sources { get; }

    /// <summary>
    /// Every distinct table, registry order — the ensure/publish/rebuild set. Always the
    /// full list: a table absent from the publish set loses its DR seed.
    /// </summary>
    public IReadOnlyList<SnapshotTableDefinition> Tables { get; }

    public SnapshotSource this[string key] =>
        byKey.TryGetValue(key, out var source)
            ? source
            : throw new KeyNotFoundException($"No source with key '{key}' in the registry.");

    public bool TryGet(string key, out SnapshotSource source) =>
        byKey.TryGetValue(key, out source!);
}
