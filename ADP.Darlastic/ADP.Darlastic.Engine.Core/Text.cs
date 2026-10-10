using System.Text;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// Run-time switches. Baseline mode reproduces the pre-2026-06-08 matcher (no Arabizi fold, no
/// mojibake repair, no name-aware phone boost) so the EXACT panel-labeled sample can be regenerated
/// and re-aligned after a sampler change. Set env CC_BASELINE=1.
///
/// <para><b>Process-wide, unless scoped.</b> Every per-tenant switch below reads the effective
/// <see cref="MatchSettings"/>: the innermost <see cref="MatchSettings.Use"/> scope on the calling
/// flow, else the process-wide configuration. Setting a switch changes the process-wide value (as it
/// always did, which is how a batch runner configures its resolve and how tests toggle a rule); it
/// never changes an open scope, so a serving host scoring under a registry's recorded configuration
/// is not disturbed by — and does not disturb — anything else in the process.</para>
/// </summary>
public static class Flags
{
    public static readonly bool Baseline = Environment.GetEnvironmentVariable("CC_BASELINE") == "1";

    // The process-wide configuration, seeded from the environment at type-init exactly as each
    // switch always seeded itself. Replaced whole (copy on write) by the setters, so a reader never
    // sees half of a change.
    private static MatchSettings processWide = MatchSettings.FromEnvironment();

    // A configuration scoped to one flow of execution (MatchSettings.Use). Null outside any scope,
    // which costs a reader one thread-local probe in a process that never opens one.
    private static readonly AsyncLocal<MatchSettings?> scoped = new();

    /// <summary>The configuration the engine reads at this point of the calling flow.</summary>
    public static MatchSettings Current => scoped.Value ?? Volatile.Read(ref processWide);

    /// <summary>The flow's open scope, or null. Only <see cref="MatchSettings.Use"/> sets it.</summary>
    internal static MatchSettings? Scoped
    {
        get => scoped.Value;
        set => scoped.Value = value;
    }

    private static void Change(Func<MatchSettings, MatchSettings> change)
    {
        MatchSettings before, after;
        do
        {
            before = Volatile.Read(ref processWide);
            after = change(before);
        }
        while (!ReferenceEquals(Interlocked.CompareExchange(ref processWide, after, before), before));
    }

    /// <summary>
    /// PER-TENANT: does e-mail participate in matching (block key + score + floor)? Ingest and
    /// survivorship are unconditional — this switch only decides whether e-mail can MOVE a pair.
    ///
    /// Off by default, deliberately. E-mail's value is entirely a function of what a tenant's
    /// sources actually put in the column: on some deployments it is dominated by dealer-staff
    /// mailboxes captured during self-service lookups (one measured corpus, 2026-07-28 — 79 distinct
    /// addresses across 2,047 goldens, 84.7% of them one-name duplicate groups), so it must be
    /// enabled only after that tenant's corpus has been measured and its junk source excluded at
    /// the feed. A tenant that never sets it scores byte-identically to the pre-e-mail engine.
    ///
    /// Settable (not readonly like <see cref="Baseline"/>) because it is run-scoped tenant config,
    /// not a build mode: the host sets it from its own settings before resolving, and tests toggle
    /// it per case. Env DARLASTIC_EMAIL_MATCHING=1 is the default for a whole process run.
    /// </summary>
    public static bool EmailMatching { get => Current.EmailMatching; set => Change(s => s with { EmailMatching = value }); }

    // ---- per-tenant name-structure and clustering rules. All four are OFF by default, and a tenant
    // that never sets them scores and clusters exactly as before. Settable for the same reason as
    // EmailMatching: they are run-scoped tenant configuration, and tests toggle them per case.

    /// <summary>
    /// PER-TENANT: read the feed-supplied name parts <see cref="RealRecord.GivenName"/> and
    /// <see cref="RealRecord.FamilyName"/>. Meant for naming conventions where the family name is a
    /// fixed, inherited surname rather than one more slice of a name chain. Two effects, both only
    /// when BOTH records carry the part in question:
    ///   - family names that do not align → the given-name-only floors (shared phone, chain-slice
    ///     address rescue, e-mail and same-as given-only) are withheld and the pair is held below the
    ///     line: same given name + different surname on a shared phone is two namesakes;
    ///   - given names that do not align → no floor at all and the pair is held below the steward
    ///     line: distinct given names with one surname on one phone are relatives, not a duplicate.
    /// A part typed into the other field (given and family swapped) is tolerated, not a conflict.
    /// Records without parts are unaffected, so a feed can supply them only where it knows them.
    /// </summary>
    public static bool StructuredNames { get => Current.StructuredNames; set => Change(s => s with { StructuredNames = value }); }

    /// <summary>
    /// PER-TENANT: name consistency aligns tokens ONE-TO-ONE. By default every token of the shorter
    /// name may align to ANY token of the longer one, so a given name contained in the family name
    /// ("Name Nameson") aligns twice and is "consistent" with every relative carrying that family
    /// name. On makes each token of the longer name usable once.
    /// </summary>
    public static bool StrictTokenAlignment { get => Current.StrictTokenAlignment; set => Change(s => s with { StrictTokenAlignment = value }); }

