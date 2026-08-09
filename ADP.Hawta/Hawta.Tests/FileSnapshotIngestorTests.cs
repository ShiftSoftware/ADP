using System.Text;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

public sealed class FileSnapshotIngestorTests : IDisposable
{
    private readonly TestSnapshot fixture = new();
    private readonly string directory;

    public FileSnapshotIngestorTests()
    {
        directory = Path.Combine(Path.GetTempPath(), "hawta-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
    }

    public void Dispose()
    {
        fixture.Dispose();
        try { Directory.Delete(directory, recursive: true); } catch { }
    }

    private string WriteFile(string name, string content, bool bom = false) =>
        WriteBytes(name, new UTF8Encoding(encoderShouldEmitUTF8Identifier: bom).GetBytes(content));

    private string WriteBytes(string name, byte[] content)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllBytes(path, content);
        return path;
    }

    private FileSnapshotIngestorOptions Options(string path, Action<FileSnapshotIngestorOptionsBuilder>? configure = null)
    {
        var builder = new FileSnapshotIngestorOptionsBuilder();
        configure?.Invoke(builder);

        return new FileSnapshotIngestorOptions
        {
            Table = fixture.Table,
            FilePath = path,
            Csv = builder.Csv ?? new CsvReadOptions(),
            SelectSql = builder.SelectSql,
            LogicalKey = FileLogicalKey.Single("Code"),
            MergeOptions = new SnapshotMergeOptions { Source = "test-file", DeletesEnabled = true },
        };
    }

    private sealed class FileSnapshotIngestorOptionsBuilder
    {
        public CsvReadOptions? Csv;
        public string? SelectSql;
    }

    [Fact]
    public void CsvWithHeader_InsertsTypedRows()
    {
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\nB,2\n");

        var result = FileSnapshotIngestor.Ingest(fixture.Store, Options(path));

        Assert.Equal(SnapshotMergeStatus.Succeeded, result.Status);
        Assert.Equal(2, result.RowsInserted);
        // The all-varchar CSV value became a typed INTEGER at the staging insert.
        Assert.Equal(1, fixture.Scalar<int>("SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'A'"));
    }

    [Fact]
    public void Utf8Bom_DoesNotPolluteFirstHeaderName()
    {
        // SSC/JPM/NonJPM all carry a UTF-8 BOM; the first header column must still bind by name.
        var path = WriteFile("bom.csv", "Code,Quantity\nA,1\n", bom: true);

        var result = FileSnapshotIngestor.Ingest(fixture.Store, Options(path));

        Assert.Equal(SnapshotMergeStatus.Succeeded, result.Status);
        Assert.Equal("A", fixture.Scalar<string>("SELECT \"Code\" FROM data.\"Widget\""));
    }

    [Fact]
    public void SemicolonDelimiter_HeadersWithSpaces_ProjectionRenames()
    {
        var path = WriteFile("parts.csv", "Part No;Qty On Hand\nX1;5\nX2;7\n");

        var result = FileSnapshotIngestor.Ingest(fixture.Store, Options(path, b =>
        {
            b.Csv = new CsvReadOptions { Delimiter = ";" };
            b.SelectSql = """SELECT "Part No" AS "Code", "Qty On Hand" AS "Quantity" FROM {source}""";
        }));

        Assert.Equal(SnapshotMergeStatus.Succeeded, result.Status);
        Assert.Equal(2, result.RowsInserted);
        Assert.Equal(7, fixture.Scalar<int>("SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'X2'"));
    }

    [Fact]
    public void HeaderlessCsv_UsesConfiguredColumnNames()
    {
        var path = WriteFile("stock.csv", "P100,3\nP200,4\n");

        var result = FileSnapshotIngestor.Ingest(fixture.Store, Options(path, b =>
            b.Csv = new CsvReadOptions { HasHeader = false, ColumnNames = ["Code", "Quantity"] }));

        Assert.Equal(SnapshotMergeStatus.Succeeded, result.Status);
        Assert.Equal(2, result.RowsInserted);
        Assert.Equal(3, fixture.Scalar<int>("SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'P100'"));
    }

