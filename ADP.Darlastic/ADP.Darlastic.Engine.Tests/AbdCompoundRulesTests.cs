using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// <see cref="Flags.AbdCompounds"/> (off by default, read with <see cref="Flags.CrossScriptNames"/>): "abd" compounds
/// align across the two scripts the way they already do within one. Asserted both ways: off reproduces the default
/// engine, on aligns the short form of a compound and a compound written inside a joined word. Every name is invented
/// (in both scripts); phones are synthetic. Settings are applied through <see cref="MatchSettings.Use"/> scopes, so
/// nothing process-wide changes.
/// </summary>
[Collection(EngineFlagsCollection.Name)]
public class AbdCompoundRulesTests
{
    private const double AutoMerge = 0.90;
    private const string Phone = "100200300";

    private static readonly MatchSettings Cross = MatchSettings.Defaults with { CrossScriptNames = true, AnchoredGivenName = true };
    private static readonly MatchSettings CrossJoined = Cross with { JoinedNames = true };
    private static readonly MatchSettings On = Cross with { AbdCompounds = true };
    private static readonly MatchSettings OnJoined = CrossJoined with { AbdCompounds = true };

    private static RealRecord Rec(int idx, string id, string raw, string? phone = Phone) =>
        new(idx, "src", id, raw, Norm.Name(raw), phone is null ? [] : [phone], [], null, null);

    private static double Score(RealRecord a, RealRecord b, out MatchFlags f, MatchSettings s)
    {
        using (s.Use()) return RealMatcher.Score(a, b, out f);
    }

    private static bool Consistent(MatchSettings s, RealRecord a, RealRecord b)
    {
        using (s.Use()) return RealMatcher.NameConsistent(a, b);
    }

    [Fact]
    public void TheShortFormOfACompound_AlignsAcrossScripts_OnlyWhenOn()
    {
        // Invented: "Zarqoon Tamdal" in Latin letters and "Abd al-Zarqoon Tamdal" in Arabic script, one number.
        var latin = Rec(0, "a", "Zarqoon Tamdal");
        var arabic = Rec(1, "b", "عبدالزرقون تمدال");
        Assert.False(Consistent(Cross, latin, arabic));
        Assert.True(Score(latin, arabic, out _, Cross) < AutoMerge);

        Assert.True(Consistent(On, latin, arabic));
        double on = Score(latin, arabic, out var f, On);
        Assert.True(on >= AutoMerge, $"on: one chain, the given name in its short form, got {on:F3}");
        Assert.NotEqual(MatchFlags.None, f & MatchFlags.NameConsistent);
    }

    [Fact]
    public void ACompoundInsideAJoinedWord_AlignsWithTheSpacedName_OnlyWhenOn()
    {
        // Invented: a given name and an "abd" father name written as one word in Arabic script, apart in Latin.
        var latin = Rec(0, "a", "Zarqoon Abdul Brannok");
        var arabic = Rec(1, "b", "زرقونعبدالبرنوك");
        Assert.False(Consistent(CrossJoined, latin, arabic));
        Assert.True(Consistent(OnJoined, latin, arabic));
        Assert.True(Score(latin, arabic, out _, OnJoined) >= AutoMerge);
    }

    [Fact]
    public void DifferentNamesAfterAbd_StayApart()
    {
        // The short form must be the compound's own name: "Abd al-Zarqoon" is not "Tamdal".
        Assert.False(Consistent(On, Rec(0, "a", "Tamdal Zarqoon"), Rec(1, "b", "عبدالزرقون تمدال")));
        // Two different compounds stay two names.
        Assert.False(Consistent(On, Rec(0, "a", "Abdul Zarqoon Tamdal"), Rec(1, "b", "عبدالبرنوك تمدال")));
        // Too short a name after "abd" (fewer than three consonants) does not stand for the compound.
        Assert.False(Consistent(On, Rec(0, "a", "Ra Tamdal"), Rec(1, "b", "عبدالرا تمدال")));
    }

    [Fact]
    public void AShortFormIsReadOnlyAsTheGivenName_NeverAsALaterWord()
    {
        // Invented: "Abd al-Zarqoon Tamdal" in Arabic script and the same person in Latin letters with the compound
        // split by a typing slip ("Abdual Zarqoon Tamdal"). The Latin second word is the compound's short form; read as
        // the father's name it would make the Arabic record's given name the Latin record's father: a parent and a child.
        var shifted = On with { ShiftedChainConflict = true };
        var latin = Rec(0, "a", "Abdual Zarqoon Tamdal");
        var arabic = Rec(1, "b", "عبدالزرقون تمدال");
        Score(latin, arabic, out var f, shifted);
        Assert.Equal(MatchFlags.None, f & MatchFlags.GivenNameConflict);
        Score(latin, arabic, out var fOff, Cross with { ShiftedChainConflict = true });
        Assert.Equal(MatchFlags.None, fOff & MatchFlags.GivenNameConflict);
    }

    [Fact]
    public void ACompoundAndItsShortForm_AreNotDistinctGivenNames()
    {
        var distinct = Cross with { DistinctGivenNames = true };
        var latin = Rec(0, "a", "Zarqoon Pemdal");
        var arabic = Rec(1, "b", "عبدالزرقون تمدال");
        Score(latin, arabic, out var fOff, distinct);
        Assert.NotEqual(MatchFlags.None, fOff & MatchFlags.GivenNameConflict);
        Score(latin, arabic, out var fOn, distinct with { AbdCompounds = true });
        Assert.Equal(MatchFlags.None, fOn & MatchFlags.GivenNameConflict);
    }

    [Fact]
    public void WithinOneScript_NothingChanges()
    {
        // Within one script the short form already aligns (one name contains the other); the option changes nothing there.
        var a = Rec(0, "a", "Zarqoon Tamdal");
        var b = Rec(1, "b", "Abdulzarqoon Tamdal");
        Assert.Equal(Score(a, b, out var f0, Cross), Score(a, b, out var f1, On), 6);
        Assert.Equal(f0, f1);
        var c = Rec(2, "c", "عبدالزرقون تمدال");
        var d = Rec(3, "d", "زرقون تمدال");
        Assert.Equal(Score(c, d, out _, Cross), Score(c, d, out _, On), 6);
    }

    [Fact]
    public void RecordedForm_IsFormat4()
    {
        var s = MatchSettings.Defaults with { AbdCompounds = true };
        string j = s.ToJson();
        Assert.StartsWith("""{"v":4,""", j);
        Assert.Contains("\"abdCompounds\":true", j);
        Assert.Equal(s, MatchSettings.FromJson(j));
        Assert.Equal("abd compounds across scripts", s.ToString());
    }
}
