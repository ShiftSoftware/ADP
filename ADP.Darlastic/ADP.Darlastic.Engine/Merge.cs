using System.Text;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// Turns scored pairs into actual identities: union-find clusters the auto-merge edges, then
/// emits identity rows, source_profile→identity backlinks, and a survived golden_attribute
/// projection — i.e. the merges you can browse in Postgres. Auto-merge only (>= threshold);
/// steward-band pairs are intentionally NOT merged (they are the Phase 5 queue).
/// </summary>
public static class Merge
{
    /// <summary>Path-halving find + union-by-rank, extracted so the case browser's single-pass
    /// walk (score + flags + index + cluster in one sweep) reuses the EXACT clustering algorithm.
    /// Union order determines roots; callers must walk pairs in the same order for identical ids.</summary>
    public sealed class UnionFind
    {
        public readonly int[] Parent;
        private readonly int[] _rank;
        public UnionFind(int n)
        {
            Parent = new int[n];
            _rank = new int[n];
            for (int i = 0; i < n; i++) Parent[i] = i;
        }
        public int Find(int x)
        {
            var p = Parent;
            while (p[x] != x) { p[x] = p[p[x]]; x = p[x]; }
            return x;
        }
        public bool Union(int a, int b)
        {
            int ra = Find(a), rb = Find(b);
            if (ra == rb) return false;
            if (_rank[ra] < _rank[rb]) (ra, rb) = (rb, ra);
            Parent[rb] = ra;
            if (_rank[ra] == _rank[rb]) _rank[ra]++;
            return true;
        }
    }

    public sealed class Result
    {
        public int[] Parent = [];
        public int IdentityCount, MultiRecordIdentities, AutoMergeEdges, LargestCluster;
        public Dictionary<int, int> ClusterSizeHistogram = new(); // size -> #clusters
        /// <summary>Pairs in the steward band [0.80, threshold) — genuine uncertainty, the Phase 5
        /// queue. Collected here (the only full pair walk a resolve does) so the registry can
        /// PERSIST the queue and a steward surface can serve it without re-scoring the corpus.</summary>
        public List<(int A, int B, float Score)> StewardPairs = [];
    }

    /// <summary>The steward band's lower edge — pairs at or above it (but below auto-merge) queue
    /// for human adjudication.</summary>
    public const double StewardThreshold = 0.80;

    /// <summary>
    /// Exhaustive merge: walk the blocks, union every pair scoring >= threshold. The earlier
    /// scoring pass only kept a per-band SAMPLE, so we re-score here for a faithful clustering
    /// (the score function is cheap and deterministic).
    /// </summary>
    public static Result ClusterFromBlocks(IReadOnlyList<RealRecord> records, RealMatcher.BlockingResult blocking, double mergeThreshold = 0.90)
    {
        int n = records.Count;
        var uf = new UnionFind(n);

        long edges = 0;
        var steward = new List<(int, int, float)>();
        var seen = new HashSet<long>();
        foreach (var block in blocking.Blocks)
            for (int i = 0; i < block.Count; i++)
                for (int j = i + 1; j < block.Count; j++)
                {
                    int a = Math.Min(block[i], block[j]), b = Math.Max(block[i], block[j]);
                    if (a == b || !seen.Add(((long)a << 32) | (uint)b)) continue;
                    double s = RealMatcher.Score(records[a], records[b]);
                    if (s >= mergeThreshold) { uf.Union(a, b); edges++; }
                    else if (s >= StewardThreshold) steward.Add((a, b, (float)s));
                }

        // A pair judged [0.80,0.90) can still land in the SAME identity via other edges
        // (transitive auto-merge) — that is a settled question, not steward work. Filter when
        // the union-find is final (adversarial review 2026-07-19 measured 32% of the raw band
        // already-merged on the full TIQ corpus).
        steward.RemoveAll(p => uf.Find(p.Item1) == uf.Find(p.Item2));

        var parent = uf.Parent;
        var size = new Dictionary<int, int>();
        for (int i = 0; i < n; i++) { int r = uf.Find(i); size[r] = size.GetValueOrDefault(r) + 1; }

        var result = new Result
        {
            Parent = parent,
            AutoMergeEdges = (int)edges,
            IdentityCount = size.Count,
            MultiRecordIdentities = size.Values.Count(v => v > 1),
            LargestCluster = size.Count == 0 ? 0 : size.Values.Max(),
            StewardPairs = steward,
        };
        foreach (var v in size.Values) result.ClusterSizeHistogram[v] = result.ClusterSizeHistogram.GetValueOrDefault(v) + 1;
        return result;
    }

