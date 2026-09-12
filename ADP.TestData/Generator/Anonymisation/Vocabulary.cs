using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ADP.TestData.Generator.Anonymisation;

/// <summary>
/// The private word list the anonymiser applies to free text: brand and programme names, abbreviations,
/// place names, people — whatever a distributor's own texts carry that would place it. The list lives with
/// the seed, outside this repository; this code only knows the file's shape. Each term names how it is
/// matched and what replaces it:
/// <list type="bullet">
/// <item><c>mode</c>: <c>word</c> (default; not glued to other letters — a digit may follow, as in a model name with a generation number), <c>substring</c>, or <c>regex</c>.</item>
/// <item><c>replace</c>: a literal (it takes the case shape of the matched text), <c>@word</c> (each word of the
/// match becomes the keyed coined word the model descriptions use for it — so a model name in a comment
/// matches the same model's name on the specification), <c>@model</c> (the match is re-keyed as a model code —
/// the prefix-preserving family the katashiki and variant fields use, so a model code in a comment matches
/// the specification's), <c>@fps</c> (a keyed, format-preserving re-keying of
/// the matched text), or <c>@family:&lt;name&gt;</c> (the matched text is re-keyed as a member of that
/// identifier family — a campaign code cited in a comment maps to the same synthetic code as the campaign
/// record).</item>
/// <item><c>ignoreCase</c>: default true; false for short abbreviations that are also words.</item>
/// </list>
/// </summary>
public sealed class Vocabulary
{
    public List<VocabularyTerm> Terms { get; set; } = new();

    public static Vocabulary Load(string? path)
    {
        if (path is null)
            return new Vocabulary();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        options.Converters.Add(new JsonStringEnumConverter());

        return JsonSerializer.Deserialize<Vocabulary>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException($"Could not read the vocabulary at '{path}'.");
    }
}

public sealed class VocabularyTerm
{
    public string Match { get; set; } = "";
    public VocabularyMode Mode { get; set; } = VocabularyMode.Word;
    public string Replace { get; set; } = "";
    public bool IgnoreCase { get; set; } = true;
    /// <summary>A note for the maintainer; ignored here.</summary>
    public string? Why { get; set; }
}

public enum VocabularyMode { Word, Substring, Regex }

