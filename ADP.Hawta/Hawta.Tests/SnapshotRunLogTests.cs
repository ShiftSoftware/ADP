using DuckDB.NET.Data;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// The run-log exporter on its own: an in-memory write DB and a local run-log directory, no loop.
/// What is pinned here is the file layout, row parity, the "nothing new" skip, the day rewrite, the
/// since-boot filter, the error cap, the pump and fetch tables and the write-DB prune. The blob half of the same code path is proven by the host
/// repository's run-log drill against a real emulator container, not here: this suite must run on
/// any machine with nothing listening.
/// </summary>
public sealed class SnapshotRunLogTests : IDisposable
{
    private readonly TestSnapshot snapshot = new();
    private readonly string root = Path.Combine(Path.GetTempPath(), $"hawta-run-log-{Guid.NewGuid():N}");
    private readonly LocalPublishStore store;

    // A boot that is safely before every row the test writes, so "since boot" includes them all.
    private readonly DateTime boot = DateTime.UtcNow.AddMinutes(-1);
    private const string BootId = "20260919120000000-0badf00d";
    private const string SnapshotName = "run-log-test";

    public SnapshotRunLogTests() => store = new LocalPublishStore(root);

    public void Dispose()
    {
        snapshot.Dispose();
        try { Directory.Delete(root, recursive: true); } catch { /* Windows file-lock stragglers */ }
    }

    private SnapshotRunLogOptions Options(int errorTextLimit = 4_000, string? hostInstance = null) => new()
    {
        Store = store,
        ErrorTextLimit = errorTextLimit,
        HostInstance = hostInstance,
    };

    private SnapshotRunLogFlushResult Flush(DateTime? lastFlushedThrough = null, SnapshotRunLogOptions? options = null) =>
        SnapshotRunLog.Flush(snapshot.Store, options ?? Options(), SnapshotName, BootId, boot, lastFlushedThrough);

    /// <summary>A separate reader connection, the way a consumer reads: nothing shared with the write DB.</summary>
    private static object? Read(string sql)
    {
        using var reader = new DuckDBConnection("Data Source=:memory:");
        reader.Open();
        using var command = reader.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    private static long Count(string parquet) =>
        Convert.ToInt64(Read($"SELECT count(*) FROM read_parquet('{Sql(parquet)}')"));

    /// <summary>The glob a reader uses: every day, every boot, hive partitions on, columns united by name.</summary>
    private string Glob(string folder) =>
        $"read_parquet('{Sql(Path.Combine(root, folder))}/date=*/*.parquet', hive_partitioning = true, union_by_name = true)";

    private static string Sql(string path) => path.Replace('\\', '/').Replace("'", "''");

    private string DayFolder(string folder) =>
        Path.Combine(root, folder, SnapshotRunLog.PartitionPrefix + snapshot.Scalar<string>(
            "SELECT CAST(CAST(min(\"StartedAt\") AS DATE) AS VARCHAR) FROM meta.SyncRuns"));

    private string SyncRunsFile => Path.Combine(DayFolder(SnapshotRunLog.SyncRunsFolder), $"{BootId}.parquet");

