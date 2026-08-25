using System.Data;
using System.Data.Common;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

public class SqlViewSnapshotIngestorTests : IDisposable
{
    private readonly TestSnapshot snapshot = new();

    private static DataTableReader Reader(params (string? Key, string? Code, int? Quantity)[] rows)
    {
        var table = new DataTable();
        table.Columns.Add("LINEKEY", typeof(string));
        table.Columns.Add("Code", typeof(string));
        table.Columns.Add("Quantity", typeof(int));

        foreach (var row in rows)
            table.Rows.Add(row.Key ?? (object)DBNull.Value, row.Code ?? (object)DBNull.Value,
                row.Quantity ?? (object)DBNull.Value);

        return table.CreateDataReader();
    }

    private SnapshotMergeResult Ingest(DataTableReader reader) =>
        SqlViewSnapshotIngestor.Ingest(snapshot.Store, reader, new SqlViewSnapshotIngestorOptions
        {
            Table = snapshot.Table,
            SelectSql = "(unused — reader supplied directly)",
            PrimaryKeyColumn = "LINEKEY",
            MergeOptions = new SnapshotMergeOptions { Source = "dms-view", DeletesEnabled = true },
        });

    [Fact]
    public void FullPull_LandsRows_WithUniformHashes_AndIsIdempotent()
    {
        var first = Ingest(Reader(("K1", "alpha", 1), ("K2", "beta", 2)));

        Assert.True(first.Succeeded);
        Assert.Equal(2, first.RowsInserted);
        Assert.Equal(2, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\" WHERE \"_RowHash\" IS NOT NULL"));

        // Identical re-pull: hash diff means nothing is touched.
        var second = Ingest(Reader(("K1", "alpha", 1), ("K2", "beta", 2)));

