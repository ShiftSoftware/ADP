using ShiftSoftware.ADP.Darlastic.Engine;

// The category rules moved into the Engine (CaseCategory.cs) so the resolve — the only pass that
// sees every pair — can stage them for hosted surfaces. These aliases keep the browser's existing
// `CaseIndex.Cat` / `CaseIndex.ClusterCat` spelling working against the single definition.
using Cat = ShiftSoftware.ADP.Darlastic.Engine.CaseCat;
using ClusterCat = ShiftSoftware.ADP.Darlastic.Engine.ClusterCat;

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

                    var cats = CaseCategories.Categorize(records[a], records[b], s, flags);
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
}
