using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// The explanations quote the records they describe, and a reader who may not see personal values
/// gets them through a <see cref="ValueQuoter"/>. These tests hold the two promises that makes:
/// without a quoter nothing changes, and with one no part of a quoted value reaches the text,
/// whatever punctuation the value carries. Values with an embedded double quote are the case a
/// pattern over the composed text got wrong (it took the quote inside a name for the end of the
/// quotation and let the rest through); values full of pattern metacharacters are the case any
/// value-to-pattern shortcut would get wrong.
/// </summary>
public class QuotedValueTests
{
    internal const string Hidden = "@@";

    /// <summary>A quoter that writes every value as a marker naming its kind, and remembers what it was given.</summary>
    internal sealed class Recorder
    {
        public readonly List<(QuotedValue Kind, string Value)> Seen = [];
        public string Quote(QuotedValue kind, string value)
        {
            Seen.Add((kind, value));
            return $"{Hidden}{kind}{Hidden}";
        }
    }

    // Synthetic values only. Quotes inside names and addresses, and every pattern metacharacter.
    private const string QuotedOrg = "Lumen \"Northwind\" Traders";
    private const string MetaName = @"Qu.ill* Sab+ad?ine (Var|den) [Orr]{2} ^Tob$ \d\w";
    private const string QuotedAddress = "12 \"Harbor\" Sorrel Rd.|(Brackwater)*|springfield";

    private static RealRecord Record(string rawName, string normName, string rawAddress = "", string normAddress = "") =>
        new(0, "src-a", "q1", rawName, normName, ["7700000701"], ["770000702"], null, null,
            RawAddress: rawAddress, NormAddress: normAddress, NormCity: normAddress.Length > 0 ? "springfield" : "");

    private static readonly System.Text.RegularExpressions.Regex Word = new(@"[\p{L}\p{N}]+");

    /// <summary>The words of a value a reader could recognise it by: each run of three or more letters or digits.</summary>
    private static IEnumerable<string> Words(string value) =>
        Word.Matches(value).Select(m => m.Value).Where(w => w.Length >= 3);

    /// <summary>
    /// No value appears in the text whole, and no word of one appears as a word of the text.
    /// <paramref name="allowed"/> names the words the text may legitimately share with a value: the
    /// city, which the contract shows, and engine wording a synthetic value happens to contain.
    /// </summary>
    internal static void AssertNoFragment(IEnumerable<string> texts, IEnumerable<string> values, params string[] allowed)
    {
        var textList = texts.ToList();
        var textWords = textList.SelectMany(t => Word.Matches(t).Select(m => m.Value)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var leaks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in values.Where(v => v.Length > 0 && !allowed.Contains(v, StringComparer.OrdinalIgnoreCase)))
        {
            // A value of punctuation alone identifies no one, and reads like the engine's own quoting.
            if (Words(v).Any() && textList.Any(t => t.Contains(v, StringComparison.OrdinalIgnoreCase))) leaks.Add(v);
            foreach (var w in Words(v))
                if (textWords.Contains(w) && !allowed.Contains(w, StringComparer.OrdinalIgnoreCase)) leaks.Add(w);
        }
        Assert.True(leaks.Count == 0, "quoted text still carries: " + string.Join(", ", leaks));
    }

    public static TheoryData<string, string, string, string> Records() => new()
    {
        { QuotedOrg, "lumen northwind traders", QuotedAddress, "harbor sorrel rd brackwater springfield" },
        { MetaName, "quill sabadine varden orr tob", "", "" },
        { "\"Northwind\"", "northwind", "\"|\"", "\" \"" },
        { "Kestrel \"\"Morrow", "kestrel morrow", "\\\"Harbor\\\"|springfield", "harbor springfield" },
    };

