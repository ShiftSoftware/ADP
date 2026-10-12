using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// The case browser's golden rows held in memory, one snapshot per registry and resolve run
/// (<see cref="Extensions.DarlasticApiOptions.GoldenRowIndex"/>, kept by <see cref="Extensions.RegistryRunCache"/>).
///
/// <para><b>Why.</b> A golden row's record count lives inside the golden's payload, so sorting, counting
/// and filtering every row through the <c>GoldenCustomer</c> view unpacks every payload on every
/// request — fine at tens of thousands of goldens, ten seconds a page at a million. Everything a page
/// needs to choose its rows (ID, record count, reasons) changes only when a resolve finishes, so it is
/// read once per run and kept: a page then costs a walk over arrays plus a read of its own fifty
/// rows.</para>
///
/// <para><b>Same rows.</b> The snapshot holds exactly the view's rows (it is read through the view), the
/// record count the view gives (the staged summary's member count, which is the same list of records;
/// the view's own count for a golden without a summary) and the reasons
/// <see cref="GoldenReasons.ForReader"/> gives — the rule the SQL path mirrors.</para>
/// </summary>
internal static class GoldenRowIndex
{
    /// <summary>The cache entry the snapshot is kept under.</summary>
    public const string CacheName = "case-browser-golden-rows";

    /// <summary>Above this many goldens without a staged summary, their record counts are read in one
    /// pass over the view rather than by ID.</summary>
    private const int MissingByIdLimit = 2000;

    /// <summary>One registry's golden rows as of one resolve run.</summary>
    internal sealed class Snapshot
    {
        public required bool ReasonsStaged { get; init; }

        /// <summary>Golden IDs in "most records first" order (record count descending, then ID).</summary>
        public required long[] Ids { get; init; }

        /// <summary>Record count per position of <see cref="Ids"/>.</summary>
        public required int[] Members { get; init; }

        /// <summary>Reason bits per position of <see cref="Ids"/>.</summary>
        public required long[] Bits { get; init; }

        /// <summary>Positions of <see cref="Ids"/> in ascending ID order.</summary>
        public required int[] ById { get; init; }

        /// <summary>Goldens per reason, over every row; reasons no golden carries are left out.</summary>
        public required Dictionary<string, int> ReasonCounts { get; init; }

        public int Count => Ids.Length;
    }

    /// <summary>Reads the snapshot of the registry <paramref name="db"/> reads (one pass through the view).</summary>
    public static async Task<Snapshot> BuildAsync(ShiftDbContext db, CancellationToken ct)
    {
        long allMask = GoldenReasons.AllMask;
        bool reasonsStaged = await db.Set<IdentitySummary>().AsNoTracking()
            .AnyAsync(s => (s.Categories & allMask) != 0, ct);

        // Through the view, so the rows are the view's; only its ID is read, which leaves the payload
        // unpacking out of the plan.
        var rows = await (from g in db.Set<GoldenCustomer>().AsNoTracking()
                          join s in db.Set<IdentitySummary>().AsNoTracking() on g.ID equals s.IdentityID into staged
                          from s in staged.DefaultIfEmpty()
                          select new
                          {
                              g.ID,
                              Members = s == null ? (int?)null : s.MemberCount,
                              Categories = s == null ? (long?)null : s.Categories,
                          })
            .ToListAsync(ct);

        // A golden without a staged summary (its identity was not in the latest run) takes its record
        // count from the view, as the SQL path does.
        var missing = rows.Where(r => r.Members is null).Select(r => r.ID).ToList();
        var countOf = new Dictionary<long, int>();
        if (missing.Count > 0)
        {
            var counts = missing.Count <= MissingByIdLimit
                ? await db.Set<GoldenCustomer>().AsNoTracking().Where(g => missing.Contains(g.ID))
                    .Select(g => new { g.ID, g.SourceCount }).ToListAsync(ct)
                : await db.Set<GoldenCustomer>().AsNoTracking()
                    .Select(g => new { g.ID, g.SourceCount }).ToListAsync(ct);
            foreach (var c in counts) countOf[c.ID] = c.SourceCount;
        }

        int n = rows.Count;
        var ids = new long[n];
        var members = new int[n];
        var bits = new long[n];
        for (int i = 0; i < n; i++)
        {
            var r = rows[i];
            ids[i] = r.ID;
            members[i] = r.Members ?? countOf.GetValueOrDefault(r.ID);
            bits[i] = (long)GoldenReasons.ForReader(r.Categories, members[i], reasonsStaged);
        }

        // "Most records first": record count descending, then ID.
        var order = Enumerable.Range(0, n).ToArray();
        Array.Sort(order, (x, y) =>
        {
            int c = members[y].CompareTo(members[x]);
            return c != 0 ? c : ids[x].CompareTo(ids[y]);
        });
        var sortedIds = new long[n];
        var sortedMembers = new int[n];
        var sortedBits = new long[n];
        for (int i = 0; i < n; i++)
        {
            sortedIds[i] = ids[order[i]];
            sortedMembers[i] = members[order[i]];
            sortedBits[i] = bits[order[i]];
        }
        var byId = Enumerable.Range(0, n).ToArray();
        Array.Sort(byId, (x, y) => sortedIds[x].CompareTo(sortedIds[y]));

        var reasonCounts = new Dictionary<string, int>();
        foreach (var reason in GoldenReasons.All)
        {
            long bit = (long)reason;
            int count = 0;
            for (int i = 0; i < n; i++)
                if ((sortedBits[i] & bit) != 0) count++;
            if (count > 0) reasonCounts[reason.ToString()] = count;
        }

        return new Snapshot
        {
            ReasonsStaged = reasonsStaged,
            Ids = sortedIds,
            Members = sortedMembers,
            Bits = sortedBits,
            ById = byId,
            ReasonCounts = reasonCounts,
        };
    }
}
