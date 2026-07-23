namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// One resolution source: yields normalized <see cref="RealRecord"/>s for a batch resolve. The
/// per-tenant flexibility lives entirely behind this contract — hosts implement feeds over
/// whatever their sources are (dealer-DMS extracts, CRM tickets, app registrations, broker
/// invoices, staged SQL tables) and hand the engine one combined record list.
///
/// Contract notes:
///   - <see cref="SourceSystem"/> must be stable across runs (it is half of every profile key).
///   - Records must be DETERMINISTIC for unchanged source data (stable ids, stable ordering,
///     no wall-clock) — the delta discipline hashes what feeds produce.
///   - A feed that cannot reach its source should THROW, not return empty: the resolve treats a
///     source with prior registry rows and zero current records as an outage and freezes it, but
///     an explicit failure lets the host decide; returning empty silently is indistinguishable
///     from "every customer left".
///   - Set <see cref="RealRecord.Idx"/> to 0; <see cref="Feeds.Combine"/> reindexes.
/// </summary>
public interface ISourceFeed
{
    string SourceSystem { get; }
    IReadOnlyList<RealRecord> Read();
}

public static class Feeds
{
    /// <summary>Concatenate record sets into one dense-indexed list (Idx is positional in the
    /// combined array — blocking, clustering, and the case browser all rely on it).</summary>
    public static List<RealRecord> Combine(params IReadOnlyList<RealRecord>[] sets)
    {
        var all = new List<RealRecord>(sets.Sum(s => s.Count));
        foreach (var set in sets)
            foreach (var r in set)
                all.Add(r with { Idx = all.Count });
        return all;
    }
}