    [Fact]
    public void Parquet_AutoDetectedByExtension_Ingests()
    {
        var path = Path.Combine(directory, "widgets.parquet");
        fixture.Store.Execute(
            $"""
            COPY (SELECT * FROM (VALUES ('A', 1), ('B', 2)) AS t("Code", "Quantity"))
            TO '{path.Replace("'", "''")}' (FORMAT parquet)
            """);

        var result = FileSnapshotIngestor.Ingest(fixture.Store, Options(path));

        Assert.Equal(SnapshotMergeStatus.Succeeded, result.Status);
        Assert.Equal(2, result.RowsInserted);
    }

    [Fact]
    public void CsvThenTypedParquet_SameValues_MergeSeesNoChanges()
    {
        // The cutover contract: a source flipping CSV → parquet with value-identical content
        // must not produce a spurious diff. The hash is computed on the TYPED staging columns,
        // so '1' (csv text) and 1 (parquet INTEGER) canonicalize identically.
        var csv = WriteFile("widgets.csv", "Code,Quantity\nA,1\nB,2\n");
        FileSnapshotIngestor.Ingest(fixture.Store, Options(csv));

        var parquet = Path.Combine(directory, "widgets.parquet");
        fixture.Store.Execute(
            $"""
            COPY (SELECT * FROM (VALUES ('A', 1), ('B', 2)) AS t("Code", "Quantity"))
            TO '{parquet.Replace("'", "''")}' (FORMAT parquet)
            """);

        var result = FileSnapshotIngestor.Ingest(fixture.Store, Options(parquet));

        Assert.Equal(SnapshotMergeStatus.Succeeded, result.Status);
        Assert.Equal(0, result.RowsInserted);
        Assert.Equal(0, result.RowsUpdated);
        Assert.Equal(0, result.RowsTombstoned);
    }

