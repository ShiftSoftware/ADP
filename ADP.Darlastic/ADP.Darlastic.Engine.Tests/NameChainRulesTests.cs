using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// The per-tenant rules for name chains, organisation lines and linked records, all OFF by default:
/// <see cref="Flags.GivenNameChainFit"/>, <see cref="Flags.TwoWordChainFit"/>, <see cref="Flags.NamedMergeNeedsFloor"/>,
/// <see cref="Flags.ShiftedChainConflict"/>, <see cref="Flags.SameAsRefPhoneFloor"/>, <see cref="Flags.OrgNamesBothSides"/>,
/// <see cref="Flags.CrossScriptLetters"/>, <see cref="Flags.JoinedNames"/> and <see cref="Flags.ServicedVinNeverLowers"/>.
/// Each rule is asserted both ways: off reproduces the default engine, on changes the shape it targets.
/// Every name is invented (in both scripts); phones and VINs are synthetic. Settings are applied through
/// <see cref="MatchSettings.Use"/> scopes, so nothing process-wide changes.
/// </summary>
[Collection(EngineFlagsCollection.Name)]
public class NameChainRulesTests
{
    private const double AutoMerge = 0.90;
    private const string Phone = "100200300";
    private const string OtherPhone = "100200399";
    private const string Vin = "SYNTHVIN000000017";

    private static readonly MatchSettings ChainFit = MatchSettings.Defaults with { GivenNameChainFit = true };
    private static readonly MatchSettings TwoWord = ChainFit with { TwoWordChainFit = true };
    private static readonly MatchSettings FloorRequired = MatchSettings.Defaults with { NamedMergeNeedsFloor = true };
    private static readonly MatchSettings Shifted = MatchSettings.Defaults with { ShiftedChainConflict = true };
    private static readonly MatchSettings SameAsPhone = MatchSettings.Defaults with { SameAsRefPhoneFloor = true };
    private static readonly MatchSettings OrgBoth = MatchSettings.Defaults with { OrgNamesBothSides = true };
    private static readonly MatchSettings Cross = MatchSettings.Defaults with { CrossScriptNames = true };
    private static readonly MatchSettings CrossLetters = Cross with { CrossScriptLetters = true };
    private static readonly MatchSettings Joined = MatchSettings.Defaults with { JoinedNames = true };
    private static readonly MatchSettings ServicedVin = MatchSettings.Defaults with { ServicedVinNeverLowers = true };

    private static RealRecord Rec(int idx, string id, string raw, string? phone = Phone, VinLink[]? vins = null, string source = "src") =>
        new(idx, source, id, raw, Norm.Name(raw), phone is null ? [] : [phone], [], null, null, VinLinks: vins);

    private static double Score(RealRecord a, RealRecord b, out MatchFlags f, MatchSettings s)
    {
        using (s.Use()) return RealMatcher.Score(a, b, out f);
    }

    private static MatchFlags Flag(RealRecord a, RealRecord b, MatchSettings s)
    {
        Score(a, b, out var f, s);
        return f;
    }

    // ---------------------------------------------------------------- GivenNameChainFit / TwoWordChainFit

