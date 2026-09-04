using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ShiftSoftware.ADP.Hawta;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;

/// <summary>
/// Where the bulk engine reads from: the declared families and the reference tables bound to one
/// DuckDB-readable source (bulk-lookup.md step 4, the source binding).
/// <list type="bullet">
/// <item><see cref="ReadSnapshot"/> — a read snapshot as a sync agent publishes it: bare tables, named as the per-VIN storage names them.</item>
/// <item><see cref="HawtaStore"/> — a Hawta store (the agent's write DB, or a scratch rebuild of one): the SERVING tables under their schema, live rows only.</item>
/// <item><see cref="HawtaPublish"/> — a Hawta published set: the same serving tables, read straight from the manifest's parquet, live rows only.</item>
/// </list>
/// The families, their filters and the reference lookups are the same on every source; only the
/// relation each one scans differs. A family whose table the source does not carry fails when the
/// stream opens — or, for a published set, when the binding is made — declared, never discovered
/// as an empty list.
/// </summary>
public sealed class BulkLookupSource
{
    private BulkLookupSource(string description, string connectionString, IReadOnlyList<AggregateFamily> families,
        PreloadedReferenceStorage.Options reference, IHashIdService hashIds)
    {
        Description = description;
        ConnectionString = connectionString;
        Families = families;
        Reference = reference;
        HashIds = hashIds;
    }

    /// <summary>What this source is, for a log line: the file or manifest, and what the manifest said about itself.</summary>
    public string Description { get; }
    public string ConnectionString { get; }
    /// <summary>The families, each bound to the relation this source scans for it.</summary>
    public IReadOnlyList<AggregateFamily> Families { get; }
    /// <summary>The reference tables, each bound the same way.</summary>
    public PreloadedReferenceStorage.Options Reference { get; }
    /// <summary>
    /// When set, a vehicle entry's identity ids are decoded from its hash ids, as the per-VIN storage
    /// does over a read snapshot — the hash is what the writer stamped, the id beside it may be stale.
    /// Null on a Hawta source: its serving projection stamps the id and the hash from the same source
    /// row, so the id is authoritative and nothing needs the salt.
    /// </summary>
    public IHashIdService HashIds { get; }

    public VinOrderedAggregateStream OpenStream(bool requireVehicleEntry = true) =>
        new VinOrderedAggregateStream(ConnectionString, Families, requireVehicleEntry, HashIds);

    public PreloadedReferenceStorage LoadReference() => PreloadedReferenceStorage.Load(ConnectionString, Reference);

    /// <summary>A read snapshot file (a sync agent publishes one; the bench measures over one), opened read-only.</summary>
    public static BulkLookupSource ReadSnapshot(string databasePath, IReadOnlyList<AggregateFamily> families,
        IHashIdService hashIds = null, PreloadedReferenceStorage.Options reference = null)
    {
        if (families is null || families.Count == 0)
            throw new ArgumentException("Declare at least one family to read.", nameof(families));
        if (!File.Exists(databasePath))
            throw new FileNotFoundException("The read snapshot was not found.", databasePath);
        return new BulkLookupSource(
            $"read snapshot {databasePath}",
            $"Data Source={databasePath};ACCESS_MODE=READ_ONLY",
            families, reference ?? new PreloadedReferenceStorage.Options(), hashIds);
    }

    /// <summary>
    /// A Hawta store: every family reads its serving table under the store's schema, live rows only
    /// (a Hawta table keeps a deleted row as a tombstone). Read-only by default — the file is the
    /// agent's write DB or a rebuild of one, and this engine never writes to it. A host that runs
    /// the engine inside the agent's own process opens the store through the agent's connection
    /// instead, which is the wiring of the host stage.
    /// </summary>
    public static BulkLookupSource HawtaStore(string databasePath, IReadOnlyList<AggregateFamily> families,
        HawtaServingConvention convention = null, bool readOnly = true)
    {
        if (families is null || families.Count == 0)
            throw new ArgumentException("Declare at least one family to read.", nameof(families));
        if (!File.Exists(databasePath))
            throw new FileNotFoundException("The Hawta store was not found.", databasePath);
        convention ??= HawtaServingConvention.Default;

        var bound = families.Select(family => family.Bound(convention.Relation(family.Table), convention.LiveRows)).ToList();
        var reference = convention.ReferenceOptions(canonical => $"SELECT * FROM {convention.Relation(canonical)} WHERE {convention.LiveRows}");
        return new BulkLookupSource(
            $"Hawta store {databasePath}",
            $"Data Source={databasePath}" + (readOnly ? ";ACCESS_MODE=READ_ONLY" : ""),
            bound, reference, hashIds: null);
    }

