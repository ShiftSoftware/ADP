using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// Per-golden reasons: the rules, the bit layout they share with the cluster categories, and the
/// default-off switch on the pair walk. Fixtures are synthetic and asserted into their score bands
/// first, so a scoring change that moves one fails here instead of making a test vacuous.
/// </summary>
public class GoldenReasonTests
{
    private const double AutoMerge = 0.90;

    private static RealRecord Rec(int idx, string src, string id, string name, string? phone = null) =>
        new(idx, src, id, name, name, phone is null ? [] : [phone], [], null, null);

    /// <summary>
    /// 0/1 — one person written as a shorter and a longer name, one phone (auto-merge).
    /// 2/3 — two people on one household phone, same family name, different first names (band).
    /// 4   — a record with nothing in common with anyone (single).
    /// 5/6 — a company line and a person sharing its phone.
    /// </summary>
    private static List<RealRecord> Corpus() =>
    [
        Rec(0, "src-a", "slice-1", "kestrel morrow",        "7700000101"),
        Rec(1, "src-b", "slice-2", "kestrel morrow tanager", "7700000101"),
        Rec(2, "src-a", "home-1",  "marek varden quill tanager",  "7700000202"),
        Rec(3, "src-b", "home-2",  "tobias varden quill tanager", "7700000202"),
        Rec(4, "src-c", "alone",   "pellam rook",           "7700000303"),
        Rec(5, "src-a", "org-1",   "sample parts stock",    "7700000404") with { IsOrgPlaceholder = true },
        Rec(6, "src-b", "org-2",   "brindle hawk",          "7700000404"),
    ];

    private static RealMatcher.BlockingResult OneBlock(int n) => new() { Blocks = [[.. Enumerable.Range(0, n)]] };

    private static Merge.Result Cluster(List<RealRecord> recs, bool evidence, StewardConstraints? c = null) =>
        Merge.ClusterFromBlocks(recs, OneBlock(recs.Count), AutoMerge, c, perGoldenEvidence: evidence);

    private static GoldenReason R(Merge.Result r, int idx) => (GoldenReason)r.RecordReasons![idx];

    [Fact]
    public void Fixtures_OccupyTheBandsTheTestsAssume()
    {
        var recs = Corpus();
        Assert.True(RealMatcher.Score(recs[0], recs[1]) >= AutoMerge, "the name slices auto-merge");
        Assert.InRange(RealMatcher.Score(recs[2], recs[3]), Merge.StewardThreshold, AutoMerge);
    }

    // ---------------------------------------------------------------- bit layout

    [Fact]
    public void ReasonBits_NeverOverlapClusterCategoryBits()
    {
        Assert.Equal(0, GoldenReasons.AllMask & GoldenReasons.ClusterCatMask);
        foreach (var c in Enum.GetValues<ClusterCat>())
            Assert.True((long)c <= GoldenReasons.ClusterCatMask, $"{c} must stay in the low 32 bits");
    }

    [Fact]
    public void EveryClusterCategory_HasItsMirrorReason_WithTheSameLabel()
    {
        foreach (var c in Enum.GetValues<ClusterCat>().Where(c => c != ClusterCat.None))
        {
            var mirror = GoldenReasons.FromClusterCats(c);
            Assert.True(Enum.IsDefined(mirror), $"{c} has no mirrored reason");
            Assert.Equal(CaseLabels.Clusters[c], CaseLabels.Reasons[mirror]);
        }
    }

    [Fact]
    public void PackedValue_RoundTripsBothHalves()
    {
        var cats = ClusterCat.CrossDealer | ClusterCat.Large;
        var reasons = GoldenReason.NeedsReview | GoldenReason.SeveralSources;
        long staged = GoldenReasons.Pack(cats, reasons);
        Assert.Equal(cats, GoldenReasons.ClusterCatsOf(staged));
        Assert.Equal(reasons, GoldenReasons.ReasonsOf(staged));
    }

    // ---------------------------------------------------------------- rules

    [Theory]
    [InlineData(MatchFlags.VinSoldMerge | MatchFlags.VinSoldOverlap, GoldenReason.MergedByVehicle)]
    [InlineData(MatchFlags.EmailMerge | MatchFlags.EmailExact, GoldenReason.MergedByEmail)]
    [InlineData(MatchFlags.SameAsMerge | MatchFlags.SameAsRef, GoldenReason.MergedByReference)]
    [InlineData(MatchFlags.PhoneExact | MatchFlags.NameConsistent, GoldenReason.MergedByPhoneAndName)]
    [InlineData(MatchFlags.PhoneExact | MatchFlags.PhoneNameMerge, GoldenReason.MergedByPhoneAndName)]
    [InlineData(MatchFlags.NamesBoth | MatchFlags.IdEqual, GoldenReason.MergedBySimilarDetails)]
    public void MergeEdge_IsExplainedByTheFloorThatFired(MatchFlags flags, GoldenReason expected) =>
        Assert.Equal(expected, GoldenReasons.OfMergeEdge(flags));

    // ---------------------------------------------------------------- phone without a name (option)

