using System.Globalization;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>Parses durations like <c>30s</c>, <c>90m</c>, <c>26h</c>, <c>2d</c>.</summary>
public static partial class DurationParser
{
    [GeneratedRegex(@"^\s*(\d+(?:\.\d+)?)\s*([smhd])\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DurationRegex();

    public static TimeSpan Parse(string value)
    {
        var m = DurationRegex().Match(value);
        if (!m.Success)
            throw new FormatException($"Invalid duration '{value}'. Use forms like 30s, 90m, 26h, 2d.");

        var n = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        return m.Groups[2].Value.ToLowerInvariant() switch
        {
            "s" => TimeSpan.FromSeconds(n),
            "m" => TimeSpan.FromMinutes(n),
            "h" => TimeSpan.FromHours(n),
            "d" => TimeSpan.FromDays(n),
            _ => throw new FormatException($"Invalid duration unit in '{value}'."),
        };
    }
}

/// <summary>One verdict for one group (null group = scalar check).</summary>
public sealed record AssertOutcome(string? GroupKey, HealthStatus Status, string Message, Dictionary<string, double?> Metrics);

/// <summary>Applies a check's <see cref="AssertSpec"/> to its measured outcomes.</summary>
public static class AssertEvaluator
{
    public static List<AssertOutcome> Evaluate(CheckDefinition check, IReadOnlyDictionary<string, MeasureOutcome> measures, DateTimeOffset nowUtc)
    {
        var errored = measures.Where(m => m.Value.Error is not null).ToList();
        if (errored.Count > 0)
            return [new AssertOutcome(null, HealthStatus.Error, string.Join("; ", errored.Select(e => $"{e.Key}: {e.Value.Error}")), [])];

        return check.Assert.Type.ToLowerInvariant() switch
        {
            "age" => EvalAge(check, measures, nowUtc),
            "threshold" => EvalThreshold(check, measures),
            "diff" => EvalDiff(check, measures),
            _ => [new AssertOutcome(null, HealthStatus.Error, $"Unknown assert type '{check.Assert.Type}'.", [])],
        };
    }

    private static HealthStatus Breach(string severity)
        => string.Equals(severity, "critical", StringComparison.OrdinalIgnoreCase) ? HealthStatus.Fail : HealthStatus.Warn;

    private static MeasureOutcome Pick(IReadOnlyDictionary<string, MeasureOutcome> m, string? key)
        => key is not null && m.TryGetValue(key, out var byKey) ? byKey : m.Values.First();

    private static string? Group(string key) => key == MeasureOutcome.ScalarKey ? null : key;

    private static List<AssertOutcome> EvalAge(CheckDefinition c, IReadOnlyDictionary<string, MeasureOutcome> m, DateTimeOffset now)
    {
        var src = Pick(m, c.Assert.Of);
        var max = DurationParser.Parse(c.Assert.Max ?? throw new FormatException($"Check '{c.Name}': age assert requires 'max'."));
        TimeSpan? warn = string.IsNullOrWhiteSpace(c.Assert.Warn) ? null : DurationParser.Parse(c.Assert.Warn);

        var results = new List<AssertOutcome>();
        foreach (var (key, cell) in src.Cells)
        {
            if (cell.Timestamp is null)
            {
                results.Add(new AssertOutcome(Group(key), HealthStatus.Error, "No timestamp for age assert (is the source empty?).", []));
                continue;
            }

            var age = now - cell.Timestamp.Value;
            var metrics = new Dictionary<string, double?> { ["ageHours"] = Math.Round(age.TotalHours, 2), ["maxHours"] = Math.Round(max.TotalHours, 2) };

            HealthStatus status;
            string msg;
            if (age < TimeSpan.Zero) { status = HealthStatus.Warn; msg = $"Newest value is {Fmt(age.Negate())} in the future — likely future-dated/bad rows; filter them in the measure SQL."; }
            else if (age > max) { status = Breach(c.Severity); msg = $"Stale: {Fmt(age)} old (max {Fmt(max)})."; }
            else if (warn is { } w && age > w) { status = HealthStatus.Warn; msg = $"Aging: {Fmt(age)} old (warn {Fmt(w)})."; }
            else { status = HealthStatus.Pass; msg = $"Fresh: {Fmt(age)} old."; }

            results.Add(new AssertOutcome(Group(key), status, msg, metrics));
        }

        if (results.Count == 0)
            results.Add(new AssertOutcome(null, HealthStatus.Error, "No rows returned for age measure.", []));
        return results;
    }

    private static List<AssertOutcome> EvalThreshold(CheckDefinition c, IReadOnlyDictionary<string, MeasureOutcome> m)
    {
        var src = Pick(m, c.Assert.Of);
        var min = ParseNullableDouble(c.Assert.Min);
        var max = ParseNullableDouble(c.Assert.Max);

        var results = new List<AssertOutcome>();
        foreach (var (key, cell) in src.Cells)
        {
            var metrics = new Dictionary<string, double?> { ["value"] = cell.Number, ["min"] = min, ["max"] = max };
            if (cell.Number is not { } v)
            {
                results.Add(new AssertOutcome(Group(key), HealthStatus.Error, "No numeric value for threshold assert.", metrics));
                continue;
            }

            HealthStatus status;
            string msg;
            if (min is { } mn && v < mn) { status = Breach(c.Severity); msg = $"{v} below min {mn}."; }
            else if (max is { } mx && v > mx) { status = Breach(c.Severity); msg = $"{v} above max {mx}."; }
            else { status = HealthStatus.Pass; msg = $"{v} within range."; }

            results.Add(new AssertOutcome(Group(key), status, msg, metrics));
        }

        if (results.Count == 0)
            results.Add(new AssertOutcome(null, HealthStatus.Error, "No rows returned for threshold measure.", []));
        return results;
    }

    private static List<AssertOutcome> EvalDiff(CheckDefinition c, IReadOnlyDictionary<string, MeasureOutcome> m)
    {
        var leftKey = c.Assert.Left ?? throw new FormatException($"Check '{c.Name}': diff assert requires 'left'.");
        var rightKey = c.Assert.Right ?? throw new FormatException($"Check '{c.Name}': diff assert requires 'right'.");
        if (!m.TryGetValue(leftKey, out var left) || !m.TryGetValue(rightKey, out var right))
            return [new AssertOutcome(null, HealthStatus.Error, $"diff assert needs measures '{leftKey}' and '{rightKey}'.", [])];

        var tolerance = c.Assert.Tolerance ?? 0;
        var tolerancePct = c.Assert.TolerancePct ?? 0;

        var keys = left.Cells.Keys.Union(right.Cells.Keys).Distinct().ToList();
        var results = new List<AssertOutcome>();
        foreach (var key in keys)
        {
            var l = left.Cells.GetValueOrDefault(key)?.Number ?? 0;
            var r = right.Cells.GetValueOrDefault(key)?.Number ?? 0;
            var diff = l - r;
            var absDiff = Math.Abs(diff);
            var withinPct = tolerancePct > 0 && absDiff <= Math.Abs(r) * tolerancePct / 100.0;
            var ok = absDiff <= tolerance || withinPct;

            var metrics = new Dictionary<string, double?> { ["left"] = l, ["right"] = r, ["diff"] = diff };
            var status = ok ? HealthStatus.Pass : Breach(c.Severity);
            var msg = ok ? $"Match (Δ {diff})." : $"Mismatch: {leftKey}={l} vs {rightKey}={r} (Δ {diff}).";
            results.Add(new AssertOutcome(Group(key), status, msg, metrics));
        }

        if (results.Count == 0)
            results.Add(new AssertOutcome(null, HealthStatus.Pass, "No keys to compare.", []));
        return results;
    }

    private static double? ParseNullableDouble(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : double.Parse(s, CultureInfo.InvariantCulture);

    private static string Fmt(TimeSpan t)
        => t.TotalDays >= 1 ? $"{t.TotalDays:0.#}d" : t.TotalHours >= 1 ? $"{t.TotalHours:0.#}h" : $"{t.TotalMinutes:0}m";
}