    /// <summary>
    /// A Hawta published set, by its manifest: every family reads the parquet the manifest names for
    /// its serving table, live rows only, in the order the publisher exported. The relation is a
    /// table function, so the vehicle's rows are ordered by file and row number instead of
    /// <c>rowid</c>. Every family and reference table must be in the manifest — a missing one fails
    /// here, with the manifest's table list in the message.
    /// </summary>
    public static BulkLookupSource HawtaPublish(string manifestPath, IReadOnlyList<AggregateFamily> families,
        HawtaServingConvention convention = null)
    {
        if (families is null || families.Count == 0)
            throw new ArgumentException("Declare at least one family to read.", nameof(families));
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("The publish manifest was not found.", manifestPath);
        convention ??= HawtaServingConvention.Default;

        var published = PublishedSnapshot.Read(manifestPath);
        var directory = PublishedSnapshot.DirectoryOf(manifestPath) ?? ".";
        var entries = new Dictionary<string, PublishedTableManifest>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in published.Tables)
            entries[entry.Table] = entry;                       // duplicates are already rejected by the manifest's own validation

        string Parquet(string canonical, bool withRowOrder)
        {
            var name = convention.TableName(canonical);
            if (!entries.TryGetValue(name, out var entry))
            {
                throw new InvalidOperationException(
                    $"The published set {manifestPath} carries no table '{name}' (for '{canonical}'). " +
                    $"Published: {string.Join(", ", published.Tables.Select(t => t.Table))}.");
            }
            var files = string.Join(", ", entry.Resolve(directory).Select(path => $"'{path.Replace('\\', '/').Replace("'", "''")}'"));
            return withRowOrder
                ? $"read_parquet([{files}], filename = true, file_row_number = true)"
                : $"read_parquet([{files}])";
        }

        var bound = families.Select(family => family.Bound(Parquet(family.Table, true), convention.LiveRows, "filename, file_row_number")).ToList();
        var reference = convention.ReferenceOptions(canonical => $"SELECT * FROM {Parquet(canonical, false)} WHERE {convention.LiveRows}");
        return new BulkLookupSource(
            $"Hawta publish {manifestPath} (published {published.PublishedAt:O}, change-sequence watermark {published.ChangeSequenceHighWatermark})",
            "Data Source=:memory:",
            bound, reference, hashIds: null);
    }

    /// <summary>The newest published set of <paramref name="snapshotName"/> under a local publish directory.</summary>
    public static BulkLookupSource HawtaPublishNewest(string publishDirectory, string snapshotName, IReadOnlyList<AggregateFamily> families,
        HawtaServingConvention convention = null)
    {
        var manifest = PublishedSnapshot.ResolveNewest(publishDirectory, snapshotName)
            ?? throw new FileNotFoundException($"No '{snapshotName}-*.json' manifest under {publishDirectory}.");
        return HawtaPublish(manifest, families, convention);
    }
}

/// <summary>
/// How a Hawta host names the serving tables the lookup reads. The default is the host's serving
/// projections as built to date: one typed table per ADP model, named <c>Serving</c> + the family's canonical name, under the <c>data</c>
/// schema, with tombstones kept as rows whose <c>_Deleted</c> is true. A host that names them
/// differently passes its own convention; the binding fails loudly on the first name that does not
/// resolve, which is the designed failure for a host and an engine that disagree.
/// </summary>
public sealed class HawtaServingConvention
{
    public static HawtaServingConvention Default { get; } = new HawtaServingConvention();

    public string Schema { get; set; } = "data";
    public Func<string, string> TableName { get; set; } = canonical => "Serving" + canonical;
    /// <summary>The live-row predicate: a Hawta table keeps a deleted row as a tombstone.</summary>
    public string LiveRows { get; set; } = $"\"{BookkeepingColumns.Deleted}\" = false";

    // The reference tables' canonical names, resolved through TableName like the families. Null
    // means the host serves no such table and the storage answers as if it were empty: a host whose
    // entries name no customer, or one without brokers.
    public string ServiceItems { get; set; } = "ServiceItem";
    public string VehicleModels { get; set; } = "VehicleModel";
    public string ExteriorColors { get; set; } = "ExteriorColor";
    public string InteriorColors { get; set; } = "InteriorColor";
    public string Customers { get; set; }
    public string BrokerStock { get; set; }

    /// <summary>The qualified, quoted serving table for a canonical family or reference name.</summary>
    public string Relation(string canonical) => $"{Schema}.\"{TableName(canonical)}\"";

    internal PreloadedReferenceStorage.Options ReferenceOptions(Func<string, string> select) => new PreloadedReferenceStorage.Options
    {
        ServiceItemsSql = select(ServiceItems),
        VehicleModelsSql = select(VehicleModels),
        ExteriorColorsSql = select(ExteriorColors),
        InteriorColorsSql = select(InteriorColors),
        LoadBrokerStock = BrokerStock is not null,
        BrokerStockSql = BrokerStock is null ? null : select(BrokerStock),
        LoadCustomers = Customers is not null,
        CustomersSql = Customers is null ? null : select(Customers),
    };
}
