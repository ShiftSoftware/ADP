using System.Globalization;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Constants;
using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// The delta-out drain: pumps Pending rows from Darlastic.ProjectionState into Cosmos and stamps
/// them projected. The other half of the discipline the resolve run stages for — the engine hashes
/// and stages; this class is a dumb, restartable pump over four artifact families:
///
///   - 'golden'   -> <see cref="GoldenCustomerModel"/> upserts into Customers/Customers.
///   - 'vinlinks' -> <see cref="GoldenCustomerVehicleLinksModel"/> upserts into Customers/Customers,
///                   in the golden's partition (the customer-360 "vehicles owned" read).
///   - 'vinowner' -> <see cref="VehicleGoldenOwnershipModel"/> upserts into CompanyData/Vehicles
///                   (current-owner lookup and ownership history by VIN).
///   - 'stamp'    -> GoldenCustomerID PATCH on the landing <see cref="CustomerModel"/> doc. The
///                   engine can't know how a tenant addresses its landing docs, so the host passes
///                   a mapping; with none wired the family is skipped and simply stays pending.
///                   A patch that finds no doc (404) is counted and marked projected — when landing
///                   docs arrive later, re-stamp with:
///                   UPDATE Darlastic.ProjectionState SET Pending = 1 WHERE ArtifactType = 'stamp'.
///
///   - Payloads are staged at resolve time — no engine recompute here. Tombstones DELETE the
///     Cosmos doc (404-tolerant). Marks Pending = 0 ONLY where the ContentHash is unchanged since
///     the batch was read — a concurrent resolve that re-staged an artifact mid-drain keeps it
///     pending for the next pass. At-least-once by design: re-running re-upserts idempotently.
///
/// Golden-family docs live in the Customers container (hierarchical PK CompanyID/CustomerID/ItemType)
/// with NO CompanyID — a golden identity may span dealers, so it sits at the undefined level-1
/// partition: point-read with (None, goldenId, ItemType). By-VIN docs mirror that in the Vehicles
/// container (PK VIN/ItemType/CompanyID): (vin, "VehicleGoldenOwnership", None).
///
/// Connection via DARLASTIC_COSMOS (defaults to the local emulator, whose well-known key is
/// public documentation, not a secret). Serialized against other drains by its own applock;
/// deliberately does NOT hold the resolve lock — the hash guard makes concurrency safe.
/// </summary>
public static class Projector
{
    private const string EmulatorConnection =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    public static string ConnectionString => Environment.GetEnvironmentVariable("DARLASTIC_COSMOS") ?? EmulatorConnection;

    /// <summary>Dev-only database-name suffix (e.g. "-alt") so two tenants can share one local
    /// emulator. Production tenants each have their OWN Cosmos account and keep the platform's
    /// standard database names — so the suffix applies ONLY when the connection is the local
    /// emulator. Against a real account it is ignored (adversarial review 2026-07-19: a suffixed
    /// ask on a real account would silently create + provision mis-named databases there, and
    /// the point-read verification would read the same wrong place and look green; an empty-string
    /// env override is inexpressible on Windows, so ignoring is the only deliverable semantics).</summary>
    private static string DbSuffix =>
        ConnectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            ? Environment.GetEnvironmentVariable("DARLASTIC_COSMOS_DB_SUFFIX") ?? ""
            : "";

    /// <summary>How one source profile's landing <see cref="CustomerModel"/> doc is addressed in
    /// Cosmos, resolved by the host per tenant (the engine has no business knowing the convention).
    /// A null <see cref="CompanyId"/> targets the NULL partition value (the platform's landing-doc
    /// writers serialize the property present-and-null), not the undefined one.</summary>
    public sealed record StampTarget(string DocId, long? CompanyId, string CustomerId);

    private enum PumpOutcome { Upserted, Deleted, MissingDoc, Failed }

