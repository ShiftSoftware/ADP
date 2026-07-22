using ShiftSoftware.ADP.Darlastic.Engine;

namespace ShiftSoftware.ADP.Darlastic.CaseBrowser;

/// <summary>
/// The case browser's in-memory index, built in ONE streaming pass over the blocked pairs
/// (the same walk Merge.ClusterFromBlocks does — union-find clustering happens here too, so
/// `cases` never scores the 12.4M pairs twice).
///
/// Scale design: the full pair population (12.4M) is never materialized — only decision-relevant
/// cases enter the index (≥0.70 or a notable rule fired), as compact structs of record indices
/// (~10⁵ entries, a few MB). Everything else (traces, candidates, cluster detail) is computed
/// on demand from the records + blocks already in memory; the noise band stays reachable through
/// search-then-score, not through storage.
/// </summary>
public static class CaseIndex
{
    /// <summary>Browsing categories. A case can carry several; band tags are mutually exclusive.</summary>
    [Flags]
    public enum Cat
    {
        None = 0,
        AutoMerged     = 1 << 0,  // score >= 0.90 — these pairs union into one identity
        StewardBand    = 1 << 1,  // 0.80–0.90 — the Phase 5 queue
        NearMiss       = 1 << 2,  // 0.70–0.80 — kept separate, close
        ConflictVetoed = 1 << 3,  // exact phone but a hard rule said two people (name conflict / ID / DOB)
        AddressRescued = 1 << 4,  // chain-slice rescue fired (district-confirmed, auto-merge 0.91)
        Increment      = 1 << 5,  // phone+given shape, no district confirm, below the line — the deferred 25K decision
        Mojibake       = 1 << 6,  // a CP1256-recovered name participates
        Arabizi        = 1 << 7,  // a chat-numeral name participates
        VinSold        = 1 << 8,  // sold-VIN ownership floor fired → 0.91 auto-merge (names fully consistent — the safe recall wins)
        VinServiced    = 1 << 9,  // serviced-VIN corroboration participated (rides along on an already-relevant case)
        VinTransfer    = 1 << 10, // shared VIN gated OUT as an ownership transfer (P7) — verify the gate held
        VinReview      = 1 << 11, // sold VIN + given name aligns but rest differs — NOT merged; the steward-review bucket (vin-sample finding)
        OrgLine        = 1 << 12, // exact phone + a business/placeholder name (Parts Stock, …) — person-merge floor withheld; needs the dealer-self/placeholder filter
    }

    /// <summary>Cluster-level categories (identity view).</summary>
    [Flags]
    public enum ClusterCat
    {
        None = 0,
        LongestChain      = 1 << 0, // golden name extended to a fuller chain than the most-attested spelling
        WeakPhoneFallback = 1 << 1, // golden phone survived from a weak 9-digit number
        CrossDealer       = 1 << 2, // members from >= 2 sources — the initiative's business case
        Large             = 1 << 3, // >= 5 records
        VinBridged        = 1 << 4, // >= 1 auto-merge edge fired on the sold-VIN floor — assembled (partly) by VIN
        VinDecisive       = 1 << 5, // a sold-VIN edge that name+phone+address alone could NOT merge (score w/o VIN < 0.90) — the clearest "power of VIN" showcase
    }

    public readonly record struct Entry(int A, int B, float Score, MatchFlags Flags, Cat Cats);

    public sealed record ClusterSummary(int Root, int Size, int Sources, string GoldenName, ClusterCat Cats);

    public sealed class Index
    {
        public required IReadOnlyList<RealRecord> Records;
        public required RealMatcher.BlockingResult Blocking;
        /// <summary>Decision-relevant pairs, sorted by score descending (stable pagination).</summary>
        public required List<Entry> Entries;
        public required Merge.UnionFind Uf;
        /// <summary>Multi-record clusters only: root → member record idxs.</summary>
        public required Dictionary<int, List<int>> ClusterMembers;
        /// <summary>root → the auto-merge edges that assembled it (A, B, score).</summary>
        public required Dictionary<int, List<Entry>> ClusterEdges;
        public required List<ClusterSummary> Clusters; // sorted by size desc
        public required Dictionary<int, ClusterCat> ClusterCatsByRoot;
        public long TotalPairs;
        public int IdentityCount;

        public string PairKey(int a, int b)
        {
            var (x, y) = Canonical(Records[a], Records[b]);
            return $"{x.SourceSystem}:{x.SourceRecordId}~{y.SourceSystem}:{y.SourceRecordId}";
        }

