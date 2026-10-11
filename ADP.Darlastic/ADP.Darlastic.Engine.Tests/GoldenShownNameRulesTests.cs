using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// Which chains may stand as a golden's shown name (<see cref="GoldenNameOptions.PlaceholderNames"/>,
/// <see cref="GoldenNameOptions.PlaceholderTokens"/>, <see cref="GoldenNameOptions.MinimumLetters"/>,
/// <see cref="GoldenNameOptions.PresentableOnly"/>): all off by default, so the engine as shipped is unchanged;
/// on, a refused chain is never shown and the best of the others is. Every name here is made up (invented
/// Latin words, ordinary Arabic words) or a placeholder.
/// </summary>
public class GoldenShownNameRulesTests
{
    private static readonly GoldenNameOptions On = new() { DisplayFromRecordNames = true };

    private static readonly GoldenNameOptions Rules = On with
    {
        PlaceholderNames = new HashSet<string>(StringComparer.Ordinal) { "unknown", Norm.Name("بدون اسم") },
        PlaceholderTokens = new HashSet<string>(StringComparer.Ordinal) { "n", "a", "na", "test", "mr", "dr" },
        MinimumLetters = 3,
        PresentableOnly = true,
    };

    private static RealRecord Rec(string id, string raw, string src = "dealer-a") =>
        new(0, src, id, raw, Norm.Name(raw), ["7000000101"], [], null, null);

    private static string? Name(List<Merge.GoldenAttr> golden) => golden.SingleOrDefault(g => g.AttrType == "full_name")?.Value;

    [Fact]
    public void Defaults_FilterNothing_TheEngineAsShipped()
    {
        Assert.False(GoldenNameOptions.Default.FiltersChains);
        Assert.False(On.FiltersChains);
        var members = new List<RealRecord> { Rec("1", "Test Test"), Rec("2", "Test Test"), Rec("3", "Kestrel Morrow") };
        Assert.Equal("Test Test", Name(Merge.SurviveGolden(members)));
        Assert.Equal("Test Test", Name(Merge.SurviveGolden(members, nameOptions: On)));
        Assert.Equal(Merge.SurviveGolden(members), Merge.SurviveGolden(members, nameOptions: GoldenNameOptions.Default));
    }

    [Theory]
    [InlineData("Test Test")]
    [InlineData("N/A N/A")]
    [InlineData("n/a")]
    [InlineData("Unknown")]
    [InlineData("بدون اسم")]
    [InlineData("Mr")]
    [InlineData("Dr.")]
    [InlineData("Ab")]
    public void APlaceholderChain_GivesWayToARealName_EvenWhenMoreRecordsCarryIt(string placeholder)
    {
        var members = new List<RealRecord> { Rec("1", placeholder), Rec("2", placeholder), Rec("3", placeholder), Rec("4", "Kestrel Morrow", "app") };
        Assert.Equal("Kestrel Morrow", Name(Merge.SurviveGolden(members, nameOptions: Rules)));
    }

    [Fact]
    public void AGoldenWhoseEveryChainIsRefused_HasNoShownName_AndKeepsEveryOtherAttribute()
    {
        var members = new List<RealRecord> { Rec("1", "Test Test"), Rec("2", "Unknown"), Rec("3", "N/A") };
        var off = Merge.SurviveGolden(members, nameOptions: On);
        var on = Merge.SurviveGolden(members, nameOptions: Rules);
        Assert.Null(Name(on));
        Assert.Equal(off.Where(g => g.AttrType != "full_name"), on);
    }

    [Theory]
    [InlineData("Unknown Kestrel")]       // contains a placeholder word: a name all the same
    [InlineData("Mr Kestrel Morrow")]     // a title before a name
    [InlineData("Kestrel Test")]
    public void ARealNameThatContainsAPlaceholderWord_IsShown(string raw) =>
        Assert.Equal(raw, Name(Merge.SurviveGolden([Rec("1", raw)], nameOptions: Rules)));

    [Fact]
    public void AnEmailShapedChain_IsNeverShown_InEitherForm()
    {
        const string address = "kestrel.morrow@example.com";
        var members = new List<RealRecord> { Rec("1", address), Rec("2", address), Rec("3", "Plover Quill", "app") };
        Assert.NotEqual("Plover Quill", Name(Merge.SurviveGolden(members, nameOptions: On)));   // the address's chain wins without the rule
        Assert.Equal("Plover Quill", Name(Merge.SurviveGolden(members, nameOptions: Rules)));
        Assert.Null(Name(Merge.SurviveGolden([Rec("1", address)], nameOptions: Rules)));
        Assert.Null(Name(Merge.SurviveGolden([Rec("1", address)], nameOptions: Rules with { DisplayFromRecordNames = false })));
    }

    [Theory]
    [InlineData("Kestrel ❤ Morrow")]      // a pictograph inside the name
    [InlineData("Kestrel Morrow ✓")]
    [InlineData("Kestrel �Morrow")]   // a letter lost in transcoding
    [InlineData("Kestrel Morrow ²")]       // a numeral that is not a decimal digit
    public void AnUnpresentableSpelling_GivesWayToTheMatchForm(string raw)
    {
        Assert.False(GoldenDisplayName.IsPresentable(raw));
        string? shown = Name(Merge.SurviveGolden([Rec("1", raw)], nameOptions: Rules));
        Assert.NotNull(shown);
        Assert.True(GoldenDisplayName.IsPresentable(shown), "the shown name is presentable");
    }

    [Fact]
    public void APresentableSpellingFromAWeakerSource_BeatsAnUnpresentableOneFromAStrongerSource()
    {
        var ranked = Rules with { SourceRank = new Dictionary<string, int> { ["dealer-a"] = 0, ["app"] = 1 } };
        var members = new List<RealRecord> { Rec("1", "Kestrel ❤ Morrow", "dealer-a"), Rec("2", "Kestrel Morrow", "app") };
        Assert.Equal("Kestrel ❤ Morrow", Name(Merge.SurviveGolden(members, nameOptions: ranked with { PresentableOnly = false })));
        Assert.Equal("Kestrel Morrow", Name(Merge.SurviveGolden(members, nameOptions: ranked)));
    }

    [Theory]
    [InlineData("Kestrel Morrow", true)]
    [InlineData("أرنب قرطاس", true)]
    [InlineData("Kestrel (Morrow)", true)]
    [InlineData("kestrel@example.com", false)]
    [InlineData("Kestrel 😀", false)]
    [InlineData("Kestrel ٧", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    public void IsPresentable_RefusesAddressesPictographsReplacementsAndNumerals(string? raw, bool presentable) =>
        Assert.Equal(presentable, GoldenDisplayName.IsPresentable(raw));

    [Fact]
    public void Describe_CountsTheRules_NeverAValue()
    {
        string d = Rules.Describe();
        Assert.StartsWith("as the records wrote them (every source alike); never shown: 2 placeholder names, chains of 6 placeholder tokens", d);
        Assert.Contains("chains under 3 letters", d);
        Assert.Contains("unpresentable spellings", d);
        Assert.DoesNotContain("unknown", d);
        Assert.Equal("match form (title-cased)", GoldenNameOptions.Default.Describe());
    }
}
