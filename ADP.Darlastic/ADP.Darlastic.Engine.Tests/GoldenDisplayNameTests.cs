using System.Text;
using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// How a golden's name is shown (<see cref="GoldenNameOptions"/>): off, the title-cased match form exactly as
/// before; on, the chain survivorship chose, as one of its records wrote it, in that record's script. Every
/// name here is made up: ordinary words in Arabic script (rabbit, stationery, test, sample), Kurdish script
/// (butterfly, rosewater) and invented Latin words.
/// </summary>
public class GoldenDisplayNameTests
{
    private static readonly GoldenNameOptions On = new() { DisplayFromRecordNames = true };

    private static RealRecord Rec(string src, string id, string raw, bool mojibake = false, string? phone = "7000000101") =>
        new(0, src, id, raw, Norm.Name(mojibake ? Mojibake.TryRepair(raw) ?? "" : raw), phone is null ? [] : [phone], [], null, null,
            NameWasMojibake: mojibake);

    private static string Name(List<Merge.GoldenAttr> golden) => golden.Single(g => g.AttrType == "full_name").Value;

    // ------------------------------------------------------------------ off: the engine as shipped

    [Fact]
    public void Off_TheNameIsTheTitleCasedMatchForm_WhateverTheScript()
    {
        var members = new List<RealRecord> { Rec("dealer-a", "1", "أرنب قرطاس"), Rec("dealer-a", "2", "أرنب قرطاس") };
        string matchForm = Norm.Name("أرنب قرطاس");

        var byDefault = Merge.SurviveGolden(members);
        var withDefaults = Merge.SurviveGolden(members, nameOptions: GoldenNameOptions.Default);

        Assert.Equal(System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(matchForm), Name(byDefault));
        Assert.DoesNotMatch("[؀-ۿ]", Name(byDefault));
        Assert.Equal(byDefault, withDefaults);
    }

    [Fact]
    public void On_OnlyTheNameValueChanges_TheChainItsReasonAndEveryOtherAttributeStay()
    {
        var members = new List<RealRecord>
        {
            Rec("dealer-a", "1", "أرنب قرطاس") with { NormCity = "sample city" },
            Rec("app", "2", "أرنب قرطاس تجربة"),
            Rec("dealer-b", "3", "أرنب قرطاس"),
        };

        var off = Merge.SurviveGolden(members);
        var on = Merge.SurviveGolden(members, nameOptions: On);

        Assert.Equal(off.Select(g => (g.AttrType, g.WonBy)), on.Select(g => (g.AttrType, g.WonBy)));
        Assert.Equal(off.Where(g => g.AttrType != "full_name"), on.Where(g => g.AttrType != "full_name"));
        Assert.Equal("longest-chain", on.Single(g => g.AttrType == "full_name").WonBy);
        // The longest chain survives in both; on shows it as its record wrote it.
        Assert.Equal("أرنب قرطاس تجربة", Name(on));
        Assert.Equal(Norm.Name(Name(on)), Name(off).ToLowerInvariant());
    }

    // ------------------------------------------------------------------ on: the record's own script

    [Fact]
    public void On_ArabicScript_IsShownInArabicScript()
    {
        var members = new List<RealRecord> { Rec("dealer-a", "1", "  أرنب   قرطاس ") };
        Assert.Equal("أرنب قرطاس", Name(Merge.SurviveGolden(members, nameOptions: On)));
    }

    [Fact]
    public void On_KurdishScript_KeepsTheLettersTheMatchFormDrops()
    {
        // ZWNJ inside a word is part of the spelling; the tatweel only stretches a letter.
        const string kurdish = "پەپوولە گوڵاو";
        var members = new List<RealRecord> { Rec("dealer-a", "1", "پەپوولـە گوڵاو"), Rec("ticket-a", "2", kurdish) };
        Assert.Equal(Norm.Name(members[0].RawName), Norm.Name(kurdish));

        string shown = Name(Merge.SurviveGolden(members, nameOptions: On));

        Assert.Equal(kurdish, shown);
        Assert.Contains('ڵ', shown);
        Assert.Contains('ە', shown);
    }

    [Theory]
    [InlineData("KESTREL MORROW", "Kestrel Morrow")]      // all capitals: title case
    [InlineData("kestrel morrow", "Kestrel Morrow")]      // all lower: title case
    [InlineData("Kestrel McMorrow", "Kestrel McMorrow")]  // mixed: as written
    [InlineData("-Kestrel Morrow.", "Kestrel Morrow")]    // edge punctuation trimmed
    public void On_LatinScript_IsShownAsWrittenWithUniformCaseTitleCased(string raw, string expected) =>
        Assert.Equal(expected, Name(Merge.SurviveGolden([Rec("dealer-a", "1", raw)], nameOptions: On)));

    [Fact]
    public void On_AGarbledCodepageName_IsRepairedLikeItsMatchForm()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        const string arabic = "أرنب قرطاس";
        string garbled = Encoding.GetEncoding(1252).GetString(Encoding.GetEncoding(1256).GetBytes(arabic));
        Assert.True(Mojibake.Looks(garbled));