    private static readonly GoldenEvidenceOptions PhoneWithoutName = new() { PhoneWithoutNameReason = true };

    [Fact]
    public void PhoneOnlyEdge_IsMatchingDetails_ByDefault() =>
        Assert.Equal(GoldenReason.MergedBySimilarDetails,
            GoldenReasons.OfMergeEdge(MatchFlags.PhonesBoth | MatchFlags.PhoneExact));

    [Fact]
    public void PhoneOnlyEdge_IsNamed_UnderTheOption()
    {
        Assert.Equal(GoldenReason.MergedByPhoneWithoutName,
            GoldenReasons.OfMergeEdge(MatchFlags.PhonesBoth | MatchFlags.PhoneExact, PhoneWithoutName));
        // Names on both sides, a company line, or a floor that fired: not a merge without a name.
        Assert.Equal(GoldenReason.MergedBySimilarDetails,
            GoldenReasons.OfMergeEdge(MatchFlags.NamesBoth | MatchFlags.PhoneExact, PhoneWithoutName));
        Assert.Equal(GoldenReason.MergedBySimilarDetails,
            GoldenReasons.OfMergeEdge(MatchFlags.PhoneExact | MatchFlags.OrgLine, PhoneWithoutName));
        Assert.Equal(GoldenReason.MergedByReference,
            GoldenReasons.OfMergeEdge(MatchFlags.PhoneExact | MatchFlags.SameAsMerge | MatchFlags.SameAsRef, PhoneWithoutName));
    }

    [Fact]
    public void PhoneWithoutName_IsStaged_OnlyUnderTheOption_AndClustersTheSame()
    {
        List<RealRecord> recs =
        [
            Rec(0, "src-a", "named-1", "kestrel morrow", "7700000505"),
            Rec(1, "src-b", "blank-1", "",               "7700000505"),
        ];
        Assert.True(RealMatcher.Score(recs[0], recs[1]) >= AutoMerge, "a phone alone combines a nameless record at the defaults");

        var byDefault = Cluster(recs, evidence: true);
        var named = Merge.ClusterFromBlocks(recs, OneBlock(recs.Count), AutoMerge, null, perGoldenEvidence: true,
            evidenceOptions: PhoneWithoutName);

        Assert.True(R(byDefault, 1).HasFlag(GoldenReason.MergedBySimilarDetails));
        Assert.False(R(byDefault, 1).HasFlag(GoldenReason.MergedByPhoneWithoutName));
        Assert.True(R(named, 0).HasFlag(GoldenReason.MergedByPhoneWithoutName));
        Assert.True(R(named, 1).HasFlag(GoldenReason.MergedByPhoneWithoutName));
        Assert.False(R(named, 1).HasFlag(GoldenReason.MergedBySimilarDetails));
        Assert.Equal(byDefault.Parent, named.Parent);
        Assert.Equal(byDefault.AutoMergeEdgeList, named.AutoMergeEdgeList);
    }

    /// <summary>
    /// Every way an auto-merge edge can clear the line — each floor, and the base-score shapes a
    /// match-quality harness tells apart (company line, phone with a name missing, corroborated or
    /// plain weighted score) — is explained by a reason with a plain label, under the default wording
    /// and under the phone-without-name option. A floor is never reported as "matching details".
    /// </summary>
    [Theory]
    [InlineData(MatchFlags.NamesBoth | MatchFlags.PhoneExact | MatchFlags.NameConsistent, true)]
    [InlineData(MatchFlags.VinSoldMerge | MatchFlags.VinSoldOverlap, true)]
    [InlineData(MatchFlags.ServicedVinNameMerge | MatchFlags.VinServiced, true)]
    [InlineData(MatchFlags.EmailMerge | MatchFlags.EmailExact, true)]
    [InlineData(MatchFlags.SameAsMerge | MatchFlags.SameAsRef, true)]
    [InlineData(MatchFlags.NamesBoth | MatchFlags.PhoneExact | MatchFlags.PhoneNameMerge, true)]
    [InlineData(MatchFlags.NamesBoth | MatchFlags.PhoneExact | MatchFlags.ChainSliceRescue | MatchFlags.AddressBoost, true)]
    [InlineData(MatchFlags.NamesBoth | MatchFlags.PhoneExact | MatchFlags.OrgLine | MatchFlags.NameConsistent, true)]
    [InlineData(MatchFlags.NamesBoth | MatchFlags.PhoneExact | MatchFlags.OrgLine, false)]
    [InlineData(MatchFlags.PhonesBoth | MatchFlags.PhoneExact, false)]
    [InlineData(MatchFlags.NamesBoth | MatchFlags.AddressBoost | MatchFlags.DobEqual, false)]
    [InlineData(MatchFlags.NamesBoth | MatchFlags.IdEqual, false)]
    public void EveryMergeLever_IsExplainedByALabelledReason(MatchFlags flags, bool floor)
    {
        foreach (var options in new[] { GoldenEvidenceOptions.Default, PhoneWithoutName })
        {
            var r = GoldenReasons.OfMergeEdge(flags, options);
            Assert.NotEqual(GoldenReason.None, r);
            foreach (var bit in GoldenReasons.All.Where(b => (r & b) != 0))
                Assert.True(CaseLabels.Reasons.ContainsKey(bit), $"{bit} has no plain label");
            if (floor) Assert.False(r.HasFlag(GoldenReason.MergedBySimilarDetails), $"{flags}: a floor reads as matching details");
        }
    }

