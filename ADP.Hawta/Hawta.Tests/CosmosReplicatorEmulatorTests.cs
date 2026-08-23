using System.Net.Sockets;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Wet-mode integration tests against the LOCAL Cosmos emulator. Skipped automatically when
/// no emulator is listening on localhost:8081 (dev machines run it; CI does not). Exercises
/// the real pump: upserts with a hierarchical partition key, stamp semantics, coordinate
/// changes (delete-old-then-write-new), and tombstone point deletes.
/// </summary>
[Collection(CosmosEmulatorCollection.Name)]
public class CosmosReplicatorEmulatorTests : IDisposable
{
    // 127.0.0.1, not localhost: the emulator listens on IPv4 only and localhost can resolve to ::1.
    private const string EmulatorEndpoint = "https://127.0.0.1:8081/";
    private const string EmulatorKey =
        "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    private readonly TestSnapshot snapshot = new();

    private static bool EmulatorIsRunning()
    {
        try
        {
            using var client = new TcpClient();
            return client.ConnectAsync("127.0.0.1", 8081).Wait(500);
        }
        catch
        {
            return false;
        }
    }

    private static CosmosClient CreateClient() => new(
        EmulatorEndpoint, EmulatorKey,
        new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            LimitToEndpoint = true,
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            }),
        });

    private static CosmosFamilyMapping Family(string database) => new()
    {
        Family = "Widget",
        Database = database,
        Container = "Widgets",
        Map = row => new CosmosDocument
        {
            // id derives from Code, so changing Code is a COORDINATE CHANGE, not just an update.
            Id = $"{row.Values["Code"]}-{row.PrimaryKey}",
            PartitionKey = [(string)row.Values["Code"]!, "Widget", 7d],
            Body = new Dictionary<string, object?>
            {
                ["Code"] = row.Values["Code"],
                ["Quantity"] = row.Values["Quantity"],
                ["ItemType"] = "Widget",
                ["CompanyID"] = 7,
            },
        },
    };

    [Fact]
    public async Task WetPump_Upserts_HandlesCoordinateChanges_AndDeletesTombstones()
    {
        Assert.SkipWhen(!EmulatorIsRunning(), "Cosmos emulator is not running on localhost:8081.");

        using var client = CreateClient();
        var databaseId = $"HawtaTest-{Guid.NewGuid():N}";
        Database? database = null;

        try
        {
            // The emulator intermittently 503s on database/container creation when its
            // provisioned-collections pool is under pressure. Retry briefly; if it persists,
            // the environment (not the code) can't run this test right now — skip, don't fail.
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    database ??= await client.CreateDatabaseAsync(databaseId);
                    await database.CreateContainerAsync(new ContainerProperties
                    {
                        Id = "Widgets",
                        PartitionKeyPaths = ["/Code", "/ItemType", "/CompanyID"],
                    });
                    break;
                }
                catch (CosmosException busy) when (busy.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    if (attempt >= 4)
                        Assert.Skip("Cosmos emulator is throttling container creation (503) — environmental, not a product failure.");
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
                }
            }

            var replicator = new CosmosSnapshotReplicator(snapshot.Store, client);
            var options = new CosmosSnapshotReplicatorOptions
            {
                Table = snapshot.Table,
                Families = [Family(databaseId)],
            };
            var container = client.GetContainer(databaseId, "Widgets");
            PartitionKey Pk(string code) =>
                new PartitionKeyBuilder().Add(code).Add("Widget").Add(7d).Build();

            // 1) First pump: two inserts reach Cosmos; stamps set; queue drains.
            snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 2)]);
            var first = await replicator.RunOnceAsync(options);

            Assert.Equal(2, first.Upserted);
            Assert.Equal(0, first.Failed);
            Assert.Equal(0, snapshot.Store.CountDirtyRows(snapshot.Table));

            using (var read = await container.ReadItemStreamAsync("alpha-W1", Pk("alpha")))
                Assert.True(read.IsSuccessStatusCode);

            // 2) Second pump with nothing dirty: zero ops (idempotence of the queue).
            var idle = await replicator.RunOnceAsync(options);
            Assert.Equal(0, idle.RowsRead);

            // 3) Coordinate change: Code alpha→gamma changes id AND partition key —
            //    the old document must be deleted, the new one written.
            snapshot.Merge([("W1", "gamma", 1), ("W2", "beta", 2)]);
            var moved = await replicator.RunOnceAsync(options);

            Assert.Equal(1, moved.Upserted);
            Assert.Equal(1, moved.Deleted);
            using (var oldDoc = await container.ReadItemStreamAsync("alpha-W1", Pk("alpha")))
                Assert.Equal(System.Net.HttpStatusCode.NotFound, oldDoc.StatusCode);
            using (var newDoc = await container.ReadItemStreamAsync("gamma-W1", Pk("gamma")))
                Assert.True(newDoc.IsSuccessStatusCode);

            // 4) Tombstone: W2 vanishes → point delete at the stamped coordinates.
            snapshot.Merge([("W1", "gamma", 1)]);
            var tombstoned = await replicator.RunOnceAsync(options);

            Assert.Equal(1, tombstoned.Deleted);
            Assert.Equal(0, snapshot.Store.CountDirtyRows(snapshot.Table));
            using (var deleted = await container.ReadItemStreamAsync("beta-W2", Pk("beta")))
                Assert.Equal(System.Net.HttpStatusCode.NotFound, deleted.StatusCode);

            // 5) Re-delete is idempotent: force the tombstone dirty again (older watermark)
            //    — 404 on the point delete counts as success.
            snapshot.Store.Execute(
                "UPDATE data.\"Widget\" SET \"_LastReplicationDate\" = \"_LastReplicationDate\" - INTERVAL 1 SECOND WHERE \"_PrimaryKey\" = 'W2'");
            var redeleted = await replicator.RunOnceAsync(options);
            Assert.Equal(0, redeleted.Failed);
        }
        finally
        {
            if (database is not null)
                await database.DeleteAsync();
        }
    }

    public void Dispose() => snapshot.Dispose();
}