    [Fact]
    public void ChainFit_SameGivenNameWithAnotherFather_LosesThePhoneFloorOnlyWhenOn()
    {
        // Invented: "Torvin Ostrid Brannoc" and "Torvin Pemdal" on one phone — one first name, another father.
        var a = Rec(0, "a", "Torvin Ostrid Brannoc");
        var b = Rec(1, "b", "Torvin Pemdal");
        double off = Score(a, b, out var fOff, MatchSettings.Defaults);
        Assert.True(off >= AutoMerge, $"default: the phone + first-name floor merges, got {off:F3}");
        Assert.True((fOff & MatchFlags.PhoneNameMerge) != 0);

        double on = Score(a, b, out var fOn, ChainFit);
        Assert.True(on < AutoMerge, $"on: no later name fits, got {on:F3}");
        Assert.True((fOn & MatchFlags.PhoneNameMerge) == 0);
        Assert.True((fOn & MatchFlags.GivenNameConflict) == 0, "not a cannot-link: a person decides");

        // First names one dropped first consonant apart look alike to the loose first-name test, but a
        // romanization slip changes a letter, it does not drop the first one: two brothers on one phone.
        var brother1 = Rec(2, "c", "Zohammek Ostrid Brannoc");
        var brother2 = Rec(3, "d", "Ahmek Ostrid Brannoc");
        Assert.True((Flag(brother1, brother2, MatchSettings.Defaults) & MatchFlags.PhoneNameMerge) != 0);
        Assert.True((Flag(brother1, brother2, ChainFit) & MatchFlags.PhoneNameMerge) == 0);

        // Also when one first name contains the other (the shared token rule reads them as one token).
        var full = Rec(4, "e", "Ghorvane Pemdal");
        Assert.True((Flag(full, Rec(5, "f", "Horvane"), MatchSettings.Defaults) & MatchFlags.PhoneNameMerge) != 0);
        Assert.True((Flag(full, Rec(6, "g", "Horvane"), ChainFit) & MatchFlags.PhoneNameMerge) == 0);
        // A dropped h is romanization drift, not another name.
        Assert.True((Flag(Rec(7, "h", "Horvane Pemdal"), Rec(8, "i", "Orvane"), ChainFit) & MatchFlags.PhoneNameMerge) != 0);
    }

    [Theory]
    [InlineData("Torvin Brannoc Zellik")]      // the grandfather aligns
    [InlineData("Torvin O Pemdal")]            // an initial of the father
    [InlineData("Torvin Ostr")]                // a truncated father name
    [InlineData("Torvin Torvin")]              // a repeated given name is filler: a given name alone fits
    [InlineData("Torvin Chalim Pemdal")]       // ch / j: one father name romanized two ways (against "Jalim")
    [InlineData("Torvin Alkes")]               // "al" + three letters is a name, here the father's (against "Alkas")
    public void ChainFit_ChainsThatCanBeOnePerson_KeepThePhoneFloor(string other)
    {
        var a = Rec(0, "a", "Torvin Ostrid Brannoc Jalim Alkas");
        var b = Rec(1, "b", other);
        Assert.True((Flag(a, b, ChainFit) & (MatchFlags.PhoneNameMerge | MatchFlags.NameConsistent)) != 0, other);
        Assert.True(Score(a, b, out _, ChainFit) >= AutoMerge, other);
    }

    [Fact]
    public void ChainFit_TwoTwoWordNames_FitByDefault_AndNeedTheirSecondWordsUnderTwoWordChainFit()
    {
        var a = Rec(0, "a", "Torvin Ostrid");
        var b = Rec(1, "b", "Torvin Pemdal");
        Assert.True((Flag(a, b, MatchSettings.Defaults) & MatchFlags.PhoneNameMerge) != 0);
        Assert.True((Flag(a, b, ChainFit) & MatchFlags.PhoneNameMerge) != 0, "chain fit alone keeps the team's slice rule");
        double two = Score(a, b, out var f, TwoWord);
        Assert.True(two < AutoMerge, $"two-word chain fit: another second word, got {two:F3}");
        Assert.True((f & MatchFlags.PhoneNameMerge) == 0);

        // A spelling variant of the second word still fits; the default rules are untouched.
        Assert.True(Score(a, Rec(2, "c", "Torvin Ostrud"), out _, TwoWord) >= AutoMerge);
        Assert.Equal(Score(a, b, out var f1, MatchSettings.Defaults), Score(a, b, out var f2, MatchSettings.Defaults with { TwoWordChainFit = true }));
        Assert.Equal(f1, f2);   // without GivenNameChainFit the two-word switch does nothing
    }

    // ---------------------------------------------------------------- NamedMergeNeedsFloor

