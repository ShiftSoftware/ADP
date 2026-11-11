using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Controllers;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Darlastic.API.Extensions;

/// <summary>
/// Reads, ahead of the first request that needs them, the per-run values the host's options ask for
/// (<see cref="RegistryRunCache"/>): the case browser's summary counts, its golden row index and customer search
/// snapshot, its record search index, and the cities enough customers share.
///
/// <para><b>Why.</b> Each is read once per resolve run by the first request that needs it, and that request
/// waits: a pass over every staged record for the record index is tens of seconds at millions of records. A host
/// calls <see cref="WarmAsync"/> when it starts and after each resolve it runs, so no person pays for it. A value
/// already read for the latest run costs one probe of the run table.</para>
///
/// <para>Registered scoped by <c>AddDarlasticApiServices</c>; resolve it from a scope of its own outside a
/// request. Reads only what an option turned on, and nothing at all with every option off.</para>
/// </summary>
public sealed class RegistryRunWarmup(ShiftDbContext db, IOptions<DarlasticApiOptions> options, RegistryRunCache cache)
{
    /// <summary>
    /// Reads every per-run value the options ask for. Returns one line per value (its name, a count and the
    /// seconds it took; never a customer value). A value that cannot be read is named in its line with the
    /// exception's type and the rest are still read: the first request that needs it reads it, as without a
    /// warm-up. A cancellation is passed on.
    /// </summary>
    public async Task<IReadOnlyList<string>> WarmAsync(CancellationToken ct = default)
    {
        var o = options.Value;
        var notes = new List<string>();

        async Task Step(string name, Func<Task<string>> read)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                string what = await read();
                notes.Add(string.Create(CultureInfo.InvariantCulture, $"{name}: {what} in {sw.Elapsed.TotalSeconds:F1}s"));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                notes.Add($"{name}: not read ({ex.GetType().Name}); the first request reads it");
            }
        }

        if (o.SummaryPerRun)
            await Step("summary counts", async () =>
            {
                var s = await cache.GetAsync(db, SummaryCounts.CacheName, SummaryCounts.CountAsync, ct);
                return string.Create(CultureInfo.InvariantCulture, $"{s.StagedRecords:N0} staged records, {s.GoldenTotal:N0} goldens");
            });

        if (o.GoldenRowIndex)
        {
            await Step("golden rows", async () =>
            {
                var index = await cache.GetAsync(db, GoldenRowIndex.CacheName, GoldenRowIndex.BuildAsync, ct);
                return string.Create(CultureInfo.InvariantCulture, $"{index.Count:N0} goldens");
            });
            await Step("golden search", async () =>
            {
                var index = await cache.GetAsync(db, GoldenSearchIndex.CacheName, GoldenSearchIndex.BuildAsync, ct);
                return string.Create(CultureInfo.InvariantCulture, $"{index.Count:N0} goldens, {index.Bytes / 1048576.0:F0} MB");
            });
        }

        if (o.RecordSearchIndex)
            await Step("record search", async () =>
            {
                var index = await cache.GetAsync(db, RecordSearchIndex.CacheName, RecordSearchIndex.BuildAsync, ct);
                return string.Create(CultureInfo.InvariantCulture, $"{index.Count:N0} staged records, {index.Bytes / 1048576.0:F0} MB");
            });

        if (o.CityMinimumShare > 0)
            await Step("shared cities", async () =>
            {
                var cities = await CityDisclosureAttribute.SharedCitiesOfRunAsync(db, cache, o.CityMinimumShare, ct);
                return string.Create(CultureInfo.InvariantCulture, $"{cities.Count:N0} cities");
            });

        return notes;
    }
}
