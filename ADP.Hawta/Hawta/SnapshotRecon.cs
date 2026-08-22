using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Cosmos;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>One family's recon configuration: the mapping (shared with the pump) plus how to enumerate the family's actual documents.</summary>
public sealed class SnapshotReconFamily
{
    public required CosmosFamilyMapping Mapping { get; init; }

    /// <summary>
    /// Cosmos SQL returning the family's FULL documents, e.g.
    /// <c>SELECT * FROM c WHERE c.ItemType = 'CatalogPart' AND c.Location = ''</c>.
    /// Must select whole documents — the comparison hashes them.
    /// </summary>
    public required string EnumerationSql { get; init; }

    /// <summary>
    /// The container's partition key paths (e.g. <c>/VIN</c>, <c>/ItemType</c>, <c>/CompanyID</c>) —
    /// used to extract actual documents' coordinates so same-id documents under different
    /// keys (e.g. the two stock locations) never cross-match.
    /// </summary>
    public required IReadOnlyList<string> PartitionKeyPaths { get; init; }

    /// <summary>
    /// Restricts this entry to one snapshot table's rows (by table name). Null = every table
    /// in the run. This is how a SHARED document space fed by several tables recons: one
    /// entry per table, same <see cref="Label"/> (JPM + NonJPM both produce CatalogPart docs).
    /// </summary>
    public string? SourceTable { get; init; }

    /// <summary>
    /// The comparison-group label; defaults to the mapping's family name. Entries with the
    /// SAME label share one actual-document enumeration (it runs once) and one verdict row.
    /// Distinct labels with the same family name keep separate universes — the stock feeds
    /// are both family "StockPart" but recon as "StockPart@UZ-UZ-BS" / "StockPart@UZ-UZ-FZS".
    /// </summary>
    public string? Label { get; init; }

    internal string ResolvedLabel => Label ?? Mapping.Family;
}

public sealed class SnapshotReconOptions
{
    /// <summary>
    /// The snapshot tables whose rows produce this recon's expected documents — usually one,
    /// but a family whose document space is fed by SEVERAL tables (JPM + NonJPM both writing
    /// CatalogPart docs) must recon them together, or each table reads the other's documents
    /// as orphans.
    /// </summary>
    public required IReadOnlyList<SnapshotTableDefinition> Tables { get; init; }

    public required IReadOnlyList<SnapshotReconFamily> Families { get; init; }

    /// <summary>Snapshot rows read per page.</summary>
    public int PageSize { get; init; } = 5000;

    /// <summary>Per-kind sample cap in the result.</summary>
    public int MaxSamples { get; init; } = 25;
}

/// <summary>A concrete example of a divergence, for eyeballing.</summary>
public sealed record SnapshotReconSample(string Kind, string Family, string? PrimaryKey, string CosmosId, string PartitionKey);

/// <summary>
/// Per-family verdicts. The <c>Pending*</c> classes are IN-FLIGHT state (the row is dirty —
/// the pump just hasn't run/succeeded yet); the <c>Divergent*</c> classes are the alarm: the
/// snapshot believes the document is settled and Cosmos disagrees.
/// </summary>
/// <param name="ContendedDelete">Queued deletes whose coordinates a DIFFERENT live row still
/// expects — only possible where several tables share one document space (JPM + NonJPM →
/// CatalogPart). When the pump runs it will destroy a document its surviving owner will not
/// re-create (that row is clean, so nothing re-queues it). Counted as divergence: it is the
/// one class a shared doc space actually endangers.</param>
public sealed record SnapshotReconFamilyResult(
    string Family,
    long ExpectedDocs,
    long ActualDocs,
    long InSync,
    long PendingAdd,
    long PendingUpdate,
    long PendingDelete,
    long DivergentMissing,
    long DivergentContent,
    long DivergentGhost,
    long Orphans,
    long DuplicateExpectedCoordinates,
    long ContendedDelete = 0)
{
    public long Divergent => DivergentMissing + DivergentContent + DivergentGhost + ContendedDelete;
}

