using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// The Cosmos read path: resume from a persisted continuation token, page-and-merge, and the
/// failure directions that matter — a cursor must never move past documents that did not land,
/// and a re-read of unchanged documents must cost nothing.
/// </summary>
public sealed class CosmosChangeFeedIngestorTests : IDisposable
{
    private readonly SnapshotStore store =
        SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });

    private static readonly SnapshotTableDefinition Table = new("SscLog",
    [
        new SnapshotColumn("id", "VARCHAR"),
        new SnapshotColumn("VIN", "VARCHAR"),
        new SnapshotColumn("SSC", "VARCHAR"),
    ]);

    private static readonly CosmosSourceRead Container = new("Logs", "SSC");

    public CosmosChangeFeedIngestorTests() => store.EnsureTable(Table);

    public void Dispose() => store.Dispose();

    /// <summary>
    /// A change feed modelled as what it is: an append log of document versions, with the
    /// continuation token as a position in it. A document mutated twice appears twice, which is
    /// exactly the case the merge refuses to be handed unresolved.
    /// </summary>
    private sealed class FakeChangeFeed : ICosmosSnapshotReader
    {
        private readonly List<JsonObject> log = [];

        public List<CosmosChangeFeedRequest> Requests { get; } = [];
        public int PageSize { get; set; } = 2;

        /// <summary>A token the service refuses, modelling an expired or malformed continuation.</summary>
        public string? RejectToken { get; set; }

        /// <summary>Called after each page is produced — the hook a test uses to interrupt a drain.</summary>
        public Action<int>? OnPage { get; set; }

        public void Write(string id, string vin, params double[] sscQuantities)
        {
            var ssc = new JsonArray();
            foreach (var quantity in sscQuantities)
                ssc.Add(new JsonObject { ["Code"] = "R21", ["Qty"] = quantity });

            log.Add(new JsonObject { ["id"] = id, ["VIN"] = vin, ["SSC"] = ssc });
        }

        public void WriteRaw(JsonObject document) => log.Add(document);

        public async IAsyncEnumerable<CosmosChangeFeedPage> ReadChangeFeedAsync(
            CosmosChangeFeedRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            Requests.Add(request);

            if (request.ContinuationToken is not null && request.ContinuationToken == RejectToken)
                throw new CosmosChangeFeedTokenException($"Token '{request.ContinuationToken}' is not resumable.");

            var position = request.ContinuationToken is null ? 0 : int.Parse(request.ContinuationToken);
            var pages = 0;

            while (position < log.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var page = log.Skip(position).Take(PageSize).ToList();
                position += page.Count;
                OnPage?.Invoke(++pages);
                yield return new CosmosChangeFeedPage(page, position.ToString(), CaughtUp: false, RequestCharge: 1);
            }

            // The 304: no documents, but the position still matters and arrives as an ordinary
            // response rather than an exception.
            yield return new CosmosChangeFeedPage([], position.ToString(), CaughtUp: true, RequestCharge: 0.5);
        }
    }

    private static CosmosChangeFeedIngestorOptions Options(
        ICosmosSnapshotReader reader,
        int mergeBatchSize = 1000,
        string? ingestVersion = null,
        Action<string>? onDiagnostic = null,
        bool deletesEnabled = false,
        CosmosSourceRead? container = null) => new()
        {
            Table = Table,
            Reader = reader,
            Source = container ?? Container,
            SourceKey = "cosmos-ssc-lookup",
            PrimaryKeyColumn = "id",
            IngestVersion = ingestVersion,
            MergeBatchSize = mergeBatchSize,
            OnDiagnostic = onDiagnostic,
            Project = document => new Dictionary<string, object?>
            {
                ["id"] = (string?)document["id"],
                ["VIN"] = (string?)document["VIN"],
                // The canonical form, not the service's raw text — see CanonicalizedJson_* below.
                ["SSC"] = CosmosDocHash.CanonicalText(document["SSC"]),
            },
            MergeOptions = new SnapshotMergeOptions
            {
                Source = "cosmos-ssc-lookup",
                RecordIdentityKind = SourceRecordIdentityKind.DatabaseKey,
                DeletesEnabled = deletesEnabled,
            },
        };

    private long LiveRows() => Convert.ToInt64(store.ExecuteScalar(
        "SELECT count(*) FROM data.\"SscLog\" WHERE \"_Deleted\" = false"));

    [Fact]
    public async Task Bootstrap_ReadsFromTheBeginning_AndPersistsTheCursor()
    {
        var feed = new FakeChangeFeed();
        feed.Write("a", "VIN-A");
        feed.Write("b", "VIN-B");
        feed.Write("c", "VIN-C");

        var result = await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));

        Assert.Equal(SnapshotMergeStatus.Succeeded, result.Status);
        Assert.Equal(3, result.RowsInserted);
        Assert.Equal(3, LiveRows());

        // No prior cursor means the beginning of the container — the bootstrap, and the only
        // routine full read.
        Assert.Null(feed.Requests[0].ContinuationToken);

        var cursor = store.ReadSourceCosmosCursor("cosmos-ssc-lookup");
        Assert.NotNull(cursor);
        Assert.Equal("3", cursor.ContinuationToken);
        Assert.Equal("Logs", cursor.Database);
        Assert.Equal("SSC", cursor.Container);
    }

    [Fact]
    public async Task SecondRun_ResumesFromTheStoredCursor_AndIngestsOnlyTheDelta()
    {
        var feed = new FakeChangeFeed();
        feed.Write("a", "VIN-A");
        feed.Write("b", "VIN-B");
        await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));

        feed.Write("c", "VIN-C");
        var second = await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));

        Assert.Equal("2", feed.Requests[^1].ContinuationToken);
        Assert.Equal(1, second.RowsStaged);
        Assert.Equal(1, second.RowsInserted);
        Assert.Equal(0, second.RowsUpdated);
        Assert.Equal(3, LiveRows());
    }

    [Fact]
    public async Task DrainStopsOnTheCaughtUpResponse_AndPersistsThatResponsesToken()
    {
        var feed = new FakeChangeFeed { PageSize = 1 };
        feed.Write("a", "VIN-A");

        await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));

        // One request; the enumerator ended at the caught-up page rather than spinning.
        Assert.Single(feed.Requests);
        Assert.Equal("1", store.ReadSourceCosmosCursor("cosmos-ssc-lookup")!.ContinuationToken);
    }

    [Fact]
    public async Task CaughtUpWithNoDocuments_StillWritesARunRecord()
    {
        // A source whose reads stop producing has to stay VISIBLE in meta.SyncRuns. Silence would
        // make "caught up" and "crashing before staging" the same absence, and only one is healthy.
        var feed = new FakeChangeFeed();

        var result = await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));

        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, result.Status);
        Assert.Equal(0, result.RowsStaged);
        Assert.Equal("Skipped:SourceUnchanged", store.ExecuteScalar(
            "SELECT \"Status\" FROM meta.SyncRuns WHERE \"Source\" = 'cosmos-ssc-lookup'"));
    }

    [Fact]
    public async Task PageAndMerge_MergesInBatches_AdvancingTheCursorWithEachOne()
    {
        // Never accumulate-then-merge: ingest holds the single-threaded write gate on a renewing
        // lease, so a bootstrap read into one giant staging blocks every other source and loses
        // everything if the lease drops.
        var feed = new FakeChangeFeed { PageSize = 1 };
        for (var i = 0; i < 6; i++)
            feed.Write($"doc-{i}", $"VIN-{i}");

        var result = await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed, mergeBatchSize: 2));

        Assert.Equal(6, result.RowsInserted);
        Assert.Equal(6, LiveRows());

        // THREE merge transactions, ONE run record. This assertion used to demand three records —
        // "each its own run record" — and that was wrong in a way only a real bootstrap showed:
        // meta.SyncRuns means "one row per SOURCE RUN" everywhere it is read. The publisher takes
        // the newest row per source into the manifest's sourceRuns, and the health checks age off
        // it. A 49,532-document bootstrap wrote 25 rows and the manifest published the last
        // batch's 1,532 as though it were the run.
        Assert.Equal(1L, Convert.ToInt64(store.ExecuteScalar(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Source\" = 'cosmos-ssc-lookup'")));

        // And that one row carries the WHOLE drain, not its last batch.
        Assert.Equal(6L, Convert.ToInt64(store.ExecuteScalar(
            "SELECT \"RowsInserted\" FROM meta.SyncRuns WHERE \"Source\" = 'cosmos-ssc-lookup'")));
        Assert.Equal(6L, Convert.ToInt64(store.ExecuteScalar(
            "SELECT \"RowsStaged\" FROM meta.SyncRuns WHERE \"Source\" = 'cosmos-ssc-lookup'")));

        // The id the caller printed is the id the record carries, so a console line and a manifest
        // entry can be matched up.
        Assert.Equal(result.RunId, store.ExecuteScalar(
            "SELECT \"RunId\" FROM meta.SyncRuns WHERE \"Source\" = 'cosmos-ssc-lookup'"));
    }

    [Fact]
    public async Task ADocumentSeenTwiceInOneDrain_MergesAsOneRow_LastVersionWinning()
    {
        // The change feed re-emits a document that is mutated while we are paging. The merge
        // refuses duplicate keys outright, so the ingestor has to resolve them — newest wins.
        var feed = new FakeChangeFeed { PageSize = 1 };
        feed.Write("a", "VIN-OLD");
        feed.Write("a", "VIN-NEW");

        var result = await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));

        Assert.Equal(SnapshotMergeStatus.Succeeded, result.Status);
        Assert.Equal(1, LiveRows());
        Assert.Equal("VIN-NEW", store.ExecuteScalar("SELECT \"VIN\" FROM data.\"SscLog\" WHERE \"id\" = 'a'"));
    }

    [Fact]
    public async Task AFullReReadOfUnchangedDocuments_ChangesNothing()
    {
        // D2's central safety claim, and the reason an expired token is survivable: re-reading the
        // whole container produces no row versions, because the merge diffs content hashes.
        var feed = new FakeChangeFeed();
        feed.Write("a", "VIN-A", 1.5);
        feed.Write("b", "VIN-B", 2);
        await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));

        var sequenceBefore = store.ExecuteScalar("SELECT max(\"_ChangeSequence\") FROM data.\"SscLog\"");

        store.ClearSourceCosmosCursor("cosmos-ssc-lookup");
        var again = await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));

        Assert.Equal(2, again.RowsStaged);
        Assert.Equal(0, again.RowsInserted);
        Assert.Equal(0, again.RowsUpdated);
        Assert.Equal(sequenceBefore, store.ExecuteScalar("SELECT max(\"_ChangeSequence\") FROM data.\"SscLog\""));
    }

    [Fact]
    public async Task CanonicalizedJson_AbsorbsCosmosNumberRendering_SoAReReadIsStillANoOp()
    {
        // _RowHash hashes the byte literal of the stored VARCHAR, and Cosmos re-renders numbers on
        // read: a written 1.500 comes back 1.5. Storing the raw text would republish every row on
        // every re-read — the promised no-op above would be false for any document with a number.
        var feed = new FakeChangeFeed();
        feed.WriteRaw(new JsonObject
        {
            ["id"] = "a",
            ["VIN"] = "VIN-A",
            ["SSC"] = new JsonArray(JsonNode.Parse("""{"Qty":1.500,"Code":"R21"}""")),
        });
        await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));

        var rehydrated = new FakeChangeFeed();
        rehydrated.WriteRaw(new JsonObject
        {
            ["id"] = "a",
            ["VIN"] = "VIN-A",
            // Same value, re-rendered and re-ordered exactly as the service does it.
            ["SSC"] = new JsonArray(JsonNode.Parse("""{"Code":"R21","Qty":1.5}""")),
        });

        store.ClearSourceCosmosCursor("cosmos-ssc-lookup");
        var again = await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(rehydrated));

        Assert.Equal(1, again.RowsStaged);
        Assert.Equal(0, again.RowsUpdated);
    }

    [Fact]
    public async Task AFailedMerge_StopsTheDrain_AndLeavesTheCursorWhereItWas()
    {
        // The one mistake this design cannot absorb: advancing past documents that did not land.
        var feed = new FakeChangeFeed { PageSize = 2 };
        feed.Write("a", "VIN-A");
        feed.Write("b", "VIN-B");
        await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed, mergeBatchSize: 2));
        var cursorAfterGoodRun = store.ReadSourceCosmosCursor("cosmos-ssc-lookup")!.ContinuationToken;

        // A document with no id. It is NOT dropped — it reaches staging with a NULL key and fails
        // the whole run loudly, rather than the source quietly ingesting most of a container.
        feed.WriteRaw(new JsonObject { ["VIN"] = "VIN-NO-ID" });
        feed.Write("c", "VIN-C");

        var result = await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed, mergeBatchSize: 2));

        Assert.Equal(SnapshotMergeStatus.FailedInvalidStagingRows, result.Status);
        Assert.Equal(cursorAfterGoodRun, store.ReadSourceCosmosCursor("cosmos-ssc-lookup")!.ContinuationToken);
        Assert.Equal(2, LiveRows());
    }

    [Fact]
    public async Task ARejectedToken_DiscardsTheCursor_RereadsFromTheBeginning_AndSaysSo()
    {
        var feed = new FakeChangeFeed();
        feed.Write("a", "VIN-A");
        feed.Write("b", "VIN-B");
        await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));

        var diagnostics = new List<string>();
        feed.RejectToken = "2";

        var result = await CosmosChangeFeedSnapshotIngestor.IngestAsync(
            store, Options(feed, onDiagnostic: diagnostics.Add));

        // Re-read everything, changed nothing, and left a cursor that works again.
        Assert.Equal(SnapshotMergeStatus.Succeeded, result.Status);
        Assert.Equal(2, result.RowsStaged);
        Assert.Equal(0, result.RowsUpdated);
        Assert.Equal("2", store.ReadSourceCosmosCursor("cosmos-ssc-lookup")!.ContinuationToken);
        Assert.Contains(diagnostics, message => message.Contains("re-reading the container from the beginning"));
    }

    [Fact]
    public async Task AChangedIngestVersion_DiscardsTheCursor()
    {
        // The operator lever, and deliberately the ONLY automatic full-re-read trigger: here a
        // re-read costs the whole container, so it happens when someone asks for it.
        var feed = new FakeChangeFeed();
        feed.Write("a", "VIN-A");
        await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed, ingestVersion: "v1"));

        var diagnostics = new List<string>();
        await CosmosChangeFeedSnapshotIngestor.IngestAsync(
            store, Options(feed, ingestVersion: "v2", onDiagnostic: diagnostics.Add));

        Assert.Null(feed.Requests[^1].ContinuationToken);
        Assert.Equal("v2", store.ReadSourceCosmosCursor("cosmos-ssc-lookup")!.IngestVersion);
        Assert.Contains(diagnostics, message => message.Contains("IngestVersion changed"));
    }

    [Fact]
    public async Task ACursorForADifferentContainer_IsNotHonoured()
    {
        // The addressing check, and the analogue of the file gate's path check: a token is only
        // meaningful against the container that issued it.
        var feed = new FakeChangeFeed();
        feed.Write("a", "VIN-A");
        await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));

        var diagnostics = new List<string>();
        await CosmosChangeFeedSnapshotIngestor.IngestAsync(
            store,
            Options(feed, container: new CosmosSourceRead("Logs", "PartLookup"), onDiagnostic: diagnostics.Add));

        Assert.Null(feed.Requests[^1].ContinuationToken);
        Assert.Equal("PartLookup", store.ReadSourceCosmosCursor("cosmos-ssc-lookup")!.Container);
        Assert.Contains(diagnostics, message => message.Contains("addresses Logs/SSC"));
    }

    [Fact]
    public async Task DeleteEnabledMerge_IsRefused()
    {
        // A change feed presents only what changed. Merging it with deletes on would read "absent"
        // as "gone" and tombstone the entire table on the first run.
        var feed = new FakeChangeFeed();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed, deletesEnabled: true)));

        Assert.Contains("DeletesEnabled must be false", exception.Message);
    }

    [Fact]
    public async Task CosmosNeverDeletes_SoAVanishedDocumentKeepsItsRow()
    {
        // Stated so nobody discovers it later: hard deletes and TTL expiry upstream are invisible
        // to the snapshot. For a compliance record that is the desirable direction — the snapshot
        // becomes the archive — but it is a decision, not an accident.
        var feed = new FakeChangeFeed();
        feed.Write("a", "VIN-A");
        await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));

        var withoutA = new FakeChangeFeed();
        withoutA.Write("b", "VIN-B");
        store.ClearSourceCosmosCursor("cosmos-ssc-lookup");
        await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(withoutA));

        Assert.Equal(2, LiveRows());
        Assert.Equal(0L, Convert.ToInt64(store.ExecuteScalar(
            "SELECT count(*) FROM data.\"SscLog\" WHERE \"_Deleted\" = true")));
    }

    [Fact]
    public async Task CancellationMidDrain_KeepsWhatAlreadyMerged_AndItsCursor()
    {
        // The write gate's lease can drop mid-read. What merged stays merged, its cursor stays
        // with it, and the next cycle resumes from there.
        var feed = new FakeChangeFeed { PageSize = 1 };
        for (var i = 0; i < 6; i++)
            feed.Write($"doc-{i}", $"VIN-{i}");

        using var cts = new CancellationTokenSource();
        // Lose the lease after the fourth page: two batches of two have merged, two documents are
        // read but unmerged.
        feed.OnPage = page => { if (page == 5) cts.Cancel(); };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed, mergeBatchSize: 2), cts.Token));

        Assert.Equal(4, LiveRows());

        // The cursor describes exactly the merged rows — never the pages that were merely read.
        Assert.Equal("4", store.ReadSourceCosmosCursor("cosmos-ssc-lookup")!.ContinuationToken);

        // And the next cycle picks up the remainder.
        var resumed = await CosmosChangeFeedSnapshotIngestor.IngestAsync(store, Options(feed));
        Assert.Equal(2, resumed.RowsInserted);
        Assert.Equal(6, LiveRows());
    }

    [Fact]
    public void CanonicalText_OrdersPropertiesAndNormalizesNumbersByValue()
    {
        Assert.Equal(
            CosmosDocHash.CanonicalText(JsonNode.Parse("""{"b":1.500,"a":[15E-1]}""")),
            CosmosDocHash.CanonicalText(JsonNode.Parse("""{"a":[1.5],"b":1.5}""")));

        Assert.Equal("null", CosmosDocHash.CanonicalText(null));
    }
}
