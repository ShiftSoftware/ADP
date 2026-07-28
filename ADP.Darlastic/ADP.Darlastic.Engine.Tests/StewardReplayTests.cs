using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ADP.Darlastic.Shared;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// Replay semantics for steward verdicts — the contract that turns the steward queue from a
/// logbook into a loop. Every test drives the REAL scorer through
/// <see cref="Merge.ClusterFromBlocks"/>; the fixtures are named for the score band they occupy
/// and each test asserts that band as a precondition, so a future scoring change that moves a
/// fixture out of its band fails loudly here instead of quietly making the test vacuous.
/// </summary>
public class StewardReplayTests
{
    // Fixture bands, measured against the live scorer (2026-07-26):
    //   siblings sharing a household phone  → 0.821  steward band [0.80, 0.90)
    //   name-chain slices sharing a phone   → 0.930  auto-merge  [0.90, ...]
    // Siblings are the archetypal band case: one phone, one father, two people — exactly the
    // question a human is supposed to answer and the engine is supposed to refuse to guess.
    private const double AutoMergeThreshold = 0.90;

    private static RealRecord Rec(int idx, string src, string id, string name, string? phone = null) =>
        new(idx, src, id, name, name, phone is null ? [] : [phone], [], null, null);

    /// <summary>Sibling A / sibling B (band 0.821), chain-slice C / chain-slice D (auto-merge 0.930),
    /// and one unrelated record used as a transitivity bridge.</summary>
    private static List<RealRecord> Corpus() =>
    [
        Rec(0, "dms", "sib-a",   "ahmed jafar salim", "7701234567"),
        Rec(1, "crm", "sib-b",   "omar jafar salim",  "7701234567"),
        Rec(2, "dms", "chain-c", "ahmed ali",         "7709998888"),
        Rec(3, "crm", "chain-d", "ahmed ali hassan",  "7709998888"),
        Rec(4, "app", "bridge",  "sara mahmood taha"),
    ];

    /// <summary>One block holding every record, so clustering considers every pair — the blocking
    /// heuristics are not under test here.</summary>
    private static RealMatcher.BlockingResult AllInOneBlock(int n) =>
        new() { Blocks = [[.. Enumerable.Range(0, n)]] };

    private static Merge.Result Cluster(List<RealRecord> recs, StewardConstraints? c = null) =>
        Merge.ClusterFromBlocks(recs, AllInOneBlock(recs.Count), AutoMergeThreshold, c);

    private static bool SameIdentity(Merge.Result r, int a, int b)
    {
        int Find(int x) { var p = r.Parent; while (p[x] != x) x = p[x]; return x; }
        return Find(a) == Find(b);
    }

    private static bool Queued(Merge.Result r, int a, int b) =>
        r.StewardPairs.Any(p => (p.A == a && p.B == b) || (p.A == b && p.B == a));

    // ---------------------------------------------------------------- preconditions

    [Fact]
    public void Fixtures_OccupyTheBandsTheTestsAssumeAndTheEngineQueuesTheAmbiguousPair()
    {
        var recs = Corpus();
        Assert.InRange(RealMatcher.Score(recs[0], recs[1]), Merge.StewardThreshold, AutoMergeThreshold);
        Assert.True(RealMatcher.Score(recs[2], recs[3]) >= AutoMergeThreshold);

        var r = Cluster(recs);
        Assert.True(Queued(r, 0, 1), "the sibling pair is the steward's question");
        Assert.True(SameIdentity(r, 2, 3), "the chain-slice pair is the engine's own auto-merge");
        Assert.False(SameIdentity(r, 0, 1), "a band pair must never be merged without a verdict");
    }

    [Fact]
    public void NoConstraints_ChangesNothing()
    {
        var recs = Corpus();
        var before = Cluster(recs);
        var after = Cluster(recs, StewardConstraints.None);

        Assert.Equal(before.IdentityCount, after.IdentityCount);
        Assert.Equal(before.StewardPairs.Count, after.StewardPairs.Count);
        Assert.Equal(0, after.StewardForcedEdges);
        Assert.Equal(0, after.StewardVetoedEdges);
        Assert.Equal(0, after.StewardSuppressedPairs);
    }