public sealed record SnapshotReconResult(
    IReadOnlyList<string> Tables,
    long RowsScanned,
    long TombstonedRows,
    IReadOnlyList<SnapshotReconFamilyResult> Families,
    IReadOnlyList<SnapshotReconSample> Samples);

/// <summary>
/// Duck-vs-Cosmos document parity: maps EVERY row of a snapshot table through the same
/// family mappings the pump uses (full intended state — dirty rows alone can't recon: a
/// clean row's document still has to match), enumerates each family's actual documents,
/// and classifies per coordinate. Comparison is the canonical <see cref="CosmosDocHash"/>
/// on both sides. Set-based inside DuckDB (<c>stage</c> tables), so a 1.4M-row family
/// doesn't need 1.4M dictionary entries in memory — which also means the caller holds the
/// write gate, like any other stage-schema writer.
/// Orphans are actual documents no snapshot row accounts for — at first cutover this class
/// includes the incumbent's accumulated delete-leak (NonJPM/stock broken delete branches).
/// </summary>
public sealed class SnapshotRecon
{
    private const string ExpectedTable = "recon$expected";
    private const string TrackedAbsentTable = "recon$absent";
    private const string ActualTable = "recon$actual";

    private readonly SnapshotStore store;
    private readonly CosmosClient? cosmosClient;

    public SnapshotRecon(SnapshotStore store, CosmosClient? cosmosClient)
    {
        this.store = store;
        this.cosmosClient = cosmosClient;
    }

    public Task<SnapshotReconResult> RunAsync(SnapshotReconOptions options, CancellationToken cancellationToken = default)
    {
        if (cosmosClient is null)
            throw new InvalidOperationException("Recon against live Cosmos requires a CosmosClient.");

        return RunAsync(options, EnumerateCosmosAsync, cancellationToken);
    }

    /// <summary>Test seam: same classification, caller-supplied actual documents.</summary>
    internal async Task<SnapshotReconResult> RunAsync(
        SnapshotReconOptions options,
        Func<SnapshotReconFamily, CancellationToken, IAsyncEnumerable<JsonObject>> actualDocuments,
        CancellationToken cancellationToken)
    {
        // A Deferred table has zero resident rows, so a recon over it would classify EVERY
        // live document as an Orphan — a guaranteed red verdict with a "corruption" flavour
        // that blocks the standing pump preflight, when nothing is actually wrong. Refuse
        // with the real reason instead of misdiagnosing. Hydration is not this class's call
        // to make: any merge against the table hydrates it automatically, and a caller that
        // wants recon-now-regardless drives one first.
        foreach (var table in options.Tables)
        {
            if (store.ReadResidency(table.Name) == SnapshotResidency.Deferred)
            {
                throw new InvalidOperationException(
                    $"Table '{table.Name}' is Deferred — its rows live in the published copy, not the " +
                    "write database, and reconciling an empty resident table against Cosmos would report " +
                    "every document as an orphan. Run recon after the table is Resident (any merge " +
                    "hydrates it), or restart without deferral qualification.");
            }
        }

        CreateWorkTables();
        try
        {
            var (rowsScanned, tombstoned) = LoadExpected(options, cancellationToken);

            // One enumeration per LABEL: several entries may share a label (a shared doc
            // space fed by several tables) — their actual universe is the same query.
            foreach (var family in options.Families.DistinctBy(f => f.ResolvedLabel))
            {
                using var appender = store.Connection.CreateAppender("stage", ActualTable);
                await foreach (var document in actualDocuments(family, cancellationToken))
                {
                    var (id, partitionKey, hash) = DigestActualDocument(family, document);
                    var row = appender.CreateRow();
                    row.AppendValue(family.ResolvedLabel);
                    row.AppendValue(id);
                    row.AppendValue(partitionKey);
                    row.AppendValue(hash);
                    row.EndRow();
                }
            }

            return new SnapshotReconResult(
                options.Tables.Select(t => t.Name).ToList(), rowsScanned, tombstoned,
                Classify(options), SampleDivergences(options));
        }
        finally
        {
            DropWorkTables();
        }
    }

    // ---- Expected side ----------------------------------------------------------------------

