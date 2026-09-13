using System.Globalization;

namespace ShiftSoftware.ADP.Rastgo;

public enum CheckDiagnosticSeverity
{
    Warning,
    Error,
}

/// <summary>A code-backed authoring problem. Paths use the YAML list index, for example <c>checks[2].assert.max</c>.</summary>
public sealed record CheckDiagnostic(
    CheckDiagnosticSeverity Severity,
    string Code,
    string Path,
    string Message,
    int? Line = null,
    int? Column = null);

/// <summary>A tolerant parse of a check pack plus strict authoring diagnostics.</summary>
public sealed record AuthoringCheckPack(
    IReadOnlyList<CheckDefinition> Checks,
    IReadOnlyList<CheckDiagnostic> Diagnostics)
{
    public bool IsValid => Diagnostics.All(d => d.Severity != CheckDiagnosticSeverity.Error);
}

/// <summary>A field in the Rastgo YAML language. The renderer and validator share this reference.</summary>
public sealed record CheckLanguageField(
    string Scope,
    string Name,
    string Type,
    string Default,
    string RequiredWhen,
    string Description);

/// <summary>The public authoring vocabulary. Keep validation and rendered documentation tied to these values.</summary>
public static class CheckLanguage
{
    public static readonly IReadOnlySet<string> Severities = Set("critical", "warning", "info");
    public static readonly IReadOnlySet<string> ValueKinds = Set("number", "timestamp", "count");
    public static readonly IReadOnlySet<string> AssertTypes = Set("age", "threshold", "diff");

    public static readonly IReadOnlyList<CheckLanguageField> Fields =
    [
        new("check", "name", "string", "—", "always", "Unique stable identifier. Dot-separated names create dashboard families."),
        new("check", "domain", "string", "—", "always", "Pack/domain identifier stored with every result."),
        new("check", "category", "string", "—", "always", "Display group. Common values are freshness, reconciliation, quality, volume, and flow; custom values are allowed."),
        new("check", "severity", "critical | warning | info", "warning", "optional", "Controls whether an assertion breach is Fail or Warn."),
        new("check", "description", "string", "—", "optional", "Short human explanation shown with the check."),
        new("check", "order", "integer", "—", "optional", "Lower values render first within a family/category."),
        new("check", "breakdown", "string", "—", "optional", "Declares a grouped check. Every measure must then return k and v columns."),
        new("check", "measures", "measure[]", "—", "always", "One or more uniquely keyed measurements."),
        new("check", "assert", "assert", "—", "always", "The assertion evaluated against the measurements."),
        new("measure", "key", "string", "value", "optional", "Unique key referenced by the assertion."),
        new("measure", "source", "registered source name", "duckdb", "optional", "SourceRegistry name. Qualified names such as sql:reporting are supported."),
        new("measure", "valueKind", "number | timestamp | count", "number", "optional", "Expected v contract. File-share number/timestamp both return modification time; count returns a file count."),
        new("measure", "sql", "string", "—", "DuckDB, SQL, Cosmos", "Read-only query. Scalar queries expose v; grouped queries expose k and v."),
        new("measure", "path", "string", "—", "file share", "Relative or absolute file/glob path. A **/ prefix enables bounded recursive matching."),
        new("measure", "database", "string", "—", "Cosmos", "Cosmos database name."),
        new("measure", "container", "string", "—", "Cosmos", "Cosmos container name."),
        new("assert", "type", "age | threshold | diff", "threshold", "optional", "Selects the assertion contract."),
        new("assert", "of", "measure key", "first measure", "age or threshold", "Measurement to evaluate."),
        new("assert", "max", "duration or number", "—", "age; optional ceiling for threshold", "Age accepts a positive duration; threshold accepts an invariant-culture number."),
        new("assert", "warn", "duration", "—", "optional for age", "Soft age boundary below max."),
        new("assert", "min", "number", "—", "optional floor for threshold", "Invariant-culture numeric floor."),
        new("assert", "left", "measure key", "—", "diff", "Left side of the comparison."),
        new("assert", "right", "measure key", "—", "diff", "Right side of the comparison."),
        new("assert", "tolerance", "number", "0", "optional for diff", "Allowed absolute difference; must be non-negative."),
        new("assert", "tolerancePct", "number", "0", "optional for diff", "Allowed difference as a percentage of the right side; must be non-negative."),
    ];

