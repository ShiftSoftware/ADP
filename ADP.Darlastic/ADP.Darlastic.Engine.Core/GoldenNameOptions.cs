using System.Globalization;
using System.Text;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// How a golden's name is shown (survivorship of the <c>full_name</c> attribute). Per tenant; the defaults
/// are the engine as shipped.
///
/// <para><b>Off (the default).</b> The golden's name is the survived MATCH form, title-cased: the
/// normalized name chain survivorship picked. For a tenant whose records are written in Latin letters
/// that is the name as people wrote it; for one whose records are in another script it is the
/// normalizer's transliteration (Arabic script, for instance, comes out as a Latin consonant skeleton).</para>
///
/// <para><b>On (<see cref="DisplayFromRecordNames"/>).</b> Survivorship still picks the same chain the
/// same way — matching, clustering and every score are untouched, because they read each record's own
/// normalized name, never the golden's — but the value staged and served as the golden's name is that
/// chain as one of its records actually wrote it, in its own script: the raw name of a member record
/// whose normalized name IS the chosen chain, after the cleanup <see cref="GoldenDisplayName.Clean"/>
/// describes (a garbled-codepage name is repaired first; a placeholder never qualifies, since its
/// normalized name is blank). When several records qualify, the one from the strongest source wins
/// (<see cref="SourceRank"/>), then the most complete spelling (most words, then most letters), then the
/// spelling most records share, then the ordinal order of spelling, source and record id — so the choice
/// never depends on input order and a re-run stages the same value. When no record qualifies (every
/// spelling of the chain carries digits, for instance) the title-cased match form is kept.</para>
/// </summary>
public sealed record GoldenNameOptions
{
    /// <summary>The engine as shipped: the golden's name is the title-cased match form.</summary>
    public static GoldenNameOptions Default { get; } = new();

    /// <summary>Show the survived chain as one of its records wrote it (see the type summary). Off by default.</summary>
    public bool DisplayFromRecordNames { get; init; }

    /// <summary>
    /// Source systems by strength for the display choice: lower ranks first; equal ranks tie. A source not
    /// listed takes <see cref="UnlistedRank"/>. Only read with <see cref="DisplayFromRecordNames"/> on; empty
    /// (the default) ranks every source alike.
    /// </summary>
    public IReadOnlyDictionary<string, int> SourceRank { get; init; } = new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>The rank of every source <see cref="SourceRank"/> does not list: after every listed one by
    /// default; a tenant that lists only its weaker sources puts the rest first with a lower value.</summary>
    public int UnlistedRank { get; init; } = int.MaxValue;

    /// <summary>The rank of <paramref name="sourceSystem"/>.</summary>
    public int RankOf(string sourceSystem) => SourceRank.TryGetValue(sourceSystem, out var rank) ? rank : UnlistedRank;

    /// <summary>One line for a run log (source systems and ranks, never a value).</summary>
    public string Describe()
    {
        if (!DisplayFromRecordNames) return "match form (title-cased)";
        if (SourceRank.Count == 0) return "as the records wrote them (every source alike)";
        var groups = SourceRank.Select(kv => (Rank: kv.Value, Name: kv.Key))
            .Append((Rank: UnlistedRank, Name: "other sources"))
            .GroupBy(x => x.Rank).OrderBy(g => g.Key)
            .Select(g => string.Join("/", g.Select(x => x.Name).Order(StringComparer.Ordinal)));
        return "as the records wrote them (" + string.Join(" > ", groups) + ")";
    }
}

