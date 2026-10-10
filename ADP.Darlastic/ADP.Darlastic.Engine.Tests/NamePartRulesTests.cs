using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// The per-tenant rules that default OFF: <see cref="Flags.StrictNameParts"/> (with
/// <see cref="Flags.EstablishedNameParts"/>, <see cref="Flags.NamePartFolds"/>,
/// <see cref="Flags.GivenNameSuffixes"/> and <see cref="Flags.FamilyNameEndings"/>),
/// <see cref="Flags.FamilyNameSexEndings"/>, and the organisation additions under
/// <see cref="Flags.OrgNameMerge"/> (word forms, the sold-VIN floor, the unresolved-line cap, the
/// one-word name the corpus does not attest). Each rule is asserted off (the default engine) and on.
/// Names are invented; phones and VINs are synthetic. Runs in the engine-flags collection, and every
/// test restores the flags through <see cref="Scope"/>.
/// </summary>
[Collection(EngineFlagsCollection.Name)]
public class NamePartRulesTests
{
    private const double AutoMerge = 0.90;
    private const double Steward = 0.80;
    private const string Phone = "100200300";
    private const string OtherPhone = "100200399";

    private sealed class Scope : IDisposable
    {
        private readonly bool structured = Flags.StructuredNames, strictTokens = Flags.StrictTokenAlignment,
            orgMerge = Flags.OrgNameMerge, vinName = Flags.ServicedVinNameFloor, strictParts = Flags.StrictNameParts;
        private readonly IReadOnlySet<string> established = Flags.EstablishedNameParts;
        private readonly IReadOnlyList<(string From, string To)> folds = Flags.NamePartFolds, tokenFolds = Flags.TokenFolds;
        private readonly IReadOnlyList<string> suffixes = Flags.GivenNameSuffixes, endings = Flags.FamilyNameEndings;
        private readonly IReadOnlyList<(string Masculine, string Feminine)> sex = Flags.FamilyNameSexEndings;

        public Scope(bool structured = false, bool strictParts = false, string[]? established = null,
            (string, string)[]? folds = null, string[]? suffixes = null, string[]? endings = null,
            (string, string)[]? sex = null, bool orgMerge = false, bool vinName = false)
        {
            Flags.StructuredNames = structured;
            Flags.StrictTokenAlignment = false;
            Flags.TokenFolds = [];
            Flags.StrictNameParts = strictParts;
            Flags.EstablishedNameParts = new HashSet<string>(established ?? []);
            Flags.NamePartFolds = folds ?? [];
            Flags.GivenNameSuffixes = suffixes ?? [];
            Flags.FamilyNameEndings = endings ?? [];
            Flags.FamilyNameSexEndings = sex ?? [];
            Flags.OrgNameMerge = orgMerge;
            Flags.ServicedVinNameFloor = vinName;
        }

        public void Dispose()
        {
            Flags.StructuredNames = structured;
            Flags.StrictTokenAlignment = strictTokens;
            Flags.TokenFolds = tokenFolds;
            Flags.StrictNameParts = strictParts;
            Flags.EstablishedNameParts = established;
            Flags.NamePartFolds = folds;
            Flags.GivenNameSuffixes = suffixes;
            Flags.FamilyNameEndings = endings;
            Flags.FamilyNameSexEndings = sex;
            Flags.OrgNameMerge = orgMerge;
            Flags.ServicedVinNameFloor = vinName;
        }
    }

    private static RealRecord Rec(int idx, string id, string name, string? phone = Phone,
        string? given = null, string? family = null, VinLink[]? vins = null) =>
        new(idx, "src", id, name, name, phone is null ? [] : [phone], [], null, null,
            VinLinks: vins, GivenName: given, FamilyName: family);

    private static double Score(RealRecord a, RealRecord b, out MatchFlags f) => RealMatcher.Score(a, b, out f);

    private static VinLink Service(string vin, int month) =>
        new(vin, VinSource.Service, new DateOnly(2026, month, 1), new DateOnly(2026, month, 1));

    private static VinLink Sale(string vin, int month) =>
        new(vin, VinSource.Sale, new DateOnly(2026, month, 1), new DateOnly(2026, month, 1));

    // ---------------------------------------------------------------- StrictNameParts: given names

