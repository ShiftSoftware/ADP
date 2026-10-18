using System.Text.RegularExpressions;
using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// The explain trace and the normalization steps are shown to reviewers word for word ("How they
/// were compared"), so they follow the same rule as the labels: plain language, no rule codes,
/// thresholds, score bands or the engine's own role names. Every branch of the scorer is reached
/// with a synthetic pair, and the test first checks that each branch was in fact reached, so a
/// scoring change that bypasses one fails here instead of leaving its wording unchecked.
/// </summary>
public class TraceWordingTests
{
    /// <summary>
    /// What a reader must never meet, matched case-insensitively so a hyphenated or lower-case
    /// spelling of an engine term ("steward-band", "auto-merge") is caught as well as the identifier.
    /// Decimals and comparison signs are thresholds or weights; the page shows strength as a percentage.
    /// </summary>
    internal static readonly Regex Jargon = new(
        @"steward|auto-?merg|king[- ]?key|domain rule|expert|\bband\b|\bfloor\b|\bcap\b|blend|weight|trigram|jaro"
        + @"|\btokens?\b|\bFK\b|\bP7\b|claude|mojibake|arabizi|increment|vin-sample|\bsignal\b"
        + @"|\d\.\d|[≥≤×]|<=|>=|\b\d{4}-\d{2}-\d{2}\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>The engine's compound identifiers (AutoMerged, VinReview, …), which never read as English.</summary>
    private static readonly string[] Identifiers = new[] { typeof(CaseCat), typeof(ClusterCat), typeof(MatchFlags), typeof(GoldenReason) }
        .SelectMany(Enum.GetNames).Where(n => Regex.IsMatch(n, "[a-z][A-Z]")).Distinct().ToArray();

    internal static List<string> Offenders(IEnumerable<string> texts) =>
        texts.Where(t => Jargon.IsMatch(t) || Identifiers.Any(id => Regex.IsMatch(t, $@"\b{id}\b"))).Distinct().ToList();

    internal static List<TraceStep> Trace(RealRecord a, RealRecord b)
    {
        var trace = new MatchTrace();
        RealMatcher.Explain(a, b, trace);
        return trace.Steps;
    }

    private static RealRecord Rec(int idx, string id, string name, string? phone = null) =>
        new(idx, idx % 2 == 0 ? "src-a" : "src-b", id, name, name, phone is null ? [] : [phone], [], null, null);

    private static VinLink[] Sold(string vin, int year) => [new(vin, VinSource.Sale, new DateOnly(year, 3, 1), new DateOnly(year, 3, 1))];
    private static VinLink[] Serviced(string vin) => [new(vin, VinSource.Service, new DateOnly(2021, 5, 1), new DateOnly(2022, 5, 1))];

    private const string Phone = "7700000501";
    private const string Vin = "SYNTHVIN000000001";

    /// <summary>One synthetic pair per branch of the scorer, keyed by the trace title it must produce.</summary>
    public static IEnumerable<(string Title, RealRecord A, RealRecord B)> Pairs()
    {
        yield return ("Only the name in common", Rec(0, "n1", "kestrel morrow"), Rec(1, "n2", "kestrel morrow"));
        yield return ("Same number, same name", Rec(0, "p1", "kestrel morrow", Phone), Rec(1, "p2", "kestrel morow", Phone));
        yield return ("Same number, same first name", Rec(0, "g1", "kestrel morrow", Phone), Rec(1, "g2", "kestrel fenwick", Phone));
        yield return ("Same number, unrelated names", Rec(0, "c1", "kestrel morrow", Phone), Rec(1, "c2", "quill sabadine", Phone));
        yield return ("Same number, different first names", Rec(0, "d1", "orrin varden", Phone), Rec(1, "d2", "tobias varden", Phone));
        yield return ("Company number", Rec(0, "o1", "varden trading company", Phone), Rec(1, "o2", "orrin varden", Phone));
        yield return ("Phone", Rec(0, "w1", "kestrel morrow") with { WeakPhones = ["770000501"] }, Rec(1, "w2", "kestrel morrow", "7770000501"));
        yield return ("Phone", Rec(0, "x1", "kestrel morrow", "7700000601"), Rec(1, "x2", "kestrel morrow", "7700000602"));

        // Address: agreement down to the district, then the same-first-name rescue on top of it.
        var addrA = Rec(0, "a1", "kestrel morrow", Phone) with { RawAddress = "north lumen 12|springfield", NormAddress = "north lumen 12 springfield", NormCity = "springfield" };
        var addrB = Rec(1, "a2", "kestrel fenwick", Phone) with { RawAddress = "north lumen 12|springfield", NormAddress = "north lumen 12 springfield", NormCity = "springfield" };
        yield return ("Address agrees", addrA, addrB);
        yield return ("Same first name, number and district", addrA, addrB);
        yield return ("Address differs", addrA with { NormAddress = "north lumen 12 springfield" },
                                         addrB with { NormAddress = "harbor sorrel 40 shelbyville", NormCity = "shelbyville" });
        yield return ("Address not compared", addrA with { NormAddress = "springfield" }, addrB);
        yield return ("Address not compared", Rec(0, "a3", "kestrel morrow", Phone), Rec(1, "a4", "kestrel morrow", Phone));

        // Vehicles.
        yield return ("Same vehicle bought, names agree",
            Rec(0, "v1", "kestrel morrow") with { VinLinks = Sold(Vin, 2021) }, Rec(1, "v2", "kestrel morrow") with { VinLinks = Sold(Vin, 2021) });
        yield return ("Same vehicle bought, similar name",
            Rec(0, "v3", "kestrel morrow") with { VinLinks = Sold(Vin, 2021) }, Rec(1, "v4", "kestrel fenwick") with { VinLinks = Sold(Vin, 2021) });
        yield return ("Same vehicle bought, different names",
            Rec(0, "v5", "kestrel morrow") with { VinLinks = Sold(Vin, 2021) }, Rec(1, "v6", "quill sabadine") with { VinLinks = Sold(Vin, 2021) });
        yield return ("Vehicle",
            Rec(0, "v7", "kestrel morrow") with { VinLinks = Serviced(Vin) }, Rec(1, "v8", "kestrel morrow") with { VinLinks = Serviced(Vin) });
        yield return ("Vehicle",
            Rec(0, "v9", "kestrel morrow") with { VinLinks = Sold(Vin, 2018) }, Rec(1, "v10", "kestrel morrow") with { VinLinks = Sold(Vin, 2023) });
        yield return ("Vehicle",
            Rec(0, "v11", "kestrel morrow") with { VinLinks = Sold(Vin, 2021) }, Rec(1, "v12", "kestrel morrow") with { VinLinks = Sold("SYNTHVIN000000002", 2021) });

        // A record that points at another.
        yield return ("Direct link, names agree",
            Rec(0, "r1", "kestrel morrow") with { SameAsRefs = ["src-b|r2"] }, Rec(1, "r2", "kestrel morrow"));
        yield return ("Direct link, names differ",
            Rec(0, "r3", "kestrel morrow") with { SameAsRefs = ["src-b|r4"] }, Rec(1, "r4", "quill sabadine"));

        // ID numbers and birth dates.
        yield return ("ID number", Rec(0, "i1", "kestrel morrow", Phone) with { NationalId = "100000000001" },
                                   Rec(1, "i2", "kestrel morrow", Phone) with { NationalId = "100000000001" });
        yield return ("Different ID numbers", Rec(0, "i3", "kestrel morrow", Phone) with { NationalId = "100000000001" },
                                              Rec(1, "i4", "kestrel morrow", Phone) with { NationalId = "100000000002" });
        yield return ("Same birth date", Rec(0, "b1", "kestrel morrow", Phone) with { Dob = new DobInfo(1990, 14, 6) },
                                         Rec(1, "b2", "kestrel morrow", Phone) with { Dob = new DobInfo(1990, 6, 14) });
        yield return ("Different birth dates", Rec(0, "b3", "kestrel morrow", Phone) with { Dob = new DobInfo(1990, 14, 6) },
                                               Rec(1, "b4", "kestrel morrow", Phone) with { Dob = new DobInfo(1992, 3, 9) });

        // Nothing to compare at all.
        yield return ("Nothing to compare", Rec(0, "z1", ""), Rec(1, "z2", ""));
        yield return ("Names", Rec(0, "z3", "", Phone), Rec(1, "z4", "kestrel morrow", Phone));
    }

    [Fact]
    public void EveryBranch_IsReached_AndReadsInPlainLanguage()
    {
        var missed = new List<string>();
        var texts = new List<string>();
        foreach (var (title, a, b) in Pairs())
        {
            var steps = Trace(a, b);
            if (!steps.Any(s => s.Title == title)) missed.Add($"{title} (got: {string.Join(" | ", steps.Select(s => s.Title))})");
            texts.AddRange(steps.SelectMany(s => new[] { s.Title, s.Detail }));
        }
        Assert.True(missed.Count == 0, "pairs that did not reach their branch:\n" + string.Join("\n", missed));

        var offenders = Offenders(texts);
        Assert.True(offenders.Count == 0, "trace text with engine terms:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void TheDecision_IsNamedTheWayTheCaseBrowserNamesIt()
    {
        foreach (var (_, a, b) in Pairs())
        {
            var trace = new MatchTrace();
            double score = RealMatcher.Explain(a, b, trace);
            var decide = trace.Steps.Last();
            Assert.Equal("decide", decide.Stage);
            Assert.Equal(score == 0 && decide.Title == "Nothing to compare" ? "Nothing to compare" : CaseLabels.Decision(score).Label, decide.Title);
        }
    }

    [Fact]
    public void NormalizationSteps_ReadInPlainLanguage()
    {
        var garbled = Rec(0, "m1", "kestrel morrow", Phone) with
        {
            RawName = "Kestrel3 Morrow7",
            NameWasMojibake = true,
            NameHadArabizi = true,
            WeakPhones = ["770000502"],
            RawAddress = "north lumen 12|springfield",
            NormAddress = "north lumen 12 springfield",
            NormCity = "springfield",
        };
        var dropped = Rec(1, "m2", "") with { RawName = "???" };

        var texts = Normalization.Steps(garbled).Concat(Normalization.Steps(dropped)).Select(s => s.Detail).ToList();
        Assert.Contains(Normalization.Steps(garbled), s => s.Stage == "repair");
        Assert.Contains(Normalization.Steps(garbled), s => s.Stage == "arabizi");

        var offenders = Offenders(texts);
        Assert.True(offenders.Count == 0, "normalization text with engine terms:\n" + string.Join("\n", offenders));
    }
}

/// <summary>
/// The same wording rule for the per-tenant match rules that are off by default
/// (<see cref="Flags.StructuredNames"/>, <see cref="Flags.NamelessNeedsCorroboration"/>,
/// <see cref="Flags.OrgNameMerge"/>, <see cref="Flags.ServicedVinNameFloor"/>): with the options on,
/// every branch they add is reached with a synthetic pair and reads in plain language, and the default
/// pairs above still do. The options are process-global, so this class runs in the engine-flags
/// collection and restores them afterwards.
/// </summary>
[Collection(EngineFlagsCollection.Name)]
public class TraceWordingOptionTests
{
    private sealed class OptionsOn : IDisposable
    {
        private readonly bool structured = Flags.StructuredNames, strictTokens = Flags.StrictTokenAlignment,
            nameless = Flags.NamelessNeedsCorroboration, orgMerge = Flags.OrgNameMerge,
            vinName = Flags.ServicedVinNameFloor, strictParts = Flags.StrictNameParts;

        public OptionsOn()
        {
            Flags.StructuredNames = true;
            Flags.NamelessNeedsCorroboration = true;
            Flags.OrgNameMerge = true;
            Flags.ServicedVinNameFloor = true;
            Flags.StrictTokenAlignment = false;
            Flags.StrictNameParts = false;
        }

        public void Dispose()
        {
            Flags.StructuredNames = structured;
            Flags.StrictTokenAlignment = strictTokens;
            Flags.NamelessNeedsCorroboration = nameless;
            Flags.OrgNameMerge = orgMerge;
            Flags.ServicedVinNameFloor = vinName;
            Flags.StrictNameParts = strictParts;
        }
    }

    private static RealRecord Rec(int idx, string id, string name, string? phone = null, string? given = null, string? family = null) =>
        new(idx, idx % 2 == 0 ? "src-a" : "src-b", id, name, name, phone is null ? [] : [phone], [], null, null,
            GivenName: given, FamilyName: family);

    private static VinLink[] Sold(string vin) => [new(vin, VinSource.Sale, new DateOnly(2021, 3, 1), new DateOnly(2021, 3, 1))];
    private static VinLink[] Serviced(string vin) => [new(vin, VinSource.Service, new DateOnly(2021, 5, 1), new DateOnly(2022, 5, 1))];

    private const string Phone = "7700000501";
    private const string Vin = "SYNTHVIN000000003";

    /// <summary>One synthetic pair per branch the options add, with every trace title it must produce.</summary>
    public static IEnumerable<(string[] Titles, RealRecord A, RealRecord B)> Pairs()
    {
        // NamelessNeedsCorroboration: a shared number is all a nameless record has.
        yield return (["Same number, no name"], Rec(0, "q1", "", Phone), Rec(1, "q2", "kestrel morrow", Phone));

        // StructuredNames: different first names, different family names, unrelated names, on one number.
        yield return (["First names differ", "Same number, different names"],
            Rec(0, "s1", "kestrel morrow", Phone, "kestrel", "morrow"), Rec(1, "s2", "tobias morrow", Phone, "tobias", "morrow"));
        yield return (["Family names differ", "Same number, different names", "First or family names differ"],
            Rec(0, "s3", "kestrel morrow", Phone, "kestrel", "morrow"), Rec(1, "s4", "kestrel fenwick", Phone, "kestrel", "fenwick"));
        yield return (["First names differ", "Same number, unrelated names"],
            Rec(0, "s5", "kestrel morrow", Phone, "kestrel", "morrow"), Rec(1, "s6", "quill sabadine", Phone, "quill", "sabadine"));

        // OrgNameMerge: one company written two ways; two bodies whose long names differ only in the place.
        yield return (["Same number, one company's names"],
            Rec(0, "o1", "varden trading company", Phone), Rec(1, "o2", "trading company varden", Phone));
        yield return (["Company number", "Company number, names differ"],
            Rec(0, "o3", "quellmar regional executive authority of the state dorvanberg company", Phone),
            Rec(1, "o4", "quellmar regional executive authority of the state sabrinthal company", Phone));

        // A bought vehicle under each option.
        yield return (["Same vehicle bought, first or family names differ"],
            Rec(0, "v1", "kestrel morrow", given: "kestrel", family: "morrow") with { VinLinks = Sold(Vin) },
            Rec(1, "v2", "tobias morrow", given: "tobias", family: "morrow") with { VinLinks = Sold(Vin) });
        yield return (["Same vehicle bought, no name, different numbers"],
            Rec(0, "v3", "", "7700000601") with { VinLinks = Sold(Vin) },
            Rec(1, "v4", "kestrel morrow", "7700000602") with { VinLinks = Sold(Vin) });
        yield return (["Same vehicle bought, one company's names"],
            Rec(0, "v5", "brimvale company", "7700000601") with { VinLinks = Sold(Vin) },
            Rec(1, "v6", "brimvail", "7700000602") with { VinLinks = Serviced(Vin) });

        // ServicedVinNameFloor: a serviced vehicle and the same full name, no number at all.
        yield return (["Same vehicle serviced, same full name"],
            Rec(0, "v7", "kestrel morrow") with { VinLinks = Serviced(Vin) }, Rec(1, "v8", "kestrel morrow") with { VinLinks = Serviced(Vin) });
    }

    [Fact]
    public void EveryOptionBranch_IsReached_AndReadsInPlainLanguage()
    {
        using var _ = new OptionsOn();
        var missed = new List<string>();
        var texts = new List<string>();
        foreach (var (titles, a, b) in Pairs())
        {
            var steps = TraceWordingTests.Trace(a, b);
            foreach (var title in titles.Where(t => !steps.Any(s => s.Title == t)))
                missed.Add($"{title} (got: {string.Join(" | ", steps.Select(s => s.Title))})");
            texts.AddRange(steps.SelectMany(s => new[] { s.Title, s.Detail }));
        }
        // The default pairs take other paths with the options on; whatever they reach must read plainly too.
        foreach (var (_, a, b) in TraceWordingTests.Pairs())
            texts.AddRange(TraceWordingTests.Trace(a, b).SelectMany(s => new[] { s.Title, s.Detail }));
        Assert.True(missed.Count == 0, "pairs that did not reach their branch:\n" + string.Join("\n", missed));

        var offenders = TraceWordingTests.Offenders(texts);
        Assert.True(offenders.Count == 0, "trace text with engine terms:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void TheDecision_IsNamedTheWayTheCaseBrowserNamesIt_WithTheOptionsOn()
    {
        using var _ = new OptionsOn();
        foreach (var (_, a, b) in Pairs())
        {
            var trace = new MatchTrace();
            double score = RealMatcher.Explain(a, b, trace);
            var decide = trace.Steps.Last();
            Assert.Equal("decide", decide.Stage);
            Assert.Equal(CaseLabels.Decision(score).Label, decide.Title);
        }
    }
}
