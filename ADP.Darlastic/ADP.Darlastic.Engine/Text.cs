using System.Text;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// Run-time switches. Baseline mode reproduces the pre-2026-06-08 matcher (no Arabizi fold, no
/// mojibake repair, no name-aware phone boost) so the EXACT panel-labeled sample can be regenerated
/// and re-aligned after a sampler change. Set env CC_BASELINE=1.
/// </summary>
public static class Flags
{
    public static readonly bool Baseline = Environment.GetEnvironmentVariable("CC_BASELINE") == "1";
}


/// <summary>Normalization — the engine-side preprocessing that makes cross-script matching possible.</summary>
public static class Norm
{
    // Cyrillic → Latin. Base Russian alphabet plus the Central-Asian extension letters
    // (Uzbek/Tajik/Turkmen/Kazakh/Kyrgyz descender and bar forms, which have NO Unicode
    // decomposition — without explicit entries they fell through to the accented-Latin fold,
    // decomposed to nothing, and VANISHED: "Ҳасан" normalized to "asan" while "Хасан" gave
    // "khasan". Verified against real activation names, 2026-07-19.) Romanization follows the
    // Latin orthography of those languages (қ→q, ғ→g, ҳ→h) so Cyrillic- and Latin-typed forms
    // of the same name land together.
    private static readonly Dictionary<char, string> Cyrillic = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ё'] = "e",
        ['ж'] = "zh", ['з'] = "z", ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m",
        ['н'] = "n", ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u",
        ['ф'] = "f", ['х'] = "kh", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "shch",
        ['ъ'] = "", ['ы'] = "y", ['ь'] = "", ['э'] = "e", ['ю'] = "yu", ['я'] = "ya",
        // Central-Asian extensions (atomic code points):
        ['қ'] = "q", ['ҡ'] = "q", ['ғ'] = "g", ['ҳ'] = "h", ['һ'] = "h", ['ҷ'] = "j", ['җ'] = "j",
        ['ҹ'] = "j", ['ң'] = "n", ['ө'] = "o", ['ә'] = "a", ['ұ'] = "u", ['ү'] = "u", ['і'] = "i",
        ['є'] = "e", ['ґ'] = "g",
    };

    // Arabic → Latin (common subset; diacritics dropped).
    private static readonly Dictionary<char, string> Arabic = new()
    {
        ['ا'] = "a", ['أ'] = "a", ['إ'] = "i", ['آ'] = "a", ['ب'] = "b", ['ت'] = "t", ['ث'] = "th",
        ['ج'] = "j", ['ح'] = "h", ['خ'] = "kh", ['د'] = "d", ['ذ'] = "dh", ['ر'] = "r", ['ز'] = "z",
        ['س'] = "s", ['ش'] = "sh", ['ص'] = "s", ['ض'] = "d", ['ط'] = "t", ['ظ'] = "z", ['ع'] = "a",
        ['غ'] = "gh", ['ف'] = "f", ['ق'] = "q", ['ك'] = "k", ['ل'] = "l", ['م'] = "m", ['ن'] = "n",
        ['ه'] = "h", ['و'] = "w", ['ي'] = "y", ['ى'] = "a", ['ة'] = "a", ['ء'] = "",
    };

    // Arabizi (Franco-Arabic chat) numerals → the same Latin letter the Arabic transliteration uses,
    // so '3aftan' and 'عفتان' land near each other. Arabic-transliterated tenant data uses these heavily; the old digit
    // gate blanked every one of them (review 2026-06-08: 'Nafi3', 'Mohammed 7assan', 'Sa3eed').
    private static readonly Dictionary<char, string> Arabizi = new()
    {
        ['2'] = "", ['3'] = "a", ['5'] = "kh", ['6'] = "t", ['7'] = "h", ['9'] = "q",
    };

    public static string Name(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        if (!Flags.Baseline) s = FoldArabizi(s);
        var sb = new StringBuilder(s.Length);
        foreach (char ch in s)
        {
            char c = char.ToLowerInvariant(ch);
            if (Cyrillic.TryGetValue(c, out var cy)) sb.Append(cy);
            else if (Arabic.TryGetValue(c, out var ar)) sb.Append(ar);
            else if (c is >= 'a' and <= 'z') sb.Append(c);
            // arabizi digits were folded to letters above; remaining digits are serials / list
            // markers ('Firka 1', '2. Mariwan') and are dropped. Baseline mode keeps digits (so the
            // old digit-gate blanks those names) to reproduce the original sample.
            else if (Flags.Baseline && c is >= '0' and <= '9') sb.Append(c);
            else if (char.IsWhiteSpace(c)) sb.Append(' ');
            else if (c > 127 && char.IsLetter(c))
            {
                // Fold accented Latin to its base letter (é→e, ç→c) instead of dropping it —
                // dropping corrupted keys like 'Naïf'→'naf' (review 2026-06-07). Combining marks vanish.
                // Gated on IsLetter: user-typed app names carry emoji and stray surrogate halves,
                // and Normalize() THROWS on invalid code points (a lone surrogate is never a letter,
                // so this gate drops them; symbols and noncharacters fold to nothing anyway).
                // Decomposed COMPOSED CYRILLIC (ӣ→и, ӯ→у, ў→у) re-enters the Cyrillic map —
                // Tajik macron vowels otherwise vanish the same way the atomic extensions did.
                foreach (char d in c.ToString().Normalize(NormalizationForm.FormD))
                    if (d is >= 'a' and <= 'z') sb.Append(d);
                    else if (Cyrillic.TryGetValue(d, out var cy2)) sb.Append(cy2);
            }
            // else: drop ASCII punctuation (e.g. '-')
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>True when Arabizi folding would change the string — the case browser's
    /// record-level "chat-numeral name" marker (no behavior change; detection only).</summary>
    public static bool HasArabiziFold(string s) => s.Any(char.IsDigit) && FoldArabizi(s) != s;

    /// <summary>
    /// Replace a digit with its Arabizi letter ONLY when it sits next to a letter (so '3aftan',
    /// 'Nafi3' become names while standalone serials like 'Firka 1' / '2.' are left to be dropped).
    /// </summary>
    private static string FoldArabizi(string s)
    {
        if (!s.Any(char.IsDigit)) return s;
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsDigit(c) && Arabizi.TryGetValue(c, out var rep))
            {
                bool letterAdjacent = (i > 0 && char.IsLetter(s[i - 1])) || (i + 1 < s.Length && char.IsLetter(s[i + 1]));
                if (letterAdjacent) { sb.Append(rep); continue; }
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    public static string? Phone(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var digits = new string(s.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00964")) digits = digits[5..];
        else if (digits.StartsWith("964")) digits = digits[3..];
        digits = digits.TrimStart('0');
        return digits.Length == 0 ? null : digits;
    }

    public static string Id(string? s) =>
        string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    /// <summary>Canonical VIN or null when implausible. VINs are case-insensitive identifiers but
    /// the engine keys on them ordinally everywhere (blocking, ownership aggregation, Cosmos doc
    /// ids/partition keys) — so every feed must emit ONE casing. Mixed-case source rows split
    /// V-blocks and stage duplicate ownership docs per physical car (adversarial review
    /// 2026-07-19: six vinowner artifacts for three cars, point-reads missing both casings).</summary>
    public static string? Vin(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().ToUpperInvariant();
        return s.Length == 17 && s.All(char.IsLetterOrDigit) && s.Distinct().Count() > 3 ? s : null;
    }
}

/// <summary>Similarity primitives. Trigram mirrors Postgres pg_trgm.similarity(); Jaro-Winkler is the edit-distance signal.</summary>
public static class Sim
{
    // pg_trgm-style trigram-set Jaccard (pads with 2 leading + 1 trailing space, as Postgres does).
    public static double Trigram(string a, string b)
    {
        var ta = Trigrams(a);
        var tb = Trigrams(b);
        if (ta.Count == 0 && tb.Count == 0) return 1;
        if (ta.Count == 0 || tb.Count == 0) return 0;
        int inter = ta.Count(tb.Contains);
        int union = ta.Count + tb.Count - inter;
        return union == 0 ? 0 : (double)inter / union;
    }

    private static HashSet<string> Trigrams(string s)
    {
        var set = new HashSet<string>();
        if (string.IsNullOrEmpty(s)) return set;
        string padded = "  " + s + " ";
        for (int i = 0; i + 3 <= padded.Length; i++) set.Add(padded.Substring(i, 3));
        return set;
    }

    public static double JaroWinkler(string a, string b)
    {
        double j = Jaro(a, b);
        int prefix = 0;
        for (int i = 0; i < Math.Min(Math.Min(a.Length, b.Length), 4); i++)
        {
            if (a[i] == b[i]) prefix++;
            else break;
        }
        return j + prefix * 0.1 * (1 - j);
    }

    private static double Jaro(string s1, string s2)
    {
        if (s1.Length == 0 && s2.Length == 0) return 1;
        if (s1.Length == 0 || s2.Length == 0) return 0;
        int matchDistance = Math.Max(0, Math.Max(s1.Length, s2.Length) / 2 - 1);
        var m1 = new bool[s1.Length];
        var m2 = new bool[s2.Length];
        int matches = 0;
        for (int i = 0; i < s1.Length; i++)
        {
            int start = Math.Max(0, i - matchDistance);
            int end = Math.Min(i + matchDistance + 1, s2.Length);
            for (int k = start; k < end; k++)
            {
                if (m2[k] || s1[i] != s2[k]) continue;
                m1[i] = true; m2[k] = true; matches++; break;
            }
        }
        if (matches == 0) return 0;
        double t = 0;
        int kk = 0;
        for (int i = 0; i < s1.Length; i++)
        {
            if (!m1[i]) continue;
            while (!m2[kk]) kk++;
            if (s1[i] != s2[kk]) t++;
            kk++;
        }
        t /= 2;
        double mm = matches;
        return (mm / s1.Length + mm / s2.Length + (mm - t) / mm) / 3;
    }
}

/// <summary>The lean matcher: weighted multi-signal scoring with conflict penalties → confidence in [0,1].</summary>