    [Fact]
    public void RowsSinceBoot_LandInOneDayFile_AndReadBackExactly()
    {
        snapshot.Merge([("W1", "alpha", 1)]);
        snapshot.Merge([("W1", "alpha", 1), ("W2", "beta", 2)]);

        var result = Flush();

        Assert.False(result.Skipped);
        Assert.Equal(2, result.RowsWritten);
        var file = Assert.Single(result.FilesWritten);
        Assert.Equal(SyncRunsFile, file);
        Assert.True(File.Exists(file));
        Assert.False(File.Exists(file + ".staging"), "the local staging name must be promoted away");

        // Row parity: the same run ids, in the same number, as the table holds.
        Assert.Equal(2, Count(file));
        var runIds = Read($"SELECT string_agg(\"RunId\", ',' ORDER BY \"RunId\") FROM read_parquet('{Sql(file)}')");
        Assert.Equal(snapshot.Scalar<string>("SELECT string_agg(\"RunId\", ',' ORDER BY \"RunId\") FROM meta.SyncRuns"), runIds);

        // Provenance columns, all facts: which estate, which boot, which package, when flushed.
        Assert.Equal(SnapshotName, Read($"SELECT DISTINCT \"SnapshotName\" FROM read_parquet('{Sql(file)}')"));
        Assert.Equal(BootId, Read($"SELECT DISTINCT \"BootId\" FROM read_parquet('{Sql(file)}')"));
        Assert.Equal(2L, Convert.ToInt64(Read($"SELECT count(*) FROM read_parquet('{Sql(file)}') WHERE \"HostInstance\" IS NULL")));
        Assert.NotEqual(string.Empty, Read($"SELECT DISTINCT \"PackageVersion\" FROM read_parquet('{Sql(file)}')") as string);
        // The watermark handed back is exactly the value the file carries, to the microsecond.
        Assert.Equal(result.FlushedThrough,
            SnapshotStore.AsUtc((DateTime)Read($"SELECT DISTINCT \"FlushedAt\" FROM read_parquet('{Sql(file)}')")!));
        Assert.Equal(DateTimeKind.Utc, result.FlushedThrough!.Value.Kind);

        // The hive folder yields the day as a column, which is what a reader prunes on.
        Assert.Equal(2L, Convert.ToInt64(Read($"SELECT count(*) FROM {Glob(SnapshotRunLog.SyncRunsFolder)} WHERE date = CAST(\"StartedAt\" AS DATE)")));
    }

    [Fact]
    public void ASecondFlushWithNothingNew_WritesNothing_AndLeavesTheFileAlone()
    {
        snapshot.Merge([("W1", "alpha", 1)]);
        var first = Flush();
        var file = Assert.Single(first.FilesWritten);
        var bytes = new FileInfo(file).Length;
        var lastWrite = File.GetLastWriteTimeUtc(file);

        var second = Flush(first.FlushedThrough);

        Assert.True(second.Skipped);
        Assert.Empty(second.FilesWritten);
        Assert.Equal(0, second.RowsWritten);
        // The watermark does not move on a skip: it is the start time of the last flush that wrote.
        Assert.Equal(first.FlushedThrough, second.FlushedThrough);
        Assert.Equal(bytes, new FileInfo(file).Length);
        Assert.Equal(lastWrite, File.GetLastWriteTimeUtc(file));
    }

    [Fact]
    public void MoreRows_RewriteTheDayFile_WithTheWholeDay()
    {
        snapshot.Merge([("W1", "alpha", 1)]);
        snapshot.Merge([("W1", "alpha", 1)]);
        var first = Flush();
        Assert.Equal(2, Count(Assert.Single(first.FilesWritten)));

        snapshot.Merge([("W1", "alpha", 2)]);
        var second = Flush(first.FlushedThrough);

        // Not a delta file: the same day file, rewritten, holding every row of the day.
        Assert.False(second.Skipped);
        Assert.Equal(SyncRunsFile, Assert.Single(second.FilesWritten));
        Assert.Equal(3, second.RowsWritten);
        Assert.Equal(3, Count(SyncRunsFile));
        Assert.Single(Directory.GetFiles(DayFolder(SnapshotRunLog.SyncRunsFolder)));
        Assert.True(second.FlushedThrough > first.FlushedThrough);
    }