    internal static readonly IReadOnlySet<string> CheckProperties = Set(
        "name", "domain", "category", "severity", "description", "order", "breakdown", "measures", "assert");

    internal static readonly IReadOnlySet<string> MeasureProperties = Set(
        "key", "source", "sql", "path", "database", "container", "valueKind");

    internal static readonly IReadOnlySet<string> AssertProperties = Set(
        "type", "of", "left", "right", "max", "min", "warn", "tolerance", "tolerancePct");

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
}

/// <summary>Semantic validation for parsed check definitions. Runtime loading stays tolerant for compatibility.</summary>
public static class CheckValidator
{
    public static IReadOnlyList<CheckDiagnostic> Validate(
        IReadOnlyList<CheckDefinition> checks,
        IEnumerable<string>? registeredSources = null)
    {
        var diagnostics = new List<CheckDiagnostic>();
        var sourceNames = registeredSources is null
            ? null
            : new HashSet<string>(registeredSources, StringComparer.OrdinalIgnoreCase);

        foreach (var duplicate in checks
            .Select((check, index) => (check, index))
            .Where(x => !string.IsNullOrWhiteSpace(x.check.Name))
            .GroupBy(x => x.check.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1))
        {
            foreach (var item in duplicate)
                Add(diagnostics, "duplicate_check_name", $"checks[{item.index}].name",
                    $"Check name '{item.check.Name}' is duplicated. Names must be unique across the loaded pack.");
        }

        for (var i = 0; i < checks.Count; i++)
            ValidateCheck(checks[i], i, sourceNames, diagnostics);

        return diagnostics;
    }

    private static void ValidateCheck(
        CheckDefinition check,
        int index,
        IReadOnlySet<string>? sources,
        List<CheckDiagnostic> diagnostics)
    {
        var root = $"checks[{index}]";
        Required(check.Name, "name");
        Required(check.Domain, "domain");
        Required(check.Category, "category");

        if (!CheckLanguage.Severities.Contains(check.Severity))
            Add(diagnostics, "unknown_severity", $"{root}.severity",
                $"Unknown severity '{check.Severity}'. Supported values: {string.Join(", ", CheckLanguage.Severities)}.");

        if (check.Measures is null || check.Measures.Count == 0)
        {
            Add(diagnostics, "missing_measures", $"{root}.measures", "At least one measure is required.");
        }
        else
        {
            foreach (var duplicate in check.Measures
                .Select((measure, measureIndex) => (measure, measureIndex))
                .GroupBy(x => x.measure.Key?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1))
            {
                foreach (var item in duplicate)
                    Add(diagnostics, "duplicate_measure_key", $"{root}.measures[{item.measureIndex}].key",
                        $"Measure key '{item.measure.Key}' is duplicated within this check.");
            }

            for (var m = 0; m < check.Measures.Count; m++)
                ValidateMeasure(check.Measures[m], $"{root}.measures[{m}]", sources, diagnostics);
        }

        ValidateAssert(check, root, diagnostics);
        return;

        void Required(string? value, string property)
        {
            if (string.IsNullOrWhiteSpace(value))
                Add(diagnostics, $"missing_{property}", $"{root}.{property}", $"Check {property} is required.");
        }
    }

    private static void ValidateMeasure(
        MeasureSpec measure,
        string path,
        IReadOnlySet<string>? sources,
        List<CheckDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(measure.Key))
            Add(diagnostics, "missing_measure_key", $"{path}.key", "Measure key is required.");
        if (string.IsNullOrWhiteSpace(measure.Source))
            Add(diagnostics, "missing_source", $"{path}.source", "Measure source is required.");
        else if (sources is not null && !sources.Contains(measure.Source))
            Add(diagnostics, "unknown_source", $"{path}.source",
                $"Source '{measure.Source}' is not registered on this host. Registered sources: {string.Join(", ", sources.OrderBy(x => x))}.");

        if (!CheckLanguage.ValueKinds.Contains(measure.ValueKind))
            Add(diagnostics, "unknown_value_kind", $"{path}.valueKind",
                $"Unknown value kind '{measure.ValueKind}'. Supported values: {string.Join(", ", CheckLanguage.ValueKinds)}.");

