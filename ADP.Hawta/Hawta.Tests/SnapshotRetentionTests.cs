using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// Retention as a standalone sweeper. The publisher's own in-process sweep is covered by
/// <see cref="SnapshotPublisherTests"/>; what is pinned here is the behaviour that only matters
/// once cleanup can run on a schedule of its own, detached from the publish that created the
/// files.
/// </summary>
public sealed class SnapshotRetentionTests : IDisposable
{
    private readonly PublisherFixture fx = new();

    public void Dispose() => fx.Dispose();

    private SnapshotRetentionResult Sweep(int keepPublishes = 3, TimeSpan? minimumAge = null) =>
        SnapshotRetention.Sweep(new SnapshotRetentionOptions
        {
            PublishDirectory = fx.PublishDirectory,
            SnapshotName = PublisherFixture.SnapshotName,
            KeepPublishes = keepPublishes,
            MinimumAge = minimumAge ?? TimeSpan.Zero,
        });

    /// <summary>Backdates a file so the age floor sees it as settled, without waiting.</summary>
    private static void Age(string path, TimeSpan by) =>
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow - by);

    private void AgeEverything(TimeSpan by)
    {
        foreach (var file in Directory.GetFiles(fx.PublishDirectory, "*", SearchOption.AllDirectories))
            Age(file, by);
    }

    /// <summary>
    /// The reason the age floor is not optional. A publish writes its parquet FIRST and commits
    /// its manifest LAST, so mid-publish there is a real file on disk that no manifest references
    /// yet. To a sweeper asking only "is this referenced?", that is indistinguishable from an
    /// orphan — and deleting it destroys the publish that is being written.
    /// </summary>
    [Fact]
    public void APublishInFlight_IsNotSweptAway_BecauseItsParquetIsMerelyYoung()
    {
        fx.MergeWidgets(("w1", "A", 1));
        fx.MergeGadgets(("g1", "G"));
        fx.Publish();
        AgeEverything(TimeSpan.FromHours(1));

        // Exactly what a publish looks like partway through: a new version file present in the
        // table's folder, its manifest not yet committed.
        var inFlight = Path.Combine(fx.PublishDirectory, "Widget", $"{DateTime.UtcNow:yyyyMMddHHmmssfff}.parquet");
        File.WriteAllText(inFlight, "not yet committed");

        var held = Sweep(minimumAge: TimeSpan.FromMinutes(15));

        Assert.True(File.Exists(inFlight),
            "a standalone sweep must not delete parquet a publish is still working on");
        Assert.Equal(0, held.ParquetFilesDeleted);
        Assert.Equal(1, held.HeldByAge);

        // And the same sweep with no floor is exactly the accident being prevented.
        var unprotected = Sweep(minimumAge: TimeSpan.Zero);

        Assert.False(File.Exists(inFlight));
        Assert.Equal(1, unprotected.ParquetFilesDeleted);
    }

    /// <summary>
    /// D11: on a filesystem, deleting a manifest a consumer holds open throws and retention
    /// retries. Blobs cannot be held open, so age is the only portable protection for a reader
    /// that is mid-refresh against an older manifest.
    /// </summary>
    [Fact]
    public void AManifestBeyondTheDepth_SurvivesUntilItIsOlderThanTheFloor()
    {
        // Published with a deep retention so the publisher's own sweep leaves all five standing;
        // the standalone sweep below is what is under test.
        for (var i = 1; i <= 5; i++)
        {
            fx.MergeWidgets(("w1", $"A{i}", i));
            fx.Publish(keepPublishes: 10);
        }

        Assert.Equal(5, fx.Manifests().Length);

        var young = Sweep(keepPublishes: 2, minimumAge: TimeSpan.FromMinutes(15));
        Assert.Equal(0, young.ManifestsDeleted);
        Assert.Equal(5, fx.Manifests().Length);
        Assert.True(young.HeldByAge >= 3, "the three manifests past the depth were spared by age, not by depth");

        AgeEverything(TimeSpan.FromHours(1));
        var settled = Sweep(keepPublishes: 2, minimumAge: TimeSpan.FromMinutes(15));

        Assert.Equal(3, settled.ManifestsDeleted);
        Assert.Equal(2, fx.Manifests().Length);
    }

    /// <summary>
    /// A file held back by age is still a referenced-set contributor while it survives, so the
    /// parquet an age-spared manifest names must not be swept out from under it.
    /// </summary>
    [Fact]
    public void ParquetNamedByAnAgeSparedManifest_IsNotDeleted()
    {
        fx.MergeWidgets(("w1", "A", 1));
        fx.Publish(keepPublishes: 10);
        var firstWidget = Assert.Single(fx.Versions("Widget"));

        for (var i = 2; i <= 4; i++)
        {
            fx.MergeWidgets(("w1", $"A{i}", i));
            fx.Publish(keepPublishes: 10);
        }

        // Everything settles except the oldest manifest, which stays young and therefore stays.
        AgeEverything(TimeSpan.FromHours(1));
        var oldest = fx.Manifests().OrderBy(name => name, StringComparer.Ordinal).First();
        Age(Path.Combine(fx.PublishDirectory, oldest), TimeSpan.Zero);

        var result = Sweep(keepPublishes: 2, minimumAge: TimeSpan.FromMinutes(15));

        Assert.Contains(oldest, fx.Manifests());
        Assert.Contains(firstWidget, fx.Versions("Widget"));
        Assert.False(result.CleanupSkipped);
    }

    /// <summary>
    /// The default floor clears a JPM-scale export and a large BI refresh with margin, rather
    /// than being cut fine against either. The publisher's own sweep opts out (zero) because it
    /// holds the write gate and its just-written set is protected by being referenced.
    /// </summary>
    [Fact]
    public void TheDefaultAgeFloor_IsFortyFiveMinutes_AndThePublishersOwnSweepOptsOut()
    {
        Assert.Equal(TimeSpan.FromMinutes(45), new SnapshotRetentionOptions
        {
            PublishDirectory = fx.PublishDirectory,
            SnapshotName = PublisherFixture.SnapshotName,
        }.MinimumAge);

        Assert.Equal(TimeSpan.Zero, new SnapshotPublishOptions
        {
            PublishDirectory = fx.PublishDirectory,
            SnapshotName = PublisherFixture.SnapshotName,
            Tables = [fx.Widget],
        }.RetentionMinimumAge);
    }

    /// <summary>
    /// At a long floor the floor, not the depth, is what bounds retention — every manifest still
    /// on disk keeps its parquet alive. Pinned because it is the surprising half of the setting.
    /// </summary>
    [Fact]
    public void AtALongFloor_TheFloorOutranksTheRetentionDepth()
    {
        for (var i = 1; i <= 5; i++)
        {
            fx.MergeWidgets(("w1", $"A{i}", i));
            fx.Publish(keepPublishes: 10);
        }

        var result = Sweep(keepPublishes: 2, minimumAge: TimeSpan.FromHours(12));

        Assert.Equal(0, result.ManifestsDeleted);
        Assert.Equal(5, fx.Manifests().Length);
        Assert.Equal(5, fx.Versions("Widget").Length);
    }

    [Fact]
    public void KeepingFewerThanTwoPublishes_IsRefused()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => Sweep(keepPublishes: 1));
        Assert.Contains("at least 2", error.Message);
    }

    [Fact]
    public void ANegativeMinimumAge_IsRefused()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Sweep(minimumAge: TimeSpan.FromMinutes(-1)));
        Assert.Contains("negative", error.Message);
    }

    /// <summary>
    /// The sweeper owns the directory or it does nothing. A second snapshot's manifest means the
    /// referenced set is only partly knowable, and a partial answer here deletes real data.
    /// </summary>
    [Fact]
    public void AForeignSnapshotSharingTheDirectory_StopsTheParquetSweep()
    {
        fx.MergeWidgets(("w1", "A", 1));
        fx.Publish();
        AgeEverything(TimeSpan.FromHours(1));

        var stray = Path.Combine(fx.PublishDirectory, "Widget", $"{DateTime.UtcNow:yyyyMMddHHmmssfff}.parquet");
        File.WriteAllText(stray, "unreferenced");
        Age(stray, TimeSpan.FromHours(1));

        File.WriteAllText(Path.Combine(fx.PublishDirectory, "other-snapshot-20260101000000000.json"), "{}");

        var result = Sweep();

        Assert.True(result.CleanupSkipped);
        Assert.Equal(0, result.ParquetFilesDeleted);
        Assert.True(File.Exists(stray), "an unreadable neighbour means stop, never guess");
    }

    /// <summary>
    /// The whole point of a standalone sweeper: it collects what a crashed publish left behind,
    /// which the publisher's own sweep never gets to because that publish never committed.
    /// </summary>
    [Fact]
    public void OrphansFromACrashedPublish_AreCollectedOnceTheyAreSettled()
    {
        fx.MergeWidgets(("w1", "A", 1));
        fx.Publish();

        var orphan = Path.Combine(fx.PublishDirectory, "Widget", $"{DateTime.UtcNow:yyyyMMddHHmmssfff}.parquet");
        File.WriteAllText(orphan, "written, then the process died before the manifest");
        AgeEverything(TimeSpan.FromHours(1));

        var result = Sweep(minimumAge: TimeSpan.FromMinutes(15));

        Assert.Equal(1, result.ParquetFilesDeleted);
        Assert.False(File.Exists(orphan));
        Assert.False(result.CleanupSkipped);
    }

    /// <summary>Sweeping is idempotent — a second pass over a clean directory changes nothing.</summary>
    [Fact]
    public void ASecondSweepOverACleanDirectory_DoesNothing()
    {
        fx.MergeWidgets(("w1", "A", 1));
        fx.Publish();
        AgeEverything(TimeSpan.FromHours(1));

        Sweep();
        var second = Sweep();

        Assert.False(second.DeletedAnything);
        Assert.Equal(0, second.Skipped);
        Assert.False(second.CleanupSkipped);
    }

    /// <summary>
    /// Ad-hoc parquet that does not follow the published version-file shape is not the
    /// publisher's to delete, however old it is.
    /// </summary>
    [Fact]
    public void ParquetThatIsNotAPublishedVersionFile_IsLeftAlone()
    {
        fx.MergeWidgets(("w1", "A", 1));
        fx.Publish();

        var adHoc = Path.Combine(fx.PublishDirectory, "Widget", "analysis-scratch.parquet");
        File.WriteAllText(adHoc, "someone's working file");
        AgeEverything(TimeSpan.FromDays(30));

        Sweep();

        Assert.True(File.Exists(adHoc));
    }
}