/// <summary>
/// Rewrites free-text values: first the names the identifier families assigned (a company or place named
/// in a comment reads as the same fictional company or place the fixture names elsewhere — and a full name
/// must win before the vocabulary sees the abbreviation inside it), then the private vocabulary, then the
/// identifiers cited verbatim (a campaign code or claim number in a comment), then any GUID or long numeric
/// token. Runs in both passes — collecting first, so the words and family members it meets are assigned
/// with everyone else's.
/// </summary>
public sealed class TextRewriter
{
    private static readonly Regex Guid = new(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", RegexOptions.Compiled);
    private static readonly Regex LongNumber = new(@"(?<![\p{L}\p{N}])\d{12,}(?![\p{L}\p{N}])", RegexOptions.Compiled);
    private static readonly Regex Letters = new(@"\p{L}+", RegexOptions.Compiled);

    private readonly Keyed keyed;
    private readonly Func<string, Family?> family;
    private readonly Family words;
    private readonly List<(Regex pattern, VocabularyTerm term)> terms = new();
    private readonly List<(Regex pattern, string replacement)> names = new();
    private readonly List<(Regex pattern, Family family)> exactFamilies = new();

    public TextRewriter(Keyed keyed, Vocabulary vocabulary, Family words, Func<string, Family?> family)
    {
        this.keyed = keyed;
        this.words = words;
        this.family = family;

        foreach (var term in vocabulary.Terms)
        {
            var options = RegexOptions.CultureInvariant | (term.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            var pattern = term.Mode switch
            {
                VocabularyMode.Word => @"(?<!\p{L})" + Regex.Escape(term.Match) + @"(?!\p{L})",
                VocabularyMode.Substring => Regex.Escape(term.Match),
                _ => term.Match,
            };

            terms.Add((new Regex(pattern, options), term));
        }
    }

    /// <summary>
    /// After the families are assigned: the real names of companies, branches, brokers, places and the like
    /// become vocabulary of their own, longest first so a company named after its city is replaced whole.
    /// Three-letter names match only in their own case (a short abbreviation is often also a word).
    /// </summary>
    public void AddNames(IEnumerable<KeyValuePair<string, string>> realToFictional)
    {
        foreach (var (real, fictional) in realToFictional.OrderByDescending(p => p.Key.Length))
        {
            if (real.Trim().Length < 3 || string.Equals(real, fictional, StringComparison.Ordinal))
                continue;

            var options = RegexOptions.CultureInvariant | (real.Trim().Length > 3 ? RegexOptions.IgnoreCase : RegexOptions.None);
            names.Add((new Regex(@"(?<!\p{L})" + Regex.Escape(real.Trim()) + @"(?!\p{L})", options), fictional));
        }
    }

    /// <summary>
    /// Every real value of a family that <paramref name="where"/> admits is replaced wherever it appears whole
    /// inside free text (a campaign code or a claim number cited in a comment). Values that are plain words
    /// (a labor code spelled "NOTES") are left to the vocabulary, or the text would be garbled.
    /// </summary>
    public void AddExactFamily(Family family, Func<string, bool>? where = null)
    {
        var alternatives = family.Real.Where(where ?? (_ => true)).OrderByDescending(v => v.Length).Select(Regex.Escape).ToList();
        if (alternatives.Count == 0)
            return;

        exactFamilies.Add((new Regex(@"(?<![\p{L}\p{N}])(?:" + string.Join("|", alternatives) + @")(?![\p{L}\p{N}])", RegexOptions.CultureInvariant), family));
    }

    public void Collect(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        foreach (var (pattern, term) in terms)
        {
            foreach (Match match in pattern.Matches(text))
            {
                if (term.Replace == "@word")
                    foreach (Match word in Letters.Matches(match.Value))
                        words.Collect(word.Value);
                else if (term.Replace.StartsWith("@family:", StringComparison.Ordinal))
                    Resolve(term.Replace).Collect(match.Value);
            }
        }
    }

    public string? Rewrite(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // The assigned names go first: a company name is longer than the abbreviation the vocabulary lists
        // for it, and must win while it is still whole.
        foreach (var (pattern, replacement) in names)
            text = pattern.Replace(text, m => CaseShape.Like(replacement, m.Value));

        foreach (var (pattern, term) in terms)
            text = pattern.Replace(text, m => Replacement(term, m.Value));

        foreach (var (pattern, exact) in exactFamilies)
            text = pattern.Replace(text, m => exact.Get(m.Value)!);

        text = Guid.Replace(text, m => KeyedGuid(m.Value));
        text = LongNumber.Replace(text, m => keyed.Digits("text-number", m.Value, m.Value.Length, nonZeroLead: m.Value[0] != '0'));

        return text;
    }

    public string Word(string word) => CaseShape.Like(words.GetOrAssign(word), word);

    public string KeyedGuid(string real)
    {
        var hex = keyed.Hex("guid", real.ToLowerInvariant(), 32, upper: real.Any(char.IsUpper));
        return $"{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..]}";
    }

    private string Replacement(VocabularyTerm term, string matched)
    {
        if (term.Replace == "@word")
            return Letters.Replace(matched, m => Word(m.Value));

        if (term.Replace == "@fps")
            return keyed.Substitute("text", matched);

        if (term.Replace == "@model")
            return keyed.SubstitutePrefixPreserving("model", matched);

        if (term.Replace.StartsWith("@family:", StringComparison.Ordinal))
            return Resolve(term.Replace).GetOrAssign(matched);

        return term.Mode == VocabularyMode.Regex ? term.Replace : CaseShape.Like(term.Replace, matched);
    }

    private Family Resolve(string directive)
    {
        var name = directive["@family:".Length..];
        return family(name) ?? throw new InvalidOperationException($"The vocabulary names an unknown family '{name}'.");
    }
}
