using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ADP.Darlastic.Shared;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// The per-tenant rules that default OFF: <see cref="Flags.StructuredNames"/>,
/// <see cref="Flags.StrictTokenAlignment"/>, <see cref="Flags.NamelessNeedsCorroboration"/>,
/// <see cref="Flags.ConflictAwareClustering"/>, <see cref="Flags.OrgNameMerge"/> and
/// <see cref="Flags.TokenFolds"/>. Every rule is asserted twice — off reproduces the
/// default engine, on changes exactly the shape the rule targets — so a later scoring change that
/// makes a fixture vacuous fails here. Names are invented; phones and VINs are synthetic.
///
/// The flags are process-global, so this class runs in a collection that never runs in parallel
/// with another test class, and every test restores the flags through <see cref="FlagScope"/>.
/// </summary>
[Collection(EngineFlagsCollection.Name)]
public class NameStructureRulesTests
{
    private const double AutoMerge = 0.90;
    private const double Steward = 0.80;
    private const string Phone = "100200300";
    private const string OtherPhone = "100200399";

    private sealed class FlagScope : IDisposable
    {
        private readonly bool structured = Flags.StructuredNames, strict = Flags.StrictTokenAlignment,
            nameless = Flags.NamelessNeedsCorroboration, conflictAware = Flags.ConflictAwareClustering,
            orgMerge = Flags.OrgNameMerge, vinName = Flags.ServicedVinNameFloor;
        private readonly IReadOnlyList<(string From, string To)> folds = Flags.TokenFolds;

        public FlagScope(bool structured = false, bool strict = false, bool nameless = false, bool conflictAware = false,
            bool orgMerge = false, IReadOnlyList<(string From, string To)>? folds = null, bool vinName = false)
        {
            Flags.ServicedVinNameFloor = vinName;
            Flags.StructuredNames = structured;
            Flags.StrictTokenAlignment = strict;
            Flags.NamelessNeedsCorroboration = nameless;
            Flags.ConflictAwareClustering = conflictAware;
            Flags.OrgNameMerge = orgMerge;
            Flags.TokenFolds = folds ?? [];
        }

        public void Dispose()
        {
            Flags.StructuredNames = structured;
            Flags.StrictTokenAlignment = strict;
            Flags.NamelessNeedsCorroboration = nameless;
            Flags.ConflictAwareClustering = conflictAware;
            Flags.OrgNameMerge = orgMerge;
            Flags.TokenFolds = folds;
            Flags.ServicedVinNameFloor = vinName;
        }
    }

    /// <summary>Name doubles as RawName and NormName (pre-normalized, as in the sibling suites).</summary>
    private static RealRecord Rec(int idx, string id, string name, string? phone = Phone,
        string? given = null, string? family = null, VinLink[]? vins = null) =>
        new(idx, "src", id, name, name, phone is null ? [] : [phone], [], null, null,
            VinLinks: vins, GivenName: given, FamilyName: family);

    private static double Score(RealRecord a, RealRecord b, out MatchFlags f) => RealMatcher.Score(a, b, out f);

    private static RealMatcher.BlockingResult AllInOneBlock(int n) => new() { Blocks = [[.. Enumerable.Range(0, n)]] };

    private static bool SameIdentity(Merge.Result r, int a, int b)
    {
        int Find(int x) { var p = r.Parent; while (p[x] != x) x = p[x]; return x; }
        return Find(a) == Find(b);
    }

    // ---------------------------------------------------------------- StructuredNames

    [Fact]
    public void StructuredNames_SameGivenDifferentFamily_OnSharedPhone_FloorsByDefault_HeldWhenOn()
    {
        var a = Rec(0, "a", "torvin quellmar", given: "torvin", family: "quellmar");
        var b = Rec(1, "b", "torvin zaddeck", given: "torvin", family: "zaddeck");

        using (new FlagScope())
        {
            Assert.True(Score(a, b, out var f) >= AutoMerge, "default: shared phone + same given name floors at 0.91");
            Assert.True((f & MatchFlags.PhoneNameMerge) != 0);
        }
        using (new FlagScope(structured: true))
        {
            double s = Score(a, b, out var f);
            Assert.True(s <= RealMatcher.PartsConflictCap, $"namesakes on one phone must stay apart, got {s:F3}");
            Assert.True((f & MatchFlags.FamilyNameConflict) != 0);
            Assert.True((f & MatchFlags.PhoneNameMerge) == 0);
            Assert.True((CaseCategories.Categorize(a, b, s, f) & CaseCat.ConflictVetoed) != 0);
        }
    }

