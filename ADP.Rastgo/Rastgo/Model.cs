namespace ShiftSoftware.ADP.Rastgo;

/// <summary>Verdict for a single check result row.</summary>
public enum HealthStatus
{
    Pass,
    Warn,
    Fail,
    Error,   // the check itself could not run (bad SQL, source unavailable, etc.)
    Skipped, // intentionally not evaluated
}

/// <summary>A declarative check, loaded from YAML config. One per row in a checks file.</summary>
public sealed class CheckDefinition
{
    public string Name { get; set; } = "";
    public string Domain { get; set; } = "";
    public string Category { get; set; } = "";          // freshness | reconciliation | quality | flow
    public string Severity { get; set; } = "warning";   // critical | warning | info
    public string? Description { get; set; }            // optional one-line blurb, surfaced as a tooltip in the dashboard
    public int? Order { get; set; }                     // display-order hint; lower = earlier. Orders checks within a family, and families within a category by the family's lowest. Unset sorts after ordered ones.
    public string? Breakdown { get; set; }              // label for the group dimension; non-null => grouped
    public List<MeasureSpec> Measures { get; set; } = [];
    public AssertSpec Assert { get; set; } = new();

    /// <summary>When set, every measure is expected to return (k, v) rows and the assert is evaluated per group.</summary>
    public bool Grouped => !string.IsNullOrWhiteSpace(Breakdown);
}

/// <summary>How to obtain one metric from one source. Scalar measures select <c>v</c>; grouped select <c>k, v</c>.</summary>
public sealed class MeasureSpec
{
    public string Key { get; set; } = "value";
    public string Source { get; set; } = "duckdb";       // duckdb | cosmos | fileshare
    public string? Sql { get; set; }                     // duckdb / cosmos
    public string? Path { get; set; }                    // fileshare (relative to configured base)
    public string? Database { get; set; }                // cosmos
    public string? Container { get; set; }               // cosmos
    public string ValueKind { get; set; } = "number";    // number | timestamp
}

/// <summary>The assertion applied to the measures. Fields are interpreted per <see cref="Type"/>.</summary>
public sealed class AssertSpec
{
    public string Type { get; set; } = "threshold";      // age | threshold | diff
    public string? Of { get; set; }                      // age/threshold: which measure key (defaults to first)
    public string? Left { get; set; }                    // diff: left measure key
    public string? Right { get; set; }                   // diff: right measure key
    public string? Max { get; set; }                     // age: duration ("26h"); threshold: numeric ceiling
    public string? Min { get; set; }                     // threshold: numeric floor
    public string? Warn { get; set; }                    // age: duration warn level (below Max)
    public double? Tolerance { get; set; }               // diff: absolute tolerance
    public double? TolerancePct { get; set; }            // diff: relative tolerance (% of right side)
}

/// <summary>One measured value: a number (counts/ratios) or a timestamp (freshness).</summary>
public sealed record MeasureCell(double? Number, DateTimeOffset? Timestamp);

/// <summary>The result of running one <see cref="MeasureSpec"/>: cells keyed by group ("" for scalar), or an error.</summary>
public sealed class MeasureOutcome
{
    public const string ScalarKey = "";

    public Dictionary<string, MeasureCell> Cells { get; init; } = [];
    public DateTimeOffset? AsOfUtc { get; init; }
    public string? Error { get; init; }
}

/// <summary>One emitted result row (one per check, or one per breakdown group). Serialized as a JSONL line.</summary>
public sealed class CheckResult
{
    public string RunId { get; set; } = "";
    public string CheckName { get; set; } = "";
    public string Domain { get; set; } = "";
    public string Category { get; set; } = "";
    public string Severity { get; set; } = "";
    public string? Description { get; set; }
    public int? Order { get; set; }
    public string? BreakdownKey { get; set; }
    public HealthStatus Status { get; set; }
    public string Message { get; set; } = "";
    public Dictionary<string, double?> Metrics { get; set; } = [];
    public DateTimeOffset StartedAtUtc { get; set; }
    public long DurationMs { get; set; }
}