    private (long RowsScanned, long Tombstoned) LoadExpected(SnapshotReconOptions options, CancellationToken cancellationToken)
    {
        long rowsScanned = 0, tombstoned = 0;

        using var expected = store.Connection.CreateAppender("stage", ExpectedTable);
        using var absent = store.Connection.CreateAppender("stage", TrackedAbsentTable);

        foreach (var table in options.Tables)
        {
            // Only this table's families produce expected documents from its rows.
            var applicable = options.Families
                .Where(f => f.SourceTable is null
                            || f.SourceTable.Equals(table.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var groupedFamilies = applicable.Where(family => family.Mapping.Grouping is not null).ToList();
            if (groupedFamilies.Count > 0)
            {
                if (applicable.Count != 1)
                {
                    throw new ArgumentException(
                        $"Grouped recon for table '{table.Name}' currently requires exactly one family.",
                        nameof(options));
                }

                var family = groupedFamilies[0];
                string? groupCursor = null;
                while (true)
                {
                    var page = store.ReadReplicationGroups(
                        table,
                        family.Mapping.Grouping!,
                        groupCursor,
                        options.PageSize,
                        dirtyGroupsOnly: false);
                    if (page.Groups.Count == 0)
                        break;

                    foreach (var group in page.Groups)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        rowsScanned += group.Rows.Count;
                        tombstoned += group.Rows.Count(row => row.Row.Deleted);

                        var groupDirty = group.Rows.Any(row => row.Dirty);
                        var latestStamp = group.Rows
                            .Where(row => row.Row.ReplicatedAt is not null)
                            .OrderByDescending(row => row.Row.ReplicatedAt)
                            .Select(row => ReplicationStamp.Parse(row.Row.ReplicationStamp))
                            .FirstOrDefault()
                            ?? new ReplicationStamp();
                        latestStamp.Families.TryGetValue(family.Mapping.Family, out var oldCoordinates);
                        var document = family.Mapping.Grouping!.Project(group.LiveRows);

                        if (document is not null)
                        {
                            var entry = expected.CreateRow();
                            entry.AppendValue(family.ResolvedLabel);
                            entry.AppendValue(document.Id);
                            entry.AppendValue(NormalizePartitionKey(document.PartitionKey));
                            entry.AppendValue(CosmosDocHash.Compute(document));
                            entry.AppendValue(group.GroupKey);
                            entry.AppendValue(groupDirty);
                            entry.EndRow();
                        }

                        if (oldCoordinates is not null
                            && (document is null
                                || !ReplicationStamp.CoordinatesEqual(oldCoordinates, document)))
                        {
                            var entry = absent.CreateRow();
                            entry.AppendValue(family.ResolvedLabel);
                            entry.AppendValue(oldCoordinates.Id);
                            entry.AppendValue(NormalizePartitionKey(oldCoordinates.PartitionKey.Select(FromStampLevel)));
                            entry.AppendValue(group.GroupKey);
                            entry.AppendValue(groupDirty);
                            entry.EndRow();
                        }
                    }

                    groupCursor = page.LastGroupKey;
                    if (!page.HasMore)
                        break;
                }

                continue;
            }

            string? cursor = null;
            while (true)
            {
                var page = store.ReadRows(table, cursor, options.PageSize);
                if (page.Count == 0)
                    break;

                foreach (var row in page)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    rowsScanned++;

                    var stamp = ReplicationStamp.Parse(row.ReplicationStamp);
                    var mappingRow = row.AsDirtyRow();

                    if (row.Deleted)
                    {
                        tombstoned++;

                        if (stamp.Families.Count > 0)
                        {
                            // Delete not yet completed: whatever the stamp says exists is a
                            // document we expect to be deleting (dirty) — or, oddly, already
                            // settled (clean with a residual stamp): either way, track it.
                            foreach (var (familyName, coordinates) in stamp.Families)
                            {
                                var family = applicable.FirstOrDefault(f => f.Mapping.Family == familyName);
                                if (family is null)
                                    continue;

                                var entry = absent.CreateRow();
                                entry.AppendValue(family.ResolvedLabel);
                                entry.AppendValue(coordinates.Id);
                                entry.AppendValue(NormalizePartitionKey(coordinates.PartitionKey.Select(FromStampLevel)));
                                entry.AppendValue(row.PrimaryKey);
                                entry.AppendValue(row.Dirty);
                                entry.EndRow();
                            }
                        }
                        else if (!row.Dirty)
                        {
                            // Delete COMPLETED — the pump stamps an EMPTY stamp after deleting, so
                            // the stamp can't say where the document was. The row's values are
                            // still in the table: map them through the current mappings — a
                            // surviving document at those coordinates is a ghost.
                            foreach (var family in applicable)
                            {
                                if (family.Mapping.Predicate?.Invoke(mappingRow) == false)
                                    continue;

                                var document = family.Mapping.Map!(mappingRow);
                                var entry = absent.CreateRow();
                                entry.AppendValue(family.ResolvedLabel);
                                entry.AppendValue(document.Id);
                                entry.AppendValue(NormalizePartitionKey(document.PartitionKey));
                                entry.AppendValue(row.PrimaryKey);
                                entry.AppendValue(false);
                                entry.EndRow();
                            }
                        }
                        // else: a dirty tombstone that was never replicated — no document ever
                        // existed; nothing to expect anywhere.

                        continue;
                    }

                    foreach (var family in applicable)
                    {
                        if (family.Mapping.Predicate?.Invoke(mappingRow) == false)
                            continue;

                        var document = family.Mapping.Map!(mappingRow);
                        var entry = expected.CreateRow();
                        entry.AppendValue(family.ResolvedLabel);
                        entry.AppendValue(document.Id);
                        entry.AppendValue(NormalizePartitionKey(document.PartitionKey));
                        entry.AppendValue(CosmosDocHash.Compute(document));
                        entry.AppendValue(row.PrimaryKey);
                        entry.AppendValue(row.Dirty);
                        entry.EndRow();
                    }

                    // A live row whose stamp points at coordinates its CURRENT mapping no longer
                    // produces (family unmatched now, or id/PK moved) has a pending delete at the
                    // old coordinates — without tracking it, that old document reads as an orphan.
                    foreach (var (familyName, coordinates) in stamp.Families)
                    {
                        var family = applicable.FirstOrDefault(f => f.Mapping.Family == familyName);
                        if (family is null)
                            continue;

                        var matchesCurrent =
                            family.Mapping.Predicate?.Invoke(mappingRow) != false
                            && ReplicationStamp.CoordinatesEqual(coordinates, family.Mapping.Map!(mappingRow));
                        if (matchesCurrent)
                            continue;

                        var entry = absent.CreateRow();
                        entry.AppendValue(family.ResolvedLabel);
                        entry.AppendValue(coordinates.Id);
                        entry.AppendValue(NormalizePartitionKey(coordinates.PartitionKey.Select(FromStampLevel)));
                        entry.AppendValue(row.PrimaryKey);
                        entry.AppendValue(row.Dirty);
                        entry.EndRow();
                    }
                }

                cursor = page[^1].PrimaryKey;
                if (page.Count < options.PageSize)
                    break;
            }
        }

        return (rowsScanned, tombstoned);
    }