    [Fact]
    public void StructuredNames_DistinctGivenNamesSameFamily_HeldBelowTheStewardLine()
    {
        var a = Rec(0, "a", "torvin quellmar", given: "torvin", family: "quellmar");
        var b = Rec(1, "b", "ostrid quellmar", given: "ostrid", family: "quellmar");

        double before;
        using (new FlagScope()) before = Score(a, b, out _);
        using (new FlagScope(structured: true))
        {
            double s = Score(a, b, out var f);
            Assert.True(s < Steward, $"relatives on one phone are not steward work, got {s:F3}");
            Assert.True(s <= before);
            Assert.True((f & MatchFlags.GivenNameConflict) != 0);
            Assert.True((CaseCategories.Categorize(a, b, s, f) & CaseCat.ConflictVetoed) != 0);
        }
    }

    [Fact]
    public void StructuredNames_RomanizationDriftInTheFamilyName_IsNotAConflict()
    {
        // One consonant apart (k/g) — the drift the skeleton bar is there to admit.
        var a = Rec(0, "a", "torvin kelmarov", given: "torvin", family: "kelmarov");
        var b = Rec(1, "b", "torvin gelmarov", given: "torvin", family: "gelmarov");
        using var _ = new FlagScope(structured: true);
        Assert.True(Score(a, b, out var f) >= AutoMerge);
        Assert.True((f & (MatchFlags.GivenNameConflict | MatchFlags.FamilyNameConflict)) == 0);
    }

    [Fact]
    public void StructuredNames_GivenAndFamilyTypedIntoEachOthersField_IsNotAConflict()
    {
        var a = Rec(0, "a", "quellmar torvin", given: "quellmar", family: "torvin");
        var b = Rec(1, "b", "torvin quellmar", given: "torvin", family: "quellmar");
        using var _ = new FlagScope(structured: true);
        Assert.True(Score(a, b, out var f) >= AutoMerge);
        Assert.True((f & (MatchFlags.GivenNameConflict | MatchFlags.FamilyNameConflict)) == 0);
    }

    [Fact]
    public void StructuredNames_RecordsWithoutParts_ScoreExactlyAsBefore()
    {
        RealRecord[] recs =
        [
            Rec(0, "a", "torvin quellmar"), Rec(1, "b", "torvin zaddeck"), Rec(2, "c", "ostrid quellmar"),
            Rec(3, "d", "torvin quellmar", phone: OtherPhone), Rec(4, "e", ""),
        ];
        for (int i = 0; i < recs.Length; i++)
            for (int j = i + 1; j < recs.Length; j++)
            {
                double off, on; MatchFlags fOff, fOn;
                using (new FlagScope()) off = Score(recs[i], recs[j], out fOff);
                using (new FlagScope(structured: true)) on = Score(recs[i], recs[j], out fOn);
                Assert.Equal(off, on);
                Assert.Equal(fOff, fOn);
            }
    }

    // ---------------------------------------------------------------- StrictTokenAlignment

    [Fact]
    public void StrictTokenAlignment_AGivenNameContainedInTheFamilyName_AlignsOnlyOnce()
    {
        // 'brannoc' is contained in 'brannocov', so by default it aligns to it AND the family token
        // aligns to itself: the pair reads as consistent although the given names differ.
        var a = Rec(0, "a", "brannoc brannocov");
        var b = Rec(1, "b", "ostrid brannocov");

        using (new FlagScope())
        {
            Assert.True(RealMatcher.NameConsistent(a.NormName, b.NormName));
            Assert.True(Score(a, b, out _) >= AutoMerge);
        }
        using (new FlagScope(strict: true))
        {
            Assert.False(RealMatcher.NameConsistent(a.NormName, b.NormName));
            Assert.True(Score(a, b, out var f) < AutoMerge);
            Assert.True((f & MatchFlags.NameConsistent) == 0);
        }
    }