    [Theory]
    [MemberData(nameof(Records))]
    public void NormalizationSteps_WithoutAQuoter_AreWhatTheyWere(string rawName, string normName, string rawAddress, string normAddress)
    {
        var r = Record(rawName, normName, rawAddress, normAddress) with { NameWasMojibake = true };
        var plain = Normalization.Steps(r);
        Assert.Equal(plain, Normalization.Steps(r, null));
        Assert.Equal(plain, Normalization.Steps(r, (_, v) => v));
        // The values themselves are still there for a reader allowed to see them.
        Assert.Contains(plain, s => s.Stage == "name" && s.Detail.Contains(rawName, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Records))]
    public void NormalizationSteps_WithAQuoter_CarryNoPartOfAnyValue(string rawName, string normName, string rawAddress, string normAddress)
    {
        var r = Record(rawName, normName, rawAddress, normAddress) with { NameWasMojibake = true };
        var quoter = new Recorder();
        var steps = Normalization.Steps(r, quoter.Quote);

        // The engine's wording survives, so the reader still sees what happened.
        Assert.Contains(steps, s => s.Stage == "repair" && s.Detail.StartsWith("garbled text repaired: ", StringComparison.Ordinal));
        Assert.Contains(steps, s => s.Stage == "name" && s.Detail == $"\"{Hidden}Name{Hidden}\" → \"{Hidden}Name{Hidden}\"");
        Assert.Contains(steps, s => s.Stage == "phone" && s.Detail == $"{Hidden}Phone{Hidden} (full number) · {Hidden}Phone{Hidden} (incomplete number)");
        if (normAddress.Length > 0)
            Assert.Contains(steps, s => s.Stage == "address" && s.Detail == $"\"{Hidden}Address{Hidden}\" → \"{Hidden}Address{Hidden}\" · city \"{Hidden}City{Hidden}\"");

        // Each value went to the quoter whole and as the kind it is.
        Assert.Contains((QuotedValue.Name, rawName), quoter.Seen);
        Assert.Contains((QuotedValue.Name, normName), quoter.Seen);
        if (Mojibake.TryRepair(rawName) is { Length: > 0 } repaired) Assert.Contains((QuotedValue.Name, repaired), quoter.Seen);
        Assert.Contains((QuotedValue.Phone, "7700000701"), quoter.Seen);
        Assert.Contains((QuotedValue.Phone, "770000702"), quoter.Seen);
        if (normAddress.Length > 0)
        {
            Assert.Contains((QuotedValue.Address, rawAddress.Replace('|', ' ')), quoter.Seen);
            Assert.Contains((QuotedValue.Address, normAddress), quoter.Seen);
            Assert.Contains((QuotedValue.City, "springfield"), quoter.Seen);
        }

        // And nothing of any of them is left in the text, the city included: it went to the quoter too.
        AssertNoFragment(steps.Select(s => s.Detail),
            [rawName, normName, rawAddress, normAddress, "7700000701", "770000702", "springfield"]);
    }

    [Fact]
    public void NormalizationSteps_DroppedName_IsQuotedToo()
    {
        var r = Record(QuotedOrg, "");
        var steps = Normalization.Steps(r, new Recorder().Quote);
        var name = Assert.Single(steps, s => s.Stage == "name");
        Assert.Equal($"\"{Hidden}Name{Hidden}\" → dropped (not a usable name)", name.Detail);
    }

    // ------------------------------------------------------------------ the pair trace

    /// <summary>Every value a record could be quoted by in a trace, the feed's name parts included.</summary>
    internal static IEnumerable<string> ValuesOf(RealRecord r)
    {
        foreach (var v in new[] { r.RawName, r.NormName, r.RawAddress, r.NormAddress, r.NationalId ?? "", r.GivenName ?? "", r.FamilyName ?? "" })
            if (v.Length > 0) yield return v;
        foreach (var p in r.Phones.Concat(r.WeakPhones)) yield return p;
        foreach (var e in r.Emails ?? []) yield return e;
        if (r.Dob is { } d) yield return $"{d.P1}/{d.P2}/{d.Year}";
    }