    // ---- Actual side ------------------------------------------------------------------------

    private static readonly string[] CosmosSystemProperties = ["_rid", "_self", "_etag", "_attachments", "_ts"];

    private async IAsyncEnumerable<JsonObject> EnumerateCosmosAsync(
        SnapshotReconFamily family,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var container = cosmosClient!.GetContainer(family.Mapping.Database, family.Mapping.Container);
        using var iterator = container.GetItemQueryStreamIterator(new QueryDefinition(family.EnumerationSql));

        while (iterator.HasMoreResults)
        {
            using var response = await iterator.ReadNextAsync(cancellationToken);
            response.EnsureSuccessStatusCode();

            var page = await JsonNode.ParseAsync(response.Content, cancellationToken: cancellationToken)
                ?? throw new InvalidDataException("Empty Cosmos query page.");

            if (page["Documents"] is not JsonArray documents)
                continue;

            foreach (var node in documents)
            {
                if (node is JsonObject document)
                    yield return document;
            }
        }
    }

    private static (string Id, string PartitionKey, string Hash) DigestActualDocument(
        SnapshotReconFamily family, JsonObject document)
    {
        foreach (var property in CosmosSystemProperties)
            document.Remove(property);

        var id = document["id"]?.GetValue<string>()
            ?? throw new InvalidDataException($"Family {family.Mapping.Family}: a document without an id.");

        var partitionKey = NormalizePartitionKey(
            family.PartitionKeyPaths.Select(path => FromJsonNode(document[path.TrimStart('/')])));

        return (id, partitionKey, CosmosDocHash.Compute(document));
    }