    [Theory]
    [InlineData("torvin quellmar", "quellmar torvin")]            // word order
    [InlineData("torvin quellmar", "torvin ardanovich quellmar")] // a middle token added
    [InlineData("torvin quellmar", "torvyn quelmar")]             // spelling drift on both tokens
    [InlineData("brannoc brannocov", "brannoc brannocov")]        // identical, overlapping tokens
    public void StrictTokenAlignment_KeepsGenuinelyConsistentNames(string x, string y)
    {
        using var _ = new FlagScope(strict: true);
        Assert.True(RealMatcher.NameConsistent(x, y));
    }

    // ---------------------------------------------------------------- NamelessNeedsCorroboration

    [Fact]
    public void Nameless_PhoneAlone_MergesByDefault_HeldWhenOn()
    {
        var a = Rec(0, "a", "");
        var b = Rec(1, "b", "torvin quellmar");

        using (new FlagScope()) Assert.True(Score(a, b, out _) >= AutoMerge, "default: the phone is the only signal and scores 1.00");
        using (new FlagScope(nameless: true))
        {
            double s = Score(a, b, out var f);
            Assert.True(s < Steward, $"a phone alone is no identity evidence for a nameless record, got {s:F3}");
            Assert.True((f & MatchFlags.NamelessHeld) != 0);
        }
    }

