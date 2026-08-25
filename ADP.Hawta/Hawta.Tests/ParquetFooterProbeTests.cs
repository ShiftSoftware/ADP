using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// <see cref="SnapshotPublisher.ParquetFooterProbe"/> — the batched footer probe.
///
/// <para>The publisher probed one table per statement, and a probe costs ~590 ms <i>fixed per
/// file</i> against blob because the cost is a round trip, not a read: 34 tables measured at
/// 26.8 s serially versus 9.3 s for the identical set in one statement. These tests run on local
/// parquet, where that difference is invisible — what they guard is the part that must never
/// change, which is the <b>verdict</b>.</para>
///
/// <para>The dangerous failure is a false "torn": the publisher refuses to publish, and for a
/// Deferred table it throws outright. A batched statement fails in its entirety when a single path
/// is missing, so every failure path has to fall back rather than answer.</para>
/// </summary>
public class ParquetFooterProbeTests
{
    private sealed class Fixture : IDisposable
    {
        internal SnapshotStore Store { get; }
        internal string Directory { get; }

        internal Fixture()
        {
            Directory = Path.Combine(Path.GetTempPath(), $"hawta-test-probe-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(Directory);
            Store = SnapshotStore.Open(new SnapshotStoreOptions { DatabasePath = ":memory:" });
        }

        /// <summary>A real parquet file of <paramref name="rows"/> rows, and its path.</summary>
        internal string Parquet(string name, int rows)
        {
            var path = Path.Combine(Directory, $"{name}.parquet");
            Store.Execute(
                $"COPY (SELECT * FROM range({rows})) TO '{path.Replace("'", "''")}' (FORMAT parquet)");
            return path;
        }

        public void Dispose()
        {
            Store.Dispose();
            try { System.IO.Directory.Delete(Directory, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void BatchedProbe_AgreesWithPerPathProbe_OnAnIntactSet()
    {
        using var fx = new Fixture();
        var a = fx.Parquet("a", 100);
        var b = fx.Parquet("b", 250);

        var probe = new SnapshotPublisher.ParquetFooterProbe(fx.Store, new[] { a, b });

        Assert.True(probe.IsIntact(new[] { a }, 100));
        Assert.True(probe.IsIntact(new[] { b }, 250));

        // Multi-file entries sum, exactly as the per-path probe does.
        Assert.True(probe.IsIntact(new[] { a, b }, 350));

        // And the per-path probe still says the same thing.
        Assert.True(SnapshotPublisher.ParquetIsIntact(fx.Store, new[] { a, b }, 350));
    }

    [Fact]
    public void BatchedProbe_ReportsTorn_OnAWrongRowCount()
    {
        using var fx = new Fixture();
        var a = fx.Parquet("a", 100);

        var probe = new SnapshotPublisher.ParquetFooterProbe(fx.Store, new[] { a });

        Assert.False(probe.IsIntact(new[] { a }, 99));
        Assert.False(probe.IsIntact(new[] { a }, 101));
    }

    [Fact]
    public void BatchedProbe_ReportsTorn_WhenTheFileIsMissing_WithoutThrowing()
    {
        using var fx = new Fixture();
        var missing = Path.Combine(fx.Directory, "never-written.parquet");

        var probe = new SnapshotPublisher.ParquetFooterProbe(fx.Store, new[] { missing });

        Assert.False(probe.IsIntact(new[] { missing }, 1));
    }

    /// <summary>
    /// The regression that matters. One unreadable path fails the WHOLE batched statement, so a
    /// naive implementation would report every table torn — refusing healthy publishes, and
    /// throwing outright on any Deferred table. Healthy paths must still come back intact.
    /// </summary>
    [Fact]
    public void OneBadCandidate_DoesNotMakeHealthyTablesLookTorn()
    {
        using var fx = new Fixture();
        var good = fx.Parquet("good", 100);
        var alsoGood = fx.Parquet("also-good", 7);
        var missing = Path.Combine(fx.Directory, "never-written.parquet");

        // The candidate set is what a baseline would hand over, and one entry has rotted.
        var probe = new SnapshotPublisher.ParquetFooterProbe(fx.Store, new[] { good, alsoGood, missing });

        Assert.True(probe.IsIntact(new[] { good }, 100));
        Assert.True(probe.IsIntact(new[] { alsoGood }, 7));

        // The rotted one is still correctly reported torn — the fallback answers, it does not guess.
        Assert.False(probe.IsIntact(new[] { missing }, 1));

        // And a wrong count on a healthy file is still torn after the fallback kicked in.
        Assert.False(probe.IsIntact(new[] { good }, 99));
    }

    [Fact]
    public void AnEmptyPathList_IsNeverIntact()
    {
        using var fx = new Fixture();
        var a = fx.Parquet("a", 10);

        var probe = new SnapshotPublisher.ParquetFooterProbe(fx.Store, new[] { a });

        Assert.False(probe.IsIntact(Array.Empty<string>(), 0));
    }

    /// <summary>
    /// An empty candidate set is the all-export publish: nothing was reusable, so nothing should be
    /// fetched. The probe must still answer correctly by falling straight through.
    /// </summary>
    [Fact]
    public void AnEmptyCandidateSet_FallsThroughToThePerPathProbe()
    {
        using var fx = new Fixture();
        var a = fx.Parquet("a", 42);

        var probe = new SnapshotPublisher.ParquetFooterProbe(fx.Store, Array.Empty<string>());

        Assert.True(probe.IsIntact(new[] { a }, 42));
        Assert.False(probe.IsIntact(new[] { a }, 41));
    }

    /// <summary>
    /// A path the cache was never told about still gets a correct answer, because an uncovered
    /// lookup takes the per-path route rather than reporting absence as torn.
    /// </summary>
    [Fact]
    public void APathOutsideTheCandidateSet_IsProbedDirectly()
    {
        using var fx = new Fixture();
        var known = fx.Parquet("known", 5);
        var unknown = fx.Parquet("unknown", 11);

        var probe = new SnapshotPublisher.ParquetFooterProbe(fx.Store, new[] { known });

        Assert.True(probe.IsIntact(new[] { unknown }, 11));
        Assert.False(probe.IsIntact(new[] { unknown }, 10));
    }
}
