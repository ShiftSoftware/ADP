using System.Text.RegularExpressions;
using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// The plain-language labels are the only names a browsing surface shows for what the engine
/// produces, so a value without one would surface as a raw identifier — or not at all. These tests
/// make adding an enum value without its label a build-breaking mistake rather than a UI bug.
/// </summary>
public class CaseLabelTests
{
    private static readonly string[] MapNames = ["cases", "clusters", "reasons", "evidence"];

    public static TheoryData<string> Maps() => [.. MapNames];

    private static (Type Enum, IEnumerable<(string Name, PlainLabel? Label)> Rows) Map(string which) => which switch
    {
        "cases" => (typeof(CaseCat), Enum.GetValues<CaseCat>().Where(v => v != CaseCat.None)
            .Select(v => (v.ToString(), CaseLabels.Cases.GetValueOrDefault(v)))),
        "clusters" => (typeof(ClusterCat), Enum.GetValues<ClusterCat>().Where(v => v != ClusterCat.None)
            .Select(v => (v.ToString(), CaseLabels.Clusters.GetValueOrDefault(v)))),
        "reasons" => (typeof(GoldenReason), Enum.GetValues<GoldenReason>().Where(v => v != GoldenReason.None)
            .Select(v => (v.ToString(), CaseLabels.Reasons.GetValueOrDefault(v)))),
        "evidence" => (typeof(MatchFlags), Enum.GetValues<MatchFlags>().Where(v => v != MatchFlags.None)
            .Select(v => (v.ToString(), CaseLabels.Evidence.GetValueOrDefault(v)))),
        _ => throw new ArgumentOutOfRangeException(nameof(which)),
    };

    [Theory]
    [MemberData(nameof(Maps))]
    public void EveryValue_HasALabelAndADescription(string which)
    {
        var (type, rows) = Map(which);
        var missing = rows.Where(r => r.Label is null
                                      || string.IsNullOrWhiteSpace(r.Label.Label)
                                      || string.IsNullOrWhiteSpace(r.Label.Description))
            .Select(r => r.Name).ToList();
        Assert.True(missing.Count == 0, $"{type.Name} values without a plain label: {string.Join(", ", missing)}");
    }

    [Theory]
    [MemberData(nameof(Maps))]
    public void Labels_AreDistinctWithinTheirList(string which)
    {
        var labels = Map(which).Rows.Select(r => r.Label!.Label).ToList();
        var dupes = labels.GroupBy(l => l).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.True(dupes.Count == 0, $"two {which} values share a label: {string.Join(", ", dupes)}");
    }

    /// <summary>
    /// The words a reader of a customer list should never meet: the engine's own identifiers, rule
    /// codes, thresholds and score bands. Checked against every label and description.
    /// </summary>
    [Fact]
    public void NoLabel_CarriesEngineJargon()
    {
        // Compound identifiers (AutoMerged, NearMiss, …) never read as English; the one-word ones that
        // are engine terms are named explicitly. "Large" is an identifier too, but also just a word.
        var identifiers = new[] { typeof(CaseCat), typeof(ClusterCat), typeof(MatchFlags), typeof(GoldenReason) }
            .SelectMany(Enum.GetNames).Where(n => Regex.IsMatch(n, "[a-z][A-Z]"))
            .Concat(["Mojibake", "Arabizi", "Increment"]).ToHashSet();
        var jargon = new Regex(@"\bP7\b|Claude|\d\.\d{2}|≥|<=|>=|\bVin[A-Z]");

        var texts = MapNames.SelectMany(m => Map(m).Rows)
            .SelectMany(r => new[] { r.Label!.Label, r.Label.Description }).ToList();

        var offenders = texts.Where(t => jargon.IsMatch(t)
                                         || identifiers.Any(id => Regex.IsMatch(t, $@"\b{id}\b")))
            .ToList();
        Assert.True(offenders.Count == 0, "labels with engine jargon:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Decision_NamesAllThreeOutcomesInWords()
    {
        Assert.Equal("Combined automatically", CaseLabels.Decision(0.95).Label);
        Assert.Equal("Needs a person", CaseLabels.Decision(0.85).Label);
        Assert.Equal("Kept separate", CaseLabels.Decision(0.40).Label);
    }
}
