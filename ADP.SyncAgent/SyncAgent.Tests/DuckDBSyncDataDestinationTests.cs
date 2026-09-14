using DuckDB.NET.Data;
using ShiftSoftware.ADP.SyncAgent;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Extensions;
using ShiftSoftware.ADP.SyncAgent.Services;

namespace ADP.SyncAgent.Tests;

/// <summary>
/// The DuckDB destination against the shapes that lost data in production: a live table behind the
/// model (trailing and mid-model columns), a value the engine rejects mid-batch, and a source whose
/// own Preparing verdict must not be masked.
/// </summary>
public sealed class DuckDBSyncDataDestinationTests : IDisposable
{
    private readonly TempDirectory temp = new();

    public void Dispose() => temp.Dispose();

    // The live table as the OLD destination would have created it before Labors, Intermediary and
    // Distributor existed on the model.
    private const string LegacyTableDdl =
        "CREATE TABLE VehicleRow (id VARCHAR, VIN VARCHAR, LaborCode1 VARCHAR, LaborHour1 DOUBLE, Price DECIMAL(38,10), InvoiceDate TIMESTAMP, PRIMARY KEY (id))";

    private static VehicleRow Row(string id, string? laborCode = "YHG14A", decimal? price = 10m) => new()
    {
        id = id,
        VIN = id.Split('-')[0],
        Labors = laborCode is null ? null : [new LaborLine { LaborCode = laborCode, LaborHour = 0.3 }],
        LaborCode1 = laborCode,
        LaborHour1 = 0.3,
        Price = price,
        InvoiceDate = new DateTime(2026, 7, 15),
        Intermediary = new Intermediary { InvoiceNumber = "INV-1" },
        Distributor = "TIQ",
    };

    [Fact]
    public async Task A_new_table_is_created_from_the_model_and_rows_land()
    {
        var db = temp.File("dest.duckdb");
        var (result, logger) = await RunAsync(db, [Row("A-1"), Row("B-1")]);

        Assert.True(result);
        using var conn = Db.Open(db);
        Assert.Equal(["id", "VIN", "Labors", "LaborCode1", "LaborHour1", "Price", "InvoiceDate", "Intermediary", "Distributor"], Db.Columns(conn, "VehicleRow"));
        Assert.Equal(2L, Db.Scalar(conn, "SELECT count(*) FROM VehicleRow"));
        Assert.Equal("YHG14A", Db.Scalar(conn, "SELECT json_extract_string(Labors, '$[0].LaborCode') FROM VehicleRow WHERE id = 'A-1'"));
        Assert.Empty(logger.Errors);
    }

    [Fact]
    public async Task A_table_behind_the_model_gains_the_missing_columns_and_values_land_by_name()
    {
        var db = temp.File("dest.duckdb");
        using (var conn = Db.Open(db))
        {
            Db.Exec(conn, LegacyTableDdl);
            Db.Exec(conn, "INSERT INTO VehicleRow (id, VIN, LaborCode1) VALUES ('A-1', 'A', 'OLD')");
        }

        DuckDBSchemaChange? change = null;
        var (result, logger) = await RunAsync(db, [Row("A-1"), Row("B-1")], configure: c => c.SchemaChanged = x => { change = x; return Task.CompletedTask; });

        Assert.True(result);
        using var after = Db.Open(db);

        // Added at the end, wherever they sit in the model — order no longer matters to the write.
        Assert.Equal(["id", "VIN", "LaborCode1", "LaborHour1", "Price", "InvoiceDate", "Labors", "Intermediary", "Distributor"], Db.Columns(after, "VehicleRow"));

        // Every value in its own column: the mid-model JSON did not shift into LaborCode1 (the SSC incident),
        // and the trailing columns did not abort the batch (the VehicleEntry incident).
        Assert.Equal("YHG14A", Db.Scalar(after, "SELECT LaborCode1 FROM VehicleRow WHERE id = 'A-1'"));
        Assert.Equal(0.3, Db.Scalar(after, "SELECT LaborHour1 FROM VehicleRow WHERE id = 'A-1'"));
        Assert.Equal("YHG14A", Db.Scalar(after, "SELECT json_extract_string(Labors, '$[0].LaborCode') FROM VehicleRow WHERE id = 'A-1'"));
        Assert.Equal("INV-1", Db.Scalar(after, "SELECT json_extract_string(Intermediary, '$.InvoiceNumber') FROM VehicleRow WHERE id = 'B-1'"));
        Assert.Equal("TIQ", Db.Scalar(after, "SELECT Distributor FROM VehicleRow WHERE id = 'B-1'"));
        Assert.Equal(2L, Db.Scalar(after, "SELECT count(*) FROM VehicleRow"));

        Assert.NotNull(change);
        Assert.Equal(["Labors", "Intermediary", "Distributor"], change!.AddedColumns);
        Assert.Contains(logger.Warnings, w => w.Contains("missing model column Labors"));
    }