    /// <summary>
    /// Emit the merge result as SQL the load can apply: assign each cluster a deterministic
    /// identity, backlink source_profile.identity_id, and survive a golden_attribute per (identity, attr_type).
    /// Survivorship rule (spike-simple): most-frequent value wins, ties broken by longest then lexical —
    /// real engine uses P4's source-priority / confidence-recency / steward-override modes.
    /// </summary>
    public static (int Identities, int Backlinks) WriteMergeCsvs(
        IReadOnlyList<RealRecord> records, Result merge, string identityCsv, string backlinkCsv, string goldenCsv)
    {
        int Find(int x) { var p = merge.Parent; while (p[x] != x) { p[x] = p[p[x]]; x = p[x]; } return x; }

        // Stable cluster-id = smallest record index in the cluster (deterministic, reproducible).
        var clusterOf = new int[records.Count];
        for (int i = 0; i < records.Count; i++) clusterOf[i] = Find(i);

        var clusters = records.Select((r, i) => (r, root: clusterOf[i]))
                              .GroupBy(x => x.root)
                              .ToDictionary(g => g.Key, g => g.Select(x => x.r).ToList());

        using var idW = new StreamWriter(identityCsv, false, new UTF8Encoding(false));
        using var blW = new StreamWriter(backlinkCsv, false, new UTF8Encoding(false));
        using var gdW = new StreamWriter(goldenCsv, false, new UTF8Encoding(false));
        idW.WriteLine("cluster_id,kind,record_count");
        blW.WriteLine("source_system,source_record_id,cluster_id");
        gdW.WriteLine("cluster_id,attr_type,value,won_by");

        foreach (var (root, members) in clusters)
        {
            idW.WriteLine($"{root},person,{members.Count}");
            foreach (var m in members)
                blW.WriteLine($"{m.SourceSystem},{m.SourceRecordId},{root}");

            foreach (var ga in SurviveGolden(members))
                gdW.WriteLine($"{root},{ga.AttrType},{(ga.AttrType is "full_name" or "city" ? Q(ga.Value) : ga.Value)},{ga.WonBy}");
        }

        return (clusters.Count, records.Count);

        static string Q(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
    }

    public sealed record GoldenAttr(string AttrType, string Value, string WonBy);

    /// <summary>Explain sink for the golden-NAME survivorship decision (case-browser cluster view).
    /// Filled only when requested; selection logic is identical either way.</summary>
    public sealed class NameSurvivalExplain
    {
        public string? Anchor;
        public int AnchorCount;
        /// <summary>Every spelling longer than the anchor, with its verdict: did the given name
        /// align, and which shorter member spelling witnessed the chain extension (the bridge).</summary>
        public List<(string Name, int Count, bool GivenAligns, string? Bridge)> Extensions = [];
        public string? Chosen; // null = anchor survived as-is
    }

    /// <summary>
    /// Survivorship for one cluster — the single source of truth used by BOTH the merge-CSV emit
    /// and the case browser's cluster view.
    ///
    /// Golden name (2026-06-12): Arabic names in this domain are a CHAIN (given + father + grandfather
    /// [+ family]) and each source captures a different slice — dealers often record only
    /// given+father, occasionally given+grandfather Western-style ("first+last"). "Most
    /// frequent wins" therefore survived the SHORTER form in 199 identities where a fuller
    /// chain was present among members ('Adel Sabr' ×3 beat 'Adil Saber Kareem';
    /// probes_enrichment.sql measures the gap). Anchor on the most-attested spelling, then
    /// survive the LONGEST member name that continues the same chain. A fuller chain qualifies
    /// when it (a) preserves the anchor's GIVEN name (first token) and (b) extends ANY shorter
    /// member spelling as an ordered fuzzy token subsequence — leaning on the cluster's own
    /// transitivity, the same mechanism that merged it: anchor 'adel sabr' cannot reach
    /// 'adil saber kareem' directly (middle tokens too far apart), but member 'adel saber'
    /// bridges exactly — and 'adil kareem' (given+grandfather, the Western "first+last"
    /// entry style) bridges by skipping the father token, which the subsequence walk allows.
    /// Order-preserving on purpose: 'ali mohammed' must NOT extend to 'mohammed ali kamil'
    /// — name-order inversion is father/son evidence, not a chain slice (those stay for
    /// the steward's enrich/compose action, Phase 5).
    /// </summary>
    public static List<GoldenAttr> SurviveGolden(List<RealRecord> members, NameSurvivalExplain? explain = null)
    {
        var golden = new List<GoldenAttr>();

        var names = members.Where(m => m.NormName.Length > 0).Select(m => m.NormName).ToList();
        if (names.Count > 0)
        {
            var groups = names.GroupBy(x => x)
                .Select(g => (Name: g.Key, Count: g.Count(), Tokens: g.Key.Split(' ', StringSplitOptions.RemoveEmptyEntries)))
                .OrderByDescending(g => g.Count).ThenByDescending(g => g.Name.Length)
                .ThenBy(g => g.Name, StringComparer.Ordinal).ToList();
            var anchor = groups[0];
            var fullest = groups
                .Where(g => g.Tokens.Length > anchor.Tokens.Length
                         && RealMatcher.TokensAlign(anchor.Tokens[0], g.Tokens[0])
                         && groups.Any(m => m.Tokens.Length < g.Tokens.Length && IsChainExtension(m.Tokens, g.Tokens)))
                .OrderByDescending(g => g.Tokens.Length).ThenByDescending(g => g.Count)
                .ThenByDescending(g => g.Name.Length).ThenBy(g => g.Name, StringComparer.Ordinal)
                .ToList();
            if (explain is not null)
            {
                explain.Anchor = anchor.Name;
                explain.AnchorCount = anchor.Count;
                foreach (var g in groups.Where(g => g.Tokens.Length > anchor.Tokens.Length))
                {
                    bool givenAligns = RealMatcher.TokensAlign(anchor.Tokens[0], g.Tokens[0]);
                    string? bridge = givenAligns
                        ? groups.Where(m => m.Tokens.Length < g.Tokens.Length && IsChainExtension(m.Tokens, g.Tokens))
                                .Select(m => m.Name).FirstOrDefault()
                        : null;
                    explain.Extensions.Add((g.Name, g.Count, givenAligns, bridge));
                }
                explain.Chosen = fullest.Count > 0 ? fullest[0].Name : null;
            }
            golden.Add(fullest.Count > 0
                ? new GoldenAttr("full_name", TitleCaseForDisplay(fullest[0].Name), "longest-chain")
                : new GoldenAttr("full_name", TitleCaseForDisplay(anchor.Name), "confidence-recency"));
        }
        // Golden city: most-frequent normalized city (display/probe value; spelling variants of
        // one city count separately — fuzzy city unification is a Phase 4/steward concern).
        var cities = members.Where(m => m.NormCity.Length > 0).Select(m => m.NormCity).ToList();
        if (cities.Count > 0)
        {
            var bestCity = cities.GroupBy(x => x).OrderByDescending(g => g.Count())
                                 .ThenBy(g => g.Key, StringComparer.Ordinal).First().Key;
            golden.Add(new GoldenAttr("city", TitleCaseForDisplay(bestCity), "confidence-recency"));
        }
        // Golden phone: most-frequent normalized strong phone; fall back to the cluster's weak
        // (9-digit truncated-form) phone when no strong one exists, so weak-only clusters still
        // surface a contact number instead of a blank (946 such clusters before this fix).
        var phones = members.SelectMany(m => m.Phones).ToList();
        string phoneWonBy = "confidence-recency";
        if (phones.Count == 0) { phones = members.SelectMany(m => m.WeakPhones).ToList(); phoneWonBy = "weak-fallback"; }
        if (phones.Count > 0)
        {
            var best = phones.GroupBy(x => x).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).First().Key;
            golden.Add(new GoldenAttr("phone", best, phoneWonBy));
        }
        // Golden national id: rare, strong. Most-frequent wins, ties broken ordinally — must NOT be
        // member-order dependent (CSV row order carries no contract; registry hashing needs stability).
        var id = members.Select(m => m.NationalId).Where(x => x is not null)
            .GroupBy(x => x!).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
            .FirstOrDefault()?.Key;
        if (id is not null) golden.Add(new GoldenAttr("national_id", id, "source-priority"));

        // Golden e-mail: same most-frequent/ordinal discipline. Only sources that carry e-mail
        // contribute, so pre-existing clusters emit nothing and their canonicals stay unchanged.
        var email = members.SelectMany(m => m.Emails ?? [])
            .GroupBy(x => x).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
            .FirstOrDefault()?.Key;
        if (email is not null) golden.Add(new GoldenAttr("email", email, "confidence-recency"));

        return golden;
    }

    /// <summary>Display casing for golden name/city: the survived value is match-normalized
    /// (lowercased); title-case it for presentation ("aza asim" → "Aza Asim"). Deterministic
    /// (InvariantCulture) so the golden payload hash stays stable across runs. Matching is
    /// unaffected — the matcher reads each source record's NormName, never the golden's stored name.</summary>
    private static string TitleCaseForDisplay(string s) =>
        System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);

    /// <summary>shorter is an ordered subsequence of longer under fuzzy token alignment
    /// (exact / containment / Jaro-Winkler / consonant-skeleton — RealMatcher.TokensAlign), so
    /// 'adel sabr' extends to 'adil saber kareem' and 'adil kareem' (given+grandfather)
    /// extends to the same chain by skipping the father token.</summary>
    private static bool IsChainExtension(string[] shorter, string[] longer)
    {
        int j = 0;
        foreach (var tok in shorter)
        {
            while (j < longer.Length && !RealMatcher.TokensAlign(tok, longer[j])) j++;
            if (j >= longer.Length) return false;
            j++;
        }
        return true;
    }
}
