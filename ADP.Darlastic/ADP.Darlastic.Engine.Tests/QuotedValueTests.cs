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
    private const string Hidden = "@@";

    /// <summary>A quoter that writes every value as a marker naming its kind, and remembers what it was given.</summary>
    private sealed class Recorder
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
    private static void AssertNoFragment(IEnumerable<string> texts, IEnumerable<string> values, params string[] allowed)
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
            Assert.Contains(steps, s => s.Stage == "address" && s.Detail == $"\"{Hidden}Address{Hidden}\" → \"{Hidden}Address{Hidden}\" · city \"springfield\"");

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
        }

        // And nothing of any of them is left in the text. The city is shown beside every record by
        // the contract, so it is the one word an address may leave behind.
        AssertNoFragment(steps.Select(s => s.Detail),
            [rawName, normName, rawAddress, normAddress, "7700000701", "770000702"], "springfield");
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

    /// <summary>Every value a record could be quoted by in a trace.</summary>
    private static IEnumerable<string> ValuesOf(RealRecord r)
    {
        foreach (var v in new[] { r.RawName, r.NormName, r.RawAddress, r.NormAddress, r.NationalId ?? "" })
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
    private static bool IsSubCityPart(string value, RealRecord a, RealRecord b) =>
        value.Length > 0 && (a.NormAddress.Contains(value, StringComparison.Ordinal) || b.NormAddress.Contains(value, StringComparison.Ordinal));
}
