using System.Globalization;
using DuckDB.NET.Data;
using ShiftSoftware.ADP.SyncAgent;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Extensions;
using ShiftSoftware.ADP.SyncAgent.Services;

namespace ADP.SyncAgent.Tests;

public class VehicleCsv : SyncCsvBase
{
    public string? Id { get; set; }
    public string? VIN { get; set; }
    public string? LaborCode { get; set; }
    public string? Price { get; set; }
}

/// <summary>
/// The CSV diff queue's failure handling end to end: a batch the destination cannot store leaves a
/// reason on every row and a loud log when rows are given up on; given-up rows come back after a rest,
/// on request, or the moment the destination reports it fixed its table; and rows whose CSV line has
/// gone are not kept around as Dead forever.
/// </summary>
public sealed class DuckDbCsvSyncDataSourceQueueTests : IDisposable
{
    private readonly TempDirectory temp = new();

    public void Dispose() => temp.Dispose();

    private const string Header = "Id,VIN,LaborCode,Price";
    private const string PoisonPrice = "79228162514264337593543950335"; // decimal.MaxValue: rejected by DECIMAL(38,10)

    private static readonly string[] GoodRows = ["A-1,A,YHG14A,10", "B-1,B,YHG14A,20", "C-1,C,YHG14A,30"];
    private static readonly string[] PoisonedRows = ["A-1,A,YHG14A,10", $"B-1,B,YHG14A,{PoisonPrice}", "C-1,C,YHG14A,30"];

    [Fact]
    public async Task A_batch_the_destination_rejects_leaves_a_reason_on_every_row_and_a_loud_log_when_rows_go_Dead()
    {
        var csv = WriteCsv(PoisonedRows);
        var (result, logger, source) = await RunAsync(csv, maxAttempts: 1);

        Assert.True(result); // RetryAndContinueAfterLastRetry: the run goes on, the batch is on record
        using var diff = Db.Open(temp.File("diff.duckdb"));
        Assert.Equal(3L, Db.Scalar(diff, "SELECT count(*) FROM VehicleCsv_changes WHERE _Status = 3"));
        Assert.Equal(0L, Db.Scalar(diff, "SELECT count(*) FROM VehicleCsv_changes WHERE _LastError IS NULL"));
        Assert.Contains("aborted at item 2 of 3", (string)Db.Scalar(diff, "SELECT min(_LastError) FROM VehicleCsv_changes")!);

        Assert.Contains(logger.Errors, e => e.Contains("3 change(s) reached MaxAttempts and are now Dead"));
        Assert.Contains(logger.Warnings, w => w.Contains("Queue after run: 0 pending, 0 in flight, 3 Dead"));
        Assert.Equal(3, source.LastRunQueueStats!.Dead);

        using var dest = Db.Open(temp.File("dest.duckdb"));
        Assert.Equal(0L, Db.Scalar(dest, "SELECT count(*) FROM VehicleRow"));
    }

    [Fact]
    public async Task Dead_rows_get_fresh_attempts_after_resting_for_DeadRetryAfter()
    {
        var csv = WriteCsv(PoisonedRows);
        await RunAsync(csv, maxAttempts: 1);

        // Still poisoned: the retry fails again, but the rest-then-retry cycle is visible.
        var (_, retried, _) = await RunAsync(csv, maxAttempts: 1, deadRetryAfter: TimeSpan.Zero);
        Assert.Contains(retried.Warnings, w => w.Contains("Re-queued 3 Dead change(s) that had rested longer than DeadRetryAfter"));

        var (_, untouched, _) = await RunAsync(csv, maxAttempts: 1, deadRetryAfter: null);
        Assert.DoesNotContain(untouched.Warnings, w => w.Contains("Re-queued"));
    }

    [Fact]
    public async Task RequeueDeadChangesAsync_outside_a_run_gives_every_Dead_row_fresh_attempts()
    {
        var csv = WriteCsv(PoisonedRows);
        var (_, _, source) = await RunAsync(csv, maxAttempts: 1);

        var revived = await source.RequeueDeadChangesAsync("operator request");

        Assert.Equal(3, revived);
        using var diff = Db.Open(temp.File("diff.duckdb"));
        Assert.Equal(3L, Db.Scalar(diff, "SELECT count(*) FROM VehicleCsv_changes WHERE _Status = 0 AND _AttemptCount = 0"));
    }

    [Fact]
    public async Task A_Dead_row_whose_CSV_line_is_gone_is_dropped_instead_of_kept_forever()
    {
        var csv = WriteCsv(PoisonedRows);
        await RunAsync(csv, maxAttempts: 1);

        File.WriteAllLines(csv, [Header, "A-1,A,YHG14A,10", "C-1,C,YHG14A,30"]);
        var (result, _, _) = await RunAsync(csv, maxAttempts: 1, deadRetryAfter: null);

        Assert.True(result);
        using var diff = Db.Open(temp.File("diff.duckdb"));
        // B's Dead row went with its line; A and C were never promoted so they are Dead still.
        Assert.Equal(0L, Db.Scalar(diff, "SELECT count(*) FROM VehicleCsv_changes WHERE Id = 'B-1'"));
        Assert.Equal(2L, Db.Scalar(diff, "SELECT count(*) FROM VehicleCsv_changes WHERE _Status = 3"));
    }