    // ---- Classification ---------------------------------------------------------------------

    private void CreateWorkTables()
    {
        DropWorkTables();
        store.Execute(
            $"""
            CREATE TABLE stage."{ExpectedTable}" (
                "Family" VARCHAR NOT NULL, "CosmosId" VARCHAR NOT NULL, "Pk" VARCHAR NOT NULL,
                "DocHash" VARCHAR NOT NULL, "RowKey" VARCHAR NOT NULL, "Dirty" BOOLEAN NOT NULL)
            """);
        store.Execute(
            $"""
            CREATE TABLE stage."{TrackedAbsentTable}" (
                "Family" VARCHAR NOT NULL, "CosmosId" VARCHAR NOT NULL, "Pk" VARCHAR NOT NULL,
                "RowKey" VARCHAR NOT NULL, "Dirty" BOOLEAN NOT NULL)
            """);
        store.Execute(
            $"""
            CREATE TABLE stage."{ActualTable}" (
                "Family" VARCHAR NOT NULL, "CosmosId" VARCHAR NOT NULL, "Pk" VARCHAR NOT NULL,
                "DocHash" VARCHAR NOT NULL)
            """);
    }

    private void DropWorkTables()
    {
        store.Execute($"DROP TABLE IF EXISTS stage.\"{ExpectedTable}\"");
        store.Execute($"DROP TABLE IF EXISTS stage.\"{TrackedAbsentTable}\"");
        store.Execute($"DROP TABLE IF EXISTS stage.\"{ActualTable}\"");
    }