        var kind = SourceKind(measure.Source);
        if (kind is "duckdb" or "sql")
        {
            Required(measure.Sql, "sql", $"{kind} measures require a read-only SQL query.");
            Reject(measure.Path, "path");
            Reject(measure.Database, "database");
            Reject(measure.Container, "container");
            if (string.Equals(measure.ValueKind, "count", StringComparison.OrdinalIgnoreCase))
                Add(diagnostics, "invalid_value_kind_for_source", $"{path}.valueKind",
                    "Use valueKind 'number' for numeric SQL results; 'count' is reserved for file-share enumeration.");
        }
        else if (kind == "cosmos")
        {
            Required(measure.Sql, "sql", "Cosmos measures require a read-only query.");
            Required(measure.Database, "database", "Cosmos measures require a database.");
            Required(measure.Container, "container", "Cosmos measures require a container.");
            Reject(measure.Path, "path");
            if (!string.Equals(measure.ValueKind, "number", StringComparison.OrdinalIgnoreCase))
                Add(diagnostics, "invalid_value_kind_for_source", $"{path}.valueKind",
                    "The Cosmos connector currently returns numeric v values only.");
        }
        else if (kind == "fileshare")
        {
            Required(measure.Path, "path", "File-share measures require a path or glob.");
            Reject(measure.Sql, "sql");
            Reject(measure.Database, "database");
            Reject(measure.Container, "container");
        }

        return;

        void Required(string? value, string property, string message)
        {
            if (string.IsNullOrWhiteSpace(value)) Add(diagnostics, $"missing_measure_{property}", $"{path}.{property}", message);
        }

