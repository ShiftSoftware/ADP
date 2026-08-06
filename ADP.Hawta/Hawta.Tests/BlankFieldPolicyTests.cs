using System.Text;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// The blank-field policy, per column: a present-but-empty CSV field is captured as the
/// EMPTY STRING in text columns (faithful raw-text capture — .NET CSV readers give
/// consumers "" too, so a pump reproducing another pipeline's documents must not silently
/// turn "" into null), but becomes NULL in TYPED columns, where empty means absent.
/// Genuinely bad values in typed columns still fail the run loudly.
/// </summary>
public sealed class BlankFieldPolicyTests : IDisposable
{
    private readonly SnapshotStore store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
    private readonly string directory;

    public BlankFieldPolicyTests()
    {
        directory = Path.Combine(Path.GetTempPath(), "hawta-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
    }

    public void Dispose()
    {
        store.Dispose();
        try { Directory.Delete(directory, recursive: true); } catch { }
    }

    private string Write(string name, string content)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(content));
        return path;
    }

    private object? Scalar(string sql)
    {
        var value = store.ExecuteScalar(sql);
        return value is DBNull ? null : value;
    }

    [Fact]
    public void BlankTextField_IsTheEmptyString_NotNull()
    {
        // The doc-parity case: every CSV feed captures raw text, and blank-vs-null must not
        // become a phantom document difference against the pipeline being replaced.
        var table = new SnapshotTableDefinition("TextRow",
            [new SnapshotColumn("Code", "VARCHAR"), new SnapshotColumn("Note", "VARCHAR")]);
        store.EnsureTable(table);

        var path = Write("text.csv", "Code,Note\nA,\nB,\"\"\nC,filled\n");

        var result = FileSnapshotIngestor.Ingest(store, new FileSnapshotIngestorOptions
        {
            Table = table,
            FilePath = path,
            PrimaryKeyColumn = "Code",
            MergeOptions = new SnapshotMergeOptions { Source = "blank-text", DeletesEnabled = true },
        });

        Assert.Equal(3, result.RowsInserted);
        Assert.Equal("", Scalar("SELECT \"Note\" FROM data.\"TextRow\" WHERE \"_PrimaryKey\" = 'A'"));
        Assert.Equal("", Scalar("SELECT \"Note\" FROM data.\"TextRow\" WHERE \"_PrimaryKey\" = 'B'"));   // quoted empty too
        Assert.Equal("filled", Scalar("SELECT \"Note\" FROM data.\"TextRow\" WHERE \"_PrimaryKey\" = 'C'"));
    }

    [Fact]
    public void BlankTypedField_IsNull_NotAFailedRun()
    {
        var table = new SnapshotTableDefinition("TypedRow",
        [
            new SnapshotColumn("Code", "VARCHAR"),
            new SnapshotColumn("Quantity", "INTEGER"),
            new SnapshotColumn("Price", "DECIMAL(18,2)"),
            new SnapshotColumn("SoldAt", "TIMESTAMP"),
        ]);
        store.EnsureTable(table);

        var path = Write("typed.csv", "Code,Quantity,Price,SoldAt\nA,,,\nB,5,1.50,2026-08-02 10:00:00\n");

        var result = FileSnapshotIngestor.Ingest(store, new FileSnapshotIngestorOptions
        {
            Table = table,
            FilePath = path,
            PrimaryKeyColumn = "Code",
            MergeOptions = new SnapshotMergeOptions { Source = "blank-typed", DeletesEnabled = true },
        });

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.RowsInserted);
        Assert.Null(Scalar("SELECT \"Quantity\" FROM data.\"TypedRow\" WHERE \"_PrimaryKey\" = 'A'"));
        Assert.Null(Scalar("SELECT \"Price\" FROM data.\"TypedRow\" WHERE \"_PrimaryKey\" = 'A'"));
        Assert.Null(Scalar("SELECT \"SoldAt\" FROM data.\"TypedRow\" WHERE \"_PrimaryKey\" = 'A'"));
        Assert.Equal(5, Scalar("SELECT \"Quantity\" FROM data.\"TypedRow\" WHERE \"_PrimaryKey\" = 'B'"));
    }

    [Fact]
    public void GarbageInATypedColumn_StillFailsTheRun()
    {
        // The blank-field policy must not become a "quietly null out anything unparseable"
        // policy — that contract is unchanged.
        var table = new SnapshotTableDefinition("StrictRow",
            [new SnapshotColumn("Code", "VARCHAR"), new SnapshotColumn("Quantity", "INTEGER")]);
        store.EnsureTable(table);

        var path = Write("garbage.csv", "Code,Quantity\nA,not-a-number\n");

        Assert.ThrowsAny<Exception>(() => FileSnapshotIngestor.Ingest(store, new FileSnapshotIngestorOptions
        {
            Table = table,
            FilePath = path,
            PrimaryKeyColumn = "Code",
            MergeOptions = new SnapshotMergeOptions { Source = "garbage", DeletesEnabled = true },
        }));

        Assert.Equal(1L, Convert.ToInt64(store.ExecuteScalar(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Source\" = 'garbage' AND \"Status\" = 'Failed:Exception'")));
    }

    [Fact]
    public void ATypedParquetColumn_IsNeverRewritten()
    {
        // The nullif rewrite applies only to VARCHAR-typed source columns; a parquet source
        // carrying real types round-trips untouched (and its NULLs stay NULL).
        var table = new SnapshotTableDefinition("ParquetRow",
            [new SnapshotColumn("Code", "VARCHAR"), new SnapshotColumn("Quantity", "INTEGER")]);
        store.EnsureTable(table);

        var parquet = Path.Combine(directory, "typed.parquet").Replace("'", "''");
        store.Execute(
            $"""
            COPY (SELECT 'A' AS "Code", CAST(NULL AS INTEGER) AS "Quantity"
                  UNION ALL SELECT 'B', 7)
            TO '{parquet}' (FORMAT parquet)
            """);

        var result = FileSnapshotIngestor.Ingest(store, new FileSnapshotIngestorOptions
        {
            Table = table,
            FilePath = Path.Combine(directory, "typed.parquet"),
            PrimaryKeyColumn = "Code",
            MergeOptions = new SnapshotMergeOptions { Source = "parquet-typed", DeletesEnabled = true },
        });

        Assert.True(result.Succeeded);
        Assert.Null(Scalar("SELECT \"Quantity\" FROM data.\"ParquetRow\" WHERE \"_PrimaryKey\" = 'A'"));
        Assert.Equal(7, Scalar("SELECT \"Quantity\" FROM data.\"ParquetRow\" WHERE \"_PrimaryKey\" = 'B'"));
    }

    [Fact]
    public void InAVarcharColumn_ABlankAndANull_AreDeliberatelyDifferentValues()
    {
        // The other half of the trade-off, pinned so it is a DECISION and not an accident:
        // raw-text capture keeps '' distinct from NULL, which is what gives blank-vs-null
        // document parity against a .NET pipeline for free — and what costs a one-time
        // re-hash if a Phase-B parquet producer encodes blanks as NULL instead.
        var table = new SnapshotTableDefinition("TextFlipRow",
            [new SnapshotColumn("Code", "VARCHAR"), new SnapshotColumn("Note", "VARCHAR")]);
        store.EnsureTable(table);

        var csv = Write("textflip.csv", "Code,Note\nA,\nB,filled\n");
        FileSnapshotIngestor.Ingest(store, new FileSnapshotIngestorOptions
        {
            Table = table,
            FilePath = csv,
            PrimaryKeyColumn = "Code",
            MergeOptions = new SnapshotMergeOptions { Source = "textflip-csv", DeletesEnabled = true },
        });

        var parquet = Path.Combine(directory, "textflip.parquet");
        store.Execute(
            $"""
            COPY (SELECT 'A' AS "Code", CAST(NULL AS VARCHAR) AS "Note"
                  UNION ALL SELECT 'B', 'filled')
            TO '{parquet.Replace("'", "''")}' (FORMAT parquet)
            """);

        var flip = FileSnapshotIngestor.Ingest(store, new FileSnapshotIngestorOptions
        {
            Table = table,
            FilePath = parquet,
            PrimaryKeyColumn = "Code",
            MergeOptions = new SnapshotMergeOptions { Source = "textflip-parquet", DeletesEnabled = true },
        });

        // Exactly the blank row re-hashes; the populated row does not.
        Assert.True(flip.Succeeded);
        Assert.Equal(1, flip.RowsUpdated);
        Assert.Null(Scalar("SELECT \"Note\" FROM data.\"TextFlipRow\" WHERE \"_PrimaryKey\" = 'A'"));
    }

    [Fact]
    public void CsvBlankToTypedNull_AndParquetNull_HashIdentically()
    {
        // The format-flip contract survives the policy: a CSV blank in a typed column and a
        // parquet NULL are the same value, so flipping formats is not a change.
        var table = new SnapshotTableDefinition("FlipRow",
            [new SnapshotColumn("Code", "VARCHAR"), new SnapshotColumn("Quantity", "INTEGER")]);
        store.EnsureTable(table);

        var csv = Write("flip.csv", "Code,Quantity\nA,\nB,3\n");
        FileSnapshotIngestor.Ingest(store, new FileSnapshotIngestorOptions
        {
            Table = table,
            FilePath = csv,
            PrimaryKeyColumn = "Code",
            MergeOptions = new SnapshotMergeOptions { Source = "flip-csv", DeletesEnabled = true },
        });

        var parquet = Path.Combine(directory, "flip.parquet");
        store.Execute(
            $"""
            COPY (SELECT 'A' AS "Code", CAST(NULL AS INTEGER) AS "Quantity"
                  UNION ALL SELECT 'B', 3)
            TO '{parquet.Replace("'", "''")}' (FORMAT parquet)
            """);

        var flip = FileSnapshotIngestor.Ingest(store, new FileSnapshotIngestorOptions
        {
            Table = table,
            FilePath = parquet,
            PrimaryKeyColumn = "Code",
            MergeOptions = new SnapshotMergeOptions { Source = "flip-parquet", DeletesEnabled = true },
        });

        Assert.True(flip.Succeeded);
        Assert.Equal(0, flip.RowsInserted);
        Assert.Equal(0, flip.RowsUpdated);
        Assert.Equal(0, flip.RowsTombstoned);
    }
}
