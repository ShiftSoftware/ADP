using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// The per-tenant rules for names written in two scripts, all OFF by default:
/// <see cref="Flags.CrossScriptNames"/>, <see cref="Flags.ExtendedArabicLetters"/>,
/// <see cref="Flags.NamelessSoldVinNeedsPhone"/> and <see cref="Flags.AnchoredGivenName"/>. Each rule is
/// asserted both ways: off reproduces the default engine, on changes exactly the shape it targets. Every
/// name is invented (in both scripts); phones and VINs are synthetic. Settings are applied through
/// <see cref="MatchSettings.Use"/> scopes, so nothing process-wide changes.
/// </summary>
[Collection(EngineFlagsCollection.Name)]
public class CrossScriptRulesTests
{
    private const double AutoMerge = 0.90;
    private const string Phone = "100200300";
    private const string OtherPhone = "100200399";
    private const string Vin = "SYNTHVIN000000017";

    private static readonly MatchSettings Cross = MatchSettings.Defaults with { CrossScriptNames = true };
    private static readonly MatchSettings Anchored = MatchSettings.Defaults with { AnchoredGivenName = true };
    private static readonly MatchSettings Extended = MatchSettings.Defaults with { ExtendedArabicLetters = true };
    private static readonly MatchSettings NamelessVin = MatchSettings.Defaults with { NamelessSoldVinNeedsPhone = true };
    private static readonly MatchSettings Distinct = MatchSettings.Defaults with { DistinctGivenNames = true };

    /// <summary>A record whose stored name is <paramref name="raw"/> as the source wrote it, normalised
    /// under whatever configuration is in effect when it is built.</summary>
    private static RealRecord Rec(int idx, string id, string raw, string? phone = Phone, VinLink[]? vins = null) =>
        new(idx, "src", id, raw, Norm.Name(raw), phone is null ? [] : [phone], [], null, null, VinLinks: vins);

    private static double Score(RealRecord a, RealRecord b, out MatchFlags f, MatchSettings s)
    {
        using (s.Use()) return RealMatcher.Score(a, b, out f);
    }

    // ---------------------------------------------------------------- CrossScriptNames

    [Fact]
    public void CrossScript_OneNameInArabicAndLatinLetters_OnASharedPhone_MergesOnlyWhenOn()
    {
        // Invented: "Tozan Qalantar" typed in Latin letters, and the same name in Arabic letters, which
        // leave the short vowels out ("tzn qlntr").
        var arabic = Rec(0, "a", "تزن قلنتر");
        var latin = Rec(1, "b", "Tozan Qalantar");
        Assert.Equal("tzn qlntr", arabic.NormName);

        double off = Score(arabic, latin, out var fOff, MatchSettings.Defaults);
        Assert.True(off < AutoMerge, $"default: the transliterated consonants do not align with the Latin spelling, got {off:F3}");
        Assert.True((fOff & MatchFlags.NameConsistent) == 0);

        double on = Score(arabic, latin, out var fOn, Cross);
        Assert.True(on >= AutoMerge, $"on: one name in two scripts on one phone merges, got {on:F3}");
        Assert.True((fOn & MatchFlags.NameConsistent) != 0);
        // Either order of the records.
        Assert.True(Score(latin, arabic, out _, Cross) >= AutoMerge);
        // A final h on one side only, and a lone given name of three consonants or more.
        Assert.True(Score(Rec(2, "c", "تزنه قلنتر"), Rec(3, "d", "Tozanah Qalantar"), out _, Cross) >= AutoMerge);
        Assert.True(Score(Rec(4, "e", "تزن"), latin, out _, Cross) >= AutoMerge);
    }