    [Fact]
    public void MissingFile_SkipsWithRunRecord_TouchesNothing()
    {
        fixture.Merge([("A", "A", 1), ("B", "B", 2)]);

        var result = FileSnapshotIngestor.Ingest(fixture.Store,
            Options(Path.Combine(directory, "renamed-away.csv")));

        Assert.Equal(SnapshotMergeStatus.SkippedSourceAbsent, result.Status);
        Assert.False(result.Succeeded);
        // Absence ≠ empty universe: nothing tombstoned, nothing changed.
        Assert.Equal(2, fixture.Scalar<int>("SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
        Assert.Equal(1, fixture.Scalar<int>(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Status\" = 'Skipped:SourceAbsent' AND \"Source\" = 'test-file'"));
    }

    [Fact]
    public void BlankKeys_FailInvalidStagingRows()
    {
        var path = WriteFile("blank-key.csv", "Code,Quantity\nA,1\n,2\n");

        var result = FileSnapshotIngestor.Ingest(fixture.Store, Options(path));

        Assert.Equal(SnapshotMergeStatus.FailedInvalidStagingRows, result.Status);
        Assert.Equal(0, fixture.Scalar<int>("SELECT count(*) FROM data.\"Widget\""));
    }

    [Fact]
    public void DuplicateKeys_FailDuplicateStagingKeys()
    {
        var path = WriteFile("dupes.csv", "Code,Quantity\nA,1\nA,2\n");

        var result = FileSnapshotIngestor.Ingest(fixture.Store, Options(path));

        Assert.Equal(SnapshotMergeStatus.FailedDuplicateStagingKeys, result.Status);
    }

    [Fact]
    public void FileRowNumber_MakesFirstInFileWinsDedupDeterministic()
    {
        // The JPM shape: duplicate keys collapse at ingest, first occurrence supplies the values.
        var path = WriteFile("dupes.csv", "Code,Quantity\nA,1\nB,7\nA,2\nA,3\n");

        var options = new FileSnapshotIngestorOptions
        {
            Table = fixture.Table,
            FilePath = path,
            IncludeFileRowNumber = true,
            SelectSql = """
                SELECT "Code", "Quantity" FROM {source}
                QUALIFY row_number() OVER (PARTITION BY "Code" ORDER BY "hawta$file_row_number") = 1
                """,
            PrimaryKeyColumn = "Code",
            MergeOptions = new SnapshotMergeOptions { Source = "test-file", DeletesEnabled = true },
        };

        var result = FileSnapshotIngestor.Ingest(fixture.Store, options);

        Assert.Equal(2, result.RowsInserted);
        Assert.Equal(1, fixture.Scalar<int>("SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'A'"));

        // Re-ingesting the same file is not a change — the winner is stable.
        var again = FileSnapshotIngestor.Ingest(fixture.Store, options);
        Assert.Equal(0, again.RowsUpdated);
    }

    [Fact]
    public void KeysAreTrimmed()
    {
        var path = WriteFile("padded.csv", "Code,Quantity\n\"  A  \",1\n");

        FileSnapshotIngestor.Ingest(fixture.Store, Options(path));

        Assert.Equal("A", fixture.Scalar<string>("SELECT \"_PrimaryKey\" FROM data.\"Widget\""));
    }

    [Fact]
    public void KeysAreTrimmed_OfAllWhitespace_LikeTheCSharpIngestors()
    {
        // DuckDB's bare trim() strips spaces only; the key contract strips the same
        // whitespace class as SqlViewSnapshotIngestor's C# Trim() — and a whitespace-ONLY
        // key must canonicalize to NULL (loud), not survive as "\t".
        var path = WriteFile("tabbed.csv", "Code,Quantity\n\"\tA\t\",1\n");

        FileSnapshotIngestor.Ingest(fixture.Store, Options(path));

        Assert.Equal("A", fixture.Scalar<string>("SELECT \"_PrimaryKey\" FROM data.\"Widget\""));

        var blankPath = WriteFile("tab-only-key.csv", "Code,Quantity\n\"\t\",1\n");
        var result = FileSnapshotIngestor.Ingest(fixture.Store, Options(blankPath));
        Assert.Equal(SnapshotMergeStatus.FailedInvalidStagingRows, result.Status);
    }

    [Fact]
    public void EmptyReadableFile_SkipsSourceEmpty_TouchesNothing()
    {
        // The review-confirmed blocker: a present-but-empty file (the mid-upload SMB window)
        // must never read as "delete everything" — especially on families below the
        // guardrail's absolute floor.
        fixture.Merge([("A", "A", 1), ("B", "B", 2)]);

        foreach (var content in new[] { "", "Code,Quantity\n" })    // 0-byte AND header-only
        {
            var path = WriteFile("mid-upload.csv", content);
            var result = FileSnapshotIngestor.Ingest(fixture.Store, Options(path));

            Assert.Equal(SnapshotMergeStatus.SkippedSourceEmpty, result.Status);
            Assert.Equal(2, fixture.Scalar<int>("SELECT count(*) FROM data.\"Widget\" WHERE \"_Deleted\" = false"));
        }

        Assert.Equal(2, fixture.Scalar<int>(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Status\" = 'Skipped:SourceEmpty'"));
    }

    [Fact]
    public void EmptyFile_WithForceDeletes_IsTheIntentionalPurgePath()
    {
        fixture.Merge([("A", "A", 1), ("B", "B", 2)]);

        var path = WriteFile("purge.csv", "Code,Quantity\n");
        var result = FileSnapshotIngestor.Ingest(fixture.Store, new FileSnapshotIngestorOptions
        {
            Table = fixture.Table,
            FilePath = path,
            PrimaryKeyColumn = "Code",
            MergeOptions = new SnapshotMergeOptions
            {
                Source = "test-file", DeletesEnabled = true, ForceDeletes = true,
            },
        });

        Assert.Equal(SnapshotMergeStatus.Succeeded, result.Status);
        Assert.Equal(2, result.RowsTombstoned);
    }

    [Fact]
    public void BracketedFilename_ReadsTheLiteralFile_NotAGlobMatch()
    {
        // DuckDB paths are glob patterns; File.Exists tests the literal. Unescaped,
        // "data[1].csv" would silently read the SIBLING data1.csv.
        WriteFile("data1.csv", "Code,Quantity\nWRONG,9\nWRONG2,9\nWRONG3,9\n");
        var path = WriteFile("data[1].csv", "Code,Quantity\nRIGHT,1\n");

        var result = FileSnapshotIngestor.Ingest(fixture.Store, Options(path));

        Assert.Equal(1, result.RowsInserted);
        Assert.Equal("RIGHT", fixture.Scalar<string>("SELECT \"Code\" FROM data.\"Widget\""));
    }

    [Fact]
    public void SourceFileCarryingTheSynthesizedColumnName_IsRejectedLoudly()
    {
        var path = WriteFile("shadow.csv", "hawta$file_row_number,Code,Quantity\n77,A,1\n");

        Assert.Throws<ArgumentException>(() => FileSnapshotIngestor.Ingest(fixture.Store,
            new FileSnapshotIngestorOptions
            {
                Table = fixture.Table,
                FilePath = path,
                IncludeFileRowNumber = true,
                PrimaryKeyColumn = "Code",
                MergeOptions = new SnapshotMergeOptions { Source = "test-file", DeletesEnabled = true },
            }));
    }

    [Fact]
    public void FirstAggregate_KeepsNullFromTheFirstRow_ThePinnedDedupSemantics()
    {
        // The JPM/NonJPM contract "first row supplies all scalars" relies on DuckDB's
        // first() NOT skipping NULLs. Pin it through the real aggregate shape so an engine
        // upgrade that changes first()'s NULL handling fails here, not in production diffs.
        var path = WriteFile("null-first.csv", "Code,Quantity\nA,\nA,5\nB,7\n");

        var result = FileSnapshotIngestor.Ingest(fixture.Store, new FileSnapshotIngestorOptions
        {
            Table = fixture.Table,
            FilePath = path,
            IncludeFileRowNumber = true,
            SelectSql = """
                SELECT trim("Code") AS "Code",
                       first("Quantity" ORDER BY "hawta$file_row_number") AS "Quantity"
                FROM {source}
                GROUP BY trim("Code")
                """,
            PrimaryKeyColumn = "Code",
            MergeOptions = new SnapshotMergeOptions { Source = "test-file", DeletesEnabled = true },
        });

        Assert.Equal(2, result.RowsInserted);
        Assert.Null(fixture.ScalarOrNull("SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'A'"));
        Assert.Equal(7, fixture.Scalar<int>("SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'B'"));
    }

    [Fact]
    public void UnreadableFile_RecordsFailedRunAndThrows()
    {
        // File.Exists passes but the read fails — the torn-upload / share-glitch class.
        var path = WriteBytes("torn.parquet", Encoding.ASCII.GetBytes("this is not a parquet file"));

        Assert.ThrowsAny<Exception>(() => FileSnapshotIngestor.Ingest(fixture.Store, Options(path)));

        Assert.Equal(1, fixture.Scalar<int>(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Status\" = 'Failed:Exception' AND \"Source\" = 'test-file'"));
    }

    [Fact]
    public void MissingSourceColumn_RecordsFailedRunAndThrows()
    {
        var path = WriteFile("narrow.csv", "Code\nA\n");

        Assert.ThrowsAny<Exception>(() => FileSnapshotIngestor.Ingest(fixture.Store, Options(path)));

        Assert.Equal(1, fixture.Scalar<int>(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Status\" = 'Failed:Exception'"));
    }

    [Fact]
    public void SelectSqlWithoutPlaceholder_Throws()
    {
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");

        Assert.Throws<ArgumentException>(() => FileSnapshotIngestor.Ingest(fixture.Store,
            Options(path, b => b.SelectSql = "SELECT * FROM somewhere_else")));
    }

    [Fact]
    public void UnknownExtension_WithoutExplicitFormat_Throws()
    {
        var path = WriteFile("widgets.dat", "Code,Quantity\nA,1\n");

        Assert.Throws<ArgumentException>(() => FileSnapshotIngestor.Ingest(fixture.Store, Options(path)));
    }
}