    /// <summary>
    /// PER-TENANT: a record with no usable name needs more than one key to auto-merge. On:
    ///   - an exact shared phone that is the ONLY positive signal is damped ×0.70, mirroring the
    ///     name-only damp (a dealer, fleet or family phone carries nameless purchases of many people);
    ///   - the sold-VIN floor is withheld for a nameless pair whose phones are both present and differ.
    /// </summary>
    public static bool NamelessNeedsCorroboration { get => Current.NamelessNeedsCorroboration; set => Change(s => s with { NamelessNeedsCorroboration = value }); }

    /// <summary>
    /// PER-TENANT: the matcher's own "two different people" verdicts (name conflict, national-ID
    /// conflict and, with <see cref="StructuredNames"/>, given/family-name conflicts) become
    /// cannot-link constraints in clustering. Auto-merge edges are then applied strongest first, and an
    /// edge whose union would put a cannot-link pair into one identity is skipped: one record with a
    /// partial name (surname only, patronymic only) can no longer chain two relatives into one golden.
    /// </summary>
    public static bool ConflictAwareClustering { get => Current.ConflictAwareClustering; set => Change(s => s with { ConflictAwareClustering = value }); }

    /// <summary>
    /// PER-TENANT: two ORGANISATION names (each carries an organisation token) that are consistent
    /// token by token on a shared exact phone are one organisation → floor 0.91. Also when only one
    /// side carries the token and the other side's two or more words are all the organisation's own
    /// words (the same organisation written without its legal form). Two forms of one word (a shared
    /// stem of five or more letters covering most of the shorter word) align. On a shared SOLD VIN the
    /// same test floors the pair too, and so does an organisation whose name is one word beside a record
    /// carrying just that word. An organisation line whose names are NOT one organisation's is held in
    /// the steward band (0.89) instead of merging on the weighted base. A one-word person name next to an
    /// organisation on a phone is never floored, unless <see cref="StrictNameParts"/> is on and the corpus
    /// does not attest the word as a name part (then it is the company typed without its legal form).
    /// By default every pair touching an organisation name only gets the weighted base, so word-order
    /// and legal-form variants of one organisation land in the steward queue.
    /// </summary>
    public static bool OrgNameMerge { get => Current.OrgNameMerge; set => Change(s => s with { OrgNameMerge = value }); }

    /// <summary>
    /// PER-TENANT: a shared SERVICED VIN is decisive when the names are fully consistent (every token
    /// of the shorter aligns) and both carry two or more words → floor 0.91, even when the phones differ
    /// or one is missing. By default a serviced VIN only corroborates, because the person at a service
    /// visit may be a relative, a driver or a new owner — all of whom carry a different name; the full
    /// name answers that objection, and people change and drop phone numbers far more than names.
    /// </summary>
    public static bool ServicedVinNameFloor { get => Current.ServicedVinNameFloor; set => Change(s => s with { ServicedVinNameFloor = value }); }

    /// <summary>
    /// PER-TENANT: extra spelling equivalences for name-token alignment, as (from, to) pairs applied
    /// in order after the built-in interdental fold — two romanizations of one letter in the tenant's
    /// scripts. Comparison only: stored and displayed names never change. Empty by default.
    /// </summary>
    public static IReadOnlyList<(string From, string To)> TokenFolds { get => Current.TokenFolds; set => Change(s => s with { TokenFolds = value }); }

    /// <summary>
    /// PER-TENANT (read with <see cref="StructuredNames"/>): a stricter test of whether two name parts
    /// are one name. By default parts also align on a high Jaro-Winkler alone, or on a near match one
    /// consonant edit apart. That admits two different given names that share their first element or
    /// differ in one consonant (siblings on a family phone or car, in naming conventions built from
    /// compounds), and two different family names one consonant apart (namesakes). On:
    ///   - two structured given names, or two structured family names, that are BOTH in
    ///     <see cref="EstablishedNameParts"/> align only when they are one name: equal, a short form,
    ///     written with or without spaces, or the same consonant skeleton (vowel and doubled-letter
    ///     drift, one 'h'), compared after <see cref="NamePartFolds"/> and also with one ending set aside
    ///     (<see cref="GivenNameSuffixes"/>, <see cref="FamilyNameEndings"/>). A name the corpus has not
    ///     established may be a typing slip of the other, and keeps the default test;
    ///   - the serviced-VIN floor (<see cref="ServicedVinNameFloor"/>), where no phone corroborates,
    ///     needs one given name in that strict sense whatever the corpus says.
    /// Off by default.
    /// </summary>
    public static bool StrictNameParts { get => Current.StrictNameParts; set => Change(s => s with { StrictNameParts = value }); }