        public static (RealRecord, RealRecord) Canonical(RealRecord a, RealRecord b)
        {
            int c = string.CompareOrdinal(a.SourceSystem, b.SourceSystem);
            if (c == 0) c = string.CompareOrdinal(a.SourceRecordId, b.SourceRecordId);
            return c <= 0 ? (a, b) : (b, a);
        }
    }

    public static Index Build(IReadOnlyList<RealRecord> records, RealMatcher.BlockingResult blocking, double mergeThreshold = 0.90)
    {
        int n = records.Count;
        var uf = new Merge.UnionFind(n);
        var entries = new List<Entry>(1 << 18);
        var edges = new List<Entry>(1 << 16);
        long total = 0;

        var seen = new HashSet<long>();
        foreach (var block in blocking.Blocks)
            for (int i = 0; i < block.Count; i++)
                for (int j = i + 1; j < block.Count; j++)
                {
                    int a = Math.Min(block[i], block[j]), b = Math.Max(block[i], block[j]);
                    if (a == b || !seen.Add(((long)a << 32) | (uint)b)) continue;
                    total++;

                    double s = RealMatcher.Score(records[a], records[b], out var flags);
                    if (s >= mergeThreshold)
                    {
                        uf.Union(a, b);
                        edges.Add(new Entry(a, b, (float)s, flags, Cat.AutoMerged));
                    }

                    var cats = Categorize(records[a], records[b], s, flags);
                    if (cats != Cat.None) entries.Add(new Entry(a, b, (float)s, flags, cats));
                }
        seen = null!; // ~12.4M longs — release before building the cluster maps

        // Cluster maps (multi-record only).
        var members = new Dictionary<int, List<int>>();
        var sizes = new Dictionary<int, int>();
        for (int i = 0; i < n; i++)
        {
            int r = uf.Find(i);
            sizes[r] = sizes.GetValueOrDefault(r) + 1;
        }
        for (int i = 0; i < n; i++)
        {
            int r = uf.Find(i);
            if (sizes[r] < 2) continue;
            if (!members.TryGetValue(r, out var list)) members[r] = list = [];
            list.Add(i);
        }

        var clusterEdges = new Dictionary<int, List<Entry>>();
        foreach (var e in edges)
        {
            int r = uf.Find(e.A);
            if (!clusterEdges.TryGetValue(r, out var list)) clusterEdges[r] = list = [];
            list.Add(e);
        }

        // Cluster categories + golden display name (survivorship run per multi-record cluster —
        // the same SurviveGolden the merge CSVs use).
        var clusterCats = new Dictionary<int, ClusterCat>(members.Count);
        var clusters = new List<ClusterSummary>(members.Count);
        foreach (var (root, idxs) in members)
        {
            var recs = idxs.Select(i => records[i]).ToList();
            var golden = Merge.SurviveGolden(recs);
            var cc = ClusterCat.None;
            if (golden.Any(g => g.AttrType == "full_name" && g.WonBy == "longest-chain")) cc |= ClusterCat.LongestChain;
            if (golden.Any(g => g.AttrType == "phone" && g.WonBy == "weak-fallback")) cc |= ClusterCat.WeakPhoneFallback;
            int sources = recs.Select(r => r.SourceSystem).Distinct().Count();
            if (sources >= 2) cc |= ClusterCat.CrossDealer;
            if (recs.Count >= 5) cc |= ClusterCat.Large;
            var vinEdges = clusterEdges.GetValueOrDefault(root, []).Where(e => (e.Flags & MatchFlags.VinSoldMerge) != 0).ToList();
            if (vinEdges.Count > 0)
            {
                cc |= ClusterCat.VinBridged;
                // VIN-decisive: at least one sold-VIN edge that would NOT have crossed the 0.90 auto-merge
                // line without VIN (name+phone+address alone < 0.90) — VIN found what nothing else could.
                if (vinEdges.Any(e => RealMatcher.Score(records[e.A], records[e.B], useAddress: true, useVin: false) < 0.90))
                    cc |= ClusterCat.VinDecisive;
            }
            clusterCats[root] = cc;
            string name = golden.FirstOrDefault(g => g.AttrType == "full_name")?.Value ?? "(no name)";
            clusters.Add(new ClusterSummary(root, recs.Count, sources, name, cc));
        }
        clusters.Sort((x, y) => y.Size != x.Size ? y.Size.CompareTo(x.Size) : x.Root.CompareTo(y.Root));

        entries.Sort((x, y) => y.Score != x.Score ? y.Score.CompareTo(x.Score) : (x.A != y.A ? x.A.CompareTo(y.A) : x.B.CompareTo(y.B)));

        return new Index
        {
            Records = records,
            Blocking = blocking,
            Entries = entries,
            Uf = uf,
            ClusterMembers = members,
            ClusterEdges = clusterEdges,
            Clusters = clusters,
            ClusterCatsByRoot = clusterCats,
            TotalPairs = total,
            IdentityCount = sizes.Count,
        };
    }