    public sealed class ProjectMetrics
    {
        public int Upserted, Deleted, Skipped, Failed;
        public int StampsMissingDoc;                       // stamp patches that found no landing doc (marked projected)
        public string? StampsNote;                         // set when the stamp family was skipped
        public readonly Dictionary<string, (int Upserted, int Deleted)> PerType = new(StringComparer.Ordinal);
        public long CosmosGoldenCount = -1, CosmosVinLinksCount = -1, CosmosVinOwnerCount = -1;
        public readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> FailureReasons = new();
        internal void RecordFailure(Exception ex)
        {
            string reason = ex is CosmosException ce ? $"{(int)ce.StatusCode} {ce.StatusCode}" : ex.GetType().Name + ": " + ex.Message.Split('\n')[0];
            FailureReasons.AddOrUpdate(reason, 1, (_, n) => n + 1);
        }
        public override string ToString() =>
            $"projected: {Upserted:N0} upserted / {Deleted:N0} tombstones deleted / {Skipped:N0} re-staged mid-drain / {Failed:N0} failed"
            + (PerType.Count > 0 ? " (" + string.Join(" / ", PerType.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key} {kv.Value.Upserted:N0}{(kv.Value.Deleted > 0 ? $"+{kv.Value.Deleted:N0}del" : "")}")) + ")" : "")
            + (StampsMissingDoc > 0 ? $"; {StampsMissingDoc:N0} stamps found no landing doc" : "")
            + (StampsNote is not null ? $"; stamps: {StampsNote}" : "")
            + (CosmosGoldenCount >= 0 ? $"\n  Cosmos now holds {CosmosGoldenCount:N0} golden docs"
               + (CosmosVinLinksCount >= 0 ? $" / {CosmosVinLinksCount:N0} vehicle-link docs" : "")
               + (CosmosVinOwnerCount >= 0 ? $" / {CosmosVinOwnerCount:N0} VIN-ownership docs" : "") : "")
            + (FailureReasons.IsEmpty ? "" : "\n  failure reasons: " + string.Join(" · ", FailureReasons.OrderByDescending(kv => kv.Value).Take(5).Select(kv => $"{kv.Key} ×{kv.Value:N0}")));
    }