    private IReadOnlyList<SnapshotReconFamilyResult> Classify(SnapshotReconOptions options)
    {
        var results = new List<SnapshotReconFamilyResult>();

        foreach (var label in options.Families.Select(f => f.ResolvedLabel).Distinct())
        {
            long Scalar(string sql) => store.ExecuteScalar(sql, label) switch
            {
                null or DBNull => 0L,
                System.Numerics.BigInteger big => (long)big,   // DuckDB sum() yields HUGEINT
                var value => Convert.ToInt64(value),
            };

            var expectedDocs = Scalar($"SELECT count(*) FROM stage.\"{ExpectedTable}\" WHERE \"Family\" = ?");
            var actualDocs = Scalar($"SELECT count(*) FROM stage.\"{ActualTable}\" WHERE \"Family\" = ?");

            var joined =
                $"""
                FROM stage."{ExpectedTable}" e
                LEFT JOIN stage."{ActualTable}" a
                  ON a."Family" = e."Family" AND a."CosmosId" = e."CosmosId" AND a."Pk" = e."Pk"
                WHERE e."Family" = ?
                """;

            var inSync = Scalar($"SELECT count(*) {joined} AND a.\"DocHash\" = e.\"DocHash\"");
            var pendingAdd = Scalar($"SELECT count(*) {joined} AND a.\"CosmosId\" IS NULL AND e.\"Dirty\"");
            var divergentMissing = Scalar($"SELECT count(*) {joined} AND a.\"CosmosId\" IS NULL AND NOT e.\"Dirty\"");
            var pendingUpdate = Scalar($"SELECT count(*) {joined} AND a.\"DocHash\" IS NOT NULL AND a.\"DocHash\" <> e.\"DocHash\" AND e.\"Dirty\"");
            var divergentContent = Scalar($"SELECT count(*) {joined} AND a.\"DocHash\" IS NOT NULL AND a.\"DocHash\" <> e.\"DocHash\" AND NOT e.\"Dirty\"");

            var absentJoined =
                $"""
                FROM stage."{TrackedAbsentTable}" t
                JOIN stage."{ActualTable}" a
                  ON a."Family" = t."Family" AND a."CosmosId" = t."CosmosId" AND a."Pk" = t."Pk"
                WHERE t."Family" = ?
                """;

            // In a shared doc space (JPM + NonJPM → one CatalogPart label) a coordinate can be
            // claimed by BOTH sides at once: one table's tombstone points at it while another
            // table's live row legitimately owns it. What that means depends on the tombstone:
            var notExpectedToo =
                $"""
                AND NOT EXISTS (SELECT 1 FROM stage."{ExpectedTable}" e
                                WHERE e."Family" = t."Family" AND e."CosmosId" = t."CosmosId" AND e."Pk" = t."Pk")
                """;

            // …SETTLED (clean): the delete already happened, the surviving owner re-created the
            // document, and the end state is correct — not a ghost.
            var divergentGhost = Scalar($"SELECT count(*) {absentJoined} AND NOT t.\"Dirty\" {notExpectedToo}");

            // …PENDING (dirty): the delete has NOT happened yet, and when the pump runs it will
            // point-delete a document another table's row still owns. That row is clean, so
            // nothing re-creates it — this is an impending destructive write, and suppressing
            // it would hide the one class the shared doc space actually endangers.
            var pendingDelete = Scalar($"SELECT count(*) {absentJoined} AND t.\"Dirty\" {notExpectedToo}");
            var contendedDelete = Scalar(
                $"""
                SELECT count(*) {absentJoined} AND t."Dirty"
                  AND EXISTS (SELECT 1 FROM stage."{ExpectedTable}" e
                              WHERE e."Family" = t."Family" AND e."CosmosId" = t."CosmosId" AND e."Pk" = t."Pk")
                """);

            var orphans = Scalar(
                $"""
                SELECT count(*) FROM stage."{ActualTable}" a
                WHERE a."Family" = ?
                  AND NOT EXISTS (SELECT 1 FROM stage."{ExpectedTable}" e
                                  WHERE e."Family" = a."Family" AND e."CosmosId" = a."CosmosId" AND e."Pk" = a."Pk")
                  AND NOT EXISTS (SELECT 1 FROM stage."{TrackedAbsentTable}" t
                                  WHERE t."Family" = a."Family" AND t."CosmosId" = a."CosmosId" AND t."Pk" = a."Pk")
                """);

            var duplicateExpected = Scalar(
                $"""
                SELECT coalesce(sum(n - 1), 0) FROM (
                    SELECT count(*) AS n FROM stage."{ExpectedTable}"
                    WHERE "Family" = ?
                    GROUP BY "CosmosId", "Pk"
                    HAVING count(*) > 1)
                """);

            results.Add(new SnapshotReconFamilyResult(
                label, expectedDocs, actualDocs, inSync,
                pendingAdd, pendingUpdate, pendingDelete,
                divergentMissing, divergentContent, divergentGhost,
                orphans, duplicateExpected, contendedDelete));
        }

        return results;
    }

    private IReadOnlyList<SnapshotReconSample> SampleDivergences(SnapshotReconOptions options)
    {
        var samples = new List<SnapshotReconSample>();

        void Collect(string kind, string sql)
        {
            using var command = store.Connection.CreateCommand();
            command.CommandText = sql;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                samples.Add(new SnapshotReconSample(
                    kind,
                    Family: reader.GetString(0),
                    PrimaryKey: reader.IsDBNull(1) ? null : reader.GetString(1),
                    CosmosId: reader.GetString(2),
                    PartitionKey: reader.GetString(3)));
            }
        }

        var limit = options.MaxSamples;

        Collect("DivergentMissing",
            $"""
            SELECT e."Family", e."RowKey", e."CosmosId", e."Pk"
            FROM stage."{ExpectedTable}" e
            LEFT JOIN stage."{ActualTable}" a
              ON a."Family" = e."Family" AND a."CosmosId" = e."CosmosId" AND a."Pk" = e."Pk"
            WHERE a."CosmosId" IS NULL AND NOT e."Dirty"
            ORDER BY e."Family", e."CosmosId" LIMIT {limit}
            """);