    [Fact]
    public void StrictNameParts_TwoAttestedGivenNamesOneConsonantApart_AreTwoPeople()
    {
        var a = Rec(0, "a", "dorvan quellmar", given: "dorvan", family: "quellmar");
        var b = Rec(1, "b", "dosvan quellmar", given: "dosvan", family: "quellmar");

        using (new Scope(structured: true))
            Assert.True(Score(a, b, out _) >= AutoMerge, "default: the given names align on Jaro-Winkler and the shared phone floors");
        using (new Scope(structured: true, strictParts: true, established: ["dorvan", "dosvan"]))
        {
            double s = Score(a, b, out var f);
            Assert.True(s <= RealMatcher.PartsConflictCap, $"two attested names on one family phone are siblings, got {s:F3}");
            Assert.True((f & MatchFlags.GivenNameConflict) != 0);
        }
    }

    [Fact]
    public void StrictNameParts_AnUnattestedSpelling_KeepsTheDefaultTest()
    {
        // One of the two is not attested in the corpus: it may be a typing slip of the other.
        var a = Rec(0, "a", "dorvan quellmar", given: "dorvan", family: "quellmar");
        var b = Rec(1, "b", "dosvan quellmar", given: "dosvan", family: "quellmar");
        double off, on;
        using (new Scope(structured: true)) off = Score(a, b, out _);
        using (new Scope(structured: true, strictParts: true, established: ["dorvan"])) on = Score(a, b, out var f);
        Assert.Equal(off, on);
        Assert.True(on >= AutoMerge);
    }

    [Theory]
    [InlineData("murvad", "murvat")]       // a voiced/unvoiced fold (d/t)
    [InlineData("torvinjon", "torvin")]    // an endearment ending set aside
    [InlineData("torvin", "turvin")]       // vowel drift: one consonant skeleton
    [InlineData("vlador", "vladorian")]    // a short form contained in the long one
    [InlineData("torvin ali", "torvinali")] // written with and without its space
    public void StrictNameParts_SpellingsOfOneAttestedName_StayOneName(string x, string y)
    {
        var a = Rec(0, "a", x + " quellmar", given: x, family: "quellmar");
        var b = Rec(1, "b", y + " quellmar", given: y, family: "quellmar");
        using var _ = new Scope(structured: true, strictParts: true, established: [x, y],
            folds: [("d", "t")], suffixes: ["jon"]);
        Score(a, b, out var f);
        Assert.True((f & (MatchFlags.GivenNameConflict | MatchFlags.FamilyNameConflict)) == 0, $"{x} / {y} are one name");
    }

    [Fact]
    public void StrictNameParts_CompoundGivenNamesSharingTheirFirstElement_AreTwoNames()
    {
        // 'brannulo' / 'brannkul' share 'brann': Jaro-Winkler aligns them, the skeletons do not.
        var a = Rec(0, "a", "brannulo quellmar", given: "brannulo", family: "quellmar");
        var b = Rec(1, "b", "brannkul quellmar", given: "brannkul", family: "quellmar");

        using (new Scope(structured: true)) Assert.True(Score(a, b, out _) >= AutoMerge);
        using (new Scope(structured: true, strictParts: true, established: ["brannulo", "brannkul"]))
        {
            Assert.True(Score(a, b, out var f) < Steward);
            Assert.True((f & MatchFlags.GivenNameConflict) != 0);
        }
    }

    // ---------------------------------------------------------------- StrictNameParts: family names

    [Fact]
    public void StrictNameParts_TwoAttestedFamilyNamesOneConsonantApart_AreNamesakes()
    {
        var a = Rec(0, "a", "torvin sabrinov", given: "torvin", family: "sabrinov");
        var b = Rec(1, "b", "torvin safrinov", given: "torvin", family: "safrinov");

        using (new Scope(structured: true)) Assert.True(Score(a, b, out _) >= AutoMerge);
        using (new Scope(structured: true, strictParts: true, established: ["sabrinov", "safrinov"]))
        {
            Assert.True(Score(a, b, out var f) <= RealMatcher.PartsConflictCap);
            Assert.True((f & MatchFlags.FamilyNameConflict) != 0);
        }
    }