    /// <summary>The scorer's branches (one pair each, from the wording suite), plus the address and
    /// name branches again with values a normalizer would never produce but a record can carry.</summary>
    public static IEnumerable<(string Title, RealRecord A, RealRecord B)> Pairs()
    {
        foreach (var p in TraceWordingTests.Pairs()) yield return p;

        const string Phone = "7700000501";
        var a = new RealRecord(0, "src-a", "t1", QuotedOrg, "kestrel \"morrow\" (var|den)", [Phone], [], null, null,
            RawAddress: QuotedAddress, NormAddress: "north \"lumen\" 12 [a-z]+ springfield", NormCity: "springfield");
        var b = new RealRecord(1, "src-b", "t2", MetaName, "kestrel \"fenwick\" (var|den)", [Phone], [], null, null,
            RawAddress: QuotedAddress, NormAddress: "north \"lumen\" 12 [a-z]+ springfield", NormCity: "springfield");
        yield return ("Names", a, b);
        yield return ("Address agrees", a, b);
        yield return ("Address differs", a, b with { NormAddress = "harbor \"sorrel\" 40 .* shelbyville", NormCity = "shelbyville" });
    }

    [Fact]
    public void Trace_WithoutAQuoter_IsWhatItWas()
    {
        foreach (var (_, a, b) in Pairs())
        {
            var plain = new MatchTrace();
            var identity = new MatchTrace((_, v) => v);
            double s1 = RealMatcher.Explain(a, b, plain);
            double s2 = RealMatcher.Explain(a, b, identity);
            Assert.Equal(s1, s2);
            Assert.Equal(plain.Steps, identity.Steps);
        }
    }

    [Fact]
    public void Trace_WithAQuoter_CarriesNoPartOfAnyValue_AndScoresTheSame()
    {
        var kinds = new HashSet<QuotedValue>();
        foreach (var (title, a, b) in Pairs())
        {
            var quoter = new Recorder();
            var masked = new MatchTrace(quoter.Quote);
            double score = RealMatcher.Explain(a, b, masked);

            // Masking is presentation only: the same score, the same steps, in the same order.
            var plain = new MatchTrace();
            Assert.Equal(RealMatcher.Explain(a, b, plain), score);
            Assert.Equal(plain.Steps.Select(s => (s.Stage, s.Title, s.ConfAfter)), masked.Steps.Select(s => (s.Stage, s.Title, s.ConfAfter)));
            Assert.Contains(masked.Steps, s => s.Title == title);

            // Every value the trace quoted came from one of the two records, whole.
            var values = ValuesOf(a).Concat(ValuesOf(b)).ToHashSet(StringComparer.Ordinal);
            foreach (var (kind, value) in quoter.Seen)
            {
                kinds.Add(kind);
                Assert.True(values.Contains(value) || IsSubCityPart(value, a, b), $"{kind} quoted a value from neither record");
            }

            // The city is shown beside every record by the contract, and one synthetic name is a
            // company's, a word the engine's own wording uses; nothing else may be left.
            AssertNoFragment(masked.Steps.Select(s => s.Detail), values, a.NormCity, b.NormCity, "company");
        }
        // Names, phones, birth dates and addresses all reach the quoter (e-mail is covered with its own flag).
        Assert.Superset(new HashSet<QuotedValue> { QuotedValue.Name, QuotedValue.Phone, QuotedValue.BirthDate, QuotedValue.Address }, kinds);
    }

    /// <summary>The district rule quotes the part of each address before the city.</summary>
    internal static bool IsSubCityPart(string value, RealRecord a, RealRecord b) =>
        value.Length > 0 && (a.NormAddress.Contains(value, StringComparison.Ordinal) || b.NormAddress.Contains(value, StringComparison.Ordinal));
}