    [Fact]
    public async Task A_matching_table_raises_no_schema_change()
    {
        var db = temp.File("dest.duckdb");
        await RunAsync(db, [Row("A-1")]);

        var fired = false;
        var (result, logger) = await RunAsync(db, [Row("A-1")], configure: c => c.SchemaChanged = _ => { fired = true; return Task.CompletedTask; });

        Assert.True(result);
        Assert.False(fired);
        Assert.Empty(logger.Warnings.Where(w => w.Contains("missing model column")));
    }

    [Fact]
    public async Task A_column_the_model_no_longer_has_is_kept_and_its_values_survive_an_upsert()
    {
        var db = temp.File("dest.duckdb");
        using (var conn = Db.Open(db))
        {
            Db.Exec(conn, "CREATE TABLE VehicleRow (id VARCHAR, VIN VARCHAR, Labors JSON, LaborCode1 VARCHAR, LaborHour1 DOUBLE, Price DECIMAL(38,10), InvoiceDate TIMESTAMP, Intermediary JSON, Distributor VARCHAR, LegacyNote VARCHAR, PRIMARY KEY (id))");
            Db.Exec(conn, "INSERT INTO VehicleRow (id, VIN, LegacyNote) VALUES ('A-1', 'A', 'keep me')");
        }

        var (result, logger) = await RunAsync(db, [Row("A-1")]);

        Assert.True(result);
        using var after = Db.Open(db);
        Assert.Equal("keep me", Db.Scalar(after, "SELECT LegacyNote FROM VehicleRow WHERE id = 'A-1'"));
        Assert.Equal("YHG14A", Db.Scalar(after, "SELECT LaborCode1 FROM VehicleRow WHERE id = 'A-1'"));
        Assert.Contains(logger.Warnings, w => w.Contains("no longer declares: [LegacyNote]"));
    }

    [Fact]
    public async Task A_type_difference_fails_preparing_and_names_the_column()
    {
        var db = temp.File("dest.duckdb");
        using (var conn = Db.Open(db))
            Db.Exec(conn, "CREATE TABLE VehicleRow (id VARCHAR, VIN VARCHAR, Labors JSON, LaborCode1 VARCHAR, LaborHour1 VARCHAR, Price DECIMAL(38,10), InvoiceDate TIMESTAMP, Intermediary JSON, Distributor VARCHAR, PRIMARY KEY (id))");

        var (result, logger) = await RunAsync(db, [Row("A-1")]);

        Assert.False(result);
        Assert.Contains(logger.Errors, e => e.Contains("LaborHour1: table VARCHAR, model DOUBLE"));
        using var after = Db.Open(db);
        Assert.Equal(0L, Db.Scalar(after, "SELECT count(*) FROM VehicleRow"));
    }

