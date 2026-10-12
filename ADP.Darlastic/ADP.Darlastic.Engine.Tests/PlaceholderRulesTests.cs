using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// <see cref="Flags.PlaceholderNamesNeedCorroboration"/> (off by default) and the record marker it reads,
/// <see cref="RealRecord.NamePlaceholder"/>. Asserted both ways: off reproduces the default engine, on holds a
/// placeholder record's phone-only pairs below the steward band and nothing else. Every name is invented;
/// phones, e-mail addresses and VINs are synthetic. Settings are applied through <see cref="MatchSettings.Use"/>
/// scopes, so nothing process-wide changes.
/// </summary>
[Collection(EngineFlagsCollection.Name)]
public class PlaceholderRulesTests
{
    private const double AutoMerge = 0.90;
    private const double Steward = 0.80;
    private const string Phone = "100200300";
    private const string Vin = "SYNTHVIN000000029";

    private static readonly MatchSettings On = MatchSettings.Defaults with { PlaceholderNamesNeedCorroboration = true };

    private static RealRecord Named(int idx, string id, string raw, string? phone = Phone) =>
        new(idx, "src-a", id, raw, Norm.Name(raw), phone is null ? [] : [phone], [], null, null);

    /// <summary>A record whose feed blanked a placeholder typed as the name, marked as such.</summary>
    private static RealRecord Placeholder(int idx, string id, string raw = "n/a", string? phone = Phone) =>
        new(idx, "src-b", id, raw, "", phone is null ? [] : [phone], [], null, null, NamePlaceholder: true);

    /// <summary>A record with no name at all (a registration that asked for a number only).</summary>
    private static RealRecord Nameless(int idx, string id, string? phone = Phone) =>
        new(idx, "src-b", id, "", "", phone is null ? [] : [phone], [], null, null);

    private static double Score(RealRecord a, RealRecord b, out MatchFlags f, MatchSettings s)
    {
        using (s.Use()) return RealMatcher.Score(a, b, out f);
    }

    [Fact]
    public void APlaceholderOnTheSameNumber_MergesByDefault_AndIsHeldBelowTheStewardBandWhenOn()
    {
        // Invented: "Torvin Ostrid" and a ticket whose name field holds "n/a", one number.
        var named = Named(0, "a", "Torvin Ostrid");
        var placeholder = Placeholder(1, "b");
        double off = Score(named, placeholder, out var fOff, MatchSettings.Defaults);
        Assert.True(off >= AutoMerge, $"default: the number alone merges a record without a usable name, got {off:F3}");
        Assert.Equal(MatchFlags.None, fOff & MatchFlags.NamelessHeld);

        double on = Score(named, placeholder, out var fOn, On);
        Assert.True(on < Steward, $"on: a placeholder's number alone is not enough, got {on:F3}");
        Assert.Equal(RealMatcher.PlaceholderUnconfirmedCap, on, 6);
        Assert.NotEqual(MatchFlags.None, fOn & MatchFlags.NamelessHeld);

        // Symmetric, and two placeholders on one number are held too.
        Assert.Equal(on, Score(placeholder, named, out _, On), 6);
        Assert.True(Score(Placeholder(2, "c", "customer"), placeholder, out _, On) < Steward);
    }

    [Fact]
    public void ARecordWithNoNameAtAll_KeepsThePhoneMatch()
    {
        var named = Named(0, "a", "Torvin Ostrid");
        var nameless = Nameless(1, "b");
        Assert.Equal(Score(named, nameless, out _, MatchSettings.Defaults), Score(named, nameless, out _, On), 6);
        Assert.True(Score(named, nameless, out _, On) >= AutoMerge);
    }

