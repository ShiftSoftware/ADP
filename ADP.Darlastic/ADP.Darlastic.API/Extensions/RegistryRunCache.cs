using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Darlastic.API.Extensions;

/// <summary>
/// Values a reader derives from the whole registry that change only when a resolve finishes — the case
/// browser's golden row index, the cities enough customers share to be shown — computed once per
/// registry and run, and kept until the next run finishes.
///
/// <para>Keyed by the registry's connection and the latest finished run (its number and finish time,
/// so a registry restored to an older state is not mistaken for the run it replaced). Each request
/// pays one probe of the run table; the first request after a resolve pays the build, and requests
/// that arrive during it wait for that one build rather than starting their own.</para>
/// </summary>
public sealed class RegistryRunCache
{
    private sealed record Entry(string Run, object Value);

    private readonly ConcurrentDictionary<(string Registry, string Name), Entry> entries = new();
    private readonly ConcurrentDictionary<(string Registry, string Name), SemaphoreSlim> gates = new();

    /// <summary>The value <paramref name="name"/> names for the registry <paramref name="db"/> reads, as of
    /// its latest finished run, built with <paramref name="build"/> when that run has none yet.</summary>
    internal async Task<T> GetAsync<T>(ShiftDbContext db, string name,
        Func<ShiftDbContext, CancellationToken, Task<T>> build, CancellationToken ct) where T : class
    {
        var key = (db.Database.GetConnectionString() ?? "", name);
        string run = await LatestRunAsync(db, ct);
        if (entries.TryGetValue(key, out var hit) && hit.Run == run) return (T)hit.Value;

        var gate = gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            if (entries.TryGetValue(key, out hit) && hit.Run == run) return (T)hit.Value;
            var value = await build(db, ct);
            entries[key] = new Entry(run, value);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<string> LatestRunAsync(ShiftDbContext db, CancellationToken ct)
    {
        var last = await db.Set<ResolveRun>().AsNoTracking()
            .Where(r => r.FinishedUtc != null)
            .OrderByDescending(r => r.RunID)
            .Select(r => new { r.RunID, r.FinishedUtc })
            .FirstOrDefaultAsync(ct);
        return last is null ? "none" : $"{last.RunID}@{last.FinishedUtc:O}";
    }
}