    [Fact]
    public void FloorRequired_SiblingsWithNearSpelledNamesAndABoost_AreHeldInTheStewardBand()
    {
        // Invented twins: near-spelled first names (not one name), one father, phone, district and birth date.
        var a = Rec(0, "a", "Torvin Ostrid Brannoc") with { NormAddress = "brannoc lane varrin", NormCity = "varrin", Dob = new DobInfo(1990, 4, 12) };
        var b = Rec(1, "b", "Morvan Ostrid Brannoc") with { NormAddress = "brannoc lane varrin", NormCity = "varrin", Dob = new DobInfo(1990, 4, 12) };
        double off = Score(a, b, out var fOff, MatchSettings.Defaults);
        Assert.True(off >= AutoMerge, $"default: the blend and the boosts cross the line, got {off:F3}");
        Assert.True((fOff & (MatchFlags.NameConsistent | MatchFlags.PhoneNameMerge)) == 0, "no name floor fired");

        double on = Score(a, b, out _, FloorRequired);
        Assert.Equal(RealMatcher.OrgUnresolvedCap, on, 3);

        // A pair a rule decides keeps its floor; a nameless pair and an organisation line are untouched.
        var c = Rec(2, "c", "Torvin Ostrid");
        Assert.True(Score(a, c, out _, FloorRequired) >= AutoMerge);
        var nameless = new RealRecord(3, "src", "d", "", "", [Phone], [], null, null);
        Assert.Equal(Score(a, nameless, out _, MatchSettings.Defaults), Score(a, nameless, out _, FloorRequired));
        var orgA = Rec(4, "e", "Brannoc Trading Company");
        var orgB = Rec(5, "f", "Brannoc Trading Group Company");
        Assert.Equal(Score(orgA, orgB, out _, MatchSettings.Defaults), Score(orgA, orgB, out _, FloorRequired));
        // Consistent names with no phone floor (another number, a district and a birth date agreeing) keep the
        // weighted base: the names are one person's.
        var c2 = Rec(6, "g", "Torvin Ostrid Brannoc", phone: OtherPhone) with { NormAddress = "brannoc lane varrin", NormCity = "varrin", Dob = new DobInfo(1990, 4, 12) };
        Assert.Equal(Score(a, c2, out _, MatchSettings.Defaults), Score(a, c2, out _, FloorRequired));
    }

    // ---------------------------------------------------------------- ShiftedChainConflict

    [Fact]
    public void Shifted_AFatherAndHisSonOnOnePhone_AreTwoPeopleOnlyWhenOn()
    {
        // Invented: the son "Torvin Ostrid Brannoc", his father "Ostrid Brannoc Quellmar".
        var son = Rec(0, "a", "Torvin Ostrid Brannoc");
        var father = Rec(1, "b", "Ostrid Brannoc Quellmar");
        Assert.True((Flag(son, father, MatchSettings.Defaults) & MatchFlags.GivenNameConflict) == 0);
        double on = Score(son, father, out var f, Shifted);
        Assert.True(on <= RealMatcher.PartsConflictCap, $"on: a parent and a child, got {on:F3}");
        Assert.True((f & MatchFlags.GivenNameConflict) != 0);

        // The son named after his grandfather, the father's two words swapped around his own.
        Assert.True((Flag(Rec(2, "c", "Brannoc Ostrid"), Rec(3, "d", "Ostrid Brannoc Quellmar"), Shifted) & MatchFlags.GivenNameConflict) != 0);
        // Not a shift: two two-word names in reverse order, one name with words joined, the same given name.
        Assert.True((Flag(Rec(4, "e", "Torvin Ostrid"), Rec(5, "f", "Ostrid Torvin"), Shifted) & MatchFlags.GivenNameConflict) == 0);
        Assert.True((Flag(Rec(6, "g", "Tor Vinostrid Brannoc"), Rec(7, "h", "Torvinostrid Brannoc"), Shifted) & MatchFlags.GivenNameConflict) == 0);
        Assert.True((Flag(son, Rec(8, "i", "Torvin Brannoc"), Shifted) & MatchFlags.GivenNameConflict) == 0);
        // A business name with a word repeated and joined differently is not a shift.
        Assert.True((Flag(Rec(9, "j", "Used Zelect Used Zelect"), Rec(10, "k", "Uzelect Uzelect"), Shifted) & MatchFlags.GivenNameConflict) == 0);
    }