    // ---------------------------------------------------------------- the two constraints

    [Fact]
    public void MergeVerdict_UnionsAPairTheEngineScoredBelowTheLine()
    {
        var recs = Corpus();
        var c = new StewardConstraints();
        c.Add(StewardVerdict.Merge, Merge.PairKey(recs[0], recs[1]));

        var r = Cluster(recs, c);

        Assert.True(SameIdentity(r, 0, 1), "a steward saying 'same' outranks a sub-threshold score");
        Assert.Equal(1, r.StewardForcedEdges);
        Assert.False(Queued(r, 0, 1), "a decided pair must not come back");
    }

    [Fact]
    public void SeparateVerdict_VetoesAnEdgeTheEngineWouldHaveAutoMerged()
    {
        var recs = Corpus();
        var c = new StewardConstraints();
        c.Add(StewardVerdict.Separate, Merge.PairKey(recs[2], recs[3]));

        var r = Cluster(recs, c);

        Assert.False(SameIdentity(r, 2, 3), "a steward saying 'different' outranks a 0.93 score");
        Assert.Equal(1, r.StewardVetoedEdges);
    }

    [Fact]
    public void SeparateVerdict_KeepsTheBandPairOutOfTheQueueForever()
    {
        var recs = Corpus();
        var c = new StewardConstraints();
        c.Add(StewardVerdict.Separate, Merge.PairKey(recs[0], recs[1]));

        var r = Cluster(recs, c);

        Assert.False(Queued(r, 0, 1), "re-asking an answered question is the complacency failure");
        Assert.False(SameIdentity(r, 0, 1));
        Assert.Equal(1, r.StewardSuppressedPairs);
    }

    [Fact]
    public void MergeVerdict_PropagatesTransitively_SettlingOtherQueuedPairs()
    {
        // The steward never rules on the siblings directly; they rule that each sibling is the same
        // person as the bridge record. The band pair is then settled by transitivity and must leave
        // the queue — a forced edge has to behave exactly like an auto-merge edge downstream.
        var recs = Corpus();
        var c = new StewardConstraints();
        c.Add(StewardVerdict.Merge, Merge.PairKey(recs[0], recs[4]));
        c.Add(StewardVerdict.Merge, Merge.PairKey(recs[1], recs[4]));

        var r = Cluster(recs, c);

        Assert.True(SameIdentity(r, 0, 1));
        Assert.False(Queued(r, 0, 1), "settled by the steward's own edges, so no longer a question");
        Assert.Equal(2, r.StewardForcedEdges);
    }

    [Fact]
    public void SeparateVerdict_DoesNotSurviveATransitiveMerge_TheDocumentedV0Boundary()
    {
        // Characterization, not aspiration: 'separate' vetoes the DIRECT edge only. If the steward
        // also merges both sides into a third record, the two land in one identity anyway. A true
        // veto needs constrained clustering; the remedy for a bad transitive merge is the split
        // action, a later steward slice. This test exists so the boundary is a decision on record
        // rather than a surprise discovered in production.
        var recs = Corpus();
        var c = new StewardConstraints();
        c.Add(StewardVerdict.Separate, Merge.PairKey(recs[2], recs[3]));
        c.Add(StewardVerdict.Merge, Merge.PairKey(recs[2], recs[4]));
        c.Add(StewardVerdict.Merge, Merge.PairKey(recs[3], recs[4]));

        var r = Cluster(recs, c);

        Assert.Equal(1, r.StewardVetoedEdges);
        Assert.True(SameIdentity(r, 2, 3), "transitivity still unifies them — the known v0 boundary");
    }

    // ---------------------------------------------------------------- robustness