/// <summary>The display-name half of golden name survivorship (<see cref="GoldenNameOptions.DisplayFromRecordNames"/>).</summary>
public static class GoldenDisplayName
{
    /// <summary>
    /// The golden's display name: the chosen chain (<paramref name="chosenNormName"/>, a member's normalized
    /// name) as the strongest qualifying member wrote it, or null when none qualifies (the caller keeps the
    /// match form). See <see cref="GoldenNameOptions"/> for the order.
    /// </summary>
    public static string? Choose(IReadOnlyList<RealRecord> members, string chosenNormName, GoldenNameOptions options)
    {
        if (chosenNormName.Length == 0) return null;
        var candidates = new List<(RealRecord Record, string Display, int Words, int Letters)>();
        foreach (var m in members)
        {
            if (!string.Equals(m.NormName, chosenNormName, StringComparison.Ordinal)) continue;
            if (Clean(m.RawName, m.NameWasMojibake) is not { } display) continue;
            candidates.Add((m, display, display.Split(' ').Length, display.Count(char.IsLetter)));
        }
        if (candidates.Count == 0) return null;

        var shared = candidates.GroupBy(c => c.Display, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        return candidates
            .OrderBy(c => options.RankOf(c.Record.SourceSystem))
            .ThenByDescending(c => c.Words)
            .ThenByDescending(c => c.Letters)
            .ThenByDescending(c => shared[c.Display])
            .ThenBy(c => c.Display, StringComparer.Ordinal)
            .ThenBy(c => c.Record.SourceSystem, StringComparer.Ordinal)
            .ThenBy(c => c.Record.SourceRecordId, StringComparer.Ordinal)
            .First().Display;
    }

    /// <summary>
    /// A record's raw name made presentable, or null when it cannot stand as a display name.
    /// <list type="bullet">
    /// <item>A name the feed flagged as garbled codepage text (<paramref name="wasMojibake"/>) is repaired the
    /// way its normalized form was (<see cref="Mojibake.TryRepair"/>); one that does not repair is refused.</item>
    /// <item>Canonical composition (NFC); control, format and direction characters dropped (the zero-width
    /// non-joiner, which Kurdish and Persian spelling needs, is kept inside a word); the Arabic tatweel dropped.</item>
    /// <item>Whitespace collapsed to single spaces; punctuation and symbols trimmed from both ends (a closing
    /// bracket whose opening bracket is in the name stays: "Name (Company)").</item>
    /// <item>Latin letters all in one case (ALL CAPS or all lower) are title-cased; mixed case stays as written.</item>
    /// <item>Refused when a digit remains (a serial or a chat numeral is not part of a name to show) or fewer than
    /// two letters do.</item>
    /// </list>
    /// </summary>
    public static string? Clean(string? raw, bool wasMojibake = false)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        string s = raw;
        if (wasMojibake)
        {
            if (Mojibake.TryRepair(s) is not { } repaired) return null;
            s = repaired;
        }
        try { s = s.Normalize(NormalizationForm.FormC); }
        catch (ArgumentException) { return null; }   // an invalid code point (a lone surrogate)

        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (char.IsWhiteSpace(c)) { sb.Append(' '); continue; }
            if (c == '‌') { sb.Append(c); continue; }                 // ZWNJ: part of Kurdish / Persian spelling
            if (c == 'ـ') continue;                                   // Arabic tatweel: stretching only
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate
                or UnicodeCategory.PrivateUse or UnicodeCategory.OtherNotAssigned) continue;
            sb.Append(c);
        }

        var words = sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('‌'))
            .Where(w => w.Length > 0);
        string joined = string.Join(' ', words);
        int start = 0, end = joined.Length;
        while (start < end && IsEdgeJunk(joined[start])) start++;
        while (end > start && IsEdgeJunk(joined[end - 1]) && !ClosesABracket(joined, start, end - 1)) end--;
        joined = joined[start..end].Trim();

        if (joined.Any(char.IsDigit) || joined.Count(char.IsLetter) < 2) return null;

        bool anyUpper = false, anyLower = false;
        foreach (char c in joined)
        {
            if (c is >= 'A' and <= 'Z') anyUpper = true;
            else if (c is >= 'a' and <= 'z') anyLower = true;
        }
        if (anyUpper != anyLower)   // Latin letters present, all in one case
            joined = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(joined.ToLowerInvariant());
        return joined;
    }

    private static bool IsEdgeJunk(char c) =>
        char.IsWhiteSpace(c) || c == '‌' || char.IsPunctuation(c) || char.IsSymbol(c);

    /// <summary>A closing bracket at <paramref name="at"/> whose opening bracket is in the name ("Name (Company)").</summary>
    private static bool ClosesABracket(string s, int start, int at) => s[at] switch
    {
        ')' => s.IndexOf('(', start, at - start) >= 0,
        ']' => s.IndexOf('[', start, at - start) >= 0,
        _ => false,
    };
}