    [Fact]
    public void Shifted_ASonsTicketOnHisFathersCustomerNumber_LosesTheReferenceFloor()
    {
        // The father's dealer record, and a ticket by the son (another phone) raised on the father's number.
        // By default the father's chain inside the son's reads as consistent and the reference floors.
        var father = Rec(0, "a", "Ostrid Brannoc", phone: OtherPhone, source: "dealer");
        var ticket = Rec(1, "t1", "Torvin Ostrid Brannoc", source: "ticket") with { SameAsRefs = ["dealer|a"] };
        double off = Score(ticket, father, out var fOff, MatchSettings.Defaults);
        Assert.True(off >= AutoMerge, $"default: the reference floors, got {off:F3}");
        Assert.True((fOff & MatchFlags.SameAsMerge) != 0);

        double on = Score(ticket, father, out var fOn, Shifted);
        Assert.True(on < AutoMerge, $"on: a parent and a child, got {on:F3}");
        Assert.True((fOn & MatchFlags.SameAsMerge) == 0);
        Assert.True((fOn & MatchFlags.GivenNameConflict) != 0);
    }

    // ---------------------------------------------------------------- SameAsRefPhoneFloor

    [Fact]
    public void SameAsPhone_ALinkedTicketOnTheSameNumber_MergesUnlessTheNamesSayTwoPeople()
    {
        var customer = Rec(0, "a", "Torvin Ostrid", source: "dealer");
        // A short title-like word the default rules cannot read as the given name.
        var ticket = Rec(1, "t1", "Hj Torvin", source: "ticket") with { SameAsRefs = ["dealer|a"] };
        double off = Score(ticket, customer, out var fOff, MatchSettings.Defaults);
        Assert.True(off < AutoMerge, $"default: the names neither agree nor share the first name, got {off:F3}");
        double on = Score(ticket, customer, out var fOn, SameAsPhone);
        Assert.True(on >= AutoMerge, $"on: the reference and the number agree, got {on:F3}");
        Assert.True((fOn & MatchFlags.SameAsMerge) != 0);

        // Clearly another person's first name, or another number: no floor.
        var other = Rec(2, "t2", "Ulfas Pemdal", source: "ticket") with { SameAsRefs = ["dealer|a"] };
        Assert.True(Score(other, customer, out _, SameAsPhone) < AutoMerge);
        var otherPhone = ticket with { Phones = [OtherPhone] };
        Assert.Equal(Score(otherPhone, customer, out _, MatchSettings.Defaults), Score(otherPhone, customer, out _, SameAsPhone));
    }

    // ---------------------------------------------------------------- OrgNamesBothSides

    [Fact]
    public void OrgBothSides_TwoNamesOfOneCompany_FloorOnThePhone()
    {
        var a = Rec(0, "a", "Brannoc Trading Company");
        var b = Rec(1, "b", "Brannoc Company");
        double off = Score(a, b, out var fOff, MatchSettings.Defaults);
        Assert.True((fOff & MatchFlags.OrgLine) != 0);
        double on = Score(a, b, out var fOn, OrgBoth);
        Assert.True(on >= AutoMerge, $"on: one company written two ways, got {on:F3}");
        Assert.True((fOn & (MatchFlags.OrgLine | MatchFlags.NameConsistent)) == (MatchFlags.OrgLine | MatchFlags.NameConsistent));
        // Written with its words joined.
        Assert.True(Score(Rec(2, "c", "Brannoc Vale Company"), Rec(3, "d", "Brannocvale Group"), out _, OrgBoth) >= AutoMerge);
        // Generic business words alone name no company; two different companies stay apart.
        Assert.True((Flag(Rec(4, "e", "Auto Trading Company"), Rec(5, "f", "Cars Company"), OrgBoth) & MatchFlags.NameConsistent) == 0);
        Assert.True((Flag(a, Rec(6, "g", "Zellik Trading Company"), OrgBoth) & MatchFlags.NameConsistent) == 0);
    }