    [Fact]
    public void ConstraintNamingAnAbsentRecord_IsCountedNotFatal()
    {
        var recs = Corpus();
        var c = new StewardConstraints();
        c.Add(StewardVerdict.Merge, "dms:gone~crm:also-gone");

        var r = Cluster(recs, c);

        Assert.Equal(1, r.StewardUnmatchedConstraints);
        Assert.Equal(0, r.StewardForcedEdges);
        Assert.True(Queued(r, 0, 1), "an inapplicable constraint must not disturb the rest of the run");
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-separator")]
    [InlineData("~")]
    [InlineData("dms:a~")]
    [InlineData("dms:a~crm:b~app:c")]   // ambiguous — refused rather than guessed
    [InlineData("dmsa~crmb")]           // no colons
    public void MalformedPairKey_IsIgnoredWithoutThrowing(string pairKey)
    {
        var recs = Corpus();
        var c = new StewardConstraints();
        c.Add(StewardVerdict.Merge, pairKey);

        var r = Cluster(recs, c);

        Assert.Equal(0, r.StewardForcedEdges);
        Assert.True(Queued(r, 0, 1));
    }

    [Fact]
    public void ContradictoryVerdicts_ResolveToSeparate()
    {
        // Between two live instructions the engine takes the one that does not fabricate an
        // identity: an over-merge is expensive to undo, a missed merge re-surfaces on its own.
        var recs = Corpus();
        var key = Merge.PairKey(recs[0], recs[1]);

        var mergeFirst = new StewardConstraints();
        mergeFirst.Add(StewardVerdict.Merge, key);
        mergeFirst.Add(StewardVerdict.Separate, key);

        var separateFirst = new StewardConstraints();
        separateFirst.Add(StewardVerdict.Separate, key);
        separateFirst.Add(StewardVerdict.Merge, key);

        Assert.False(SameIdentity(Cluster(recs, mergeFirst), 0, 1));
        Assert.False(SameIdentity(Cluster(recs, separateFirst), 0, 1));
    }

    // ---------------------------------------------------------------- the drift guard

    [Fact]
    public void PairKey_IsIndependentOfArgumentOrder()
    {
        var recs = Corpus();
        Assert.Equal(Merge.PairKey(recs[0], recs[1]), Merge.PairKey(recs[1], recs[0]));
    }

    [Fact]
    public void QueuedPairKey_RoundTripsBackIntoAnAppliedConstraint()
    {
        // The one failure mode no output would reveal: if the key the queue publishes is not the
        // key replay resolves, every verdict silently becomes a no-op. So take the key exactly as a
        // steward surface would receive it — built from the queued pair — and feed it back.
        var recs = Corpus();
        var queued = Cluster(recs).StewardPairs.Single();
        var keyAsPublished = Merge.PairKey(recs[queued.A], recs[queued.B]);

        var c = new StewardConstraints();
        c.Add(StewardVerdict.Merge, keyAsPublished);
        var r = Cluster(recs, c);

        Assert.Equal(1, r.StewardForcedEdges);
        Assert.Equal(0, r.StewardUnmatchedConstraints);
        Assert.True(SameIdentity(r, queued.A, queued.B));
    }

    [Fact]
    public void DeferAndRelease_AreKnownVerdictsThatConstrainNothing()
    {
        Assert.True(StewardVerdict.IsKnown(StewardVerdict.Defer));
        Assert.True(StewardVerdict.IsKnown(StewardVerdict.Release));
        Assert.False(StewardVerdict.IsConstraint(StewardVerdict.Defer));
        Assert.False(StewardVerdict.IsConstraint(StewardVerdict.Release));

        var recs = Corpus();
        var c = new StewardConstraints();
        c.Add(StewardVerdict.Defer, Merge.PairKey(recs[0], recs[1]));
        c.Add(StewardVerdict.Release, Merge.PairKey(recs[0], recs[1]));

        Assert.True(c.IsEmpty, "only merge/separate are engine constraints");
        Assert.True(Queued(Cluster(recs, c), 0, 1), "a deferred pair stays the steward's open question");
    }
}