    [Fact]
    public void CrossScript_ALongVowelOnOneSideOnly_KeepsTwoNamesApart()
    {
        var arabic = Rec(0, "a", "تزن قلنتر");
        // A Latin long vowel ("ee", "oo") the Arabic side does not write is another name.
        foreach (var other in new[] { "Tozeen Qalantar", "Toozan Qalantar" })
        {
            double s = Score(arabic, Rec(1, "b", other), out var f, Cross);
            Assert.True(s < AutoMerge, $"{other}: got {s:F3}");
            Assert.True((f & MatchFlags.NameConsistent) == 0);
        }
        // An Arabic long vowel (y, w) the Latin side writes with a vowel of another class is another name.
        Assert.True(Score(Rec(2, "c", "تزين قلنتر"), Rec(3, "d", "Tozoon Qalantar"), out _, Cross) < AutoMerge);
        Assert.True(Score(Rec(4, "e", "تزون قلنتر"), Rec(5, "f", "Tozeen Qalantar"), out _, Cross) < AutoMerge);
    }

    [Fact]
    public void CrossScript_AppliesOnlyAcrossScripts_NotBetweenTwoLatinNames()
    {
        // A Latin spelling without vowels next to a Latin spelling with them stays on the default rules.
        var a = Rec(0, "a", "Tzn Qlntr");
        var b = Rec(1, "b", "Tozan Qalantar");
        Assert.Equal(Score(a, b, out var fOff, MatchSettings.Defaults), Score(a, b, out var fOn, Cross));
        Assert.Equal(fOff, fOn);
    }

    [Fact]
    public void CrossScript_AShortConsonantFrameNeedsTheFatherName_NeverStandsAlone()
    {
        // A two-consonant given name ("Qadu", "qdw") aligns only together with the father name.
        var arabic = Rec(0, "a", "قدو قلنتر");
        var latinBoth = Rec(1, "b", "Qadu Qalantar");
        using (MatchSettings.Defaults.Use()) Assert.False(RealMatcher.NameConsistent(arabic, latinBoth));
        using (Cross.Use()) Assert.True(RealMatcher.NameConsistent(arabic, latinBoth));
        Assert.True(Score(arabic, latinBoth, out _, Cross) >= AutoMerge);

        // The same short given name beside another father name, or alone, is not enough.
        var latinOther = Rec(2, "c", "Qadu Morvenst");
        using (Cross.Use()) Assert.False(RealMatcher.GivenNamesMatch(arabic, latinOther));
        Assert.True(Score(arabic, latinOther, out _, Cross) < AutoMerge);
        Assert.True(Score(Rec(3, "d", "قدو"), latinBoth, out _, Cross) < AutoMerge);

        // A given name of three consonants or more floors on the phone like any given name.
        var arabicLong = Rec(4, "e", "تزن قلنتر");
        var latinGiven = Rec(5, "f", "Tozan Morvenst");
        using (MatchSettings.Defaults.Use()) Assert.False(RealMatcher.GivenNamesMatch(arabicLong, latinGiven));
        using (Cross.Use()) Assert.True(RealMatcher.GivenNamesMatch(arabicLong, latinGiven));
        Assert.True((Flag(arabicLong, latinGiven, Cross) & MatchFlags.PhoneNameMerge) != 0);
    }

    [Fact]
    public void CrossScript_AbdCompounds_SplitInOneScriptAndJoinedInTheOther_ReadAlike()
    {
        var arabic = Rec(0, "a", "عبد التزن قلنتره");
        var latin = Rec(1, "b", "Abdultozan Qalantar");
        Assert.True(Score(arabic, latin, out _, MatchSettings.Defaults) < AutoMerge);
        Assert.True(Score(arabic, latin, out var f, Cross) >= AutoMerge);
        Assert.True((f & MatchFlags.NameConsistent) != 0);
    }

    private static MatchFlags Flag(RealRecord a, RealRecord b, MatchSettings s)
    {
        Score(a, b, out var f, s);
        return f;
    }

    // ---------------------------------------------------------------- ExtendedArabicLetters