    [Fact]
    public void Nameless_PhonePlusSharedVin_IsUnchanged()
    {
        var vin = new VinLink("SYNTHVIN000000001", VinSource.Service, new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 10));
        var a = Rec(0, "a", "", vins: [vin]);
        var b = Rec(1, "b", "torvin quellmar", vins: [vin with { First = new DateOnly(2026, 3, 1), Last = new DateOnly(2026, 3, 1) }]);
        double off, on;
        using (new FlagScope()) off = Score(a, b, out _);
        using (new FlagScope(nameless: true)) on = Score(a, b, out _);
        Assert.Equal(off, on);
    }

    [Fact]
    public void Nameless_SoldVinAgainstDifferingPhones_FloorsByDefault_NotWhenOn()
    {
        var sale = new VinLink("SYNTHVIN000000002", VinSource.Sale, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1));
        var service = new VinLink("SYNTHVIN000000002", VinSource.Service, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1));
        var a = Rec(0, "a", "", phone: Phone, vins: [sale]);
        var b = Rec(1, "b", "torvin quellmar", phone: OtherPhone, vins: [service]);

        using (new FlagScope())
        {
            Assert.True(Score(a, b, out var f) >= AutoMerge);
            Assert.True((f & MatchFlags.VinSoldMerge) != 0);
        }
        using (new FlagScope(nameless: true))
        {
            Assert.True(Score(a, b, out var f) < AutoMerge);
            Assert.True((f & MatchFlags.NamelessHeld) != 0);
            Assert.True((f & MatchFlags.VinSoldMerge) == 0);
        }
    }

    // ---------------------------------------------------------------- OrgNameMerge

    [Fact]
    public void OrgNameMerge_WordOrderVariantsOfOneOrganisation_FloorOnlyWhenOn()
    {
        var a = Rec(0, "a", "brimvale holdings company");
        var b = Rec(1, "b", "holdinks brimvail company");   // word order + drift on both words

        using (new FlagScope())
        {
            Assert.True(Score(a, b, out var f) < AutoMerge, "default: an organisation line gets only the weighted base");
            Assert.True((f & MatchFlags.OrgLine) != 0);
            Assert.True((f & MatchFlags.NameConsistent) == 0);
        }
        using (new FlagScope(orgMerge: true))
        {
            Assert.True(Score(a, b, out var f) >= AutoMerge);
            Assert.True((f & (MatchFlags.OrgLine | MatchFlags.NameConsistent)) == (MatchFlags.OrgLine | MatchFlags.NameConsistent));
        }
    }

    [Theory]
    [InlineData("holdings brimvale")]           // all its words, no legal form
    [InlineData("brimvale holdings")]           // shortened: two of its three words
    public void OrgNameMerge_TheOrganisationWrittenWithoutItsLegalForm_Floors(string written)
    {
        var a = Rec(0, "a", "brimvale north holdings company");
        var b = Rec(1, "b", written);
        using var _ = new FlagScope(orgMerge: true);
        Assert.True(Score(a, b, out var f) >= AutoMerge);
        Assert.True((f & MatchFlags.NameConsistent) != 0);
    }

    [Fact]
    public void OrgNameMerge_WordsWrittenJoined_AreTheSameWords()
    {
        var a = Rec(0, "a", "brimvaleholdings company");
        var b = Rec(1, "b", "brimvale holdings");
        using var _ = new FlagScope(orgMerge: true);
        Assert.True(Score(a, b, out var f) >= AutoMerge);
        Assert.True((f & MatchFlags.NameConsistent) != 0);
    }

    [Theory]
    [InlineData("brimvale")]            // a lone word beside a company: the owner, a contact — never floored
    [InlineData("brimvale quellmar")]   // a person whose given name the company carries
    public void OrgNameMerge_APersonNameBesideACompany_IsNeverFloored(string person)
    {
        var a = Rec(0, "a", "brimvale company");
        var b = Rec(1, "b", person);
        double off, on; MatchFlags fOff, fOn;
        using (new FlagScope()) off = Score(a, b, out fOff);
        using (new FlagScope(orgMerge: true)) on = Score(a, b, out fOn);
        Assert.Equal(off, on);
        Assert.Equal(fOff, fOn);
        Assert.True(on < AutoMerge);
    }

    // ---------------------------------------------------------------- ServicedVinNameFloor

    private static VinLink Service(string vin, int month) =>
        new(vin, VinSource.Service, new DateOnly(2026, month, 1), new DateOnly(2026, month, 1));

    [Fact]
    public void ServicedVinName_SameFullNameSameCarDifferentPhones_FloorOnlyWhenOn()
    {
        var a = Rec(0, "a", "torvin ardanovich quellmar", phone: Phone, vins: [Service("SYNTHVIN000000003", 3)]);
        var b = Rec(1, "b", "torvin quellmar", phone: OtherPhone, vins: [Service("SYNTHVIN000000003", 9)]);

        using (new FlagScope()) Assert.True(Score(a, b, out _) < Steward, "default: a serviced VIN only corroborates");
        using (new FlagScope(vinName: true))
        {
            Assert.True(Score(a, b, out var f) >= AutoMerge);
            Assert.True((f & MatchFlags.ServicedVinNameMerge) != 0);
        }
    }

    [Theory]
    [InlineData("ostrid quellmar")]   // a relative brought the car in
    [InlineData("torvin")]            // one word is not a full name
    public void ServicedVinName_NeedsTheSameFullName(string other)
    {
        var a = Rec(0, "a", "torvin quellmar", phone: Phone, vins: [Service("SYNTHVIN000000004", 3)]);
        var b = Rec(1, "b", other, phone: OtherPhone, vins: [Service("SYNTHVIN000000004", 9)]);
        double off, on;
        using (new FlagScope()) off = Score(a, b, out _);
        using (new FlagScope(vinName: true)) on = Score(a, b, out var f);
        Assert.Equal(off, on);
        Assert.True(on < AutoMerge);
    }

    // ---------------------------------------------------------------- TokenFolds

    [Fact]
    public void TokenFolds_ATenantSpellingEquivalence_AlignsTokensOnlyWhenSupplied()
    {
        using (new FlagScope())
            Assert.False(RealMatcher.TokensAlign("chorvald", "jorvald"));
        using (new FlagScope(folds: [("ch", "j")]))
        {
            Assert.True(RealMatcher.TokensAlign("chorvald", "jorvald"));
            Assert.True(RealMatcher.NameConsistent("chorvald quellmar", "jorvald quellmar"));
            Assert.False(RealMatcher.TokensAlign("chorvald", "brimvale"), "a fold makes spellings equal, not names");
        }
    }

    // ---------------------------------------------------------------- ConflictAwareClustering

    /// <summary>A surname-only record and two relatives with distinct given names, all on one phone.</summary>
    private static List<RealRecord> BridgeCorpus() =>
    [
        Rec(0, "bridge", "quellmar", family: "quellmar"),
        Rec(1, "rel-a", "torvin quellmar", given: "torvin", family: "quellmar"),
        Rec(2, "rel-b", "ostrid quellmar", given: "ostrid", family: "quellmar"),
    ];

    [Fact]
    public void ConflictAware_APartialNameBridge_JoinsOneRelativeOnly()
    {
        var recs = BridgeCorpus();

        using (new FlagScope(structured: true))
        {
            var r = Merge.ClusterFromBlocks(recs, AllInOneBlock(recs.Count));
            Assert.True(SameIdentity(r, 1, 2), "default clustering chains the relatives through the bridge");
            Assert.Equal(0, r.ConflictBlockedEdges);
        }
        using (new FlagScope(structured: true, conflictAware: true))
        {
            var r = Merge.ClusterFromBlocks(recs, AllInOneBlock(recs.Count));
            Assert.False(SameIdentity(r, 1, 2), "the matcher said two people; no chain may join them");
            Assert.True(SameIdentity(r, 0, 1) ^ SameIdentity(r, 0, 2), "the bridge joins exactly one relative");
            Assert.Equal(1, r.CannotLinkPairs);
            Assert.Equal(1, r.ConflictBlockedEdges);
            Assert.Equal(2, r.IdentityCount);

            var again = Merge.ClusterFromBlocks(recs, AllInOneBlock(recs.Count));
            Assert.Equal(r.Parent.Select((_, i) => SameIdentity(r, 0, i)), again.Parent.Select((_, i) => SameIdentity(again, 0, i)));
        }
    }

    [Fact]
    public void ConflictAware_WithoutConflicts_ClustersExactlyAsBefore()
    {
        List<RealRecord> recs =
        [
            Rec(0, "a", "torvin quellmar"), Rec(1, "b", "torvin quellmar"), Rec(2, "c", "torvin ardan quellmar"),
            Rec(3, "d", "ostrid zaddeck", phone: OtherPhone), Rec(4, "e", "ostrid zaddeck", phone: OtherPhone),
        ];
        Merge.Result off, on;
        using (new FlagScope()) off = Merge.ClusterFromBlocks(recs, AllInOneBlock(recs.Count));
        using (new FlagScope(conflictAware: true)) on = Merge.ClusterFromBlocks(recs, AllInOneBlock(recs.Count));

        Assert.Equal(off.IdentityCount, on.IdentityCount);
        Assert.Equal(off.AutoMergeEdges, on.AutoMergeEdges);
        Assert.Equal(off.StewardPairs.Count, on.StewardPairs.Count);
        for (int i = 0; i < recs.Count; i++)
            for (int j = i + 1; j < recs.Count; j++)
                Assert.Equal(SameIdentity(off, i, j), SameIdentity(on, i, j));
        Assert.Equal(0, on.ConflictBlockedEdges);
    }

    [Fact]
    public void ConflictAware_AStewardSeparateVerdict_AlsoStopsTheTransitiveChain()
    {
        // Three spellings of one name on one phone: every pair auto-merges. A steward said the first
        // and last are different people; by default only that edge drops and the chain re-joins them.
        List<RealRecord> recs =
        [
            Rec(0, "a", "torvin quellmar"), Rec(1, "b", "torvin quellmar"), Rec(2, "c", "torvin quellmar"),
        ];
        var c = new StewardConstraints();
        c.Add(StewardVerdict.Separate, Merge.PairKey(recs[0], recs[2]));

        using (new FlagScope())
            Assert.True(SameIdentity(Merge.ClusterFromBlocks(recs, AllInOneBlock(3), 0.90, c), 0, 2));
        using (new FlagScope(conflictAware: true))
        {
            var r = Merge.ClusterFromBlocks(recs, AllInOneBlock(3), 0.90, c);
            Assert.False(SameIdentity(r, 0, 2));
            Assert.Equal(1, r.ConflictBlockedEdges);
        }
    }
}

/// <summary>Tests that flip process-global engine flags run alone, never beside another class.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EngineFlagsCollection
{
    public const string Name = "Engine flags (process-global)";
}