    [Fact]
    public void StrictNameParts_FamilyNameEndingsExchanged_AreOneFamilyName()
    {
        var a = Rec(0, "a", "torvin quellmarov", given: "torvin", family: "quellmarov");
        var b = Rec(1, "b", "torvin quellmarsen", given: "torvin", family: "quellmarsen");
        string[] attested = ["quellmarov", "quellmarsen"];

        using (new Scope(structured: true, strictParts: true, established: attested))
        {
            Score(a, b, out var f);
            Assert.True((f & MatchFlags.FamilyNameConflict) != 0, "without the endings list the two attested forms conflict");
        }
        using (new Scope(structured: true, strictParts: true, established: attested, endings: ["ov", "sen"]))
        {
            Score(a, b, out var f);
            Assert.True((f & MatchFlags.FamilyNameConflict) == 0);
        }
    }

    [Fact]
    public void StrictNameParts_AFamilyNameContainedInAnother_IsNotAShortForm()
    {
        // Short forms are a given name's; a family name inside a longer one is another family.
        var a = Rec(0, "a", "torvin sabrov", given: "torvin", family: "sabrov");
        var b = Rec(1, "b", "torvin sabrovdurov", given: "torvin", family: "sabrovdurov");
        using var _ = new Scope(structured: true, strictParts: true, established: ["sabrov", "sabrovdurov"], endings: ["ov"]);
        Score(a, b, out var f);
        Assert.True((f & MatchFlags.FamilyNameConflict) != 0);
    }

    [Fact]
    public void StrictNameParts_RecordsWithoutParts_ScoreExactlyAsBefore()
    {
        RealRecord[] recs = [Rec(0, "a", "dorvan quellmar"), Rec(1, "b", "dosvan quellmar"), Rec(2, "c", "brannulo"), Rec(3, "d", "brannkul")];
        for (int i = 0; i < recs.Length; i++)
            for (int j = i + 1; j < recs.Length; j++)
            {
                double off, on; MatchFlags fOff, fOn;
                using (new Scope(structured: true)) off = Score(recs[i], recs[j], out fOff);
                using (new Scope(structured: true, strictParts: true, established: ["dorvan", "dosvan", "brannulo", "brannkul"])) on = Score(recs[i], recs[j], out fOn);
                Assert.Equal(off, on);
                Assert.Equal(fOff, fOn);
            }
    }

    // ---------------------------------------------------------------- StrictNameParts: serviced VIN

    [Fact]
    public void StrictNameParts_ServicedVinFloor_NeedsOneGivenNameWhateverTheCorpusSays()
    {
        var a = Rec(0, "a", "brannulo ardan quellmar", phone: Phone, given: "brannulo", family: "quellmar", vins: [Service("SYNTHVIN000000011", 3)]);
        var b = Rec(1, "b", "brannkul ardan quellmar", phone: OtherPhone, given: "brannkul", family: "quellmar", vins: [Service("SYNTHVIN000000011", 9)]);

        using (new Scope(vinName: true))
        {
            Assert.True(Score(a, b, out var f) >= AutoMerge, "default: every word aligns, so the serviced VIN floors");
            Assert.True((f & MatchFlags.ServicedVinNameMerge) != 0);
        }
        using (new Scope(vinName: true, strictParts: true))   // no attested names at all
        {
            Assert.True(Score(a, b, out var f) < AutoMerge, "two compound names that share a first element on one car are relatives");
            Assert.True((f & MatchFlags.ServicedVinNameMerge) == 0);
        }

        var c = Rec(2, "c", "brannulo quellmar", phone: OtherPhone, given: "brannulo", family: "quellmar", vins: [Service("SYNTHVIN000000011", 9)]);
        using (new Scope(vinName: true, strictParts: true))
            Assert.True(Score(a, c, out _) >= AutoMerge, "the same given name still floors");
    }

    // ---------------------------------------------------------------- FamilyNameSexEndings

    [Fact]
    public void FamilyNameSexEndings_ASurnameAndItsOtherSexForm_AreTwoPeople()
    {
        var a = Rec(0, "a", "quellmarov", family: "quellmarov");
        var b = Rec(1, "b", "quellmarova", family: "quellmarova");

        using (new Scope(structured: true)) Assert.True(Score(a, b, out _) >= AutoMerge, "default: the forms align by containment and the phone floors");
        using (new Scope(structured: true, sex: [("ov", "ova")]))
        {
            Assert.True(Score(a, b, out var f) <= RealMatcher.PartsConflictCap);
            Assert.True((f & MatchFlags.FamilyNameConflict) != 0);
        }
    }

