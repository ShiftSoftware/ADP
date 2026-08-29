namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// The per-check view both pages are built from: one row per check name, carrying that check's
/// <b>latest run only</b>, its rolled-up status, and the identity (title, family, category) the pages label
/// it with.
/// </summary>
internal sealed record CheckView(
    string Name, string Domain, string Family, string Category, string Severity, string? Description, int? Order,
    string RunId, HealthStatus Status, DateTimeOffset When, List<CheckResult> Rows)
{
    /// <summary>Friendly title, via the consumer's <see cref="DashboardOptions.TokenLabels"/>.</summary>
    public required string Title { get; init; }

    /// <summary>The check's latest result is not from its DOMAIN's newest run — renamed, or removed.</summary>
    public required bool Stale { get; init; }

    /// <summary>Carries breakdown rows, so its detail renders per group rather than as one message.</summary>
    public bool Grouped => Rows.Count > 1 || Rows[0].BreakdownKey is not null;
}

/// <summary>A name-family within a category. <paramref name="Tail"/> is null when the family IS the category.</summary>
internal sealed record FamilyView(string Key, string Slug, string? Tail, List<CheckView> Checks);

/// <summary>A category (the Freshness / Reconciliation / Quality / Volume / Flow axis) and its families.</summary>
internal sealed record CategoryView(string Key, string Slug, string Label, string Blurb, List<CheckView> Checks, List<FamilyView> Families);

/// <summary>A federated pack, and how many checks it contributes.</summary>
internal sealed record DomainView(string Id, int Count);

/// <summary>
/// Collapses raw result rows into the view model both renderers draw: the per-check rollup, the per-domain
/// staleness rule, the category → family axis, and the friendly-name <see cref="Labeler"/>.
/// <para>
/// Shared on purpose. The two pages previously kept separate copies of this, with separate label tables, so
/// the same check could be titled differently on each — the trends page's copy had none of the framework's
/// structural tokens, which is why a "duck_vs_cosmos" family read "Duck vs cosmos" there and "DuckDB vs
/// Cosmos" on the dashboard. One model, one vocabulary, both pages.
/// </para>
/// </summary>
internal sealed class CheckModel
{
    public required IReadOnlyList<CheckView> Checks { get; init; }
    public required IReadOnlyList<CategoryView> Categories { get; init; }
    public required IReadOnlyList<DomainView> Domains { get; init; }
    public required Labeler Labels { get; init; }

    /// <summary>More than one federated pack — domain chips and the per-row badge only earn their space then.</summary>
    public bool MultiDomain => Domains.Count > 1;

    public int Count(HealthStatus status) => Checks.Count(c => c.Status == status);

    public static CheckModel Build(IReadOnlyList<CheckResult> all, DashboardOptions options)
    {
        var L = new Labeler(options);

        // Category display order: the consumer's CategoryOrder wins; categories it omits fall after, by the
        // framework's built-in order then name.
        int CatRank(string key)
        {
            if (options.CategoryOrder is { } co)
            {
                for (var i = 0; i < co.Count; i++)
                    if (string.Equals(co[i], key, StringComparison.OrdinalIgnoreCase)) return i;
                return co.Count + CategoryRank(key);
            }
            return CategoryRank(key);
        }

        var checks = all
            .GroupBy(r => r.CheckName)
            .Select(g =>
            {
                var latestRun = g.Max(x => x.StartedAtUtc);
                var rows = g.Where(x => x.StartedAtUtc == latestRun)
                            .OrderByDescending(x => CheckRunner.Rank(x.Status))
                            .ThenBy(x => x.BreakdownKey, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                return new CheckView(
                    Name: g.Key,
                    Domain: string.IsNullOrWhiteSpace(rows[0].Domain) ? "unknown" : rows[0].Domain,
                    Family: Family(g.Key),
                    Category: rows[0].Category,
                    Severity: rows[0].Severity,
                    Description: rows[0].Description,
                    Order: rows[0].Order,
                    RunId: rows[0].RunId,
                    Status: CheckRunner.Rollup(rows.Select(x => x.Status)),
                    When: latestRun,
                    Rows: rows)
                {
                    Title = L.Title(g.Key),
                    Stale = false,
                };
            })
            .ToList();

        // A check whose latest result isn't from its DOMAIN's newest run is a leftover (renamed/removed).
        // Per-domain by design: in the federated model each domain runs on its own schedule with its own
        // RunId, so a single global "latest run" would wrongly flag every other domain as stale. (Per-check
        // start times also differ by ms within one run, so compare by RunId, not timestamp.)
        var latestRunByDomain = checks
            .GroupBy(c => c.Domain, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.MaxBy(c => c.When)!.RunId, StringComparer.OrdinalIgnoreCase);

        checks = checks
            .Select(c => c with { Stale = latestRunByDomain.TryGetValue(c.Domain, out var newest) && c.RunId != newest })
            .ToList();

        var domains = checks
            .GroupBy(c => c.Domain, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new DomainView(g.Key, g.Count()))
            .ToList();

        // Top axis is the Category field (Freshness, Reconciliation, …); within it, group by name-Family.
        var categories = checks
            .GroupBy(c => string.IsNullOrWhiteSpace(c.Category) ? "other" : c.Category.Trim().ToLowerInvariant())
            .OrderBy(g => CatRank(g.Key)).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(cat => new CategoryView(
                Key: cat.Key,
                Slug: Slug(cat.Key),
                Label: L.CategoryName(cat.Key),
                Blurb: CategoryBlurb(cat.Key),
                Checks: Ordered(cat).ToList(),
                Families: cat.GroupBy(c => c.Family)
                             .Select(f => new FamilyView(f.Key, Slug(f.Key), L.FamilyTail(cat.Key, f.Key), Ordered(f).ToList()))
                             .OrderBy(f => f.Checks.Min(c => c.Order ?? int.MaxValue))
                             .ThenBy(f => f.Tail ?? "~", StringComparer.OrdinalIgnoreCase)
                             .ToList()))
            .ToList();

        return new CheckModel { Checks = checks, Categories = categories, Domains = domains, Labels = L };
    }

    /// <summary>
    /// Display order within a category or family: the consumer's <c>order</c> hint, then name. Deterministic
    /// on purpose — the alternative, the order rows happen to arrive in, changes with file enumeration.
    /// </summary>
    private static IEnumerable<CheckView> Ordered(IEnumerable<CheckView> checks) =>
        checks.OrderBy(c => c.Order ?? int.MaxValue).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Count chips for a set of checks: worst first, zeroes omitted. Skipped is counted — leaving it out
    /// rendered a category whose only check is skipped with no chips at all, which reads as "nothing here"
    /// rather than "one check, deliberately not evaluated".
    /// </summary>
    public static string Chips(IEnumerable<CheckView> checks)
    {
        var list = checks as ICollection<CheckView> ?? checks.ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var s in PageChrome.ChipOrder)
        {
            var n = list.Count(c => c.Status == s);
            if (n > 0) sb.Append($"<span class=\"rounded-selector {PageChrome.Tone(s).Solid} px-1.5 text-[9px] leading-4 font-bold tabular-nums\">{n}</span>");
        }
        return sb.ToString();
    }