    [Fact]
    public void AnythingBesidesThePhone_StillDecides()
    {
        var sold = new[] { new VinLink(Vin, VinSource.Sale, new DateOnly(2023, 4, 1), new DateOnly(2023, 4, 1)) };
        var named = Named(0, "a", "Torvin Ostrid") with { VinLinks = sold };
        var placeholder = Placeholder(1, "b") with { VinLinks = sold };
        Assert.Equal(Score(named, placeholder, out _, MatchSettings.Defaults), Score(named, placeholder, out _, On), 6);
        Assert.True(Score(named, placeholder, out _, On) >= AutoMerge, "a shared bought car decides as before");

        // A direct reference (a ticket raised for that customer number) decides as before.
        var linked = Placeholder(1, "b") with { SameAsRefs = ["src-a|a"] };
        Assert.Equal(Score(Named(0, "a", "Torvin Ostrid"), linked, out _, MatchSettings.Defaults),
            Score(Named(0, "a", "Torvin Ostrid"), linked, out _, On), 6);
        Assert.True(Score(Named(0, "a", "Torvin Ostrid"), linked, out _, On) >= AutoMerge);

        // The same ID number decides as before.
        var withId = Named(0, "a", "Torvin Ostrid") with { NationalId = "100000000017" };
        var placeholderWithId = Placeholder(1, "b") with { NationalId = "100000000017" };
        Assert.Equal(Score(withId, placeholderWithId, out _, MatchSettings.Defaults), Score(withId, placeholderWithId, out _, On), 6);
    }

    [Fact]
    public void WithoutTheMarker_NothingChanges()
    {
        // A tenant whose feeds never set the marker scores exactly as before with the option on.
        var named = Named(0, "a", "Torvin Ostrid");
        var blanked = Placeholder(1, "b") with { NamePlaceholder = false };
        Assert.Equal(Score(named, blanked, out var f0, MatchSettings.Defaults), Score(named, blanked, out var f1, On), 6);
        Assert.Equal(f0, f1);
    }

    [Fact]
    public void TheMarker_IsLeftOutOfPayloadsAndHashesWhenNotSet()
    {
        var plain = Nameless(0, "a");
        string json = System.Text.Json.JsonSerializer.Serialize(plain);
        Assert.DoesNotContain(nameof(RealRecord.NamePlaceholder), json);
        var marked = Placeholder(0, "a");
        string markedJson = System.Text.Json.JsonSerializer.Serialize(marked);
        Assert.Contains(nameof(RealRecord.NamePlaceholder), markedJson);
        Assert.True(System.Text.Json.JsonSerializer.Deserialize<RealRecord>(markedJson)!.NamePlaceholder);
    }

    [Fact]
    public void TheTrace_ReadsInPlainLanguage()
    {
        using (On.Use())
        {
            var trace = new MatchTrace();
            double score = RealMatcher.Explain(Named(0, "a", "Torvin Ostrid"), Placeholder(1, "b"), trace);
            Assert.Contains(trace.Steps, s => s.Title == "A placeholder for a name");
            Assert.Equal(CaseLabels.Decision(score).Label, trace.Steps.Last().Title);
            var offenders = TraceWordingTests.Offenders(trace.Steps.SelectMany(s => new[] { s.Title, s.Detail }));
            Assert.True(offenders.Count == 0, "trace text with engine terms:\n" + string.Join("\n", offenders));
        }
    }

    [Fact]
    public void RecordedForm_Format4OnlyWhenTheSwitchIsOn()
    {
        string j = On.ToJson();
        Assert.StartsWith("""{"v":4,""", j);
        Assert.Contains("\"placeholderNamesNeedCorroboration\":true", j);
        Assert.Equal(On, MatchSettings.FromJson(j));
        Assert.Equal(j, MatchSettings.FromJson(j).ToJson());
        Assert.True(On.WorthRecording(registryHoldsOne: false));
        Assert.Equal("placeholder names need corroboration", On.ToString());

        // Configurations without the switch keep their older bytes.
        var format3 = MatchSettings.Defaults with { GivenNameChainFit = true };
        Assert.StartsWith("""{"v":3,""", format3.ToJson());
        Assert.DoesNotContain("placeholderNamesNeedCorroboration", format3.ToJson());
        Assert.StartsWith("""{"v":1,""", MatchSettings.Defaults.ToJson());
    }
}