    [Fact]
    public async Task A_different_primary_key_fails_preparing()
    {
        var db = temp.File("dest.duckdb");
        using (var conn = Db.Open(db))
            Db.Exec(conn, "CREATE TABLE VehicleRow (id VARCHAR, VIN VARCHAR, Labors JSON, LaborCode1 VARCHAR, LaborHour1 DOUBLE, Price DECIMAL(38,10), InvoiceDate TIMESTAMP, Intermediary JSON, Distributor VARCHAR, PRIMARY KEY (VIN))");

        var (result, logger) = await RunAsync(db, [Row("A-1")]);

        Assert.False(result);
        Assert.Contains(logger.Errors, e => e.Contains("Table primary key: [VIN]; model primary key: [id]"));
    }

    [Fact]
    public async Task A_value_the_engine_rejects_aborts_the_batch_with_nothing_written_and_a_real_reason()
    {
        var db = temp.File("dest.duckdb");
        await RunAsync(db, [Row("A-1")]);

        // decimal.MaxValue has 29 integer digits: it does not fit DECIMAL(38,10).
        var batch = new[] { Row("A-1", price: 99m), Row("B-1", price: decimal.MaxValue), Row("C-1") };
        SyncBatchCompleteRetryInput<VehicleRow, VehicleRow>? completed = null;

        var (result, logger) = await RunAsync(db, batch, maxRetryCount: 1, onBatchCompleted: x => completed = x);

        // RetryAndContinueAfterLastRetry moves on once retries are spent, so the RUN still reports true —
        // that is the engine's contract. What changed: the batch is on record as failed, with its cause.
        Assert.True(result);
        using var after = Db.Open(db);
        // Neither the good rows before the bad one nor the ones after it were written.
        Assert.Equal(1L, Db.Scalar(after, "SELECT count(*) FROM VehicleRow"));
        Assert.Equal(10m, Db.Scalar(after, "SELECT Price FROM VehicleRow WHERE id = 'A-1'"));

        Assert.NotNull(completed);
        Assert.NotNull(completed!.Exception);
        Assert.Contains("aborted at item 2 of 3", completed.Exception!.Message);
        Assert.Equal(3, completed.StoreDataResult!.FailedItems!.Count());
        Assert.Empty(completed.StoreDataResult.SucceededItems!);
        Assert.Contains(logger.Errors, e => e.Contains("aborted at item 2 of 3"));
    }

    [Fact]
    public async Task With_ContinueAfterFail_bad_rows_are_skipped_and_the_rest_land()
    {
        var db = temp.File("dest.duckdb");
        var batch = new[] { Row("A-1"), Row("B-1", price: decimal.MaxValue), Row("C-1") };
        SyncBatchCompleteRetryInput<VehicleRow, VehicleRow>? completed = null;

        var (result, _) = await RunAsync(db, batch, configure: c => c.ContinueAfterFail = true, onBatchCompleted: x => completed = x);

        Assert.True(result);
        using var after = Db.Open(db);
        Assert.Equal(2L, Db.Scalar(after, "SELECT count(*) FROM VehicleRow"));
        Assert.Equal(SyncStoreDataResultType.Partial, completed!.StoreDataResult!.ResultType);
        Assert.Equal(["B-1"], completed.StoreDataResult.FailedItems!.Select(x => x!.id));
    }

    [Fact]
    public async Task Rows_are_deleted_by_key()
    {
        var db = temp.File("dest.duckdb");
        await RunAsync(db, [Row("A-1"), Row("B-1")]);

        var (result, _) = await RunAsync(db, [Row("A-1")], action: SyncActionType.Delete);

        Assert.True(result);
        using var after = Db.Open(db);
        Assert.Equal(["B-1"], ReadIds(after));
    }

    [Fact]
    public async Task A_skipped_source_stays_skipped_when_the_table_needed_nothing()
    {
        var db = temp.File("dest.duckdb");
        await RunAsync(db, [Row("A-1")]);

        var batches = 0;
        var (result, _) = await RunAsync(db, [Row("B-1")],
            sourcePreparing: _ => new ValueTask<SyncPreparingResponseAction>(SyncPreparingResponseAction.Skiped),
            onBatchCompleted: _ => batches++);

        Assert.True(result);
        Assert.Equal(0, batches);
        using var after = Db.Open(db);
        Assert.Equal(["A-1"], ReadIds(after));
    }