    /// <summary>
    /// Category tags from the score + the flags the live scorer set. The Increment shape
    /// re-applies FirstTokensAlign only on the bounded exact-phone subset (never in Score's
    /// own hot path): exact phone + aligned given name + names not consistent/conflicting +
    /// no district rescue + below the auto-merge line — the deferred 25,531-pair decision.
    /// </summary>
    private static Cat Categorize(RealRecord ra, RealRecord rb, double s, MatchFlags f)
    {
        Cat cats = Cat.None;
        if (s >= 0.90) cats |= Cat.AutoMerged;
        else if (s >= 0.80) cats |= Cat.StewardBand;
        else if (s >= 0.70) cats |= Cat.NearMiss;

        bool phoneExact = (f & MatchFlags.PhoneExact) != 0;
        if ((f & MatchFlags.ChainSliceRescue) != 0) cats |= Cat.AddressRescued;
        // ConflictVetoed = a hard rule ACTUALLY held the pair below auto-merge, so it can never co-occur
        // with Auto-merged (the contradiction a steward spotted: 'Parts Stock'~'Parts Stock' was tagged
        // both). NameConflict caps to 0.55 and ID conflict ×0.3 — both land < 0.90. DobConflict no longer
        // penalizes (2026-06-22) and the org guard only WITHHOLDS the person floor (identical org names
        // still merge on the base) — neither is a veto, so neither belongs here.
        if (phoneExact && s < 0.90 && (f & (MatchFlags.NameConflictCap | MatchFlags.IdConflict)) != 0)
            cats |= Cat.ConflictVetoed;
        if ((f & MatchFlags.OrgLine) != 0) cats |= Cat.OrgLine;

        // VIN tags (build plan step 5 / case-browser adjudication): the sold-VIN auto-merges are the
        // recall wins to spot-check (incl. the same-given-different-father watch-point); transfers are
        // the P7 gate to verify. Serviced corroboration rides along on an already-relevant case (band
        // tag or another rule fired) so service-VIN noise never grows the index on its own.
        if ((f & MatchFlags.VinSoldMerge) != 0) cats |= Cat.VinSold;
        if ((f & MatchFlags.VinTransfer) != 0) cats |= Cat.VinTransfer;
        if ((f & MatchFlags.VinServiced) != 0 && cats != Cat.None) cats |= Cat.VinServiced;
        // Sold VIN that did NOT auto-merge but the given name aligns = the demoted given-only bucket
        // (vin-sample 2026-06-22): a car shared by a same-given-name pair whose later tokens differ —
        // could be a chain slice or relatives/resale. The steward-review queue for the VIN lever.
        // The `s < 0.90` guard is what makes the label honest: VinSoldMerge==0 only means the *VIN floor*
        // didn't fire — the pair can still auto-merge on another lever (exact phone+given, chain-slice
        // address rescue, both 0.91), and without this guard those ride-alongs showed up tagged BOTH
        // Auto-merged AND "VIN review (NOT merged)". Same category-honesty fix ConflictVetoed got (s<0.90),
        // and it leaves exactly the population the deferred sold-VIN-given-only expert call needs to judge.
        if (s < 0.90 && (f & MatchFlags.VinSoldOverlap) != 0 && (f & MatchFlags.VinSoldMerge) == 0
            && (f & MatchFlags.NamesBoth) != 0 && RealMatcher.GivenNamesMatch(ra.NormName, rb.NormName))
            cats |= Cat.VinReview;

        if (phoneExact && s < 0.90
            && (f & MatchFlags.NamesBoth) != 0
            && (f & (MatchFlags.NameConsistent | MatchFlags.NameConflictCap | MatchFlags.ChainSliceRescue | MatchFlags.OrgLine)) == 0
            && RealMatcher.FirstTokensAlign(ra.NormName, rb.NormName))
            cats |= Cat.Increment;

        // Script-recovery tags ride along on cases that are interesting for another reason, plus
        // any exact-phone meeting of a recovered name (e.g. "recovered but still unmatched" QA).
        bool moji = ra.NameWasMojibake || rb.NameWasMojibake;
        bool arz = ra.NameHadArabizi || rb.NameHadArabizi;
        if ((moji || arz) && (cats != Cat.None || phoneExact))
        {
            if (moji) cats |= Cat.Mojibake;
            if (arz) cats |= Cat.Arabizi;
        }
        return cats;
    }
}