    /// <summary>
    /// PER-TENANT, used by <see cref="StrictNameParts"/> only: the given and family names the tenant's
    /// corpus attests (in the feeds' normalized form, as in <see cref="RealRecord.GivenName"/> and
    /// <see cref="RealRecord.FamilyName"/>), for example every name part seen on several distinct
    /// phones. The host computes it from the records it resolves. Empty by default, which leaves every
    /// name part to the default comparison.
    /// </summary>
    public static IReadOnlySet<string> EstablishedNameParts { get => Current.EstablishedNameParts; set => Change(s => s with { EstablishedNameParts = value }); }

    /// <summary>
    /// PER-TENANT, used by <see cref="StrictNameParts"/> only: letters that one name is written with
    /// across the tenant's scripts and transliterations, as (from, to) pairs applied in order after
    /// <see cref="TokenFolds"/>. Examples are voiced and unvoiced consonants, or one letter that two
    /// alphabets write differently. Comparison only. Empty by default.
    /// </summary>
    public static IReadOnlyList<(string From, string To)> NamePartFolds { get => Current.NamePartFolds; set => Change(s => s with { NamePartFolds = value }); }

    /// <summary>
    /// PER-TENANT, used by <see cref="StrictNameParts"/> only: endearment or honorific endings that one
    /// person's given name carries in some records and not in others. One is set aside before two given
    /// names are compared, when at least three letters remain. Empty by default.
    /// </summary>
    public static IReadOnlyList<string> GivenNameSuffixes { get => Current.GivenNameSuffixes; set => Change(s => s with { GivenNameSuffixes = value }); }

    /// <summary>
    /// PER-TENANT, used by <see cref="StrictNameParts"/> only: family-name endings that one family name
    /// is written with, or exchanges for one another (an ending borrowed from another language and the
    /// native one that replaced it). One is set aside before two family names are compared, when at
    /// least three letters remain. Empty by default.
    /// </summary>
    public static IReadOnlyList<string> FamilyNameEndings { get => Current.FamilyNameEndings; set => Change(s => s with { FamilyNameEndings = value }); }

    /// <summary>
    /// PER-TENANT (read with <see cref="StructuredNames"/>): family-name endings that mark the bearer's
    /// sex, as (masculine, feminine) pairs, for a naming convention where a woman carries the feminine
    /// form of the family name. A family name and its other-sex form are then two people (spouses,
    /// siblings, a parent and child on one phone), not a spelling variant, so the pair is a family-name
    /// conflict. The exception is when both records also carry given names that align: then the ending
    /// is a typing slip on one person's record. Empty by default, so no ending carries meaning.
    /// </summary>
    public static IReadOnlyList<(string Masculine, string Feminine)> FamilyNameSexEndings { get => Current.FamilyNameSexEndings; set => Change(s => s with { FamilyNameSexEndings = value }); }
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

    /// <summary>
    /// Canonical e-mail, or "" when the value cannot be one. Every transform here is
    /// PROVABLY-SAME-MAILBOX — a rewrite two spellings of one mailbox always share — never a guess
    /// at what the typist meant. Specifically NOT done: domain spell-correction ("gmal.com" →
    /// "gmail.com"). If the typo'd domain is itself a real mailbox that rewrite merges two
    /// strangers, and nothing in the corpus can say which case you're in.
    ///
    /// Same reasoning makes the matcher compare e-mail EXACTLY and never fuzzily. Name matching can
    /// be fuzzy because transliteration drift is systematic (see RealMatcher.TokensAlign's skeleton
    /// and interdental folds); local parts are a dense identifier space where a single edit
    /// routinely separates two people ("a.smith@" / "b.smith@"). There is no structure to exploit.
    ///
    /// Applied:
    ///   - trim + lowercase (domains are case-insensitive by RFC; every provider in the corpus
    ///     treats the local part that way too)
    ///   - drop the "+tag" suffix (universally routed to the base mailbox)
    ///   - googlemail.com → gmail.com (a documented alias of one mailbox, not a guess)
    ///   - Gmail ONLY: strip dots from the local part. Google documents them as insignificant; no
    ///     other provider guarantees it, so this stays provider-gated rather than general.
    /// </summary>
    public static string Email(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        string e = s.Trim().ToLowerInvariant();
        if (e.Length > 254 || e.Any(char.IsWhiteSpace)) return "";

        // Split on the LAST '@' — a quoted local part may legally contain one, and taking the last
        // keeps the domain correct for every shape that actually appears in the corpus.
        int at = e.LastIndexOf('@');
        if (at <= 0 || at == e.Length - 1) return "";
        string local = e[..at], domain = e[(at + 1)..].TrimEnd('.');

        if (domain == "googlemail.com") domain = "gmail.com";

        // A real dotted host with a >=2-char TLD. Rejects "x@localhost", "x@a.", "x@a..b".
        int dot = domain.LastIndexOf('.');
        if (dot <= 0 || domain.Length - dot - 1 < 2 || domain.Contains("..")) return "";

        int plus = local.IndexOf('+');
        if (plus >= 0) local = local[..plus];
        if (domain == "gmail.com") local = local.Replace(".", "");

        return local.Length == 0 ? "" : local + "@" + domain;
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
