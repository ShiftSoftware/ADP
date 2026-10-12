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
    // First: Default below is built with it (static initializers run in declaration order).
    private static readonly IReadOnlySet<string> NoNames = new HashSet<string>(StringComparer.Ordinal);

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

    // ── which chains may be shown (all off by default) ─────────────────────────────────────────────
    // A chain these rules refuse never stands as the golden's name: survivorship picks the best chain among
    // the others (same order as always), and a golden whose every chain is refused has no shown name. They
    // read only names the resolve already normalized, and change only the name-bearing artifacts — matching,
    // clustering and every score read each record's own normalized name, never the golden's.

    /// <summary>
    /// Whole normalized names (as <see cref="Norm.Name"/> writes them) that name nobody — the placeholders a
    /// tenant's sources type into a required name field ("unknown", "no name" ...). Exact match on the whole
    /// chain. Empty (the default): every chain may be shown.
    /// </summary>
    public IReadOnlySet<string> PlaceholderNames { get; init; } = NoNames;

    /// <summary>
    /// Normalized tokens that name nobody on their own (the letters of "n/a", "test", a bare title such as
    /// "mr"): a chain made ONLY of these tokens is a placeholder ("n a n a", "test test", "mr"). A real name
    /// that merely contains one is untouched. Empty (the default): no token rule.
    /// </summary>
    public IReadOnlySet<string> PlaceholderTokens { get; init; } = NoNames;

    /// <summary>A chain with fewer letters than this is junk and never shown ("ab", "x y"). 0 (the default): no minimum.</summary>
    public int MinimumLetters { get; init; }

    /// <summary>
    /// Show only presentable spellings (off by default). With <see cref="DisplayFromRecordNames"/> on, a record's
    /// spelling is never the shown name when it is e-mail-shaped (holds an @), carries an emoji or another
    /// pictograph, the replacement character U+FFFD (a letter lost in transcoding) or any numeral (the cleanup
    /// already refuses decimal digits); another spelling of the chain, or its match form, is shown instead. And
    /// a chain every spelling of which is e-mail-shaped is an address, not a name: it is never shown, in either
    /// form.
    /// </summary>
    public bool PresentableOnly { get; init; }

    /// <summary>True when a rule above may refuse a chain.</summary>
    public bool FiltersChains => PlaceholderNames.Count > 0 || PlaceholderTokens.Count > 0 || MinimumLetters > 0 || PresentableOnly;

    /// <summary>
    /// Whether the chain <paramref name="normName"/> (a member's normalized name) may stand as the golden's
    /// shown name under these options. <paramref name="members"/> are the cluster's records (read only for
    /// <see cref="PresentableOnly"/>: the spellings of the chain).
    /// </summary>
    public bool MayShow(string normName, IReadOnlyList<RealRecord> members)
    {
        if (normName.Length == 0) return false;
        if (PlaceholderNames.Contains(normName)) return false;
        if (PlaceholderTokens.Count > 0 && normName.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(PlaceholderTokens.Contains))
            return false;
        if (MinimumLetters > 0 && normName.Count(char.IsLetter) < MinimumLetters) return false;
        if (PresentableOnly)
        {
            bool spelled = false;
            foreach (var m in members)
            {
                if (!string.Equals(m.NormName, normName, StringComparison.Ordinal)) continue;
                if (!GoldenDisplayName.IsEmailShaped(m.RawName)) return true;
                spelled = true;
            }
            if (spelled) return false;
        }
        return true;
    }

    /// <summary>One line for a run log (source systems, ranks and rule sizes, never a value).</summary>
    public string Describe()
    {
        string rules = FiltersChains
            ? FormattableString.Invariant($"; never shown: {PlaceholderNames.Count} placeholder names, chains of {PlaceholderTokens.Count} placeholder tokens")
              + (MinimumLetters > 0 ? FormattableString.Invariant($", chains under {MinimumLetters} letters") : "")
              + (PresentableOnly ? ", unpresentable spellings" : "")
            : "";
        if (!DisplayFromRecordNames) return "match form (title-cased)" + rules;
        if (SourceRank.Count == 0) return "as the records wrote them (every source alike)" + rules;
        var groups = SourceRank.Select(kv => (Rank: kv.Value, Name: kv.Key))
            .Append((Rank: UnlistedRank, Name: "other sources"))
            .GroupBy(x => x.Rank).OrderBy(g => g.Key)
            .Select(g => string.Join("/", g.Select(x => x.Name).Order(StringComparer.Ordinal)));
        return "as the records wrote them (" + string.Join(" > ", groups) + ")" + rules;
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
            if (options.PresentableOnly && !IsPresentable(m.RawName, m.NameWasMojibake)) continue;
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

    /// <summary>A raw name that holds an @: an e-mail address (or part of one) typed into the name field.</summary>
    public static bool IsEmailShaped(string? raw) => raw is not null && raw.Contains('@');

    /// <summary>
    /// Whether a record's spelling may be shown under <see cref="GoldenNameOptions.PresentableOnly"/>: not
    /// e-mail-shaped, and (after the garbled-codepage repair <see cref="Clean"/> makes) no emoji or other
    /// pictograph (a character outside the Basic Multilingual Plane, or an "other symbol" such as a heart or a
    /// check mark), no replacement character U+FFFD and no numeral of any kind.
    /// </summary>
    public static bool IsPresentable(string? raw, bool wasMojibake = false)
    {
        if (string.IsNullOrWhiteSpace(raw) || IsEmailShaped(raw)) return false;
        string s = raw;
        if (wasMojibake)
        {
            if (Mojibake.TryRepair(s) is not { } repaired) return false;
            s = repaired;
        }
        foreach (char c in s)
        {
            if (c == '�' || char.IsSurrogate(c) || char.IsNumber(c)) return false;
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.OtherSymbol) return false;
        }
        return true;
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