    [Fact]
    public void ExtendedLetters_KurdishAndPersianLettersAndHamzaSeats_AreReadOnlyWhenOn()
    {
        // Invented names written with the Kurdish letters گ ە ڤ ڵ ۆ چ, Persian ی and ک, and the hamza seats.
        using (MatchSettings.Defaults.Use())
        {
            Assert.Equal("", Norm.Name("گەڤڵۆچ"));      // every letter dropped: the name vanishes
            Assert.Equal("al", Norm.Name("علی"));
            Assert.Equal("rban", Norm.Name("رؤبان"));
        }
        using (Extended.Use())
        {
            Assert.Equal("gevloch", Norm.Name("گەڤڵۆچ"));
            Assert.Equal("aly", Norm.Name("علی"));
            Assert.Equal("rwban", Norm.Name("رؤبان"));
            Assert.Equal("ezhdok", Norm.Name("ئەژدۆک"));          // the word-initial carrier is silent
            Assert.Equal("tayrp ezhdok", Norm.Name("تائرپ ئەژدۆک")); // a medial seat is read
            Assert.Equal("pvzh", Norm.Name("پڤژ"));
        }
    }

    [Fact]
    public void ExtendedLetters_LetOneNameTypedOnTwoKeyboardsMatch()
    {
        // The same invented name typed with the Arabic yeh and kaf, and with the Persian ones.
        Assert.NotEqual(NormUnder(MatchSettings.Defaults, "كريزدي"), NormUnder(MatchSettings.Defaults, "کریزدی"));
        Assert.Equal(NormUnder(Extended, "كريزدي"), NormUnder(Extended, "کریزدی"));

        static string NormUnder(MatchSettings s, string raw) { using (s.Use()) return Norm.Name(raw); }
    }

    // ---------------------------------------------------------------- NamelessSoldVinNeedsPhone

    [Fact]
    public void NamelessSoldVin_DifferentPhones_HoldsTheVinFloor_ButLeavesNamelessPhoneMergesAlone()
    {
        var sold = new[] { new VinLink(Vin, VinSource.Sale, new DateOnly(2024, 3, 1), new DateOnly(2024, 3, 1)) };
        var buyer = Rec(0, "a", "Torvin Quellmar", Phone, sold);
        var nameless = new RealRecord(1, "src", "b", "", "", [OtherPhone], [], null, null, VinLinks: sold);

        Assert.True(Score(buyer, nameless, out var fOff, MatchSettings.Defaults) >= AutoMerge, "default: the sold VIN floors a nameless pair");
        Assert.True((fOff & MatchFlags.VinSoldMerge) != 0);
        double on = Score(buyer, nameless, out var fOn, NamelessVin);
        Assert.True(on < AutoMerge, $"on: different phones contradict the nameless side, got {on:F3}");
        Assert.True((fOn & MatchFlags.NamelessHeld) != 0);

        // Same phone: corroborated, still merges.
        var namelessSamePhone = nameless with { Phones = [Phone] };
        Assert.True(Score(buyer, namelessSamePhone, out _, NamelessVin) >= AutoMerge);

        // A buyer's record without any phone does not confirm the nameless record's number either.
        var buyerNoPhone = buyer with { Phones = [] };
        Assert.True(Score(buyerNoPhone, nameless, out _, MatchSettings.Defaults) >= AutoMerge);
        Assert.True(Score(buyerNoPhone, nameless, out var fNoPhone, NamelessVin) < AutoMerge);
        Assert.True((fNoPhone & MatchFlags.NamelessHeld) != 0);
        // A nameless record with no phone of its own keeps the floor: nothing contradicts the car.
        var namelessNoPhone = nameless with { Phones = [] };
        Assert.True(Score(buyer, namelessNoPhone, out _, NamelessVin) >= AutoMerge);

        // A nameless pair on a shared phone alone is not damped by the narrow rule (the whole
        // NamelessNeedsCorroboration rule does damp it).
        var namedPhoneOnly = Rec(2, "c", "Torvin Quellmar");
        var namelessPhoneOnly = new RealRecord(3, "src", "d", "", "", [Phone], [], null, null);
        Assert.Equal(Score(namedPhoneOnly, namelessPhoneOnly, out _, MatchSettings.Defaults), Score(namedPhoneOnly, namelessPhoneOnly, out _, NamelessVin));
        Assert.True(Score(namedPhoneOnly, namelessPhoneOnly, out _, MatchSettings.Defaults with { NamelessNeedsCorroboration = true }) < AutoMerge);
    }