/// <summary>
/// The same two promises for the scorer branches the per-tenant match rules add, which are off by
/// default (<see cref="Flags.StructuredNames"/>, <see cref="Flags.NamelessNeedsCorroboration"/>,
/// <see cref="Flags.OrgNameMerge"/>, <see cref="Flags.ServicedVinNameFloor"/>, and on top of them
/// <see cref="Flags.StrictTokenAlignment"/>, <see cref="Flags.StrictNameParts"/> and
/// <see cref="Flags.FamilyNameSexEndings"/>). Two of those branches quote the feed's name parts. With
/// the options on, every option branch is driven again with names and name parts carrying quotes and
/// pattern metacharacters: a reader who may not see them gets no part of any value, and without a
/// quoter the text is what it was, values included. A vehicle number is written as recorded either
/// way, as on the default branches: the record view shows it beside the record. The options are
/// process-global, so this class runs in the engine-flags collection and restores them.
/// </summary>
[Collection(EngineFlagsCollection.Name)]
public class QuotedValueOptionTests
{
    /// <summary>The scorer options the per-tenant rules read, set for one configuration and restored afterwards.</summary>
    private sealed class Options : IDisposable
    {
        private readonly bool structured = Flags.StructuredNames, strictTokens = Flags.StrictTokenAlignment,
            nameless = Flags.NamelessNeedsCorroboration, orgMerge = Flags.OrgNameMerge,
            vinName = Flags.ServicedVinNameFloor, strictParts = Flags.StrictNameParts, email = Flags.EmailMatching;
        private readonly IReadOnlySet<string> established = Flags.EstablishedNameParts;
        private readonly IReadOnlyList<(string From, string To)> folds = Flags.NamePartFolds, tokenFolds = Flags.TokenFolds;
        private readonly IReadOnlyList<string> suffixes = Flags.GivenNameSuffixes, endings = Flags.FamilyNameEndings;
        private readonly IReadOnlyList<(string Masculine, string Feminine)> sex = Flags.FamilyNameSexEndings;

        /// <param name="strict">Also the stricter rules a tenant layers on the four: one-to-one token
        /// alignment, strict name parts over an attested set, family-name sex endings, e-mail matching.</param>
        public Options(bool strict)
        {
            Flags.StructuredNames = true;
            Flags.NamelessNeedsCorroboration = true;
            Flags.OrgNameMerge = true;
            Flags.ServicedVinNameFloor = true;
            Flags.StrictTokenAlignment = strict;
            Flags.StrictNameParts = strict;
            Flags.EmailMatching = strict;
            Flags.EstablishedNameParts = strict ? new HashSet<string> { AttestedGivenA, AttestedGivenB } : new HashSet<string>();
            Flags.FamilyNameSexEndings = strict ? [("ov", "ova")] : [];
            Flags.TokenFolds = [];
            Flags.NamePartFolds = [];
            Flags.GivenNameSuffixes = [];
            Flags.FamilyNameEndings = [];
        }

        public void Dispose()
        {
            Flags.StructuredNames = structured;
            Flags.StrictTokenAlignment = strictTokens;
            Flags.NamelessNeedsCorroboration = nameless;
            Flags.OrgNameMerge = orgMerge;
            Flags.ServicedVinNameFloor = vinName;
            Flags.StrictNameParts = strictParts;
            Flags.EmailMatching = email;
            Flags.EstablishedNameParts = established;
            Flags.TokenFolds = tokenFolds;
            Flags.NamePartFolds = folds;
            Flags.GivenNameSuffixes = suffixes;
            Flags.FamilyNameEndings = endings;
            Flags.FamilyNameSexEndings = sex;
        }
    }

    // Synthetic values only: name parts and names with embedded quotes and pattern metacharacters.
    private const string GivenA = "kes\"trel (var|den)*";
    private const string GivenB = "to[b]ias.+? ^qu$";
    private const string FamilyA = "mor\"row\\d";
    private const string FamilyB = "fen[wick]{2} \\w+";
    private const string AttestedGivenA = "ma[r]vin\"";   // two attested given names one consonant apart
    private const string AttestedGivenB = "ma[l]vin\"";
    private const string SexFamilyA = "dor\"vanov";      // one family name in its two sex forms
    private const string SexFamilyB = "dor\"vanova";
    private const string Phone = "7700000501";
    private const string Vin = "SYNTHVIN000000004";

    private static RealRecord Rec(int idx, string id, string name, string? phone = Phone, string? given = null, string? family = null) =>
        new(idx, idx % 2 == 0 ? "src-a" : "src-b", id, name, name, phone is null ? [] : [phone], [], null, null,
            GivenName: given, FamilyName: family);

