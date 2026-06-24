using System.Diagnostics;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>Runs check definitions against resolved sources and produces result rows.</summary>
public sealed class CheckRunner(SourceRegistry sources)
{
    public async Task<List<CheckResult>> RunAsync(
        IEnumerable<CheckDefinition> checks,
        string runId,
        Func<string, Task>? log = null,
        CancellationToken ct = default)
    {
        var all = new List<CheckResult>();

        foreach (var check in checks)
        {
            ct.ThrowIfCancellationRequested();

            var startedAt = DateTimeOffset.UtcNow;
            var sw = Stopwatch.StartNew();
            List<AssertOutcome> outcomes;

            try
            {
                var measures = new Dictionary<string, MeasureOutcome>();
                foreach (var spec in check.Measures)
                {
                    var source = sources.Resolve(spec.Source);
                    measures[spec.Key] = source is null
                        ? new MeasureOutcome { Error = $"Unknown source '{spec.Source}'." }
                        : await source.MeasureAsync(spec, check.Grouped, ct);
                }

                outcomes = AssertEvaluator.Evaluate(check, measures, DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                outcomes = [new AssertOutcome(null, HealthStatus.Error, ex.Message, [])];
            }

            sw.Stop();

            foreach (var o in outcomes)
            {
                all.Add(new CheckResult
                {
                    RunId = runId,
                    CheckName = check.Name,
                    Domain = check.Domain,
                    Category = check.Category,
                    Severity = check.Severity,
                    Description = check.Description,
                    Order = check.Order,
                    BreakdownKey = o.GroupKey,
                    Status = o.Status,
                    Message = o.Message,
                    Metrics = o.Metrics,
                    StartedAtUtc = startedAt,
                    DurationMs = sw.ElapsedMilliseconds,
                });
            }

            if (log is not null)
            {
                var worst = Rollup(outcomes.Select(x => x.Status));
                await log($"[{worst}] {check.Name} — {outcomes.Count} result(s), {sw.ElapsedMilliseconds} ms");
            }
        }

        return all;
    }

    /// <summary>Worst status across a set (Fail/Error dominate, then Warn, Skipped, Pass).</summary>
    public static HealthStatus Rollup(IEnumerable<HealthStatus> statuses)
    {
        HealthStatus worst = HealthStatus.Pass;
        var worstRank = -1;
        foreach (var s in statuses)
        {
            var r = Rank(s);
            if (r > worstRank) { worstRank = r; worst = s; }
        }
        return worst;
    }

    public static int Rank(HealthStatus s) => s switch
    {
        HealthStatus.Fail => 4,
        HealthStatus.Error => 4,
        HealthStatus.Warn => 2,
        HealthStatus.Skipped => 1,
        _ => 0,
    };
}