    [Fact]
    public void FamilyNameSexEndings_WithAlignedGivenNames_TheEndingIsATypingSlip()
    {
        var a = Rec(0, "a", "torvin quellmarov", given: "torvin", family: "quellmarov");
        var b = Rec(1, "b", "torvin quellmarova", given: "torvin", family: "quellmarova");
        using var _ = new Scope(structured: true, sex: [("ov", "ova")]);
        Assert.True(Score(a, b, out var f) >= AutoMerge);
        Assert.True((f & MatchFlags.FamilyNameConflict) == 0);
    }

    [Fact]
    public void FamilyNameSexEndings_AlsoDecideTheCrosswiseCheck()
    {
        // The feminine form typed into the given field does not "explain" the masculine family name.
        var a = Rec(0, "a", "quellmarova ostrid", given: "quellmarova", family: "ostrid");
        var b = Rec(1, "b", "quellmarov", family: "quellmarov");
        using (new Scope(structured: true)) { Score(a, b, out var f); Assert.True((f & MatchFlags.FamilyNameConflict) == 0); }
        using (new Scope(structured: true, sex: [("ov", "ova")]))
        {
            Score(a, b, out var f);
            Assert.True((f & MatchFlags.FamilyNameConflict) != 0);
        }
    }

    // ---------------------------------------------------------------- OrgNameMerge additions

    [Fact]
    public void OrgNameMerge_TwoFormsOfOneWord_Align()
    {
        // An adjective and a genitive of one place name: a shared stem of six letters.
        var a = Rec(0, "a", "dorvanskoe legation company");
        var b = Rec(1, "b", "legation dorvanii company");

        using (new Scope()) Assert.True(Score(a, b, out _) < AutoMerge);
        using (new Scope(orgMerge: true))
        {
            Assert.True(Score(a, b, out var f) >= AutoMerge);
            Assert.True((f & MatchFlags.NameConsistent) != 0);
        }
    }

    [Fact]
    public void OrgNameMerge_LongTitlesDifferingInThePlaceName_AreHeldInTheStewardBand()
    {
        // Every word but the place name shared: a high name blend on a shared phone, two bodies.
        var a = Rec(0, "a", "quellmar regional executive authority of the state dorvanberg company");
        var b = Rec(1, "b", "quellmar regional executive authority of the state sabrinthal company");

        double off;
        using (new Scope()) off = Score(a, b, out _);
        Assert.True(off >= AutoMerge, $"fixture: the default weighted base must clear the line, got {off:F3}");
        using (new Scope(orgMerge: true))
        {
            double s = Score(a, b, out var f);
            Assert.True(s is >= Steward and <= RealMatcher.OrgUnresolvedCap, $"held in the steward band, got {s:F3}");
            Assert.True((f & MatchFlags.NameConsistent) == 0);
        }
    }

    [Fact]
    public void OrgNameMerge_OnASoldVin_TheOrganisationsOneWordNameFloors()
    {
        var a = Rec(0, "a", "brimvale company", phone: Phone, vins: [Sale("SYNTHVIN000000012", 2)]);
        var b = Rec(1, "b", "brimvail", phone: OtherPhone, vins: [Service("SYNTHVIN000000012", 6)]);

        using (new Scope()) Assert.True(Score(a, b, out _) < AutoMerge);
        using (new Scope(orgMerge: true))
        {
            Assert.True(Score(a, b, out var f) >= AutoMerge);
            Assert.True((f & MatchFlags.VinSoldMerge) != 0);
        }

        var person = Rec(2, "c", "brimvale quellmar", phone: OtherPhone, vins: [Service("SYNTHVIN000000012", 6)]);
        using (new Scope(orgMerge: true))
            Assert.True(Score(a, person, out _) < AutoMerge, "a person whose given name the company carries is not the company");
    }

    [Fact]
    public void OrgNameMerge_OnAPhone_AOneWordNameFloorsOnlyWhenTheCorpusDoesNotAttestIt()
    {
        var a = Rec(0, "a", "brimvale company");
        var b = Rec(1, "b", "brimvale");

        using (new Scope(orgMerge: true)) Assert.True(Score(a, b, out _) < AutoMerge, "without StrictNameParts a lone word is a contact's name");
        using (new Scope(orgMerge: true, strictParts: true))
            Assert.True(Score(a, b, out _) >= AutoMerge, "not an attested name: the company typed without its legal form");
        using (new Scope(orgMerge: true, strictParts: true, established: ["brimvale"]))
            Assert.True(Score(a, b, out _) < AutoMerge, "an attested given name beside a company is a contact person");
    }
}