    [Fact]
    public async Task Rows_that_died_while_the_table_was_behind_are_replayed_in_the_run_that_fixes_it()
    {
        // 1. A healthy sync: three rows promoted, queue empty.
        var csv = WriteCsv(GoodRows);
        Assert.True((await RunAsync(csv)).Result);

        // 2. The incident, as the OLD destination left it: rows the queue gave up on while the live table
        //    lagged the model. Mark every promoted row as a Dead Update, and put the live table back to the
        //    legacy shape (no Labors/Intermediary/Distributor) with those rows missing.
        using (var diff = Db.Open(temp.File("diff.duckdb")))
        {
            Db.Exec(diff, "INSERT INTO VehicleCsv_changes (Id, VIN, LaborCode, Price, _PrimaryKey, _RowHash, _LoadedAt, _ChangeType, _DetectedAt, _AttemptCount, _LastAttemptAt, _LastError, _Status) " +
                          "SELECT Id, VIN, LaborCode, Price, _PrimaryKey, _RowHash, _LoadedAt, 1, now(), 5, now(), 'died while the table was behind', 3 FROM VehicleCsv_source");
        }
        using (var dest = Db.Open(temp.File("dest.duckdb")))
        {
            Db.Exec(dest, "DROP TABLE VehicleRow");
            Db.Exec(dest, "CREATE TABLE VehicleRow (id VARCHAR, VIN VARCHAR, LaborCode1 VARCHAR, LaborHour1 DOUBLE, Price DECIMAL(38,10), InvoiceDate TIMESTAMP, PRIMARY KEY (id))");
        }

        // 3. The run that fixes it: the CSV is unchanged (the source has nothing new), the destination adds
        //    the missing columns, the host's SchemaChanged handler re-queues the Dead rows, and — because
        //    the table changed — the actions run now rather than next cycle.
        var (result, logger, source) = await RunAsync(csv, deadRetryAfter: null, wireSchemaChangedToRequeue: true);

        Assert.True(result);
        Assert.Equal("destination schema changed: VehicleRow: added [Labors, Intermediary, Distributor]", source.LastRunQueueStats!.RequeueReason);
        Assert.Equal(0, source.LastRunQueueStats.Dead);

        using var after = Db.Open(temp.File("dest.duckdb"));
        Assert.Equal(3L, Db.Scalar(after, "SELECT count(*) FROM VehicleRow"));
        Assert.Equal("YHG14A", Db.Scalar(after, "SELECT json_extract_string(Labors, '$[0].LaborCode') FROM VehicleRow WHERE id = 'B-1'"));
        Assert.Contains(logger.Warnings, w => w.Contains("missing model column Labors"));
    }

    [Fact]
    public async Task With_nothing_new_and_nothing_changed_the_run_is_skipped()
    {
        var csv = WriteCsv(GoodRows);
        await RunAsync(csv);

        var (result, logger, _) = await RunAsync(csv);

        Assert.True(result);
        Assert.DoesNotContain(logger.Lines, l => l.Message.Contains("Batch started"));
    }

    // ===== harness =====

    private string WriteCsv(IEnumerable<string> rows)
    {
        var path = temp.File("vehicles.csv");
        File.WriteAllLines(path, [Header, .. rows]);
        return path;
    }

    private async Task<(bool Result, CapturingLogger Logger, DuckDbCsvSyncDataSource<VehicleCsv, VehicleRow> Source)> RunAsync(
        string csvPath,
        int maxAttempts = 5,
        TimeSpan? deadRetryAfter = null,
        bool wireSchemaChangedToRequeue = false)
    {
        var logger = new CapturingLogger();
        var engine = new SyncEngine<VehicleCsv, VehicleRow>();
        engine.Configure([SyncActionType.Delete, SyncActionType.Update, SyncActionType.Add], batchSize: 100, maxRetryCount: 1, operationTimeoutInSeconds: 60, defaultRetryAction: RetryAction.RetryAndContinueAfterLastRetry);

        var source = new DuckDbCsvSyncDataSource<VehicleCsv, VehicleRow>();
        source.SetSyncService(engine);
        source.Configure(new DuckDbCsvSyncDataSourceConfigurations<VehicleCsv, VehicleRow>
        {
            DuckDbFilePath = temp.File("diff.duckdb"),
            CsvFilePath = csvPath,
            HasHeaderRecord = true,
            KeyColumns = x => x.Id!,
            DestinationKey = x => x.id,
            MaxAttempts = maxAttempts,
            DeadRetryAfter = deadRetryAfter,
        });

        engine.SetupMapping((rows, _) => new ValueTask<IEnumerable<VehicleRow?>?>(rows!.Where(x => x is not null).Select(x => new VehicleRow
        {
            id = x!.Id!,
            VIN = x.VIN,
            LaborCode1 = x.LaborCode,
            LaborHour1 = 0.3,
            Labors = x.LaborCode is null ? null : [new LaborLine { LaborCode = x.LaborCode, LaborHour = 0.3 }],
            Price = x.Price is null ? null : decimal.Parse(x.Price, CultureInfo.InvariantCulture),
            InvoiceDate = new DateTime(2026, 7, 15),
            Intermediary = new Intermediary { InvoiceNumber = "INV-1" },
            Distributor = "TIQ",
        }).Cast<VehicleRow?>()));

        using var conn = Db.Open(temp.File("dest.duckdb"));
        new DuckDBSyncDataDestination<VehicleCsv, VehicleRow, DuckDBConnection>(conn)
            .SetSyncService(engine)
            .Configure(new DuckDBSyncDataDestinationConfigurations<VehicleCsv, VehicleRow>
            {
                TableName = "VehicleRow",
                PrimaryKey = x => x.id,
                SchemaChanged = wireSchemaChangedToRequeue
                    ? async change => await source.RequeueDeadChangesAsync($"destination schema changed: {change}")
                    : null,
            });

        engine.AddLogger(logger);

        try
        {
            return (await engine.RunAsync(), logger, source);
        }
        finally
        {
            await engine.Reset();
            await source.DisposeAsync();
        }
    }
}
