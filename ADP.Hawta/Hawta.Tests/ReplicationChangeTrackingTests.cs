using System.Text;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

public sealed class ReplicationChangeTrackingTests : IDisposable
{
    private sealed class ExampleRow
    {
        public string? Code { get; set; }
        public string? MappedValue { get; set; }

        [SnapshotIgnoreForReplication]
        public string? SourceOnlyValue { get; set; }

        [SnapshotRawSource]
        public string? AuditRawLine { get; set; }
    }

    private readonly SnapshotStore store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
    private readonly SnapshotTableDefinition<ExampleRow> table = new("Example");
    private readonly string directory = Path.Combine(Path.GetTempPath(), "hawta-tracking-tests", Guid.NewGuid().ToString("N"));

    public ReplicationChangeTrackingTests()
    {
        Directory.CreateDirectory(directory);
        store.EnsureTable(table);
    }

    public void Dispose()
    {
        store.Dispose();
        try { Directory.Delete(directory, recursive: true); } catch { }
    }

    [Fact]
    public void SourceOnlyChange_RefreshesSnapshot_WithoutEnqueueingReplication()
    {
        var path = Write("example.csv", "Code,MappedValue,SourceOnlyValue\nA,one,metadata-v1\n");
        Ingest(path);
        MarkStandingRowReplicated();

        Write("example.csv", "Code,MappedValue,SourceOnlyValue\nA,one,metadata-v2\n");
        var result = Ingest(path);

        Assert.Equal(1, result.RowsUpdated);
        Assert.Equal("metadata-v2", Scalar<string>("SELECT \"SourceOnlyValue\" FROM data.\"Example\""));
        Assert.Equal(0, store.CountDirtyRows(table));
        Assert.True(Scalar<DateTime>("SELECT \"_LastModified\" FROM data.\"Example\"")
                    > Scalar<DateTime>("SELECT \"_ReplicationModified\" FROM data.\"Example\""));
    }

    [Fact]
    public void DestinationChange_RefreshesSnapshot_AndEnqueuesReplication()
    {
        var path = Write("example.csv", "Code,MappedValue,SourceOnlyValue\nA,one,metadata\n");
        Ingest(path);
        MarkStandingRowReplicated();

        Write("example.csv", "Code,MappedValue,SourceOnlyValue\nA,two,metadata\n");
        var result = Ingest(path);

        Assert.Equal(1, result.RowsUpdated);
        Assert.Equal("two", Scalar<string>("SELECT \"MappedValue\" FROM data.\"Example\""));
        Assert.Equal(1, store.CountDirtyRows(table));
    }

    [Fact]
    public void SnapshotOnlySource_StillRefreshesItsCanonicalRow()
    {
        var path = Write("snapshot-only.csv", "Code,MappedValue,SourceOnlyValue\nA,first,metadata\n");
        var options = Options(path);
        var source = new SnapshotSource
        {
            Key = "snapshot-only",
            RecordIdentity = SourceRecordIdentityDescriptor.LogicalKey(options.LogicalKey!),
            Table = table,
            Cadence = TimeSpan.FromMinutes(1),
            Families = null,
            FileIngestion = options,
            Ingest = context => FileSnapshotIngestor.Ingest(context.Store, options),
        };

        source.Ingest(new SnapshotSourceContext { Store = store, CancellationToken = TestContext.Current.CancellationToken });
        Write("snapshot-only.csv", "Code,MappedValue,SourceOnlyValue\nA,second,metadata\n");
        var result = source.Ingest(new SnapshotSourceContext { Store = store, CancellationToken = TestContext.Current.CancellationToken });

        Assert.Null(source.Families);
        Assert.Equal(1, result.RowsUpdated);
        Assert.Equal("second", Scalar<string>("SELECT \"MappedValue\" FROM data.\"Example\""));
    }

    [Fact]
    public void GeneratedCompositeIdentity_IsNormalizedAndStableAcrossOtherChanges()
    {
        var path = Write("composite.csv", "Code,MappedValue,SourceOnlyValue\n abcd , Campaign-1 ,metadata\n");
        var key = new FileLogicalKey(
            new("Code", FileKeyNormalization.TrimUpperInvariant),
            new("MappedValue"));

        Ingest(path, key);
        var originalKey = Scalar<string>("SELECT \"_PrimaryKey\" FROM data.\"Example\"");

        Write("composite.csv", "Code,MappedValue,SourceOnlyValue\nABCD,Campaign-1,changed-metadata\n");
        Ingest(path, key);

        Assert.Equal("ABCD|Campaign-1", originalKey);
        Assert.Equal(originalKey, Scalar<string>("SELECT \"_PrimaryKey\" FROM data.\"Example\""));
        Assert.Equal(1L, Scalar<long>("SELECT count(*) FROM data.\"Example\""));
    }

    [Fact]
    public void RawSourceCapture_IsExplicitAndExcludedFromReplicationDecisions()
    {
        var path = Write("audit.csv", "Code,MappedValue,SourceOnlyValue\nA,one,metadata\n");

        // Default-off: the typed audit column exists but direct binding injects NULL.
        Ingest(path);
        Assert.IsType<DBNull>(store.ExecuteScalar("SELECT \"AuditRawLine\" FROM data.\"Example\""));

        MarkStandingRowReplicated();
        var captured = Options(path, captureRawSource: true);
        FileSnapshotIngestor.Ingest(store, captured);
        Assert.Equal("A,one,metadata", Scalar<string>("SELECT \"AuditRawLine\" FROM data.\"Example\""));
        Assert.Equal(0, store.CountDirtyRows(table));

        Write("audit.csv", "Code,MappedValue,SourceOnlyValue\n\"A\",one,metadata\n");
        var result = FileSnapshotIngestor.Ingest(store, captured);

        Assert.Equal(1, result.RowsUpdated);
        Assert.Equal("\"A\",one,metadata", Scalar<string>("SELECT \"AuditRawLine\" FROM data.\"Example\""));
        Assert.Equal(0, store.CountDirtyRows(table));
    }

    private SnapshotMergeResult Ingest(string path, FileLogicalKey? key = null) =>
        FileSnapshotIngestor.Ingest(store, Options(path, logicalKey: key));

    private FileSnapshotIngestorOptions Options(
        string path,
        bool captureRawSource = false,
        FileLogicalKey? logicalKey = null) => new()
    {
        Table = table,
        FilePath = path,
        LogicalKey = logicalKey ?? FileLogicalKey.Single(table.Column(row => row.Code)),
        CaptureRawSource = captureRawSource,
        MergeOptions = new SnapshotMergeOptions { Source = "test-file", DeletesEnabled = true },
    };

    private void MarkStandingRowReplicated()
    {
        var row = Assert.Single(store.ReadDirtyRows(table));
        store.MarkReplicated(table, row.PrimaryKey, row.CapturedLastModified, "{}");
    }

    private string Write(string name, string content)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }

    private T Scalar<T>(string sql) =>
        (T)Convert.ChangeType(store.ExecuteScalar(sql)!, typeof(T));
}