    // ---------------------------------------------------------------- AnchoredGivenName

    [Fact]
    public void Anchored_AFathersNameChainInsideTheSons_IsNotOneName()
    {
        // Invented chain: the son "Torvin Ostrid Quellmar" and his father "Ostrid Quellmar" on one phone.
        var son = Rec(0, "a", "Torvin Ostrid Quellmar");
        var father = Rec(1, "b", "Ostrid Quellmar");
        double off = Score(son, father, out var fOff, MatchSettings.Defaults);
        Assert.True(off >= AutoMerge, "default: every token of the shorter name aligns somewhere");
        Assert.True((fOff & MatchFlags.NameConsistent) != 0);

        double on = Score(son, father, out var fOn, Anchored);
        Assert.True(on < AutoMerge, $"anchored: the given names differ, got {on:F3}");
        Assert.True((fOn & (MatchFlags.NameConsistent | MatchFlags.PhoneNameMerge)) == 0);
    }

    [Fact]
    public void Anchored_ChainSlicesAndATrailingFamilyName_StillAlign()
    {
        bool Consistent(MatchSettings s, string x, string y)
        {
            using (s.Use()) return RealMatcher.NameConsistent(Rec(0, "a", x), Rec(1, "b", y));
        }

        Assert.True(Consistent(Anchored, "Torvin Ostrid", "Torvin Ostrid Quellmar"));
        Assert.True(Consistent(Anchored, "Torvin Quellmar", "Torvin Ostrid Brannoc Quellmar"));   // given + family
        Assert.True(Consistent(Anchored, "Torvin", "Torvin Ostrid"));
        Assert.False(Consistent(Anchored, "Ostrid", "Torvin Ostrid"));                             // the father alone
        Assert.False(Consistent(Anchored, "Quellmar Torvin", "Torvin Ostrid Quellmar"));           // out of order

        Assert.True(Consistent(MatchSettings.Defaults, "Ostrid", "Torvin Ostrid"));
        Assert.True(Consistent(MatchSettings.Defaults, "Quellmar Torvin", "Torvin Ostrid Quellmar"));
        // The string overload is the plain comparison whatever the configuration (organisation words, name parts).
        using (Anchored.Use()) Assert.True(RealMatcher.NameConsistent("ostrid", "torvin ostrid"));
    }

    // ---------------------------------------------------------------- DistinctGivenNames