    private static VinLink[] Sold() => [new(Vin, VinSource.Sale, new DateOnly(2021, 3, 1), new DateOnly(2021, 3, 1))];
    private static VinLink[] Serviced() => [new(Vin, VinSource.Service, new DateOnly(2021, 5, 1), new DateOnly(2022, 5, 1))];

    /// <summary>The option branches again, each with values a normalizer would never produce but a record can carry.</summary>
    private static IEnumerable<(string[] Titles, RealRecord A, RealRecord B)> MetaPairs()
    {
        yield return (["Same number, no name"], Rec(0, "m1", ""), Rec(1, "m2", $"{GivenA} {FamilyA}"));
        yield return (["First names differ"],
            Rec(0, "m3", $"{GivenA} {FamilyA}", given: GivenA, family: FamilyA), Rec(1, "m4", $"{GivenB} {FamilyA}", given: GivenB, family: FamilyA));
        yield return (["Family names differ"],
            Rec(0, "m5", $"{GivenA} {FamilyA}", given: GivenA, family: FamilyA), Rec(1, "m6", $"{GivenA} {FamilyB}", given: GivenA, family: FamilyB));
        yield return (["Same number, one company's names"],
            Rec(0, "m7", "var\"den\" (a|b)* trading company"), Rec(1, "m8", "trading company var\"den\" (a|b)*"));
        yield return (["Same vehicle bought, first or family names differ", "First names differ"],
            Rec(0, "m9", $"{GivenA} {FamilyA}", null, GivenA, FamilyA) with { VinLinks = Sold() },
            Rec(1, "m10", $"{GivenB} {FamilyA}", null, GivenB, FamilyA) with { VinLinks = Sold() });
        yield return (["Same vehicle bought, no name, different numbers"],
            Rec(0, "m11", "", "7700000601") with { VinLinks = Sold() }, Rec(1, "m12", $"{GivenA} {FamilyA}", "7700000602") with { VinLinks = Sold() });
        yield return (["Same vehicle bought, one company's names"],
            Rec(0, "m13", "brim\"vale company", "7700000601") with { VinLinks = Sold() }, Rec(1, "m14", "brim\"vail", "7700000602") with { VinLinks = Serviced() });
        yield return (["Same vehicle serviced, same full name"],
            Rec(0, "m15", $"{GivenA} {FamilyA}", null) with { VinLinks = Serviced() }, Rec(1, "m16", $"{GivenA} {FamilyA}", null) with { VinLinks = Serviced() });
    }

    /// <summary>The branches only the stricter rules reach: two attested given names one consonant
    /// apart, and one family name in its two sex forms without given names to vouch for it.</summary>
    private static IEnumerable<(string[] Titles, RealRecord A, RealRecord B)> StrictPairs()
    {
        yield return (["First names differ"],
            Rec(0, "s1", $"{AttestedGivenA} {FamilyA}", given: AttestedGivenA, family: FamilyA),
            Rec(1, "s2", $"{AttestedGivenB} {FamilyA}", given: AttestedGivenB, family: FamilyA));
        yield return (["Family names differ"],
            Rec(0, "s3", $"{GivenA} {SexFamilyA}", given: GivenA, family: SexFamilyA),
            Rec(1, "s4", SexFamilyB, family: SexFamilyB));
    }

    /// <summary>Per configuration, every pair with the titles it must reach. Under the stricter rules the
    /// four options' pairs may take other paths; whatever they reach must still mask.</summary>
    public static TheoryData<bool> Configurations() => new() { false, true };

    private static IEnumerable<(string[] Titles, RealRecord A, RealRecord B)> PairsFor(bool strict) => strict
        ? StrictPairs().Concat(MetaPairs().Concat(TraceWordingOptionTests.Pairs()).Select(p => (Array.Empty<string>(), p.A, p.B)))
        : TraceWordingOptionTests.Pairs().Concat(MetaPairs());