        var members = new List<RealRecord> { Rec("dealer-a", "1", garbled, mojibake: true) };
        Assert.Equal(arabic, Name(Merge.SurviveGolden(members, nameOptions: On)));
    }

    // ------------------------------------------------------------------ which record's spelling

    [Fact]
    public void On_MixedScripts_TheChosenChainsRecordsDecide_NotAnotherSpellingInTheCluster()
    {
        // Two Latin records (the most-attested chain) and one Arabic record whose match form is another chain:
        // the golden shows the chain survivorship chose, in its records' script, even with the Arabic record's
        // source ranked strongest.
        var members = new List<RealRecord>
        {
            Rec("app", "1", "Kestrel Morrow"),
            Rec("app", "2", "Kestrel Morrow"),
            Rec("dealer-a", "3", "أرنب قرطاس"),
        };
        var ranked = On with { SourceRank = new Dictionary<string, int> { ["dealer-a"] = 0, ["app"] = 1 } };

        Assert.Equal("Kestrel Morrow", Name(Merge.SurviveGolden(members, nameOptions: ranked)));
    }

    [Fact]
    public void On_SeveralSpellingsOfTheChain_TheStrongestSourceWins_ThenTheMostCompleteSpelling()
    {
        // Same match form (the hamza folds away), two spellings.
        const string withHamza = "أرنب قرطاس", without = "ارنب قرطاس";
        Assert.Equal(Norm.Name(withHamza), Norm.Name(without));
        var members = new List<RealRecord> { Rec("app", "1", without), Rec("app", "2", without), Rec("dealer-a", "3", withHamza) };

        var ranked = On with { SourceRank = new Dictionary<string, int> { ["dealer-a"] = 0, ["app"] = 1 } };
        Assert.Equal(withHamza, Name(Merge.SurviveGolden(members, nameOptions: ranked)));

        // Unranked, equally complete: the spelling more records share.
        Assert.Equal(without, Name(Merge.SurviveGolden(members, nameOptions: On)));

        // A listed source beats an unlisted one, whatever the counts.
        var appFirst = On with { SourceRank = new Dictionary<string, int> { ["app"] = 0 } };
        Assert.Equal(without, Name(Merge.SurviveGolden(members, nameOptions: appFirst)));
    }

    [Fact]
    public void On_TheChoiceDoesNotDependOnMemberOrder()
    {
        const string a = "أرنب قرطاس", b = "ارنب قرطاس";
        var members = new List<RealRecord> { Rec("dealer-b", "9", b), Rec("dealer-a", "3", a), Rec("dealer-c", "1", b), Rec("dealer-a", "4", a) };
        string first = Name(Merge.SurviveGolden(members, nameOptions: On));
        for (int seed = 1; seed <= 20; seed++)
        {
            var rnd = new Random(seed);
            var shuffled = members.OrderBy(_ => rnd.Next()).ToList();
            Assert.Equal(first, Name(Merge.SurviveGolden(shuffled, nameOptions: On)));
        }
    }

    [Fact]
    public void On_NoPresentableSpelling_KeepsTheMatchForm()
    {
        // A serial in the only spelling of the chain: not shown; the title-cased match form is.
        var members = new List<RealRecord> { Rec("dealer-a", "1", "Kestrel Morrow 2") };
        Assert.Equal("Kestrel Morrow", Name(Merge.SurviveGolden(members, nameOptions: On)));
    }

    [Fact]
    public void On_ANamelessCluster_HasNoName()
    {
        var members = new List<RealRecord> { Rec("dealer-a", "1", "") with { NormName = "" } };
        Assert.DoesNotContain(Merge.SurviveGolden(members, nameOptions: On), g => g.AttrType == "full_name");
    }

    // ------------------------------------------------------------------ the cleanup

    [Theory]
    [InlineData("  Kestrel \t Morrow  ", "Kestrel Morrow")]
    [InlineData("Kestrel‏ Morrow‎", "Kestrel Morrow")]   // direction marks
    [InlineData("﻿Kestrel Morrow", "Kestrel Morrow")]          // a byte-order mark
    [InlineData("أرنب‌ قرطاس", "أرنب قرطاس")]                  // a ZWNJ at a word's edge goes
    [InlineData("(Kestrel Morrow)", "Kestrel Morrow")]              // brackets around the whole name
    [InlineData("Kestrel Morrow (Sample)", "Kestrel Morrow (Sample)")] // a closing bracket that closes one stays
    public void Clean_DropsInvisibleAndEdgeCharacters(string raw, string expected) =>
        Assert.Equal(expected, GoldenDisplayName.Clean(raw));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("K")]
    [InlineData("Kestrel 7")]
    [InlineData("كستريل ٧")]                                        // an Arabic-Indic digit
    [InlineData("--")]
    public void Clean_RefusesWhatCannotStandAsAName(string? raw) =>
        Assert.Null(GoldenDisplayName.Clean(raw));

    [Fact]
    public void Clean_RefusesAGarbledNameThatDoesNotRepair() =>
        Assert.Null(GoldenDisplayName.Clean("Kestrel Morrow", wasMojibake: true));

    [Fact]
    public void Describe_NamesTheRanksNeverAValue()
    {
        Assert.Equal("match form (title-cased)", GoldenNameOptions.Default.Describe());
        var o = On with { SourceRank = new Dictionary<string, int> { ["dealer-b"] = 0, ["dealer-a"] = 0, ["app"] = 2, ["broker"] = 1 } };
        Assert.Equal("as the records wrote them (dealer-a/dealer-b > broker > app > other sources)", o.Describe());
        var weakerListed = On with { SourceRank = new Dictionary<string, int> { ["app"] = 2, ["broker"] = 1 }, UnlistedRank = 0 };
        Assert.Equal("as the records wrote them (other sources > broker > app)", weakerListed.Describe());
    }

    [Fact]
    public void On_UnlistedSourcesCanRankFirst()
    {
        const string withHamza = "أرنب قرطاس", without = "ارنب قرطاس";
        var members = new List<RealRecord> { Rec("app", "1", without), Rec("app", "2", without), Rec("dealer-z", "3", withHamza) };
        var weakerListed = On with { SourceRank = new Dictionary<string, int> { ["app"] = 2 }, UnlistedRank = 0 };
        Assert.Equal(withHamza, Name(Merge.SurviveGolden(members, nameOptions: weakerListed)));
    }
}