    [Fact]
    public void RowsOnTwoUtcDays_MakeTwoFiles_AndRowsBeforeBootAreLeftOut()
    {
        // A fixed boot, and rows placed around it by hand: one from an earlier boot (before this
        // one), one on the boot's day, one on the next day.
        var fixedBoot = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
        InsertRun("run-before-boot", fixedBoot.AddDays(-1));
        InsertRun("run-day-one", fixedBoot.AddMinutes(1));
        InsertRun("run-day-two", fixedBoot.AddDays(1).AddMinutes(1));

        var result = SnapshotRunLog.Flush(snapshot.Store, Options(), SnapshotName, BootId, fixedBoot, lastFlushedThrough: null);

        Assert.Equal(2, result.FilesWritten.Count);
        Assert.Equal(2, result.RowsWritten);
        Assert.Contains(Path.Combine(root, SnapshotRunLog.SyncRunsFolder, "date=2026-03-10", $"{BootId}.parquet"), result.FilesWritten);
        Assert.Contains(Path.Combine(root, SnapshotRunLog.SyncRunsFolder, "date=2026-03-11", $"{BootId}.parquet"), result.FilesWritten);

        var ids = Read($"SELECT string_agg(\"RunId\", ',' ORDER BY \"RunId\") FROM {Glob(SnapshotRunLog.SyncRunsFolder)}");
        Assert.Equal("run-day-one,run-day-two", ids);
        Assert.Equal(2L, Convert.ToInt64(Read(
            $"SELECT count(DISTINCT date) FROM {Glob(SnapshotRunLog.SyncRunsFolder)}")));
    }

    [Fact]
    public void ErrorText_IsShortenedToTheLimit_AndNullStaysNull()
    {
        InsertRun("run-long-error", boot.AddSeconds(1), error: new string('x', 10_000));
        InsertRun("run-no-error", boot.AddSeconds(2));

        Flush(options: Options(errorTextLimit: 100));

        Assert.Equal(100L, Convert.ToInt64(Read(
            $"SELECT length(\"Error\") FROM read_parquet('{Sql(SyncRunsFile)}') WHERE \"RunId\" = 'run-long-error'")));
        Assert.Equal(1L, Convert.ToInt64(Read(
            $"SELECT count(*) FROM read_parquet('{Sql(SyncRunsFile)}') WHERE \"RunId\" = 'run-no-error' AND \"Error\" IS NULL")));
    }

    [Fact]
    public void TheHostInstance_IsWrittenAsAColumn_WhenTheHostSuppliesOne()
    {
        snapshot.Merge([("W1", "alpha", 1)]);

        Flush(options: Options(hostInstance: "instance-7"));

        Assert.Equal("instance-7", Read($"SELECT DISTINCT \"HostInstance\" FROM read_parquet('{Sql(SyncRunsFile)}')"));
    }

    [Fact]
    public void PublishRuns_AreCopiedToo_UnderTheirOwnFolder()
    {
        using var fx = new PublisherFixture();
        fx.MergeWidgets(("W1", "alpha", 1));
        var publish = fx.Publish();
        Assert.Equal(SnapshotPublishStatus.Published, publish.Status);

        var result = SnapshotRunLog.Flush(fx.Store, Options(), SnapshotName, BootId, boot, lastFlushedThrough: null);

        var publishRuns = Assert.Single(result.FilesWritten, file => file.Contains(SnapshotRunLog.PublishRunsFolder));
        Assert.Equal(1, Count(publishRuns));
        Assert.Equal(publish.PublishId, Read($"SELECT \"PublishId\" FROM read_parquet('{Sql(publishRuns)}')"));
        Assert.Equal("Published", Read($"SELECT \"Status\" FROM read_parquet('{Sql(publishRuns)}')"));
        // The source run that fed the publish rides beside it.
        Assert.Contains(result.FilesWritten, file => file.Contains(SnapshotRunLog.SyncRunsFolder));

        // The publish table records the snapshot name itself. The file carries that one column,
        // with the value the publisher recorded, and no second copy named "SnapshotName_1".
        Assert.Equal(1, FileColumns(publishRuns).Count(column => column.StartsWith("SnapshotName", StringComparison.Ordinal)));
        Assert.Equal(PublisherFixture.SnapshotName, Read($"SELECT \"SnapshotName\" FROM read_parquet('{Sql(publishRuns)}')"));
    }