    [Theory]
    [MemberData(nameof(Configurations))]
    public void OptionTrace_WithoutAQuoter_IsWhatItWas_ValuesIncluded(bool strict)
    {
        using var options = new Options(strict);
        var partSteps = 0;
        foreach (var (titles, a, b) in PairsFor(strict))
        {
            var plain = new MatchTrace();
            var identity = new MatchTrace((_, v) => v);
            Assert.Equal(RealMatcher.Explain(a, b, plain), RealMatcher.Explain(a, b, identity));
            Assert.Equal(plain.Steps, identity.Steps);
            foreach (var title in titles) Assert.Contains(plain.Steps, s => s.Title == title);

            // The name-part branches quote both parts as recorded, punctuation and all.
            foreach (var s in plain.Steps.Where(s => s.Title == "First names differ"))
            {
                Assert.Contains($"\"{a.GivenName}\" and \"{b.GivenName}\"", s.Detail, StringComparison.Ordinal);
                partSteps++;
            }
            foreach (var s in plain.Steps.Where(s => s.Title == "Family names differ"))
            {
                Assert.Contains($"\"{a.FamilyName}\" and \"{b.FamilyName}\"", s.Detail, StringComparison.Ordinal);
                partSteps++;
            }
        }
        Assert.True(partSteps >= 2, "the name-part branches were not reached");
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void OptionTrace_WithAQuoter_CarriesNoPartOfAnyValue_AndScoresTheSame(bool strict)
    {
        using var options = new Options(strict);
        var reached = new HashSet<string>();
        foreach (var (titles, a, b) in PairsFor(strict))
        {
            var quoter = new QuotedValueTests.Recorder();
            var masked = new MatchTrace(quoter.Quote);
            double score = RealMatcher.Explain(a, b, masked);

            // Masking is presentation only: the same score, the same steps, in the same order.
            var plain = new MatchTrace();
            Assert.Equal(RealMatcher.Explain(a, b, plain), score);
            Assert.Equal(plain.Steps.Select(s => (s.Stage, s.Title, s.ConfAfter)), masked.Steps.Select(s => (s.Stage, s.Title, s.ConfAfter)));
            foreach (var title in titles) Assert.Contains(masked.Steps, s => s.Title == title);
            reached.UnionWith(masked.Steps.Select(s => s.Title));

            // Every value the trace quoted came from one of the two records, whole; the name-part
            // branches handed both parts over as names.
            var values = QuotedValueTests.ValuesOf(a).Concat(QuotedValueTests.ValuesOf(b)).ToHashSet(StringComparer.Ordinal);
            foreach (var (kind, value) in quoter.Seen)
                Assert.True(values.Contains(value) || QuotedValueTests.IsSubCityPart(value, a, b), $"{kind} quoted a value from neither record");
            const string M = QuotedValueTests.Hidden + nameof(QuotedValue.Name) + QuotedValueTests.Hidden;
            foreach (var s in masked.Steps.Where(s => s.Title == "First names differ"))
            {
                Assert.Contains((QuotedValue.Name, a.GivenName!), quoter.Seen);
                Assert.Contains((QuotedValue.Name, b.GivenName!), quoter.Seen);
                Assert.StartsWith($"the first names \"{M}\" and \"{M}\" are ", s.Detail, StringComparison.Ordinal);
            }
            foreach (var s in masked.Steps.Where(s => s.Title == "Family names differ"))
            {
                Assert.Contains((QuotedValue.Name, a.FamilyName!), quoter.Seen);
                Assert.Contains((QuotedValue.Name, b.FamilyName!), quoter.Seen);
                Assert.StartsWith($"the family names \"{M}\" and \"{M}\" are ", s.Detail, StringComparison.Ordinal);
            }

            // Words the engine's own wording shares with a synthetic company name ("company", and
            // "the" from a long official title) are the only ones allowed to remain.
            QuotedValueTests.AssertNoFragment(masked.Steps.Select(s => s.Detail), values, "company", "the");
        }
        Assert.Contains("First names differ", reached);
        Assert.Contains("Family names differ", reached);
    }
}
