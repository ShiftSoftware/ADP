using System.Data;
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

    public void Dispose() => snapshot.Dispose();
}
