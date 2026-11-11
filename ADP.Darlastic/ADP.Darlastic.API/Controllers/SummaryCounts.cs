using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ADP.Darlastic.Shared;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// The whole-registry counts in the case browser's summary that only a resolve changes: staged records and
/// records per source, golden customers, customers per category, the catalogue's categories.
///
/// <para>Each is a count over a whole table (the staged records twice), which is seconds per page load once the
/// staged records number in the millions. A host that sets <see cref="Extensions.DarlasticApiOptions.SummaryPerRun"/>
/// counts them once per resolve run (<see cref="Extensions.RegistryRunCache"/>); otherwise they are counted on
/// every call, as before. Either way the numbers are counted the same way.</para>
/// </summary>
/// <summary>
/// The number of live identities without a golden yet, kept for one resolve run and one newest identity number
/// (<see cref="Extensions.DarlasticApiOptions.SummaryPerRun"/>): a host creating a customer allocates the next
/// number, so a count made for the same run and the same newest number is still right.
/// </summary>
internal sealed class AwaitingResolveCount
{
    /// <summary>The cache entry the count is kept under.</summary>
    public const string CacheName = "case-browser-awaiting-resolve";

    private sealed record Counted(long Newest, int Count);

    private volatile Counted? last;

    /// <summary>The count for <paramref name="newest"/>, counted with <paramref name="count"/> when it moved.</summary>
    public async Task<int> GetAsync(long newest, Func<Task<int>> count)
    {
        if (last is { } known && known.Newest == newest) return known.Count;
        int n = await count();
        last = new Counted(newest, n);
        return n;
    }
}

internal sealed class SummaryCounts
{
    /// <summary>The cache entry the counts are kept under.</summary>
    public const string CacheName = "case-browser-summary";

    public required List<CaseCategoryCount> CategoryCounts { get; init; }
    public required int Profiles { get; init; }
    public required int Identities { get; init; }

    /// <summary>The category bits of every catalogued pair. The catalogue is capped per category by the resolve,
    /// so this is thousands of values.</summary>
    public required List<long> CatalogCategories { get; init; }

    public required int StagedRecords { get; init; }
    public required Dictionary<string, int> Sources { get; init; }
    public required Dictionary<string, long> ClusterCategories { get; init; }
    public required bool ReasonsStaged { get; init; }
    public required int MultiRecordIdentities { get; init; }
    public required int GoldenTotal { get; init; }

    public static async Task<SummaryCounts> CountAsync(ShiftDbContext db, CancellationToken ct)
    {
        var counts = await db.Set<CaseCategoryCount>().AsNoTracking().ToListAsync(ct);
        int profiles = await db.Set<SourceProfile>().AsNoTracking().CountAsync(ct);
        int identities = await db.Set<GoldenIdentity>().AsNoTracking()
            .CountAsync(i => i.Status == IdentityStatus.Active, ct);
        var catalog = await db.Set<CaseCatalogEntry>().AsNoTracking().Select(c => c.Categories).ToListAsync(ct);

        // One pass over the staged records gives both their number and their number per source.
        var sources = await db.Set<StewardRecord>().AsNoTracking()
            .GroupBy(r => r.SourceSystem)
            .Select(g => new { g.Key, N = g.Count() })
            .ToListAsync(ct);

        // Cluster categories are staged per identity as a bitmask. Multi-record identities only: a run with
        // per-golden evidence summarises single-record identities too, and the identity view is about how
        // several records came together. One read of the multi-record summaries' bits counts every category.
        var multiBits = await db.Set<IdentitySummary>().AsNoTracking()
            .Where(s => s.MemberCount > 1).Select(s => s.Categories).ToListAsync(ct);
        var clusterCats = new Dictionary<string, long>();
        foreach (var cc in Enum.GetValues<ClusterCat>())
        {
            if (cc == ClusterCat.None) continue;
            long mask = (long)cc;
            clusterCats[cc.ToString()] = multiBits.LongCount(b => (b & mask) != 0);
        }

        long allMask = GoldenReasons.AllMask;
        bool reasonsStaged = await db.Set<IdentitySummary>().AsNoTracking().AnyAsync(s => (s.Categories & allMask) != 0, ct);

        return new SummaryCounts
        {
            CategoryCounts = counts,
            Profiles = profiles,
            Identities = identities,
            CatalogCategories = catalog,
            StagedRecords = sources.Sum(s => s.N),
            Sources = sources.ToDictionary(s => s.Key, s => s.N),
            ClusterCategories = clusterCats,
            ReasonsStaged = reasonsStaged,
            MultiRecordIdentities = multiBits.Count,
            GoldenTotal = await db.Set<GoldenCustomer>().AsNoTracking().CountAsync(ct),
        };
    }
}