        Assert.True(second.Succeeded);
        Assert.Equal(0, second.RowsInserted);
        Assert.Equal(0, second.RowsUpdated);
        Assert.Equal(0, second.RowsTombstoned);
    }

    [Fact]
    public void ChangesAndDisappearances_BecomeUpdatesAndTombstones()
    {
        Ingest(Reader(("K1", "alpha", 1), ("K2", "beta", 2)));

        var result = Ingest(Reader(("K1", "alpha", 99)));   // K1 changed, K2 vanished

        Assert.Equal(1, result.RowsUpdated);
        Assert.Equal(1, result.RowsTombstoned);
        Assert.Equal(99, snapshot.Scalar<int>("SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", "K1"));
        Assert.Equal(true, snapshot.ScalarOrNull("SELECT \"_Deleted\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = ?", "K2"));
    }

    [Fact]
    public void KeysAreTrimmed_TheNcharTrap()
    {
        // 1C nchar right-pads; a Cosmos id with trailing whitespace can be written but never
        // read or deleted by id. The ingestor trims key text before it becomes identity.
        Ingest(Reader(("K1   ", "alpha", 1)));

        Assert.Equal("K1", snapshot.ScalarOrNull("SELECT \"_PrimaryKey\" FROM data.\"Widget\""));
    }

    [Fact]
    public void ABlankKey_FailsTheRunLoudly()
    {
        var result = Ingest(Reader((null, "alpha", 1)));

        Assert.Equal(SnapshotMergeStatus.FailedInvalidStagingRows, result.Status);
        Assert.Equal(0, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\""));
    }

    [Fact]
    public void AMissingContractColumn_ThrowsByName_BeforeAnyRowLands()
    {
        var table = new DataTable();
        table.Columns.Add("LINEKEY", typeof(string));
        table.Columns.Add("Code", typeof(string));   // "Quantity" missing from the view

        var exception = Assert.ThrowsAny<Exception>(() => Ingest(table.CreateDataReader()));
        Assert.Contains("Quantity", exception.Message);
    }

    [Fact]
    public void NullValues_HashDistinctly_FromEmptyStrings()
    {
        Ingest(Reader(("K1", null, 1)));
        var result = Ingest(Reader(("K1", "", 1)));   // NULL → empty string IS a change

        Assert.Equal(1, result.RowsUpdated);
    }

    // ---- The two-phase form ----------------------------------------------------------------
    //
    // The claim these pin is equivalence: splitting the remote read off onto a worker must not
    // change one row, one hash, one status or one run record. If it did, every measurement of the
    // fan-out would be measuring a different ingest.

    private static SqlViewSnapshotIngestorOptions Options() => new()
    {
        Table = new SnapshotTableDefinition("Widget",
            [new SnapshotColumn("Code", "VARCHAR"), new SnapshotColumn("Quantity", "INTEGER")]),
        SelectSql = "(unused — reader supplied directly)",
        PrimaryKeyColumn = "LINEKEY",
        MergeOptions = new SnapshotMergeOptions { Source = "dms-view", DeletesEnabled = true },
    };

    [Fact]
    public void TwoPhase_LandsExactlyWhatOnePhaseLands()
    {
        (string?, string?, int?)[] rows =
            [("K1", "alpha", 1), ("K2", null, 2), ("K3   ", "gamma", 3)];

        using var buffered = new TestSnapshot();
        var onePhase = Ingest(Reader(rows));
        var twoPhase = SqlViewSnapshotIngestor.Ingest(
            buffered.Store,
            BufferedRowSet.Drain(Reader(rows)),
            OptionsFor(buffered));

        Assert.Equal(onePhase.Status, twoPhase.Status);
        Assert.Equal(onePhase.RowsStaged, twoPhase.RowsStaged);
        Assert.Equal(onePhase.RowsInserted, twoPhase.RowsInserted);

        // Identity, values and the in-database hashes — the whole row, not just the counts.
        Assert.Equal(
            snapshot.Store.ExecuteScalar(
                "SELECT string_agg(\"_PrimaryKey\" || '|' || coalesce(\"Code\", '~') || '|' || \"Quantity\" " +
                "|| '|' || \"_RowHash\", ',' ORDER BY \"_PrimaryKey\") FROM data.\"Widget\""),
            buffered.Store.ExecuteScalar(
                "SELECT string_agg(\"_PrimaryKey\" || '|' || coalesce(\"Code\", '~') || '|' || \"Quantity\" " +
                "|| '|' || \"_RowHash\", ',' ORDER BY \"_PrimaryKey\") FROM data.\"Widget\""));
    }

    [Fact]
    public void TwoPhase_ReachesTheSameTerminalPaths_AndWritesTheirRunRecords()
    {
        // A zero-row full pull is presumed torn, never a purge. It is one of the nine terminal
        // paths that end a run before the merge — and it is written by the DRAIN, on the store,
        // never by the worker that did the fetching.
        var result = SqlViewSnapshotIngestor.Ingest(
            snapshot.Store, BufferedRowSet.Drain(Reader()), OptionsFor(snapshot));

        Assert.Equal(SnapshotMergeStatus.SkippedSourceEmpty, result.Status);
        Assert.Equal(1, snapshot.Scalar<long>(
            "SELECT count(*) FROM meta.\"SyncRuns\" WHERE \"Source\" = 'dms-view' AND \"Status\" = 'Skipped:SourceEmpty'"));
    }

    [Fact]
    public void AMissingContractColumn_ThroughTheBuffer_StillThrowsByName()
    {
        var table = new DataTable();
        table.Columns.Add("LINEKEY", typeof(string));
        table.Columns.Add("Code", typeof(string));   // "Quantity" missing from the view

        var exception = Assert.ThrowsAny<Exception>(() => SqlViewSnapshotIngestor.Ingest(
            snapshot.Store, BufferedRowSet.Drain(table.CreateDataReader()), OptionsFor(snapshot)));

        Assert.Contains("Quantity", exception.Message);
    }

    [Fact]
    public void ACancelledDrain_ThrowsRatherThanReturningAShortSet()
    {
        // Fetch-ahead's containment argument in one fact: a partial read never becomes a
        // BufferedRowSet, so it can never reach a staging table and be merged as a whole universe.
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            BufferedRowSet.Drain(Reader(("K1", "alpha", 1)), cancellation.Token));
    }

    [Fact]
    public void TheBuffer_ReportsRowsAsTheyAccumulate_AndKeepsNullsDistinctFromValues()
    {
        var reported = new List<int>();
        var buffered = BufferedRowSet.Drain(
            Reader(("K1", null, 1), ("K2", "beta", 2)), onRowsBuffered: reported.Add);

        Assert.Equal(2, buffered.RowCount);
        Assert.Equal(2, reported.Sum());
        Assert.Equal(["LINEKEY", "Code", "Quantity"], buffered.ColumnNames);

        using var reader = buffered.CreateReader();
        Assert.True(reader.Read());
        Assert.True(reader.IsDBNull(reader.GetOrdinal("Code")));
        Assert.Equal("K1", reader.GetString(reader.GetOrdinal("LINEKEY")));
        Assert.True(reader.Read());
        Assert.Equal("beta", reader.GetString(reader.GetOrdinal("Code")));
        Assert.False(reader.Read());
    }

    [Fact]
    public void Fetch_ClosesTheSourceConnection_BeforeAnythingIsMerged()
    {
        // Review item #6, for free: the one-phase form holds the source connection open through
        // staging, hashing and the merge, because the appender reads off the live reader. This
        // form is done with the dealer box the moment the rows are in hand.
        var connection = new RecordingConnection(Reader(("K1", "alpha", 1)));
        var options = OptionsFor(snapshot);

        var fetch = SqlViewSnapshotIngestor.Fetch(
            () => connection,
            options,
            new SnapshotSourceFetchContext { CancellationToken = TestContext.Current.CancellationToken });

        Assert.True(connection.Disposed);
        Assert.Equal(options.SelectSql, connection.LastCommandText);
        Assert.Equal(options.CommandTimeoutSeconds, connection.LastCommandTimeout);
        Assert.Equal(1, fetch.BufferedRows);
        Assert.Equal(0, snapshot.Scalar<long>("SELECT count(*) FROM data.\"Widget\""));
    }

    private static SqlViewSnapshotIngestorOptions OptionsFor(TestSnapshot target) => new()
    {
        Table = target.Table,
        SelectSql = Options().SelectSql,
        PrimaryKeyColumn = "LINEKEY",
        MergeOptions = new SnapshotMergeOptions { Source = "dms-view", DeletesEnabled = true },
    };

    /// <summary>The smallest ADO.NET surface <see cref="SqlViewSnapshotIngestor.Fetch"/> actually touches.</summary>
    private sealed class RecordingConnection(DataTableReader reader) : DbConnection
    {
        public bool Disposed { get; private set; }
        public string? LastCommandText { get; private set; }
        public int LastCommandTimeout { get; private set; }

        public override string ConnectionString { get; set; } = "fake";
        public override string Database => "fake";
        public override string DataSource => "fake";
        public override string ServerVersion => "0";
        public override ConnectionState State { get; } = ConnectionState.Closed;

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) =>
            throw new NotSupportedException();

        protected override DbCommand CreateDbCommand() => new RecordingCommand(this, reader);

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }

        private sealed class RecordingCommand(RecordingConnection owner, DataTableReader reader) : DbCommand
        {
            public override string CommandText { get; set; } = string.Empty;
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get; set; } = owner;
            protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();
            protected override DbTransaction? DbTransaction { get; set; }

            public override void Cancel() { }
            public override int ExecuteNonQuery() => throw new NotSupportedException();
            public override object? ExecuteScalar() => throw new NotSupportedException();
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => throw new NotSupportedException();

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            {
                owner.LastCommandText = CommandText;
                owner.LastCommandTimeout = CommandTimeout;
                return reader;
            }
        }
    }

    public void Dispose() => snapshot.Dispose();
}