    [Fact]
    public void EveryFile_CarriesTheTablesOwnColumns_ThenEachProvenanceColumnOnce()
    {
        // All five run tables hold a row: a merge and a publish through the real code, then a
        // cycle row, a pump row and a fetch row placed by hand, the way the loop and the
        // dispatcher write them.
        using var fx = new PublisherFixture();
        fx.MergeWidgets(("W1", "alpha", 1));
        Assert.Equal(SnapshotPublishStatus.Published, fx.Publish().Status);
        InsertCycle("cycle-1", boot.AddSeconds(1), into: fx.Store);
        InsertPump("cycle-1", "Widget", boot.AddSeconds(1), into: fx.Store);
        InsertFetch("run-1", "sql-a", boot.AddSeconds(1), into: fx.Store);

        var result = SnapshotRunLog.Flush(fx.Store, Options(), SnapshotName, BootId, boot, lastFlushedThrough: null);

        // A provenance column that a table already has must not be added again. Both copies would
        // survive the export, the second one renamed, so this compares the whole column list.
        string[] provenance = ["SnapshotName", "BootId", "HostInstance", "PackageVersion", "FlushedAt"];
        (string Table, string Folder)[] tables =
        [
            ("SyncRuns", SnapshotRunLog.SyncRunsFolder),
            ("PublishRuns", SnapshotRunLog.PublishRunsFolder),
            ("CycleRuns", SnapshotRunLog.CycleRunsFolder),
            ("PumpRuns", SnapshotRunLog.PumpRunsFolder),
            ("FetchRuns", SnapshotRunLog.FetchRunsFolder),
        ];
        Assert.Equal(tables.Length, result.FilesWritten.Count);
        foreach (var (table, folder) in tables)
        {
            var file = Assert.Single(result.FilesWritten, f => f.Contains(folder));
            var tableColumns = TableColumns(fx.Store, table);
            string[] expected = [.. tableColumns, .. provenance.Where(column => !tableColumns.Contains(column))];
            Assert.Equal(expected, FileColumns(file));
        }
    }

    [Fact]
    public void AnOlderFileWithoutAColumn_ReadsBesideANewerOne_WithUnionByName()
    {
        // A file an earlier package version might have written: fewer columns, another boot id,
        // the same day folder. A reader unions by name and gets NULL where the column is missing.
        snapshot.Merge([("W1", "alpha", 1)]);
        Flush();
        var older = Path.Combine(DayFolder(SnapshotRunLog.SyncRunsFolder), "20260919110000000-01d0b007.parquet");
        snapshot.Store.Execute(
            $"""
            COPY (SELECT 'older-run' AS "RunId", "Source", "StartedAt" FROM meta.SyncRuns LIMIT 1)
            TO '{Sql(older)}' (FORMAT parquet)
            """);

        var glob = Glob(SnapshotRunLog.SyncRunsFolder);

        Assert.Equal(2L, Convert.ToInt64(Read($"SELECT count(*) FROM {glob}")));
        Assert.Equal(1L, Convert.ToInt64(Read($"SELECT count(*) FROM {glob} WHERE \"BootId\" IS NULL AND \"RunId\" = 'older-run'")));
        Assert.Equal(1L, Convert.ToInt64(Read($"SELECT count(*) FROM {glob} WHERE \"BootId\" = '{BootId}'")));
    }

    [Fact]
    public void ABootIdNeverLooksLikeAPublishedParquetName()
    {
        // Retention only ever deletes files shaped like the publisher's own. A run-log file in a
        // shared location must stay outside that shape, and the boot id is what keeps it there.
        var bootId = SnapshotRunLog.NewBootId(new DateTime(2026, 9, 19, 6, 15, 0, 123, DateTimeKind.Utc));

        Assert.StartsWith("20260919061500123-", bootId);
        Assert.Equal(26, bootId.Length);
        Assert.DoesNotMatch(SnapshotPublisher.PublishedParquetShape, $"{bootId}.parquet");
        Assert.NotEqual(bootId, SnapshotRunLog.NewBootId(new DateTime(2026, 9, 19, 6, 15, 0, 123, DateTimeKind.Utc)));
    }