    [Fact]
    public void MergeEdge_OnSeveralFloors_CarriesEachOfThem() =>
        Assert.Equal(GoldenReason.MergedByVehicle | GoldenReason.MergedByPhoneAndName,
            GoldenReasons.OfMergeEdge(MatchFlags.VinSoldMerge | MatchFlags.PhoneExact | MatchFlags.NameConsistent));

    [Fact]
    public void NearMiss_IsADuplicateOnlyAcrossCustomers()
    {
        Assert.Equal(GoldenReason.PossibleDuplicate, GoldenReasons.OfPair(CaseCat.NearMiss, sameCustomer: false));
        Assert.Equal(GoldenReason.None, GoldenReasons.OfPair(CaseCat.NearMiss, sameCustomer: true));
        Assert.Equal(GoldenReason.SamePhoneDifferentNames, GoldenReasons.OfPair(CaseCat.ConflictVetoed, sameCustomer: true));
        Assert.Equal(GoldenReason.VehicleChangedOwner, GoldenReasons.OfPair(CaseCat.VinTransfer, sameCustomer: false));
    }

    [Fact]
    public void Reader_DerivesWhatItCan_FromWhatWasStaged()
    {
        long withReasons = GoldenReasons.Pack(ClusterCat.CrossDealer, GoldenReason.SeveralSources | GoldenReason.NeedsReview);
        Assert.Equal(GoldenReason.SeveralSources | GoldenReason.NeedsReview,
            GoldenReasons.ForReader(withReasons, memberCount: 2, reasonsStaged: true));

        // A registry staged without evidence: the cluster bits still say something.
        Assert.Equal(GoldenReason.SeveralSources | GoldenReason.LargeGroup,
            GoldenReasons.ForReader((long)(ClusterCat.CrossDealer | ClusterCat.Large), memberCount: 6, reasonsStaged: false));

        // No summary row.
        Assert.Equal(GoldenReason.SingleRecord, GoldenReasons.ForReader(null, memberCount: 1, reasonsStaged: false));
        Assert.Equal(GoldenReason.SingleRecord | GoldenReason.NotInLatestRun,
            GoldenReasons.ForReader(null, memberCount: 1, reasonsStaged: true));
        Assert.Equal(GoldenReason.NotInLatestRun, GoldenReasons.ForReader(null, memberCount: 3, reasonsStaged: true));
    }

    // ---------------------------------------------------------------- the switch on the pair walk

    [Fact]
    public void Default_CollectsNothing_AndChangesNothingElse()
    {
        var recs = Corpus();
        var off = Cluster(recs, evidence: false);
        var on = Cluster(recs, evidence: true);

        Assert.Null(off.RecordReasons);
        Assert.NotNull(on.RecordReasons);

        // Everything the resolve stages from the walk is identical with the switch on.
        Assert.Equal(off.Parent, on.Parent);
        Assert.Equal(off.IdentityCount, on.IdentityCount);
        Assert.Equal(off.AutoMergeEdges, on.AutoMergeEdges);
        Assert.Equal(off.StewardPairs, on.StewardPairs);
        Assert.Equal(off.AutoMergeEdgeList, on.AutoMergeEdgeList);
        Assert.Equal(off.Catalog, on.Catalog);
        Assert.Equal(off.CategoryCounts, on.CategoryCounts);
    }

    [Fact]
    public void Evidence_ExplainsEachRecordsCustomer()
    {
        var r = Cluster(Corpus(), evidence: true);

        Assert.True(R(r, 0).HasFlag(GoldenReason.MergedByPhoneAndName));
        Assert.True(R(r, 1).HasFlag(GoldenReason.MergedByPhoneAndName));

        Assert.True(R(r, 2).HasFlag(GoldenReason.NeedsReview));
        Assert.True(R(r, 3).HasFlag(GoldenReason.NeedsReview));
        Assert.False(R(r, 2).HasFlag(GoldenReason.MergedByPhoneAndName), "a queued pair is not a merge");

        Assert.Equal(GoldenReason.None, R(r, 4));

        Assert.True(R(r, 5).HasFlag(GoldenReason.CompanyOrFleet), "an organisation record is a company row");
    }

    [Fact]
    public void ReviewerMerge_IsReported_AndNoLongerNeedsReview()
    {
        var recs = Corpus();
        var c = new StewardConstraints();
        c.Add("merge", Merge.PairKey(recs[2], recs[3]));

        var r = Cluster(recs, evidence: true, c);

        Assert.True(R(r, 2).HasFlag(GoldenReason.MergedByReviewer));
        Assert.True(R(r, 3).HasFlag(GoldenReason.MergedByReviewer));
        Assert.False(R(r, 2).HasFlag(GoldenReason.NeedsReview), "a decided pair leaves the queue");
    }
}