        void Reject(string? value, string property)
        {
            if (!string.IsNullOrWhiteSpace(value))
                Add(diagnostics, "invalid_measure_combination", $"{path}.{property}",
                    $"Property '{property}' is not used by source '{measure.Source}'.");
        }
    }

    private static void ValidateAssert(CheckDefinition check, string root, List<CheckDiagnostic> diagnostics)
    {
        if (check.Assert is null)
        {
            Add(diagnostics, "missing_assert", $"{root}.assert", "An assert mapping is required.");
            return;
        }

        var assertion = check.Assert;
        var path = $"{root}.assert";
        if (!CheckLanguage.AssertTypes.Contains(assertion.Type))
        {
            Add(diagnostics, "unknown_assert_type", $"{path}.type",
                $"Unknown assert type '{assertion.Type}'. Supported values: {string.Join(", ", CheckLanguage.AssertTypes)}.");
            return;
        }

        var measureList = check.Measures ?? [];
        var measures = new HashSet<string>(measureList.Select(m => m.Key), StringComparer.OrdinalIgnoreCase);
        var type = assertion.Type.ToLowerInvariant();
        if (type is "age" or "threshold")
        {
            if (!string.IsNullOrWhiteSpace(assertion.Of) && !measures.Contains(assertion.Of))
                Add(diagnostics, "unknown_measure_reference", $"{path}.of", $"Measure '{assertion.Of}' does not exist in this check.");
        }

        if (type == "age")
        {
            Duration(assertion.Max, "max", required: true);
            Duration(assertion.Warn, "warn", required: false);
            Reject(assertion.Min, "min"); Reject(assertion.Left, "left"); Reject(assertion.Right, "right");
            RejectNumber(assertion.Tolerance, "tolerance"); RejectNumber(assertion.TolerancePct, "tolerancePct");

            if (TryDuration(assertion.Warn, out var warn) && TryDuration(assertion.Max, out var max) && warn >= max)
                Add(diagnostics, "invalid_age_boundaries", $"{path}.warn", "Age warn must be less than max.");

            var selected = SelectedMeasure(check, assertion.Of);
            if (selected is not null && !ReturnsTimestamp(selected))
                Add(diagnostics, "invalid_assert_value_kind", $"{path}.of",
                    $"Age requires a timestamp measure; '{selected.Key}' is configured as '{selected.ValueKind}'.");
        }
        else if (type == "threshold")
        {
            var hasMin = Number(assertion.Min, "min");
            var hasMax = Number(assertion.Max, "max");
            if (!hasMin && !hasMax)
                Add(diagnostics, "missing_threshold_boundary", path, "Threshold requires at least one numeric min or max.");
            if (TryNumber(assertion.Min, out var min) && TryNumber(assertion.Max, out var max) && min > max)
                Add(diagnostics, "invalid_threshold_boundaries", path, "Threshold min must be less than or equal to max.");
            Reject(assertion.Warn, "warn"); Reject(assertion.Left, "left"); Reject(assertion.Right, "right");
            RejectNumber(assertion.Tolerance, "tolerance"); RejectNumber(assertion.TolerancePct, "tolerancePct");

            var selected = SelectedMeasure(check, assertion.Of);
            if (selected is not null && ReturnsTimestamp(selected))
                Add(diagnostics, "invalid_assert_value_kind", $"{path}.of",
                    $"Threshold requires a numeric measure; '{selected.Key}' returns timestamps.");
        }
        else
        {
            Reference(assertion.Left, "left");
            Reference(assertion.Right, "right");
            if (!string.IsNullOrWhiteSpace(assertion.Left) &&
                string.Equals(assertion.Left, assertion.Right, StringComparison.OrdinalIgnoreCase))
                Add(diagnostics, "same_diff_measure", $"{path}.right", "Diff left and right must reference different measures.");
            if (assertion.Tolerance < 0) Add(diagnostics, "negative_tolerance", $"{path}.tolerance", "Tolerance must be non-negative.");
            if (assertion.TolerancePct < 0) Add(diagnostics, "negative_tolerance_pct", $"{path}.tolerancePct", "TolerancePct must be non-negative.");
            Reject(assertion.Of, "of"); Reject(assertion.Min, "min"); Reject(assertion.Max, "max"); Reject(assertion.Warn, "warn");

            foreach (var (key, property) in new[] { (assertion.Left, "left"), (assertion.Right, "right") })
            {
                var measure = measureList.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));
                if (measure is not null && ReturnsTimestamp(measure))
                    Add(diagnostics, "invalid_assert_value_kind", $"{path}.{property}", "Diff requires numeric measures.");
            }
        }

        return;

        void Reference(string? value, string property)
        {
            if (string.IsNullOrWhiteSpace(value))
                Add(diagnostics, $"missing_diff_{property}", $"{path}.{property}", $"Diff requires '{property}'.");
            else if (!measures.Contains(value))
                Add(diagnostics, "unknown_measure_reference", $"{path}.{property}", $"Measure '{value}' does not exist in this check.");
        }

        void Duration(string? value, string property, bool required)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required) Add(diagnostics, $"missing_age_{property}", $"{path}.{property}", $"Age requires '{property}'.");
                return;
            }
            if (!TryDuration(value, out var parsed) || parsed <= TimeSpan.Zero)
                Add(diagnostics, "invalid_duration", $"{path}.{property}",
                    $"'{value}' is not a positive duration. Use forms such as 30s, 90m, 26h, or 2d.");
        }

        bool Number(string? value, string property)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (!TryNumber(value, out _))
                Add(diagnostics, "invalid_number", $"{path}.{property}", $"'{value}' is not an invariant-culture number.");
            return true;
        }

        void Reject(string? value, string property)
        {
            if (!string.IsNullOrWhiteSpace(value))
                Add(diagnostics, "invalid_assert_combination", $"{path}.{property}",
                    $"Property '{property}' is not used by assert type '{assertion.Type}'.");
        }

        void RejectNumber(double? value, string property)
        {
            if (value is not null)
                Add(diagnostics, "invalid_assert_combination", $"{path}.{property}",
                    $"Property '{property}' is not used by assert type '{assertion.Type}'.");
        }
    }

    private static MeasureSpec? SelectedMeasure(CheckDefinition check, string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? check.Measures?.FirstOrDefault()
            : check.Measures?.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));

    private static bool ReturnsTimestamp(MeasureSpec measure) =>
        string.Equals(measure.ValueKind, "timestamp", StringComparison.OrdinalIgnoreCase) ||
        (SourceKind(measure.Source) == "fileshare" && !string.Equals(measure.ValueKind, "count", StringComparison.OrdinalIgnoreCase));

    private static string SourceKind(string? name)
    {
        var value = name?.Trim() ?? "";
        var separator = value.IndexOf(SourceName.Separator);
        return (separator < 0 ? value : value[..separator]).ToLowerInvariant();
    }

    private static bool TryDuration(string? value, out TimeSpan duration)
    {
        try { duration = DurationParser.Parse(value ?? ""); return true; }
        catch (FormatException) { duration = default; return false; }
        catch (OverflowException) { duration = default; return false; }
    }

    private static bool TryNumber(string? value, out double number) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && double.IsFinite(number);

    private static void Add(List<CheckDiagnostic> diagnostics, string code, string path, string message) =>
        diagnostics.Add(new(CheckDiagnosticSeverity.Error, code, path, message));
}