    [Fact]
    public void DistinctGiven_SiblingsOnAFamilyPhone_AreHeldBelowTheLine_WhateverTheRestOfTheNamesBlend()
    {
        // Two brothers (invented): clearly different first names, the same father and family names, one
        // phone and one district. By default they sit in the steward queue; with the same birth date
        // (twins) the weighted base even crosses the auto-merge line.
        var a = Rec(0, "a", "Torvin Ostrid Brannoc Quellmar") with { NormAddress = "brannoc lane varrin", NormCity = "varrin" };
        var b = Rec(1, "b", "Ulfas Ostrid Brannoc Quellmar") with { NormAddress = "brannoc lane varrin", NormCity = "varrin" };
        var twinA = a with { Dob = new DobInfo(1990, 4, 12) };
        var twinB = b with { Dob = new DobInfo(1990, 4, 12) };

        Assert.True(Score(a, b, out var fOff, MatchSettings.Defaults) >= 0.80);
        Assert.True((fOff & MatchFlags.GivenNameConflict) == 0);
        double twinsOff = Score(twinA, twinB, out _, MatchSettings.Defaults);
        Assert.True(twinsOff >= AutoMerge, $"default: the shared names, district and birth date carry the pair over the line, got {twinsOff:F3}");

        foreach (var (x, y) in new[] { (a, b), (twinA, twinB) })
        {
            double on = Score(x, y, out var fOn, Distinct);
            Assert.True(on <= RealMatcher.PartsConflictCap, $"on: two first names on one phone are two people, got {on:F3}");
            Assert.True((fOn & MatchFlags.GivenNameConflict) != 0);
            Assert.True((Merge.CannotLinkFlags & fOn) != 0, "a cannot-link under conflict-aware clustering");
        }

        // Not two names: a near spelling of one first name, a first name joined to the next word, a title.
        foreach (var other in new[] { "Torvan Ostrid Brannoc Quellmar", "Torvinostrid Brannoc Quellmar", "Mr Torvin Brannoc Quellmar" })
        {
            var c = Rec(2, "c", other) with { NormAddress = "brannoc lane varrin", NormCity = "varrin" };
            Assert.True((Flag(a, c, Distinct) & MatchFlags.GivenNameConflict) == 0, other);
        }
        // One first name, two father names: the phone + first-name floor is untouched.
        var d = Rec(3, "d", "Torvin Pemdal");
        Assert.Equal(Score(a, d, out var f1, MatchSettings.Defaults), Score(a, d, out var f2, Distinct));
        Assert.Equal(f1, f2);
    }

    [Fact]
    public void DistinctGiven_WithConflictAwareClustering_ANamelessRecordCannotChainTwoBrothers()
    {
        var a = Rec(0, "a", "Torvin Ostrid Quellmar");
        var b = Rec(1, "b", "Ulfas Ostrid Quellmar");
        var nameless = new RealRecord(2, "src", "c", "", "", [Phone], [], null, null);
        var records = new List<RealRecord> { a, b, nameless };
        var blocks = new RealMatcher.BlockingResult { Blocks = [[0, 1, 2]] };
        Merge.Result Cluster(MatchSettings s) { using (s.Use()) return Merge.ClusterFromBlocks(records, blocks); }
        int Find(Merge.Result r, int x) { while (r.Parent[x] != x) x = r.Parent[x]; return x; }

        var off = Cluster(MatchSettings.Defaults with { ConflictAwareClustering = true });
        Assert.Equal(Find(off, 0), Find(off, 1));   // the nameless record's phone-only edges chain the brothers
        var on = Cluster(MatchSettings.Defaults with { ConflictAwareClustering = true, DistinctGivenNames = true });
        Assert.NotEqual(Find(on, 0), Find(on, 1));
    }

    // ---------------------------------------------------------------- the recorded form

    [Fact]
    public void RecordedForm_ConfigurationsWithoutTheNewSwitches_KeepTheirFormat1Bytes()
    {
        var old = MatchSettings.Defaults with { StructuredNames = true, OrgNameMerge = true, TokenFolds = [("qx", "k")] };
        string json = old.ToJson();
        Assert.StartsWith("""{"v":1,""", json);
        Assert.DoesNotContain("crossScriptNames", json);
        Assert.Equal(old, MatchSettings.FromJson(json));

        foreach (var s in new[] { Cross, Anchored, Extended, NamelessVin, Distinct })
        {
            string j = s.ToJson();
            Assert.StartsWith("""{"v":2,""", j);
            Assert.Equal(s, MatchSettings.FromJson(j));
            Assert.Equal(j, MatchSettings.FromJson(j).ToJson());
            Assert.False(s.IsDefault);
            Assert.True(s.WorthRecording(registryHoldsOne: false));
        }
        Assert.Equal("cross-script names, extended Arabic letters, nameless sold-VIN phone check, anchored given name, distinct given names",
            (MatchSettings.Defaults with { CrossScriptNames = true, ExtendedArabicLetters = true, NamelessSoldVinNeedsPhone = true, AnchoredGivenName = true, DistinctGivenNames = true }).ToString());
    }
}