        Collect("DivergentContent",
            $"""
            SELECT e."Family", e."RowKey", e."CosmosId", e."Pk"
            FROM stage."{ExpectedTable}" e
            JOIN stage."{ActualTable}" a
              ON a."Family" = e."Family" AND a."CosmosId" = e."CosmosId" AND a."Pk" = e."Pk"
            WHERE a."DocHash" <> e."DocHash" AND NOT e."Dirty"
            ORDER BY e."Family", e."CosmosId" LIMIT {limit}
            """);

        Collect("DivergentGhost",
            $"""
            SELECT t."Family", t."RowKey", t."CosmosId", t."Pk"
            FROM stage."{TrackedAbsentTable}" t
            JOIN stage."{ActualTable}" a
              ON a."Family" = t."Family" AND a."CosmosId" = t."CosmosId" AND a."Pk" = t."Pk"
            WHERE NOT t."Dirty"
              AND NOT EXISTS (SELECT 1 FROM stage."{ExpectedTable}" e
                              WHERE e."Family" = t."Family" AND e."CosmosId" = t."CosmosId" AND e."Pk" = t."Pk")
            ORDER BY t."Family", t."CosmosId" LIMIT {limit}
            """);

        Collect("ContendedDelete",
            $"""
            SELECT t."Family", t."RowKey", t."CosmosId", t."Pk"
            FROM stage."{TrackedAbsentTable}" t
            WHERE t."Dirty"
              AND EXISTS (SELECT 1 FROM stage."{ExpectedTable}" e
                          WHERE e."Family" = t."Family" AND e."CosmosId" = t."CosmosId" AND e."Pk" = t."Pk")
            ORDER BY t."Family", t."CosmosId" LIMIT {limit}
            """);

        Collect("Orphan",
            $"""
            SELECT a."Family", NULL, a."CosmosId", a."Pk"
            FROM stage."{ActualTable}" a
            WHERE NOT EXISTS (SELECT 1 FROM stage."{ExpectedTable}" e
                              WHERE e."Family" = a."Family" AND e."CosmosId" = a."CosmosId" AND e."Pk" = a."Pk")
              AND NOT EXISTS (SELECT 1 FROM stage."{TrackedAbsentTable}" t
                              WHERE t."Family" = a."Family" AND t."CosmosId" = a."CosmosId" AND t."Pk" = a."Pk")
            ORDER BY a."Family", a."CosmosId" LIMIT {limit}
            """);

        Collect("DuplicateExpectedCoordinates",
            $"""
            SELECT e."Family", min(e."RowKey"), e."CosmosId", e."Pk"
            FROM stage."{ExpectedTable}" e
            GROUP BY e."Family", e."CosmosId", e."Pk"
            HAVING count(*) > 1
            ORDER BY e."Family", e."CosmosId" LIMIT {limit}
            """);

        return samples;
    }

    // ---- Coordinate normalization -----------------------------------------------------------

    /// <summary>
    /// One textual form per logical coordinate, whatever the container: CLR values on the
    /// expected side, stamp JSON on the tracked side, document JSON on the actual side.
    /// Numbers canonicalize through decimal so <c>7</c>, <c>7.0</c>, and <c>7m</c> agree.
    /// </summary>
    private static string NormalizePartitionKey(IEnumerable<object?> levels) =>
        string.Join("|", levels.Select(level => level switch
        {
            null => "∅",
            bool b => b ? "true" : "false",
            string s => s,
            _ => Convert.ToDecimal(level, CultureInfo.InvariantCulture)
                .ToString("0.####################", CultureInfo.InvariantCulture),
        }));

    private static object? FromStampLevel(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetDecimal(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.GetRawText(),
    };

    private static object? FromJsonNode(JsonNode? node) => node switch
    {
        null => null,
        JsonValue value when value.TryGetValue<string>(out var s) => s,
        JsonValue value when value.TryGetValue<bool>(out var b) => b,
        JsonValue value when value.TryGetValue<decimal>(out var d) => d,
        _ => node.ToJsonString(),
    };
}