    // ---- category axis -----------------------------------------------------

    private static readonly string[] CatOrder = ["freshness", "reconciliation", "quality", "volume", "flow"];
    private static int CategoryRank(string key) { var i = Array.IndexOf(CatOrder, key); return i < 0 ? 99 : i; }

    private static readonly Dictionary<string, string> Blurbs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["freshness"] = "Is the data recent? Source-delivery times plus the newest dates inside each feed and the snapshot publish.",
        ["reconciliation"] = "Do the replicas agree? Source-vs-loaded gap, and an independent recomputation vs the production store’s counts.",
        ["quality"] = "Integrity rules: required fields present, no file-sync conflict copies left behind.",
        ["volume"] = "Row-count floors for tables with no date column — catches an empty or half-loaded table.",
        ["flow"] = "End-to-end pipeline flow checks.",
        ["other"] = "Uncategorised checks.",
    };

    public static string CategoryBlurb(string key) => Blurbs.TryGetValue(key, out var b) ? b : "Checks in this category.";

    public static string Slug(string s)
    {
        var slug = new string(s.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }

    private static string Family(string name)
    {
        var i = name.LastIndexOf('.');
        return i <= 0 ? "" : name[..i];
    }
}

/// <summary>
/// Turns raw check / family / breakdown tokens into display labels. Defaults to Rastgo's own structural
/// vocabulary (categories, families) plus generic title-casing; a consumer extends it via
/// <see cref="DashboardOptions"/> (domain token labels, acronyms, breakdown-key formatting) so
/// domain-specific names stay in the domain pack rather than the framework.
/// </summary>
internal sealed class Labeler
{
    private readonly Dictionary<string, string> _tokens;
    private readonly IReadOnlySet<string> _acronyms;
    private readonly Func<string, string> _breakdown;

    public Labeler(DashboardOptions options)
    {
        _tokens = new Dictionary<string, string>(FrameworkTokens, StringComparer.OrdinalIgnoreCase);
        if (options.TokenLabels is not null)
            foreach (var (k, v) in options.TokenLabels) _tokens[k] = v;   // consumer overrides win
        _acronyms = options.Acronyms ?? EmptyAcronyms;
        _breakdown = options.BreakdownKeyFormatter ?? (k => k);
    }

    public string CategoryName(string key) => PrettyToken(key);

    /// <summary>Family label relative to its category: drops a leading token that just echoes the
    /// category (so "freshness.source" → "Source"), and null when the family IS the category.</summary>
    public string? FamilyTail(string categoryKey, string family)
    {
        var tokens = family.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (tokens.Count > 0 && string.Equals(PrettyToken(tokens[0]), CategoryName(categoryKey), StringComparison.OrdinalIgnoreCase))
            tokens.RemoveAt(0);
        return tokens.Count == 0 ? null : string.Join(" › ", tokens.Select(PrettyToken));
    }

    public string Title(string name)
    {
        var i = name.LastIndexOf('.');
        return PrettyToken(i < 0 ? name : name[(i + 1)..]);
    }

    public string BreakdownLabel(string key) => _breakdown(key);

    private string PrettyToken(string t)
    {
        if (_tokens.TryGetValue(t, out var v)) return v;
        if (_acronyms.Contains(t)) return t.ToUpperInvariant();
        var s = t.Replace('_', ' ').Trim();
        return s.Length == 0 ? s : char.ToUpper(s[0]) + s[1..];
    }

    private static readonly IReadOnlySet<string> EmptyAcronyms = new HashSet<string>();

    // Rastgo's own structural vocabulary (categories + generic family/step tokens). Domain check names
    // (e.g. a per-table freshness check) are supplied by the consumer via DashboardOptions.TokenLabels.
    private static readonly Dictionary<string, string> FrameworkTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        ["freshness"] = "Freshness", ["quality"] = "Quality", ["reconciliation"] = "Reconciliation", ["recon"] = "Reconciliation",
        ["volume"] = "Volume", ["flow"] = "Flow", ["other"] = "Other", ["gap"] = "Gap", ["source"] = "Source", ["source_data"] = "Source data",
        ["snapshot_published"] = "Snapshot published", ["load"] = "Load",
        ["conflict_copies"] = "File-sync conflict copies", ["duck_vs_cosmos"] = "DuckDB vs Cosmos",
    };
}