    [Fact]
    public void OrgBothSides_ACompanyAndThePersonItIsNamedAfter_AreHeldNotMerged()
    {
        var company = Rec(0, "a", "Torvin Ostrid Brannoc Company") with { NormAddress = "brannoc lane varrin", NormCity = "varrin" };
        var person = Rec(1, "b", "Torvin Ostrid Brannoc") with { NormAddress = "brannoc lane varrin", NormCity = "varrin" };
        double off = Score(company, person, out _, MatchSettings.Defaults);
        Assert.True(off >= AutoMerge, $"default: the weighted base merges a company and a person, got {off:F3}");
        Assert.Equal(RealMatcher.OrgUnresolvedCap, Score(company, person, out _, OrgBoth), 3);
        // A short name beside a company is as often the company without its legal form: weighted base kept.
        var shortCompany = Rec(4, "e", "Brannoc Vale Company");
        var shortName = Rec(5, "f", "Brannoc Vale");
        Assert.Equal(Score(shortCompany, shortName, out _, MatchSettings.Defaults), Score(shortCompany, shortName, out _, OrgBoth));

        // The district chain-slice rescue is a person rule: an organisation line never takes it.
        var other = Rec(2, "c", "Torvin Pemdal Company") with { NormAddress = "brannoc lane varrin", NormCity = "varrin" };
        var otherPerson = Rec(3, "d", "Torvin Zellik") with { NormAddress = "brannoc lane varrin", NormCity = "varrin" };
        Assert.True((Flag(other, otherPerson, MatchSettings.Defaults) & MatchFlags.ChainSliceRescue) != 0);
        Assert.True((Flag(other, otherPerson, OrgBoth) & MatchFlags.ChainSliceRescue) == 0);
    }

    // ---------------------------------------------------------------- CrossScriptLetters

    [Theory]
    [InlineData("رضوك قلنتر", "Radhok Qalantar")]   // the emphatic d: d in the Arabic transliteration, dh in Latin
    [InlineData("فاران قلنتر", "Varan Qalantar")]   // v typed with the Arabic f
    [InlineData("زيكور قلنتر", "Zegur Qalantar")]   // a Kurdish g typed with the Arabic kaf
    [InlineData("تحلوم قلنتر", "Talum Qalantar")]   // t then h (two letters), and an h the Latin leaves out
    [InlineData("زيرالله قلنتر", "Zir Allah Qalantar")] // a compound joined in one script, split in the other
    public void CrossLetters_LettersTheScriptsWriteDifferently_AlignOnlyWhenOn(string arabic, string latin)
    {
        var a = Rec(0, "a", arabic);
        var b = Rec(1, "b", latin);
        using (Cross.Use()) Assert.False(RealMatcher.NameConsistent(a, b), $"cross-script alone: {latin}");
        using (CrossLetters.Use()) Assert.True(RealMatcher.NameConsistent(a, b), $"cross-script letters: {latin}");
        using ((MatchSettings.Defaults with { CrossScriptLetters = true }).Use()) Assert.False(RealMatcher.NameConsistent(a, b), "needs CrossScriptNames");
    }

    [Fact]
    public void CrossLetters_ADifferentNameStaysApart()
    {
        // Another consonant frame and a long vowel of another class are still two names.
        using (CrossLetters.Use())
        {
            Assert.False(RealMatcher.NameConsistent(Rec(0, "a", "رضوك قلنتر"), Rec(1, "b", "Rabok Qalantar")));
            Assert.False(RealMatcher.NameConsistent(Rec(2, "c", "رضوك قلنتر"), Rec(3, "d", "Radheek Qalantar")));
        }
    }