    [Theory]
    [InlineData("meta.CycleRuns")]
    [InlineData("meta.PumpRuns")]
    [InlineData("meta.FetchRuns")]
    public void ARunTableAddedLater_IsAddedToAnExistingWriteDb_WithoutAVersionBump(string table)
    {
        // A write DB from before the table existed: opened, the table dropped, closed. Reopening
        // must add it back on the same schema version and never call for a rebuild.
        var path = Path.Combine(root, "existing.duckdb");
        Directory.CreateDirectory(root);
        using (var existing = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = path }))
            existing.Execute($"DROP TABLE {table}");

        using var reopened = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = path });

        Assert.Equal(0L, Convert.ToInt64(reopened.ExecuteScalar($"SELECT count(*) FROM {table}")));
        Assert.Equal(SnapshotStore.CurrentSchemaVersion,
            Convert.ToInt32(reopened.ExecuteScalar("SELECT max(\"SchemaVersion\") FROM meta.schema_info")));
    }

    [Fact]
    public void PumpRuns_AreCopiedToo_UnderTheirOwnFolder_WithoutAnErrorColumn()
    {
        // Two tables drained in one cycle, placed by hand the way the loop writes them.
        var cycleStart = boot.AddSeconds(1);
        InsertPump("cycle-1", "Widget", cycleStart, rowsRead: 12, drained: true, stopReason: "QueueEmpty");
        InsertPump("cycle-1", "Gadget", cycleStart.AddSeconds(2), rowsRead: 3, drained: false, stopReason: "BatchBound");

        var result = Flush();

        var file = Assert.Single(result.FilesWritten, f => f.Contains(SnapshotRunLog.PumpRunsFolder));
        Assert.Equal(Path.Combine(root, SnapshotRunLog.PumpRunsFolder, DayOf(cycleStart), $"{BootId}.parquet"), file);
        Assert.Equal(2, Count(file));
        // Ordered by start time; every pump fact read back as written; the provenance beside it.
        Assert.Equal("Widget", Read($"SELECT \"Table\" FROM read_parquet('{Sql(file)}') LIMIT 1"));
        Assert.Equal(12L, Convert.ToInt64(Read($"SELECT \"RowsRead\" FROM read_parquet('{Sql(file)}') WHERE \"Table\" = 'Widget'")));
        Assert.Equal("BatchBound", Read($"SELECT \"StopReason\" FROM read_parquet('{Sql(file)}') WHERE \"Table\" = 'Gadget'"));
        Assert.Equal(false, Read($"SELECT \"Drained\" FROM read_parquet('{Sql(file)}') WHERE \"Table\" = 'Gadget'"));
        Assert.Equal("cycle-1", Read($"SELECT DISTINCT \"CycleId\" FROM read_parquet('{Sql(file)}')"));
        Assert.Equal(BootId, Read($"SELECT DISTINCT \"BootId\" FROM read_parquet('{Sql(file)}')"));
        Assert.Equal(SnapshotName, Read($"SELECT DISTINCT \"SnapshotName\" FROM read_parquet('{Sql(file)}')"));
        // The pump table has no Error column to shorten, and the exporter must not invent one.
        Assert.Equal(0L, Convert.ToInt64(Read($"SELECT count(*) FROM parquet_schema('{Sql(file)}') WHERE name = 'Error'")));
        Assert.Equal(1L, Convert.ToInt64(Read($"SELECT count(*) FROM parquet_schema('{Sql(file)}') WHERE name = 'FlushedAt'")));
        // No sync run was placed, so no sync-runs file was written beside it.
        Assert.DoesNotContain(result.FilesWritten, f => f.Contains(SnapshotRunLog.SyncRunsFolder));
    }

    [Fact]
    public void FetchRuns_AreCopiedToo_UnderTheirOwnFolder_WithoutAnErrorColumn()
    {
        // Two reads placed by hand, the way the dispatcher writes them: one that fed a run, and
        // one whose drain threw, so it has no run id.
        var readStart = boot.AddSeconds(1);
        InsertFetch("run-1", "sql-a", readStart, rowsFetched: 7, took: TimeSpan.FromSeconds(30));
        InsertFetch(null, "sql-b", readStart.AddSeconds(2), rowsFetched: 2);

        var result = Flush();

        var file = Assert.Single(result.FilesWritten, f => f.Contains(SnapshotRunLog.FetchRunsFolder));
        Assert.Equal(Path.Combine(root, SnapshotRunLog.FetchRunsFolder, DayOf(readStart), $"{BootId}.parquet"), file);
        Assert.Equal(2, Count(file));
        // Ordered by start time; the read's facts read back as written; the provenance beside them.
        Assert.Equal("sql-a", Read($"SELECT \"Source\" FROM read_parquet('{Sql(file)}') LIMIT 1"));
        Assert.Equal(30_000L, Convert.ToInt64(Read(
            $"SELECT date_diff('millisecond', \"StartedAt\", \"FinishedAt\") FROM read_parquet('{Sql(file)}') WHERE \"Source\" = 'sql-a'")));
        Assert.Equal(7L, Convert.ToInt64(Read($"SELECT \"RowsFetched\" FROM read_parquet('{Sql(file)}') WHERE \"RunId\" = 'run-1'")));
        Assert.Equal(1L, Convert.ToInt64(Read($"SELECT count(*) FROM read_parquet('{Sql(file)}') WHERE \"Source\" = 'sql-b' AND \"RunId\" IS NULL")));
        Assert.Equal(BootId, Read($"SELECT DISTINCT \"BootId\" FROM read_parquet('{Sql(file)}')"));
        Assert.Equal(SnapshotName, Read($"SELECT DISTINCT \"SnapshotName\" FROM read_parquet('{Sql(file)}')"));
        // A failed read's error is on its Failed:Fetch run, so this table has no Error column.
        Assert.Equal(0L, Convert.ToInt64(Read($"SELECT count(*) FROM parquet_schema('{Sql(file)}') WHERE name = 'Error'")));
        Assert.DoesNotContain(result.FilesWritten, f => f.Contains(SnapshotRunLog.SyncRunsFolder));
    }

    [Fact]
    public void Prune_DeletesFlushedRowsOfEarlierDays_AndKeepsToday_TheUnflushed_AndTheNewestPerSource()
    {
        // A fixed calendar: "today" is the 12th, and the last flush that wrote started at 23:30 on
        // the 11th. Rows on the 9th, 10th and 11th are earlier days; rows on the 12th are today's.
        var today = new DateOnly(2026, 3, 12);
        var flushedThrough = new DateTime(2026, 3, 11, 23, 30, 0, DateTimeKind.Utc);
        var ninth = new DateTime(2026, 3, 9, 8, 0, 0, DateTimeKind.Utc);
        var tenth = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);
        var eleventh = new DateTime(2026, 3, 11, 8, 0, 0, DateTimeKind.Utc);
        var twelfth = new DateTime(2026, 3, 12, 1, 0, 0, DateTimeKind.Utc);

        InsertRun("a-older", ninth, source: "A");                 // flushed, not the newest for A: goes
        InsertRun("a-old", tenth, source: "A");                   // flushed, not the newest for A: goes
        InsertRun("a-newest", eleventh, source: "A");             // the newest for A: stays, whatever its age
        InsertRun("b-late", eleventh.AddHours(15), source: "B",   // finished after the watermark: not flushed yet, stays
            finishedAt: flushedThrough.AddMinutes(5));
        InsertRun("b-today", twelfth, source: "B");               // today: stays
        InsertRun("c-today", twelfth, source: "C");               // today: stays
        InsertCycle("cycle-old", tenth);
        InsertCycle("cycle-today", twelfth);
        InsertPublish("publish-old", tenth);
        InsertPublish("publish-today", twelfth);
        InsertPump("cycle-old", "Widget", tenth);
        InsertPump("cycle-today", "Widget", twelfth);
        InsertFetch("a-old", "A", tenth);
        InsertFetch("b-today", "B", twelfth);
        var newestBefore = snapshot.Store.ReadLatestRunPerSource().Select(run => $"{run.SourceKey} {run.StartedAt:O}").ToArray();

        var result = SnapshotRunLog.Prune(snapshot.Store, flushedThrough, today);

        Assert.Equal(today, result.Before);
        Assert.Equal(2L, result.RowsDeleted["meta.SyncRuns"]);
        Assert.Equal(1L, result.RowsDeleted["meta.CycleRuns"]);
        Assert.Equal(1L, result.RowsDeleted["meta.PublishRuns"]);
        Assert.Equal(1L, result.RowsDeleted["meta.PumpRuns"]);
        Assert.Equal(1L, result.RowsDeleted["meta.FetchRuns"]);
        Assert.Equal(6, result.Total);
        Assert.Equal("a-newest,b-late,b-today,c-today",
            snapshot.Scalar<string>("SELECT string_agg(\"RunId\", ',' ORDER BY \"RunId\") FROM meta.SyncRuns"));
        Assert.Equal("cycle-today", snapshot.Scalar<string>("SELECT string_agg(\"CycleId\", ',') FROM meta.CycleRuns"));
        Assert.Equal("publish-today", snapshot.Scalar<string>("SELECT string_agg(\"PublishId\", ',') FROM meta.PublishRuns"));
        Assert.Equal("cycle-today", snapshot.Scalar<string>("SELECT string_agg(\"CycleId\", ',') FROM meta.PumpRuns"));
        // A read keeps nothing back for the manifest: its row goes with its day, like the cycle's.
        Assert.Equal("b-today", snapshot.Scalar<string>("SELECT string_agg(\"RunId\", ',') FROM meta.FetchRuns"));
        // What the publisher reads into the manifest is exactly what it read before.
        Assert.Equal(newestBefore, snapshot.Store.ReadLatestRunPerSource().Select(run => $"{run.SourceKey} {run.StartedAt:O}").ToArray());

        // Pruning again finds nothing: it is idempotent, and today's rows are never eligible.
        Assert.Equal(0, SnapshotRunLog.Prune(snapshot.Store, flushedThrough, today).Total);
    }

    [Fact]
    public void Prune_NeverTouchesTheDestination_AndDeletesNothingAFlushHasNotCopied()
    {
        // Rows on an earlier day, flushed under a fixed boot on that day, then pruned.
        var fixedBoot = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
        InsertRun("old-1", fixedBoot.AddMinutes(1), source: "A");
        InsertRun("old-2", fixedBoot.AddMinutes(2), source: "A");
        var flush = SnapshotRunLog.Flush(snapshot.Store, Options(), SnapshotName, BootId, fixedBoot, lastFlushedThrough: null);
        var file = Assert.Single(flush.FilesWritten);
        var bytes = File.ReadAllBytes(file);
        var lastWrite = File.GetLastWriteTimeUtc(file);
        var later = new DateOnly(2026, 3, 12);

        // A watermark from before the rows means no flush has copied them: nothing goes.
        Assert.Equal(0, SnapshotRunLog.Prune(snapshot.Store, fixedBoot, later).Total);

        // The real watermark: the older row goes, the newest for its source stays.
        var pruned = SnapshotRunLog.Prune(snapshot.Store, flush.FlushedThrough!.Value, later);

        Assert.Equal(1, pruned.Total);
        Assert.Equal("old-2", snapshot.Scalar<string>("SELECT \"RunId\" FROM meta.SyncRuns"));
        // The day file is the record now and holds both rows, byte for byte as written.
        Assert.Equal(bytes, File.ReadAllBytes(file));
        Assert.Equal(lastWrite, File.GetLastWriteTimeUtc(file));
        Assert.Equal(2, Count(file));
        // A later flush has nothing new for that day, so it never rewrites the file from the thinner table.
        Assert.True(SnapshotRunLog.Flush(snapshot.Store, Options(), SnapshotName, BootId, fixedBoot, flush.FlushedThrough).Skipped);
    }

    private static string DayOf(DateTime value) =>
        SnapshotRunLog.PartitionPrefix + value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// The columns stored in one file, in file order. Hive detection is off, because a single file
    /// under a <c>date=</c> folder would otherwise gain a <c>date</c> column it does not store.
    /// </summary>
    private static string[] FileColumns(string parquet)
    {
        using var reader = new DuckDBConnection("Data Source=:memory:");
        reader.Open();
        return Names(reader, $"DESCRIBE SELECT * FROM read_parquet('{Sql(parquet)}', hive_partitioning = false)");
    }

    /// <summary>The columns of one run table in the write DB, in table order.</summary>
    private static string[] TableColumns(SnapshotStore store, string table) =>
        Names(store.Connection,
            $"""
            SELECT column_name FROM information_schema.columns
            WHERE table_schema = 'meta' AND lower(table_name) = lower('{table}')
            ORDER BY ordinal_position
            """);

    /// <summary>The first column of every row, as text.</summary>
    private static string[] Names(DuckDBConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
            names.Add(reader.GetString(0));
        return [.. names];
    }

    /// <summary>A run row placed by hand, the way the merge writes one but with a chosen start time.</summary>
    private void InsertRun(string runId, DateTime startedAt, string? error = null, string source = "test-source", DateTime? finishedAt = null) =>
        snapshot.Store.Execute(
            """
            INSERT INTO meta.SyncRuns
            ("RunId", "Source", "TargetTable", "StartedAt", "FinishedAt", "Status", "Error")
            VALUES (?, ?, 'Widget', ?, ?, ?, ?)
            """,
            runId, source, startedAt, finishedAt ?? startedAt.AddSeconds(1), error is null ? "Succeeded" : "Failed:Exception", error);

    /// <summary>A cycle row placed by hand, with a chosen start time, in this suite's write DB unless another is named.</summary>
    private void InsertCycle(string cycleId, DateTime startedAt, SnapshotStore? into = null) =>
        (into ?? snapshot.Store).Execute(
            "INSERT INTO meta.CycleRuns (\"CycleId\", \"StartedAt\", \"FinishedAt\", \"Outcome\") VALUES (?, ?, ?, 'Ran')",
            cycleId, startedAt, startedAt.AddSeconds(1));

    /// <summary>A publish row placed by hand, with a chosen start time.</summary>
    private void InsertPublish(string publishId, DateTime startedAt) =>
        snapshot.Store.Execute(
            "INSERT INTO meta.PublishRuns (\"PublishId\", \"SnapshotName\", \"StartedAt\", \"FinishedAt\", \"Status\") VALUES (?, ?, ?, ?, 'Published')",
            publishId, SnapshotName, startedAt, startedAt.AddSeconds(1));

    /// <summary>A pump row placed by hand, the way the loop writes one but with a chosen start time, in this suite's write DB unless another is named.</summary>
    private void InsertPump(
        string cycleId, string table, DateTime startedAt, long rowsRead = 0, bool drained = true, string stopReason = "QueueEmpty",
        SnapshotStore? into = null) =>
        (into ?? snapshot.Store).Execute(
            """
            INSERT INTO meta.PumpRuns
            ("CycleId", "Table", "StartedAt", "FinishedAt", "RowsRead", "Upserted", "Drained", "StopReason")
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            """,
            cycleId, table, startedAt, startedAt.AddSeconds(1), rowsRead, rowsRead, drained, stopReason);

    /// <summary>A read's row placed by hand, the way the dispatcher writes one but with a chosen start time, in this suite's write DB unless another is named.</summary>
    private void InsertFetch(
        string? runId, string source, DateTime startedAt, long rowsFetched = 1, TimeSpan? took = null, SnapshotStore? into = null) =>
        (into ?? snapshot.Store).Execute(
            """
            INSERT INTO meta.FetchRuns ("RunId", "Source", "StartedAt", "FinishedAt", "RowsFetched", "Failed")
            VALUES (?, ?, ?, ?, ?, false)
            """,
            runId, source, startedAt, startedAt + (took ?? TimeSpan.FromSeconds(1)), rowsFetched);
}
