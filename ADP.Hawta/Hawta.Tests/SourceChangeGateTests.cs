using System.Text;
using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// The change gate skips a feed's read when nothing about it changed. Everything expensive sits
/// behind that decision, so the tests that matter most are the ones pinning when it must NOT skip —
/// a gate that holds on stale metadata looks exactly like a healthy idle feed.
/// </summary>
public sealed class SourceChangeGateTests : IDisposable
{
    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan by) => now += by;
    }

    private readonly TestSnapshot fixture = new();
    private readonly ManualClock clock = new();
    private readonly string directory;

    public SourceChangeGateTests()
    {
        directory = Path.Combine(Path.GetTempPath(), "hawta-gate", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
    }

    public void Dispose()
    {
        fixture.Dispose();
        try { Directory.Delete(directory, recursive: true); } catch { }
    }

    private string WriteFile(string name, string content)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(content));
        return path;
    }

    private SourceChangeGate Gate(TimeSpan? reingestAfter = null) =>
        new(reingestAfter ?? TimeSpan.FromHours(6), clock);

    private FileSnapshotIngestorOptions Options(
        string path,
        SourceChangeGate? gate,
        string? ingestVersion = null,
        string source = "test-file",
        string? scope = null,
        FileLogicalKey? key = null,
        TimeSpan? reingestAfter = null) =>
        new()
        {
            Table = fixture.Table,
            FilePath = path,
            LogicalKey = key ?? FileLogicalKey.Single("Code"),
            ChangeGate = gate,
            IngestVersion = ingestVersion,
            ReingestAfter = reingestAfter,
            MergeOptions = new SnapshotMergeOptions
            {
                Source = source,
                SourceScope = scope,
                DeletesEnabled = true,
            },
        };

    /// <summary>A fresh probe per call — the agent creates one per cycle, and so must a test.</summary>
    private SnapshotMergeResult Ingest(FileSnapshotIngestorOptions options) =>
        FileSnapshotIngestor.Ingest(fixture.Store, options, new DirectoryListingFileMetadataProbe());

    // ---- The skip ---------------------------------------------------------------------------

    [Fact]
    public void AnUnchangedFile_SkipsTheReadEntirely_AndSaysSoAsARunRecord()
    {
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\nB,2\n");
        var gate = Gate();

        Assert.Equal(SnapshotMergeStatus.Succeeded, Ingest(Options(path, gate)).Status);

        var second = Ingest(Options(path, gate));

        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, second.Status);
        Assert.Equal(0, second.RowsStaged);
        // Not silence: a source that stops being read must remain visible, or a gate holding on
        // stale metadata is indistinguishable from a healthy quiet feed.
        Assert.Equal(1, fixture.Scalar<int>(
            "SELECT count(*) FROM meta.SyncRuns WHERE \"Status\" = 'Skipped:SourceUnchanged'"));
        // The rows the first run merged are untouched.
        Assert.Equal(2, fixture.Scalar<int>("SELECT count(*) FROM data.\"Widget\""));
    }

    [Fact]
    public void WithNoGateConfigured_EveryCycleStillReads()
    {
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");

        Assert.Equal(SnapshotMergeStatus.Succeeded, Ingest(Options(path, gate: null)).Status);
        Assert.Equal(SnapshotMergeStatus.Succeeded, Ingest(Options(path, gate: null)).Status);
    }

    [Fact]
    public void WithNoProbe_TheGateCannotSkip()
    {
        // A caller that supplies no probe reads. Safe direction by construction: the gate is opt-in
        // at BOTH ends, so a half-wired host over-reads rather than under-reads.
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");
        var gate = Gate();

        Assert.Equal(SnapshotMergeStatus.Succeeded,
            FileSnapshotIngestor.Ingest(fixture.Store, Options(path, gate)).Status);
        Assert.Equal(SnapshotMergeStatus.Succeeded,
            FileSnapshotIngestor.Ingest(fixture.Store, Options(path, gate)).Status);
    }

    // ---- When it must NOT skip --------------------------------------------------------------

    [Fact]
    public void AChangedLength_ReadsAgain()
    {
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");
        var gate = Gate();
        Ingest(Options(path, gate));

        WriteFile("widgets.csv", "Code,Quantity\nA,1\nB,2\n");

        Assert.Equal(SnapshotMergeStatus.Succeeded, Ingest(Options(path, gate)).Status);
        Assert.Equal(2, fixture.Scalar<int>("SELECT count(*) FROM data.\"Widget\""));
    }

    [Fact]
    public void ASameLengthRewrite_StillReadsAgain_BecauseTheTimestampMoved()
    {
        // The case a naive size-only check would miss entirely: identical byte count, different
        // content. Only the last-write time separates them.
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");
        var gate = Gate();
        Ingest(Options(path, gate));

        File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddMinutes(1));
        WriteFile("widgets.csv", "Code,Quantity\nA,9\n");
        File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddMinutes(2));

        Assert.Equal(SnapshotMergeStatus.Succeeded, Ingest(Options(path, gate)).Status);
        Assert.Equal(9, fixture.Scalar<int>("SELECT \"Quantity\" FROM data.\"Widget\" WHERE \"_PrimaryKey\" = 'A'"));
    }

    [Fact]
    public void AChangedIngestConfiguration_ReadsAgain_EvenThoughTheFileIsIdentical()
    {
        // The failure this exists to prevent: a deploy changes how a feed is interpreted, the file
        // does not change, and the new behaviour is silently deferred until the producer happens to
        // rewrite it — possibly days.
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");
        var gate = Gate();
        Ingest(Options(path, gate));

        var reconfigured = Options(path, gate,
            key: new FileLogicalKey(new FileLogicalKeyPart("Code", FileKeyNormalization.TrimUpperInvariant)));

        Assert.Equal(SnapshotMergeStatus.Succeeded, Ingest(reconfigured).Status);
    }

    [Fact]
    public void AChangedIngestVersion_ReadsAgain_WithNoConfigurationOrFileChange()
    {
        // The operator's lever, and the only one that works without a deploy.
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");
        var gate = Gate();
        Ingest(Options(path, gate, ingestVersion: "v1"));

        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, Ingest(Options(path, gate, ingestVersion: "v1")).Status);
        Assert.Equal(SnapshotMergeStatus.Succeeded, Ingest(Options(path, gate, ingestVersion: "v2")).Status);
    }

    [Fact]
    public void AnExpiredTrustWindow_ReadsAgain_HoweverUnchangedTheMetadataLooks()
    {
        // Length + mtime cannot see a timestamp-preserving rewrite. This bound is what turns
        // "invisible forever" into "invisible for at most N hours".
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");
        var gate = Gate(TimeSpan.FromHours(6));
        Ingest(Options(path, gate));

        clock.Advance(TimeSpan.FromHours(5));
        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, Ingest(Options(path, gate)).Status);

        clock.Advance(TimeSpan.FromHours(2));
        Assert.Equal(SnapshotMergeStatus.Succeeded, Ingest(Options(path, gate)).Status);
    }

    [Fact]
    public void ASubMicrosecondTimestamp_StillComparesEqual_AndSkips()
    {
        // NTFS resolves file times to 100 ns; DuckDB's TIMESTAMP holds microseconds. Persisting the
        // stamp through a TIMESTAMP truncates, so an unchanged file reads as changed — intermittently,
        // because a time that happens to land on a microsecond boundary still compares equal. Ticks
        // are stored instead. 1237 ticks is deliberately not a whole microsecond.
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");
        File.SetLastWriteTimeUtc(path, new DateTime(2026, 8, 16, 9, 0, 0, DateTimeKind.Utc).AddTicks(1237));
        var gate = Gate();

        Assert.Equal(SnapshotMergeStatus.Succeeded, Ingest(Options(path, gate)).Status);
        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, Ingest(Options(path, gate)).Status);
    }

    [Fact]
    public void WithNoReingestBound_AStampNeverExpires()
    {
        // The default. A blind periodic re-read costs the whole file and detects nothing when
        // nothing changed — indefensible for a 391 MiB feed that changes twice a year.
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");
        var gate = new SourceChangeGate(reingestAfter: null, clock);
        Ingest(Options(path, gate));

        clock.Advance(TimeSpan.FromDays(400));

        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, Ingest(Options(path, gate)).Status);
    }

    [Fact]
    public void APerSourceBound_OverridesAnUnboundedGate()
    {
        // The knob belongs per source: the cost of a periodic re-read is a property of the feed,
        // so a cheap file can afford one while a huge one cannot.
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");
        var gate = new SourceChangeGate(reingestAfter: null, clock);
        Ingest(Options(path, gate, reingestAfter: TimeSpan.FromHours(1)));
        clock.Advance(TimeSpan.FromHours(2));

        Assert.Equal(SnapshotMergeStatus.Succeeded,
            Ingest(Options(path, gate, reingestAfter: TimeSpan.FromHours(1))).Status);
    }

    [Fact]
    public void AZeroReingestInterval_IsRefused_BecauseOmittingItIsHowYouSayNever()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceChangeGate(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceChangeGate(TimeSpan.FromHours(-1)));
    }

    [Fact]
    public void ASkipReportsHowLongSinceTheSourceWasActuallyRead()
    {
        // With no re-ingest bound this is the only signal that a feed has gone unread for a long
        // time — and recon cannot help, since it compares the snapshot against Cosmos and both go
        // stale together when a source read is missed.
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");
        var gate = new SourceChangeGate(reingestAfter: null, clock);
        Ingest(Options(path, gate));

        clock.Advance(TimeSpan.FromDays(3));
        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged, Ingest(Options(path, gate)).Status);

        var note = fixture.Scalar<string>(
            "SELECT \"Error\" FROM meta.SyncRuns WHERE \"Status\" = 'Skipped:SourceUnchanged'");
        Assert.Contains("3.0 day(s)", note);
    }

    // ---- Stamps are earned, not assumed ------------------------------------------------------

    [Fact]
    public void AFailedMerge_WritesNoStamp_SoTheNextCycleReadsAgain()
    {
        // Duplicate logical keys fail the merge's contract. If a failed run stamped, the gate would
        // then skip a file that was never actually ingested — for as long as it stayed unchanged.
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\nA,2\n");
        var gate = Gate();

        var first = Ingest(Options(path, gate));
        Assert.NotEqual(SnapshotMergeStatus.Succeeded, first.Status);

        var second = Ingest(Options(path, gate));
        Assert.NotEqual(SnapshotMergeStatus.SkippedSourceUnchanged, second.Status);
        Assert.Equal(0, fixture.Scalar<int>("SELECT count(*) FROM meta.SourceFileStamps"));
    }

    [Fact]
    public void AnAbsentFile_StillReportsSourceAbsent_AndTheGateNeverSwallowsIt()
    {
        // Absence has its own outcome for a reason (a renamed feed must not tombstone its family).
        // The gate sits after that guard and must not be able to reinterpret it.
        var path = WriteFile("widgets.csv", "Code,Quantity\nA,1\n");
        var gate = Gate();
        Ingest(Options(path, gate));

        File.Delete(path);

        Assert.Equal(SnapshotMergeStatus.SkippedSourceAbsent, Ingest(Options(path, gate)).Status);
    }

    [Fact]
    public void TwoScopesOfOneSource_DoNotShareAStamp()
    {
        // The per-dealer pattern: many sources, one shared table, distinguished by scope. Aliasing
        // their stamps would let one dealer's ingest suppress another's.
        var alpha = WriteFile("alpha.csv", "Code,Quantity\nA,1\n");
        var beta = WriteFile("beta.csv", "Code,Quantity\nB,2\n");
        var gate = Gate();

        Assert.Equal(SnapshotMergeStatus.Succeeded, Ingest(Options(alpha, gate, source: "feed", scope: "AAD")).Status);
        Assert.Equal(SnapshotMergeStatus.Succeeded, Ingest(Options(beta, gate, source: "feed", scope: "TAJ")).Status);

        Assert.Equal(2, fixture.Scalar<int>("SELECT count(*) FROM meta.SourceFileStamps"));
        Assert.Equal(SnapshotMergeStatus.SkippedSourceUnchanged,
            Ingest(Options(alpha, gate, source: "feed", scope: "AAD")).Status);
    }

    [Fact]
    public void AStampKey_CannotAliasTwoDifferentSourceAndScopePairs()
    {
        // "a/b" + "c" and "a" + "b/c" both join to "a/b/c" under a naive delimiter, and source keys
        // legitimately contain slashes (dms-order-lines/AAD).
        var left = FileSnapshotIngestor.StampKey(new SnapshotMergeOptions { Source = "a/b", SourceScope = "c" });
        var right = FileSnapshotIngestor.StampKey(new SnapshotMergeOptions { Source = "a", SourceScope = "b/c" });

        Assert.NotEqual(left, right);
    }

    // ---- The probe --------------------------------------------------------------------------

    [Fact]
    public void TheProbe_ReportsAbsentForAMissingNameAndFoundForARealOne()
    {
        var path = WriteFile("present.csv", "x");
        var missing = Path.Combine(directory, "missing.csv");
        var probe = new DirectoryListingFileMetadataProbe();

        var results = probe.Read([path, missing]);

        Assert.Equal(FileProbeStatus.Found, results[path].Status);
        Assert.Equal(1, results[path].Metadata.Length);
        Assert.Equal(FileProbeStatus.Absent, results[missing].Status);
        // Every requested path answers — a failure is never a missing key.
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void TheProbe_ReportsAbsentUnderAMissingFolder_WithoutDegrading()
    {
        var probe = new DirectoryListingFileMetadataProbe();
        var path = Path.Combine(directory, "no-such-folder", "file.csv");

        Assert.Equal(FileProbeStatus.Absent, probe.Read(path).Status);
        Assert.Equal(0, probe.FoldersDegradedToPerFileProbing);
    }

    [Fact]
    public void TheProbe_ReadsOneFolderOnce_AndServesEveryFileInItFromThatListing()
    {
        var first = WriteFile("one.csv", "a");
        var second = WriteFile("two.csv", "bb");
        var probe = new DirectoryListingFileMetadataProbe();

        var results = probe.Read([first, second]);

        Assert.Equal(1, results[first].Metadata.Length);
        Assert.Equal(2, results[second].Metadata.Length);
        Assert.Equal(0, probe.FoldersDegradedToPerFileProbing);
    }

    [Fact]
    public void TheProbe_CachesWithinAnInstance_SoOneCycleSeesOnePicture()
    {
        var path = WriteFile("widgets.csv", "a");
        var probe = new DirectoryListingFileMetadataProbe();
        Assert.Equal(1, probe.Read(path).Metadata.Length);

        WriteFile("widgets.csv", "abcdef");

        // Same instance: still the cycle's original view. A fresh instance — what the agent builds
        // each cycle — sees the change.
        Assert.Equal(1, probe.Read(path).Metadata.Length);
        Assert.Equal(6, new DirectoryListingFileMetadataProbe().Read(path).Metadata.Length);
    }
}