    // Bulk mode batches ~100 docs per partition-range request and holds items until a batch fills —
    // low concurrency starves the batcher and every op pays the flush timer (measured: 76/s at 50).
    // With bulk on, thousands must be in flight; the SDK, not the caller, is the real throttle.
    public static async Task<ProjectMetrics> RunProjectAsync(int batchSize = 10_000, int maxConcurrency = 2_000,
        Func<string, string, StampTarget?>? stampAddress = null, Action<string>? progress = null)
    {
        progress ??= Console.WriteLine;
        var m = new ProjectMetrics();

        using var sql = new SqlConnection(Registry.ConnectionString);
        sql.Open();
        AcquireProjectLock(sql);
        Registry.AssertTenantReadOnly(sql);   // a mis-paired drain would pump the wrong tenant's corpus into this Cosmos account

        bool isEmulator = ConnectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        var options = new CosmosClientOptions
        {
            AllowBulkExecution = true,
            ConnectionMode = isEmulator ? ConnectionMode.Gateway : ConnectionMode.Direct,
            ServerCertificateCustomValidationCallback = isEmulator ? (_, _, _) => true : null,
        };
        using var cosmos = new CosmosClient(ConnectionString, options);
        if (!isEmulator && Environment.GetEnvironmentVariable("DARLASTIC_COSMOS_DB_SUFFIX") is { Length: > 0 } sfx)
            progress($"  (DARLASTIC_COSMOS_DB_SUFFIX \"{sfx}\" ignored — a real account keeps the platform's standard database names)");
        var customersDb = (await cosmos.CreateDatabaseIfNotExistsAsync(NoSQLConstants.Databases.Customers + DbSuffix)).Database;
        var companyDb = (await cosmos.CreateDatabaseIfNotExistsAsync(NoSQLConstants.Databases.CompanyData + DbSuffix)).Database;
        // A dev-created container defaults to 400 RU/s ≈ 40 writes/s — a 646K backfill would take
        // hours. On the EMULATOR, create at bulk-friendly throughput and bump an existing low-RU
        // container. DARLASTIC_COSMOS_RU overrides for small tenants: the emulator has a finite
        // partition budget, and a 20K-RU ask for a few-thousand-doc corpus exhausts it when other
        // tenants' databases already exist.
        // On a REAL account, never pick a throughput uninvited: an explicit value would silently
        // provision billable RU (20K manual ≈ $1.2K/mo) and THROWS on serverless accounts — so
        // containers are created with NO throughput parameter (account/database defaults apply)
        // unless DARLASTIC_COSMOS_RU is explicitly set. Prefer creating tenant containers via the
        // platform's own provisioning utility before the first drain.
        bool hasExplicitRu = int.TryParse(Environment.GetEnvironmentVariable("DARLASTIC_COSMOS_RU"), out var ru) && ru >= 400;
        int BulkRUs = hasExplicitRu ? ru : 20_000;
        int? createRUs = isEmulator ? BulkRUs : hasExplicitRu ? ru : (int?)null;
        var customers = (await customersDb.CreateContainerIfNotExistsAsync(new ContainerProperties
        {
            Id = NoSQLConstants.Containers.Customers_Customers,
            PartitionKeyPaths =
            [
                NoSQLConstants.PartitionKeys.Customers.Level1,
                NoSQLConstants.PartitionKeys.Customers.Level2,
                NoSQLConstants.PartitionKeys.Customers.Level3,
            ],
        }, throughput: createRUs)).Container;
        var vehicles = (await companyDb.CreateContainerIfNotExistsAsync(new ContainerProperties
        {
            Id = NoSQLConstants.Containers.Vehicles,
            PartitionKeyPaths =
            [
                NoSQLConstants.PartitionKeys.Vehicles.Level1,
                NoSQLConstants.PartitionKeys.Vehicles.Level2,
                NoSQLConstants.PartitionKeys.Vehicles.Level3,
            ],
        }, throughput: createRUs)).Container;
        if (isEmulator)
            foreach (var container in new[] { customers, vehicles })
                try
                {
                    int? current = await container.ReadThroughputAsync();
                    if (current is not null && current < BulkRUs)
                    {
                        await container.ReplaceThroughputAsync(ThroughputProperties.CreateManualThroughput(BulkRUs));
                        progress($"  {container.Id} throughput raised {current} -> {BulkRUs} RU/s");
                    }
                }
                catch { /* shared/serverless/autoscale offers — leave as provisioned */ }

        int projectedRun;
        using (var cmd = new SqlCommand("SELECT ISNULL(MAX(RunID), 0) FROM Darlastic.ResolveRun", sql))
            projectedRun = Convert.ToInt32(cmd.ExecuteScalar());

        var pendingByType = new Dictionary<string, long>(StringComparer.Ordinal);
        using (var cmd = new SqlCommand("SELECT ArtifactType, COUNT_BIG(*) FROM Darlastic.ProjectionState WHERE Pending = 1 GROUP BY ArtifactType", sql))
        using (var r = cmd.ExecuteReader())
            while (r.Read()) pendingByType[r.GetString(0)] = r.GetInt64(1);
        progress($"Drain: {pendingByType.Values.Sum():N0} pending artifacts ({string.Join(" / ", pendingByType.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key} {kv.Value:N0}"))}) " +
                 $"-> {(isEmulator ? "local emulator" : "configured account")}, batch {batchSize}, concurrency {maxConcurrency}");

        var gate = new SemaphoreSlim(maxConcurrency);

        // ---- one pump per family; each returns the item's outcome and never throws
        Task<PumpOutcome> PumpGolden(string key, string hash, string? payload)
        {
            var pk = new PartitionKeyBuilder().AddNoneType().Add(key).Add(ModelTypes.GoldenCustomer).Build();
            return hash == "TOMBSTONE"
                ? DeleteAsync<GoldenCustomerModel>(customers, key, pk)
                : payload is null ? Task.FromResult(PumpOutcome.Failed)   // pre-backfill row — run resolve to stage payloads
                : UpsertAsync(customers, MapGolden(key, payload), pk);
        }
        Task<PumpOutcome> PumpVinLinks(string key, string hash, string? payload)
        {
            var pk = new PartitionKeyBuilder().AddNoneType().Add(key).Add(ModelTypes.GoldenCustomerVehicleLinks).Build();
            return hash == "TOMBSTONE"
                ? DeleteAsync<GoldenCustomerVehicleLinksModel>(customers, key, pk)
                : payload is null ? Task.FromResult(PumpOutcome.Failed)
                : UpsertAsync(customers, MapVinLinks(key, payload), pk);
        }
        Task<PumpOutcome> PumpVinOwner(string key, string hash, string? payload)
        {
            var pk = new PartitionKeyBuilder().Add(key).Add(ModelTypes.VehicleGoldenOwnership).AddNoneType().Build();
            return hash == "TOMBSTONE"
                ? DeleteAsync<VehicleGoldenOwnershipModel>(vehicles, key, pk)
                : payload is null ? Task.FromResult(PumpOutcome.Failed)
                : UpsertAsync(vehicles, MapVinOwner(key, payload), pk);
        }
        async Task<PumpOutcome> PumpStamp(string key, string hash, string? payload)
        {
            int sep = key.IndexOf('|');
            var target = stampAddress!(key[..sep], key[(sep + 1)..]);
            if (target is null) return PumpOutcome.MissingDoc;             // host says: no landing doc for this profile
            // Null (not None/undefined): the platform's landing-doc writers serialize CustomerModel
            // with CompanyID present-and-null and build the partition key with AddNullValue — in a
            // hierarchical PK those are DIFFERENT partition values, and AddNoneType here would 404
            // against every null-company landing doc and silently consume its stamp.
            var pkb = new PartitionKeyBuilder();
            if (target.CompanyId is long c) pkb.Add(c); else pkb.AddNullValue();
            var pk = pkb.Add(target.CustomerId).Add(ModelTypes.DealerCustomer).Build();
            try
            {
                await customers.PatchItemAsync<CustomerModel>(target.DocId, pk,
                    [PatchOperation.Set("/" + nameof(CustomerModel.GoldenCustomerID), hash)]);   // stamp hash IS the golden id
                return PumpOutcome.Upserted;
            }
            catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.NotFound) { return PumpOutcome.MissingDoc; }
        }

        // ---- the restartable batch loop, once per family
        async Task DrainType(string type, Func<string, string, string?, Task<PumpOutcome>> pump)
        {
            long total = pendingByType.GetValueOrDefault(type);
            if (total == 0) return;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            long done = 0; int batchNo = 0, upserted = 0, deleted = 0;
            // keyset cursor: failed rows STAY pending but the cursor moves past them, so a poison row
            // can never spin the loop — it simply waits for a later drain run. The first batch takes
            // no key predicate (an empty-string key would satisfy no "greater than" cursor and be
            // invisible forever); later batches are strictly-after, so a failed first row can't spin.
            string after = ""; bool firstBatch = true;
            while (true)
            {
                var batch = new List<(string Key, string Hash, string? Payload)>(batchSize);
                using (var cmd = new SqlCommand(
                    $"SELECT TOP ({batchSize}) ArtifactKey, ContentHash, Payload FROM Darlastic.ProjectionState " +
                    "WHERE ArtifactType = @type AND Pending = 1 AND (@first = 1 OR ArtifactKey > @after) ORDER BY ArtifactKey", sql))
                {
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@first", firstBatch ? 1 : 0);
                    cmd.Parameters.AddWithValue("@after", after);
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) batch.Add((r.GetString(0), r.GetString(1).TrimEnd(), r.IsDBNull(2) ? null : r.GetString(2)));
                }
                if (batch.Count == 0) break;
                after = batch[^1].Key; firstBatch = false;

                var results = new PumpOutcome[batch.Count];
                var tasks = new List<Task>(batch.Count);
                for (int i = 0; i < batch.Count; i++)
                {
                    int slot = i;
                    var (key, hash, payload) = batch[i];
                    await gate.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try { results[slot] = await pump(key, hash, payload); }
                        catch (Exception ex) { results[slot] = PumpOutcome.Failed; m.RecordFailure(ex); }
                        finally { gate.Release(); }
                    }));
                }
                await Task.WhenAll(tasks);

                // mark projected — hash-guarded so a mid-drain re-stage stays pending
                var okKeys = new List<(string Key, string Hash)>();
                for (int i = 0; i < batch.Count; i++)
                {
                    switch (results[i])
                    {
                        case PumpOutcome.Upserted: okKeys.Add((batch[i].Key, batch[i].Hash)); m.Upserted++; upserted++; break;
                        case PumpOutcome.Deleted: okKeys.Add((batch[i].Key, batch[i].Hash)); m.Deleted++; deleted++; break;
                        case PumpOutcome.MissingDoc: okKeys.Add((batch[i].Key, batch[i].Hash)); m.StampsMissingDoc++; break;
                        default: m.Failed++; break;
                    }
                }
                foreach (var chunk in okKeys.Chunk(500))
                {
                    using var mark = new SqlCommand(
                        "UPDATE Darlastic.ProjectionState SET Pending = 0, ProjectedRunID = @run " +
                        "WHERE ArtifactType = @type AND Pending = 1 AND (" +
                        string.Join(" OR ", chunk.Select((_, j) => $"(ArtifactKey = @k{j} AND ContentHash = @h{j})")) + ")", sql);
                    mark.Parameters.AddWithValue("@run", projectedRun);
                    mark.Parameters.AddWithValue("@type", type);
                    for (int j = 0; j < chunk.Length; j++)
                    {
                        mark.Parameters.AddWithValue($"@k{j}", chunk[j].Key);
                        mark.Parameters.AddWithValue($"@h{j}", chunk[j].Hash);
                    }
                    int marked = mark.ExecuteNonQuery();
                    m.Skipped += chunk.Length - marked;   // hash moved mid-drain -> stays pending
                }

                done += batch.Count; batchNo++;
                if (m.Failed > 0 && okKeys.Count == 0 && batchNo == 1)
                    throw new InvalidOperationException($"Every {type} artifact in the first batch failed — check the Cosmos endpoint/emulator before retrying.");
                if (batchNo % 10 == 0 || done >= total)
                    progress($"  {type}: {done:N0}/{total:N0} ({done * 100.0 / Math.Max(1, total):F1}%) — {done / Math.Max(0.1, sw.Elapsed.TotalSeconds):F0}/s");
            }
            m.PerType[type] = (upserted, deleted);
        }

        await DrainType(Registry.ArtGolden, PumpGolden);
        await DrainType(Registry.ArtVinLinks, PumpVinLinks);
        await DrainType(Registry.ArtVinOwner, PumpVinOwner);
        if (stampAddress is not null) await DrainType(Registry.ArtStamp, PumpStamp);
        else if (pendingByType.GetValueOrDefault(Registry.ArtStamp) > 0)
            m.StampsNote = $"{pendingByType[Registry.ArtStamp]:N0} pending, skipped (no landing-doc address mapping wired)";

        m.CosmosGoldenCount = await CountAsync(customers, ModelTypes.GoldenCustomer);
        m.CosmosVinLinksCount = await CountAsync(customers, ModelTypes.GoldenCustomerVehicleLinks);
        m.CosmosVinOwnerCount = await CountAsync(vehicles, ModelTypes.VehicleGoldenOwnership);
        return m;
    }

    private static async Task<PumpOutcome> UpsertAsync<T>(Container container, T item, PartitionKey pk)
    {
        await container.UpsertItemAsync(item, pk);
        return PumpOutcome.Upserted;
    }

    private static async Task<PumpOutcome> DeleteAsync<T>(Container container, string id, PartitionKey pk)
    {
        try { await container.DeleteItemAsync<T>(id, pk); }
        catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.NotFound) { /* already gone */ }
        return PumpOutcome.Deleted;
    }

    private static async Task<long> CountAsync(Container container, string itemType)
    {
        try
        {
            long n = -1;
            using var count = container.GetItemQueryIterator<long>(new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c WHERE c.ItemType = @t").WithParameter("@t", itemType));
            while (count.HasMoreResults)
                foreach (var v in await count.ReadNextAsync()) n = v;
            return n;
        }
        catch { return -1; }   // count is best-effort
    }

    // ------------------------------------------------------------------ payload -> serving model

    /// <summary>Map the staged canonical payload ({"goldenId","attrs":[{t,v}],"members":[...]})
    /// onto the serving model. Unknown attr types are ignored (forward-compatible).</summary>
    private static GoldenCustomerModel MapGolden(string key, string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var model = new GoldenCustomerModel
        {
            id = key,
            GoldenCustomerID = key,
            CustomerID = key,
        };
        if (root.TryGetProperty("attrs", out var attrs))
            foreach (var a in attrs.EnumerateArray())
            {
                string t = a.GetProperty("t").GetString() ?? "";
                string v = a.GetProperty("v").GetString() ?? "";
                switch (t)
                {
                    case "full_name": model.FullName = v; break;
                    case "phone": model.PhoneNumbers = [v]; break;
                    case "city": model.City = v; break;
                    case "national_id": model.IDNumber = v; break;
                    case "email": model.Email = v; break;
                }
            }
        if (root.TryGetProperty("members", out var members))
            model.SourceProfiles = members.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList();
        return model;
    }

    private static GoldenCustomerVehicleLinksModel MapVinLinks(string key, string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        return new GoldenCustomerVehicleLinksModel
        {
            id = key,
            GoldenCustomerID = key,
            CustomerID = key,
            Links = doc.RootElement.GetProperty("links").EnumerateArray().Select(MapLink).ToList(),
        };
    }

    private static VehicleGoldenOwnershipModel MapVinOwner(string key, string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        return new VehicleGoldenOwnershipModel
        {
            id = key,
            VIN = key,
            Owners = doc.RootElement.GetProperty("owners").EnumerateArray().Select(MapLink).ToList(),
        };
    }

    private static GoldenVehicleLinkModel MapLink(JsonElement a) => new()
    {
        VIN = a.GetProperty("vin").GetString(),
        GoldenCustomerID = a.GetProperty("goldenId").GetInt64().ToString(CultureInfo.InvariantCulture),
        FullName = a.GetProperty("name").GetString(),
        Role = a.GetProperty("role").GetString(),
        EffectiveFrom = ParseDate(a, "from"),
        EffectiveTo = ParseDate(a, "to"),
        SaleCount = a.GetProperty("sales").GetInt32(),
        ServiceCount = a.GetProperty("services").GetInt32(),
        FirstSale = ParseDate(a, "firstSale"),
        LastSale = ParseDate(a, "lastSale"),
        FirstService = ParseDate(a, "firstService"),
        LastService = ParseDate(a, "lastService"),
        Sources = a.GetProperty("sources").EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList(),
    };

    private static DateTime? ParseDate(JsonElement e, string prop) =>
        e.GetProperty(prop).GetString() is { Length: > 0 } s
            ? DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;

    // ------------------------------------------------------------------ purge

    public sealed class PurgeMetrics
    {
        public Dictionary<string, long> DeletedByType { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, long> RemainingByType { get; } = new(StringComparer.Ordinal);
        public bool DryRun { get; set; }
        public override string ToString() =>
            (DryRun ? "would delete: " : "deleted: ") +
            string.Join(" / ", DeletedByType.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key} {kv.Value:N0}")) +
            "; remaining: " +
            string.Join(" / ", RemainingByType.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key} {kv.Value:N0}"));
    }

    /// <summary>
    /// Remove every document Darlastic OWNS, by ItemType, leaving the containers themselves alone.
    ///
    /// Deleting the containers (let alone the databases) is not an option beyond a private emulator:
    /// BOTH of them are shared platform containers that Darlastic is only a tenant of.
    /// <c>Customers/Customers</c> also holds the landing customer docs the stamp family points AT, and
    /// <c>CompanyData/Vehicles</c> is the platform's entire vehicle catalogue — VIN specs, warranty,
    /// SSC, everything the vehicle lookup serves. A container drop to clear ~8K ownership docs would
    /// take the whole catalogue with it. So the purge is always by ItemType, and the three types below
    /// are exhaustive: they are the only things <see cref="RunProjectAsync"/> ever writes (stamps patch
    /// a field on someone else's doc and are therefore NOT ours to delete — see the note below).
    ///
    /// The caller is expected to re-stage the registry afterwards (<c>Registry.RestageProjections</c>):
    /// ProjectionState claims "Cosmos holds hash H" for every artifact, and a purge makes that a lie —
    /// without re-staging, the next resolve reads zero-delta and never repopulates what was removed.
    /// </summary>
    public static async Task<PurgeMetrics> PurgeAsync(Action<string>? progress = null, bool dryRun = false, int maxConcurrency = 200)
    {
        progress ??= Console.WriteLine;
        var m = new PurgeMetrics { DryRun = dryRun };

        bool isEmulator = ConnectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        var options = new CosmosClientOptions
        {
            AllowBulkExecution = true,
            ConnectionMode = isEmulator ? ConnectionMode.Gateway : ConnectionMode.Direct,
            ServerCertificateCustomValidationCallback = isEmulator ? (_, _, _) => true : null,
        };
        using var cosmos = new CosmosClient(ConnectionString, options);
        var customers = cosmos.GetContainer(NoSQLConstants.Databases.Customers + DbSuffix, NoSQLConstants.Containers.Customers_Customers);
        var vehicles = cosmos.GetContainer(NoSQLConstants.Databases.CompanyData + DbSuffix, NoSQLConstants.Containers.Vehicles);

        // (container, ItemType, id -> partition key) — mirrors the write-side pumps exactly.
        (Container Container, PartitionedItemType Type, Func<string, PartitionKey> Pk)[] families =
        [
            (customers, ModelTypes.GoldenCustomer,
                id => new PartitionKeyBuilder().AddNoneType().Add(id).Add(ModelTypes.GoldenCustomer).Build()),
            (customers, ModelTypes.GoldenCustomerVehicleLinks,
                id => new PartitionKeyBuilder().AddNoneType().Add(id).Add(ModelTypes.GoldenCustomerVehicleLinks).Build()),
            (vehicles, ModelTypes.VehicleGoldenOwnership,
                id => new PartitionKeyBuilder().Add(id).Add(ModelTypes.VehicleGoldenOwnership).AddNoneType().Build()),
        ];

        foreach (var (container, type, pk) in families)
        {
            string t = type.ToString();
            long deleted = 0, failed = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (dryRun)
            {
                // Report what a real run WOULD remove — the full count, not the first page. A dry run
                // that under-reports by a page size is worse than no dry run: it reads as "small".
                long n = await CountAsync(container, t);
                m.DeletedByType[t] = n;
                m.RemainingByType[t] = n;
                continue;
            }

            // Page ids rather than materializing the whole key set: a large tenant's golden family is
            // ~1M ids, and deletes free the pages as we go.
            while (true)
            {
                var ids = new List<string>();
                using (var it = container.GetItemQueryIterator<IdOnly>(
                    new QueryDefinition($"SELECT TOP {maxConcurrency * 5} c.id FROM c WHERE c.ItemType = @t").WithParameter("@t", t)))
                    while (it.HasMoreResults && ids.Count < maxConcurrency * 5)
                        foreach (var row in await it.ReadNextAsync())
                            if (row.id is { Length: > 0 }) ids.Add(row.id);

                if (ids.Count == 0) break;

                var gate = new SemaphoreSlim(maxConcurrency);
                var tasks = ids.Select(async id =>
                {
                    await gate.WaitAsync();
                    try
                    {
                        using var resp = await container.DeleteItemStreamAsync(id, pk(id));
                        return resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.NotFound;
                    }
                    catch { return false; }
                    finally { gate.Release(); }
                });
                foreach (var ok in await Task.WhenAll(tasks)) { if (ok) deleted++; else failed++; }
                progress($"  {t}: {deleted:N0} deleted — {deleted / Math.Max(0.1, sw.Elapsed.TotalSeconds):F0}/s");

                // Every id in the page failing means the partition-key shape is wrong for this account
                // (or the endpoint is gone). Looping would re-read the same page forever.
                if (failed >= ids.Count)
                    throw new InvalidOperationException(
                        $"Every {t} delete in a page failed — refusing to spin. Check the container's partition-key definition against this account.");
            }

            m.DeletedByType[t] = deleted;
            m.RemainingByType[t] = await CountAsync(container, t);
        }

        // Stamps are deliberately absent: that family writes a GoldenCustomerID onto a LANDING doc the
        // platform owns. Purging Darlastic must never delete those; clearing the field back out is a
        // separate migration against someone else's data, and today nothing drains stamps at all.
        return m;
    }

    private sealed class IdOnly { public string? id { get; set; } }

    // ------------------------------------------------------------------ point-reads (verification/debug)

    /// <summary>Point-read one golden doc: (None, id, GoldenCustomer).</summary>
    public static Task<string?> ReadGoldenAsync(string goldenId) => ReadAsync(
        NoSQLConstants.Databases.Customers + DbSuffix, NoSQLConstants.Containers.Customers_Customers, goldenId,
        new PartitionKeyBuilder().AddNoneType().Add(goldenId).Add(ModelTypes.GoldenCustomer).Build());

    /// <summary>Point-read one golden's vehicle-links doc: (None, id, GoldenCustomerVehicleLinks).</summary>
    public static Task<string?> ReadCustomerVehicleLinksAsync(string goldenId) => ReadAsync(
        NoSQLConstants.Databases.Customers + DbSuffix, NoSQLConstants.Containers.Customers_Customers, goldenId,
        new PartitionKeyBuilder().AddNoneType().Add(goldenId).Add(ModelTypes.GoldenCustomerVehicleLinks).Build());

    /// <summary>Point-read one VIN's ownership-timeline doc: (vin, VehicleGoldenOwnership, None).</summary>
    public static Task<string?> ReadVehicleOwnershipAsync(string vin) => ReadAsync(
        NoSQLConstants.Databases.CompanyData + DbSuffix, NoSQLConstants.Containers.Vehicles, vin,
        new PartitionKeyBuilder().Add(vin).Add(ModelTypes.VehicleGoldenOwnership).AddNoneType().Build());

    /// <summary>
    /// Phone → golden customers, via an INDEXED query over the EXISTING golden docs — no extra
    /// projection family, no extra writes: PhoneNumbers is an array and Cosmos default indexing
    /// covers arrays, so ARRAY_CONTAINS is an index seek, not a scan. Cross-partition by design
    /// (goldens all live at the undefined-CompanyID level keyed by their own id); a shared phone
    /// can legitimately return several goldens — callers disambiguate. Returns (docs, RU charge)
    /// so consumers/probes can watch the cost.
    /// </summary>
    public static async Task<(List<string> Docs, double RequestCharge)> QueryGoldensByPhoneAsync(string phoneDigits, int max = 10)
    {
        bool isEmulator = ConnectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        var options = new CosmosClientOptions
        {
            ConnectionMode = isEmulator ? ConnectionMode.Gateway : ConnectionMode.Direct,
            ServerCertificateCustomValidationCallback = isEmulator ? (_, _, _) => true : null,
        };
        using var cosmos = new CosmosClient(ConnectionString, options);
        var container = cosmos.GetContainer(NoSQLConstants.Databases.Customers + DbSuffix, NoSQLConstants.Containers.Customers_Customers);
        var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.ItemType = @t AND ARRAY_CONTAINS(c.PhoneNumbers, @p)")
            .WithParameter("@t", ModelTypes.GoldenCustomer.ToString())
            .WithParameter("@p", phoneDigits);
        var docs = new List<string>();
        double ru = 0;
        using var it = container.GetItemQueryStreamIterator(q, requestOptions: new QueryRequestOptions { MaxItemCount = max });
        while (it.HasMoreResults && docs.Count < max)
        {
            using var page = await it.ReadNextAsync();
            ru += page.Headers.RequestCharge;
            if (!page.IsSuccessStatusCode) break;
            using var doc = await System.Text.Json.JsonDocument.ParseAsync(page.Content);
            foreach (var d in doc.RootElement.GetProperty("Documents").EnumerateArray())
            {
                docs.Add(d.GetRawText());
                if (docs.Count >= max) break;
            }
        }
        return (docs, ru);
    }

    private static async Task<string?> ReadAsync(string database, string containerName, string id, PartitionKey pk)
    {
        bool isEmulator = ConnectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        var options = new CosmosClientOptions
        {
            ConnectionMode = isEmulator ? ConnectionMode.Gateway : ConnectionMode.Direct,
            ServerCertificateCustomValidationCallback = isEmulator ? (_, _, _) => true : null,
        };
        using var cosmos = new CosmosClient(ConnectionString, options);
        var container = cosmos.GetContainer(database, containerName);
        try
        {
            using var resp = await container.ReadItemStreamAsync(id, pk);
            if (!resp.IsSuccessStatusCode) return null;
            using var reader = new StreamReader(resp.Content);
            return await reader.ReadToEndAsync();
        }
        catch { return null; }
    }

    private static void AcquireProjectLock(SqlConnection conn)
    {
        using var cmd = new SqlCommand("sp_getapplock", conn) { CommandType = System.Data.CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@Resource", "Darlastic.Project");
        cmd.Parameters.AddWithValue("@LockMode", "Exclusive");
        cmd.Parameters.AddWithValue("@LockOwner", "Session");
        cmd.Parameters.AddWithValue("@LockTimeout", 0);
        var ret = cmd.Parameters.Add("@rv", System.Data.SqlDbType.Int);
        ret.Direction = System.Data.ParameterDirection.ReturnValue;
        cmd.ExecuteNonQuery();
        if ((int)ret.Value! < 0)
            throw new InvalidOperationException("Another Darlastic projection drain is already running.");
    }
}