    [Fact]
    public async Task A_skipped_source_still_gets_its_table_reconciled_and_the_actions_run_when_it_changed()
    {
        // The source decided "nothing to do" before the table was fixed; a SchemaChanged handler may have
        // re-queued rows in between, so the actions must run now rather than a cycle later.
        var db = temp.File("dest.duckdb");
        using (var conn = Db.Open(db))
            Db.Exec(conn, LegacyTableDdl);

        var batches = 0;
        var (result, _) = await RunAsync(db, [Row("A-1")],
            sourcePreparing: _ => new ValueTask<SyncPreparingResponseAction>(SyncPreparingResponseAction.Skiped),
            onBatchCompleted: _ => batches++);

        Assert.True(result);
        Assert.Equal(1, batches);
        using var after = Db.Open(db);
        Assert.Contains("Labors", Db.Columns(after, "VehicleRow"));
        Assert.Equal(["A-1"], ReadIds(after));
    }

    [Fact]
    public async Task A_failed_source_is_not_masked_by_a_healthy_destination()
    {
        var db = temp.File("dest.duckdb");

        var (result, _) = await RunAsync(db, [Row("A-1")],
            sourcePreparing: _ => new ValueTask<SyncPreparingResponseAction>(SyncPreparingResponseAction.Failed));

        Assert.False(result);
    }

    // ===== harness =====

    private static async Task<(bool Result, CapturingLogger Logger)> RunAsync(
        string dbPath,
        IReadOnlyList<VehicleRow> items,
        SyncActionType action = SyncActionType.Add,
        long maxRetryCount = 0,
        Action<DuckDBSyncDataDestinationConfigurations<VehicleRow, VehicleRow>>? configure = null,
        Func<SyncFunctionInput, ValueTask<SyncPreparingResponseAction>>? sourcePreparing = null,
        Action<SyncBatchCompleteRetryInput<VehicleRow, VehicleRow>>? onBatchCompleted = null)
    {
        using var conn = Db.Open(dbPath);
        var logger = new CapturingLogger();

        var engine = new SyncEngine<VehicleRow, VehicleRow>();
        engine.Configure([action], batchSize: 100, maxRetryCount: maxRetryCount, operationTimeoutInSeconds: 60, defaultRetryAction: RetryAction.RetryAndContinueAfterLastRetry);

        // One batch, then "no more" — and, like the real sources, the same items again on a retry.
        var served = false;
        engine.SetupGetSourceBatchItems(x =>
        {
            if (x.Input.Status.CurrentRetryCount > 0 && x.Input.PreviousItems is not null)
                return new ValueTask<IEnumerable<VehicleRow?>?>(x.Input.PreviousItems);
            if (served) return new ValueTask<IEnumerable<VehicleRow?>?>([]);
            served = true;
            return new ValueTask<IEnumerable<VehicleRow?>?>(items);
        });
        engine.SetupMapping((rows, _) => new ValueTask<IEnumerable<VehicleRow?>?>(rows));

        if (sourcePreparing is not null)
            engine.SetupPreparing(sourcePreparing);

        if (onBatchCompleted is not null)
            engine.SetupBatchCompleted(x => { onBatchCompleted(x.Input); return new ValueTask<bool>(true); });

        var configuration = new DuckDBSyncDataDestinationConfigurations<VehicleRow, VehicleRow>
        {
            TableName = "VehicleRow",
            PrimaryKey = x => x.id,
        };
        configure?.Invoke(configuration);

        new DuckDBSyncDataDestination<VehicleRow, VehicleRow, DuckDBConnection>(conn)
            .SetSyncService(engine)
            .Configure(configuration);

        engine.AddLogger(logger);

        try
        {
            return (await engine.RunAsync(), logger);
        }
        finally
        {
            await engine.Reset();
        }
    }

    private static List<string> ReadIds(DuckDBConnection conn)
    {
        var ids = new List<string>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id FROM VehicleRow ORDER BY id";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) ids.Add(reader.GetString(0));
        return ids;
    }
}