    // ---------------------------------------------------------------- JoinedNames

    [Fact]
    public void Joined_WordsWrittenTogether_AlignWithTheSpacedNameOnlyWhenOn()
    {
        // Under the given-name anchor (as a tenant with these rules runs it), so containment of the given name
        // does not already align a joined first word.
        var anchored = MatchSettings.Defaults with { AnchoredGivenName = true };
        var joined = anchored with { JoinedNames = true };
        bool Consistent(MatchSettings s, string x, string y)
        {
            using (s.Use()) return RealMatcher.NameConsistent(Rec(0, "a", x), Rec(1, "b", y));
        }
        foreach (var (x, y) in new[] { ("Torvanostrid", "Torvin Ostrid Brannoc"), ("Torvan Ostrid", "Torvinostrid Brannoc") })
        {
            Assert.False(Consistent(anchored, x, y), x);
            Assert.True(Consistent(joined, x, y), x);
        }
        // A joined run must be the same letters, not merely contain the other name, and it starts at the given name.
        Assert.False(Consistent(joined, "Torvin Pemdal", "Torvinostrid"));
        Assert.False(Consistent(joined, "Ostridbrannoc", "Torvin Ostrid Brannoc"));
    }

    // ---------------------------------------------------------------- ServicedVinNeverLowers

    [Fact]
    public void ServicedVin_ASharedServicedCarNeverLowersAPhoneMatch()
    {
        var serviced = new[] { new VinLink(Vin, VinSource.Service, new DateOnly(2024, 3, 1), new DateOnly(2024, 3, 1)) };
        var named = Rec(0, "a", "Torvin Ostrid", vins: serviced);
        var nameless = new RealRecord(1, "src", "b", "", "", [Phone], [], null, null, VinLinks: serviced);
        double off = Score(named, nameless, out _, MatchSettings.Defaults);
        Assert.True(off < AutoMerge, $"default: the serviced car pulls the phone match down, got {off:F3}");
        double withoutCar = Score(named with { VinLinks = null }, nameless with { VinLinks = null }, out _, MatchSettings.Defaults);
        Assert.Equal(withoutCar, Score(named, nameless, out _, ServicedVin), 6);
    }

    // ---------------------------------------------------------------- the recorded form

    [Fact]
    public void RecordedForm_Format3OnlyWhenAFormat3SwitchIsOn()
    {
        var format2 = MatchSettings.Defaults with { CrossScriptNames = true, DistinctGivenNames = true };
        Assert.StartsWith("""{"v":2,""", format2.ToJson());
        Assert.DoesNotContain("givenNameChainFit", format2.ToJson());

        foreach (var s in new[] { ChainFit, TwoWord, FloorRequired, Shifted, SameAsPhone, OrgBoth, CrossLetters, Joined, ServicedVin })
        {
            string j = s.ToJson();
            Assert.StartsWith("""{"v":3,""", j);
            Assert.Equal(s, MatchSettings.FromJson(j));
            Assert.Equal(j, MatchSettings.FromJson(j).ToJson());
            Assert.True(s.WorthRecording(registryHoldsOne: false));
        }
        Assert.Equal("given-name chain fit, two-word chain fit, named merge needs a floor, shifted chain conflict, same-as reference phone floor, "
            + "organisation names both sides, cross-script letters, joined names, serviced VIN never lowers",
            (MatchSettings.Defaults with
            {
                GivenNameChainFit = true, TwoWordChainFit = true, NamedMergeNeedsFloor = true, ShiftedChainConflict = true, SameAsRefPhoneFloor = true,
                OrgNamesBothSides = true, CrossScriptLetters = true, JoinedNames = true, ServicedVinNeverLowers = true,
            }).ToString());
    }
}
