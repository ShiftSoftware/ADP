using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// Renders the latest run per check into a single self-contained HTML page — no external assets, so it
/// works opened straight off a file share. Layout is "big picture → drill down": a fixed left sidebar
/// gives an at-a-glance health summary plus jump-navigation (Category → Family), while the detail table
/// on the right keeps EVERY row in the DOM, expanded by default. Drill-down is navigation, not
/// view-swapping, so native Ctrl+F still finds everything on load. JS only adds opt-in filtering,
/// scroll-spy, and collapsible groups.
/// </summary>
public static partial class DashboardRenderer
{
    public static string Render(IReadOnlyList<CheckResult> all, DateTimeOffset generatedAtUtc, DashboardOptions? options = null)
    {
        var opt = options ?? DashboardOptions.Default;
        var L = new Labeler(opt);

        // Category display order: the consumer's CategoryOrder wins; categories it omits fall after, by the
        // framework's built-in order then name.
        int CatRank(string key)
        {
            if (opt.CategoryOrder is { } co)
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
                    Rows: rows);
            })
            .ToList();

        int Count(HealthStatus s) => checks.Count(c => c.Status == s);

        // A check whose latest result isn't from its DOMAIN's newest run is a leftover (renamed/removed).
        // Per-domain by design: in the federated model each domain runs on its own schedule with its own
        // RunId, so a single global "latest run" would wrongly flag every other domain as stale. (Per-check
        // start times also differ by ms within one run, so compare by RunId, not timestamp.)
        var latestRunByDomain = checks
            .GroupBy(c => c.Domain)
            .ToDictionary(g => g.Key, g => g.MaxBy(c => c.When)!.RunId, StringComparer.OrdinalIgnoreCase);

        // Domains present (federated packs). Shown as filter chips + a per-row badge only when more than one.
        var domains = checks.GroupBy(c => c.Domain).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase).ToList();
        var multiDomain = domains.Count > 1;

        // Top axis is the Category field (Freshness, Reconciliation, …); within it, group by name-Family.
        var categories = checks
            .GroupBy(c => string.IsNullOrWhiteSpace(c.Category) ? "other" : c.Category.Trim().ToLowerInvariant())
            .OrderBy(g => CatRank(g.Key)).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sb = new StringBuilder();
        sb.Append(HeadAndStyle);
        sb.Append("<div class=\"app\">");

        // ---- sidebar: health summary + jump nav -------------------------------
        sb.Append("<aside class=\"side\">");
        sb.Append("<div class=\"brand\"><div class=\"logo\">").Append(Logo).Append("</div>")
          .Append($"<div class=\"meta\">{generatedAtUtc:yyyy-MM-dd HH:mm} UTC · {checks.Count} checks</div></div>");

        sb.Append("<div class=\"kpis\">");
        foreach (var (status, label) in new[] { (HealthStatus.Fail, "Fail"), (HealthStatus.Error, "Error"), (HealthStatus.Warn, "Warn"), (HealthStatus.Pass, "Pass") })
            sb.Append($"<button class=\"kpi {status}\" data-filter=\"{status}\"><span class=\"n\">{Count(status)}</span><span class=\"l\">{label}</span></button>");
        sb.Append("</div>");

        if (multiDomain)
        {
            sb.Append("<div class=\"doms\"><span class=\"domlbl\">Domains</span>");
            foreach (var d in domains)
                sb.Append($"<button class=\"dom\" data-domain=\"{Esc(d.Key)}\">{Esc(d.Key)} <b>{d.Count()}</b></button>");
            sb.Append("</div>");
        }

        sb.Append("<nav class=\"toc\">");
        foreach (var cat in categories)
        {
            var key = cat.Key;
            var slug = Slug(key);
            sb.Append($"<div class=\"toccat\" data-cat=\"{slug}\"><div class=\"tcatrow\">")
              .Append("<button class=\"tfold\" type=\"button\" aria-label=\"Fold section\">▾</button>")
              .Append($"<a class=\"tcat\" data-cat=\"{slug}\" href=\"#cat-{slug}\" data-tip=\"{Esc(CategoryBlurb(key))}\"><span class=\"tlabel\">{Esc(L.CategoryName(key))} {InfoCue()}</span>{RollupCounts(cat.ToList())}</a></div>");

            sb.Append("<div class=\"tfams\">");
            foreach (var fam in cat.GroupBy(c => c.Family).OrderBy(f => f.Min(c => c.Order ?? int.MaxValue)).ThenBy(f => L.FamilyTail(key, f.Key) ?? "~", StringComparer.OrdinalIgnoreCase))
            {
                var tail = L.FamilyTail(key, fam.Key);
                if (tail is null) continue;   // family == category (e.g. "volume"): covered by the category line
                var famSlug = Slug(fam.Key);
                sb.Append($"<a class=\"tfam\" data-target=\"fam-{famSlug}\" href=\"#fam-{famSlug}\"><span class=\"tlabel\">{Esc(tail)}</span>{RollupCounts(fam.ToList())}</a>");
            }
            sb.Append("</div></div>");
        }
        sb.Append("</nav></aside>");

        // ---- main: toolbar + detail table -------------------------------------
        sb.Append("<main class=\"main\">");
        sb.Append("<section class=\"toolbar\">")
          .Append("<input id=\"q\" type=\"search\" placeholder=\"Filter checks…  (native Ctrl+F still works)\" autocomplete=\"off\">")
          .Append("<button id=\"expand\" class=\"btn\">Expand all</button>")
          .Append("<button id=\"collapse\" class=\"btn\">Collapse all</button>")
          .Append("<span class=\"hint\">Click a card to filter by status · click a section in the sidebar to jump</span>")
          .Append("</section>");

        sb.Append("<table><thead><tr><th>Status</th><th>Check</th><th>Detail</th></tr></thead>");
        foreach (var cat in categories)
        {
            var key = cat.Key;
            var slug = Slug(key);
            sb.Append($"<tbody class=\"cat\" data-cat=\"{slug}\"><tr class=\"cathdr\" id=\"cat-{slug}\"><td colspan=\"3\" data-tip=\"{Esc(CategoryBlurb(key))}\"><span class=\"ccaret\">▾</span><span class=\"clabel\">{Esc(L.CategoryName(key))} {InfoCue()}</span>{RollupCounts(cat.ToList())}</td></tr></tbody>");

            foreach (var fam in cat.GroupBy(c => c.Family).OrderBy(f => f.Min(c => c.Order ?? int.MaxValue)).ThenBy(f => L.FamilyTail(key, f.Key) ?? "~", StringComparer.OrdinalIgnoreCase))
            {
                var tail = L.FamilyTail(key, fam.Key);
                var famSlug = Slug(fam.Key);
                var famChecks = fam.OrderBy(c => c.Order ?? int.MaxValue).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();

                sb.Append($"<tbody class=\"group{(tail is null ? " nohdr" : "")}\" data-cat=\"{slug}\">");
                if (tail is not null)
                    sb.Append($"<tr class=\"grp\" id=\"fam-{famSlug}\"><td colspan=\"3\"><span class=\"caret\">▾</span> {Esc(tail)} {RollupCounts(famChecks)}</td></tr>");
                foreach (var c in famChecks)
                    AppendCheckRow(sb, c, latestRunByDomain.GetValueOrDefault(c.Domain, ""), multiDomain, L);
                sb.Append("</tbody>");
            }
        }

        sb.Append("</table></main></div>");
        sb.Append(Script);
        return sb.ToString();
    }

    private static void AppendCheckRow(StringBuilder sb, CheckView c, string domainLatestRunId, bool showDomain, Labeler L)
    {
        var title = L.Title(c.Name);
        var isGrouped = c.Rows.Count > 1 || c.Rows[0].BreakdownKey is not null;
        var stale = !string.IsNullOrEmpty(domainLatestRunId) && c.RunId != domainLatestRunId;

        var searchText = Esc((string.Join(' ',
            new[] { c.Name, title, c.Category, c.Severity, c.Domain, c.Description ?? "" }
            .Concat(c.Rows.Select(r => $"{L.BreakdownLabel(r.BreakdownKey ?? "")} {r.Message}")))).ToLowerInvariant());

        sb.Append($"<tr class=\"check\" data-status=\"{c.Status}\" data-domain=\"{Esc(c.Domain)}\" data-text=\"{searchText}\">");
        sb.Append($"<td><span class=\"pill {c.Status}\">{c.Status}</span></td>");

        sb.Append("<td><div class=\"title\">").Append(Esc(title)).Append(' ').Append(Info(c.Description)).Append("</div>")
          .Append($"<div class=\"sub\"><code>{Esc(c.Name)}</code>");
        if (showDomain)
            sb.Append($" <span class=\"tag dom\">{Esc(c.Domain)}</span>");
        sb.Append($" <span class=\"tag sev-{Esc(c.Severity)}\">{Esc(c.Severity)}</span>");
        if (stale)
            sb.Append($"<span class=\"tag stale\" tabindex=\"0\" data-tip=\"From an earlier run ({c.When:yyyy-MM-dd HH:mm} UTC) — not in the latest run, so likely renamed or removed.\">stale</span>");
        sb.Append("</div></td>");

        sb.Append("<td class=\"detail\">");
        if (!isGrouped)
        {
            sb.Append(Esc(c.Rows[0].Message));
        }
        else
        {
            const int cap = 60;
            // The threshold ("max 2d") belongs to the whole check, yet every breached breakdown row repeats it.
            // When the rows agree on it, lift it to a single group-level chip and drop it from each row's message.
            var rule = SharedRule(c.Rows);
            if (rule is not null)
                sb.Append($"<div class=\"bkrule\">{Esc(rule)}</div>");
            foreach (var r in c.Rows.Take(cap))
            {
                var msg = rule is null ? r.Message : StripRule(r.Message);
                sb.Append($"<div class=\"bk\"><span class=\"dot {r.Status}\"></span><b>{Esc(L.BreakdownLabel(r.BreakdownKey ?? "—"))}</b> <span class=\"bkmsg\">{Esc(msg)}</span></div>");
            }
            if (c.Rows.Count > cap)
                sb.Append($"<div class=\"bk muted\">+{c.Rows.Count - cap} more…</div>");
        }
        sb.Append("</td></tr>");
    }

    private static string Info(string? tip) => string.IsNullOrWhiteSpace(tip) ? "" :
        $"<span class=\"info\" tabindex=\"0\" role=\"button\" aria-label=\"What this checks\" data-tip=\"{Esc(tip)}\">i</span>";

    private static string InfoCue() => "<span class=\"info\" aria-hidden=\"true\">i</span>";

    private static string RollupCounts(List<CheckView> checks)
    {
        var sb = new StringBuilder("<span class=\"counts\">");
        foreach (var s in new[] { HealthStatus.Fail, HealthStatus.Error, HealthStatus.Warn, HealthStatus.Pass })
        {
            var n = checks.Count(c => c.Status == s);
            if (n > 0) sb.Append($"<span class=\"cnt {s}\">{n}</span>");
        }
        return sb.Append("</span>").ToString();
    }

    private static string Esc(string? s) => WebUtility.HtmlEncode(s ?? "");

    // A check's threshold parenthetical — "(max 2d)", "(warn 6h)", "(min 100)" — is a property of the whole
    // check, not of each breakdown row, so a grouped check repeats it on every line. These two lift it to a
    // single group-level note: matched only when it's a real rule keyword (so a diff's "(Δ 3)" value, which
    // legitimately varies per row, is never mistaken for one).
    [GeneratedRegex(@"\s*\((?:max|min|warn)\s+[^()]*\)(?=\.?\s*$)", RegexOptions.IgnoreCase)]
    private static partial Regex RuleSuffixRegex();

    /// <summary>The shared rule chip for a grouped check (e.g. "max 2d"), or null when the rows carry none or
    /// disagree (a mix of "(max 2d)" and "(warn 1d)") — in which case messages are left verbatim.</summary>
    private static string? SharedRule(IReadOnlyList<CheckResult> rows)
    {
        string? shared = null;
        foreach (var r in rows)
        {
            var m = RuleSuffixRegex().Match(r.Message ?? "");
            if (!m.Success) continue;
            var rule = m.Value.Trim().Trim('(', ')');   // "(max 2d)" -> "max 2d"
            if (shared is null) shared = rule;
            else if (!string.Equals(shared, rule, StringComparison.Ordinal)) return null;
        }
        return shared;
    }

    /// <summary>Drops the rule suffix from a row message, keeping it terminated: "Stale: 4.5d old (max 2d)." → "Stale: 4.5d old.".</summary>
    private static string StripRule(string? msg)
    {
        var s = RuleSuffixRegex().Replace(msg ?? "", "").TrimEnd();
        if (s.Length > 0 && s[^1] is not ('.' or '!' or '?')) s += ".";
        return s;
    }

    // ---- category axis -----------------------------------------------------

    private static readonly string[] CatOrder = { "freshness", "reconciliation", "quality", "volume", "flow" };
    private static int CategoryRank(string key) { var i = Array.IndexOf(CatOrder, key); return i < 0 ? 99 : i; }

    private static readonly Dictionary<string, string> Blurbs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["freshness"] = "Is the data recent? Source-delivery times plus the newest dates inside each feed and the snapshot publish.",
        ["reconciliation"] = "Do the replicas agree? Source-vs-loaded gap, and an independent recomputation vs the production store's counts.",
        ["quality"] = "Integrity rules: required fields present, no file-sync conflict copies left behind.",
        ["volume"] = "Row-count floors for tables with no date column — catches an empty or half-loaded table.",
        ["flow"] = "End-to-end pipeline flow checks.",
        ["other"] = "Uncategorised checks.",
    };
    private static string CategoryBlurb(string key) => Blurbs.TryGetValue(key, out var b) ? b : "Checks in this category.";

    private static string Slug(string s)
    {
        var slug = new string(s.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }

    // ---- friendly names ----------------------------------------------------

    private static string Family(string name)
    {
        var i = name.LastIndexOf('.');
        return i <= 0 ? "" : name[..i];
    }

    /// <summary>
    /// Turns raw check / family / breakdown tokens into display labels. Defaults to Rastgo's own structural
    /// vocabulary (categories, families) plus generic title-casing; a consumer extends it via
    /// <see cref="DashboardOptions"/> (domain token labels, acronyms, breakdown-key formatting) so
    /// domain-specific names stay in the domain pack rather than the framework.
    /// </summary>
    private sealed class Labeler
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
            ["volume"] = "Volume", ["flow"] = "Flow", ["gap"] = "Gap", ["source"] = "Source", ["source_data"] = "Source data",
            ["snapshot_published"] = "Snapshot published", ["load"] = "Load",
            ["conflict_copies"] = "File-sync conflict copies", ["duck_vs_cosmos"] = "DuckDB vs Cosmos",
        };
    }

    private sealed record CheckView(
        string Name, string Domain, string Family, string Category, string Severity, string? Description, int? Order,
        string RunId, HealthStatus Status, DateTimeOffset When, List<CheckResult> Rows);

    // ---- static assets -----------------------------------------------------

    // Rastgo horizontal lockup (on-dark variant: white letters, yellow star + "A" accent). Inlined because the
    // page ships no external assets; the viewBox is tightened around the artwork for the sidebar header.
    private const string Logo = """
    <svg viewBox="48 24 412 128" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Rastgo">
    <g transform="translate(64,40) scale(0.8)"><path fill-rule="evenodd" fill="#F2C230" d="M60,0 75,45 120,60 75,75 60,120 45,75 0,60 45,45 Z M60,28 68,52 92,60 68,68 60,92 52,68 28,60 52,52 Z"/></g>
    <text x="184" y="109" textLength="260" lengthAdjust="spacing" font-family="'Sora','Exo 2','Rajdhani','Segoe UI',sans-serif" font-size="58" font-weight="600"><tspan fill="#ffffff">R</tspan><tspan fill="#F2C230">A</tspan><tspan fill="#ffffff">STGO</tspan></text>
    </svg>
    """;

    private const string HeadAndStyle = """
    <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Rastgo</title>
    <style>
    :root{--bg:#f6f8fa;--fg:#1c2128;--muted:#656d76;--line:#d0d7de;--pass:#1a7f37;--warn:#9a6700;--fail:#cf222e;--error:#6e7781;--card:#fff;
          --side:#0d1117;--side-fg:#cdd5df;--side-muted:#8b949e;--side-line:#21262d;--side-hov:#161b22;--accent:#2f81f7;--sideW:290px;--barH:57px}
    *{box-sizing:border-box}
    html{scroll-behavior:smooth}
    body{margin:0;font-family:system-ui,'Segoe UI',Arial,sans-serif;color:var(--fg);background:var(--bg)}

    /* layout */
    .side{position:fixed;top:0;left:0;bottom:0;width:var(--sideW);background:var(--side);color:var(--side-fg);overflow:auto;border-right:1px solid #000}
    .main{margin-left:var(--sideW)}
    @media(max-width:880px){.side{position:static;width:auto;height:auto;bottom:auto}.main{margin-left:0}}

    /* sidebar header + KPIs */
    .brand{padding:15px 18px 8px}
    .brand .logo svg{display:block;height:40px;width:auto}
    .brand .meta{font-size:11px;color:var(--side-muted);margin-top:3px;line-height:1.4}
    .kpis{display:flex;gap:6px;padding:6px 14px 12px}
    .kpi{flex:1;cursor:pointer;border:1px solid var(--side-line);background:#161b22;border-radius:8px;padding:8px 4px;color:var(--side-fg);font:inherit}
    .kpi .n{display:block;font-size:18px;font-weight:700;line-height:1}
    .kpi .l{display:block;font-size:9px;letter-spacing:.04em;text-transform:uppercase;color:var(--side-muted);margin-top:3px}
    .kpi.Fail .n{color:#ff7b72}.kpi.Warn .n{color:#e3b341}.kpi.Pass .n{color:#3fb950}.kpi.Error .n{color:#9aa4af}
    .kpi:hover{border-color:#3b434d}
    .kpi.active{outline:2px solid var(--accent);outline-offset:1px}
    .doms{display:flex;flex-wrap:wrap;gap:5px;align-items:center;padding:0 14px 12px}
    .domlbl{width:100%;font-size:9px;letter-spacing:.04em;text-transform:uppercase;color:var(--side-muted)}
    .dom{cursor:pointer;border:1px solid var(--side-line);background:#161b22;border-radius:7px;padding:4px 9px;color:var(--side-fg);font:inherit;font-size:11px}
    .dom b{color:#fff}
    .dom:hover{border-color:#3b434d}
    .dom.active{outline:2px solid var(--accent);outline-offset:1px}

    /* sidebar nav */
    .toc{padding:4px 8px 28px}
    .toccat{margin-bottom:1px}
    .tcatrow{display:flex;align-items:center}
    .tfold{cursor:pointer;background:none;border:0;color:var(--side-muted);width:20px;height:28px;font-size:10px;padding:0;flex:0 0 20px}
    .tfold:hover{color:#fff}
    .toccat.folded .tfold{transform:rotate(-90deg)}
    .toccat.folded .tfams{display:none}
    .tcat{flex:1;min-width:0;display:flex;align-items:center;gap:8px;text-decoration:none;color:var(--side-fg);font-weight:600;font-size:13px;padding:5px 8px;border-radius:6px}
    .tcat:hover{background:var(--side-hov)}
    .tcat.active{background:#1f2733;color:#fff}
    .tfams{margin:1px 0 7px 20px;display:flex;flex-direction:column;gap:1px}
    .tfam{display:flex;align-items:center;gap:8px;text-decoration:none;color:var(--side-muted);font-size:12px;padding:4px 8px;border-radius:6px;border-left:1px solid var(--side-line)}
    .tfam:hover{background:var(--side-hov);color:var(--side-fg)}
    .tfam.active{color:#fff;border-left-color:var(--accent);background:var(--side-hov)}
    .tlabel{flex:1;min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
    .dim{opacity:.3}

    /* count chips */
    .counts{display:inline-flex;gap:4px;flex:0 0 auto}
    .cnt{min-width:18px;text-align:center;border-radius:9px;padding:0 6px;color:#fff;font-size:10px;font-weight:700;line-height:17px}
    .cnt.Fail{background:var(--fail)}.cnt.Warn{background:var(--warn)}.cnt.Pass{background:var(--pass)}.cnt.Error{background:var(--error)}

    /* main toolbar */
    .toolbar{position:sticky;top:0;z-index:6;display:flex;align-items:center;gap:10px;padding:10px 22px;
             background:rgba(246,248,250,.93);backdrop-filter:blur(6px);border-bottom:1px solid var(--line)}
    #q{flex:1;max-width:460px;padding:8px 12px;border:1px solid var(--line);border-radius:8px;font:inherit;background:var(--card)}
    .btn{cursor:pointer;border:1px solid var(--line);background:var(--card);border-radius:8px;padding:7px 12px;font:inherit;color:var(--fg)}
    .btn:hover{background:#eef1f4}
    .hint{color:var(--muted);font-size:12px;margin-left:auto}
    @media(max-width:880px){.hint{display:none}}

    /* table */
    table{border-collapse:collapse;width:100%;font-size:13px;background:var(--card)}
    thead th{position:sticky;top:var(--barH);background:#eef1f4;text-align:left;padding:7px 22px;color:var(--muted);font-weight:600;border-bottom:1px solid var(--line);z-index:4}
    td{padding:9px 22px;border-bottom:1px solid #eef1f4;vertical-align:top}

    .cathdr{scroll-margin-top:96px;cursor:pointer}
    .cathdr td{background:var(--side);color:#fff;font-weight:700;padding:9px 22px;font-size:12px;text-transform:uppercase;letter-spacing:.05em}
    .cathdr .counts{float:right}
    .clabel{vertical-align:middle}
    .ccaret{display:inline-block;width:14px;color:#fff;opacity:.75;transition:transform .1s}
    .cat.catcollapsed .ccaret{transform:rotate(-90deg)}
    tbody.group.gcollapsed{display:none}

    .grp{cursor:pointer;scroll-margin-top:96px}
    .grp td{background:#f0f3f6;font-weight:600;border-bottom:1px solid var(--line)}
    .caret{display:inline-block;width:14px;color:var(--muted);transition:transform .1s}
    .group.collapsed .caret{transform:rotate(-90deg)}
    .group.collapsed .check{display:none}
    .grp .counts{float:right}

    .pill{display:inline-block;padding:2px 9px;border-radius:999px;font-size:11px;font-weight:700;color:#fff}
    .pill.Pass{background:var(--pass)}.pill.Warn{background:var(--warn)}.pill.Fail{background:var(--fail)}.pill.Error{background:var(--error)}.pill.Skipped{background:#8c959f}
    .title{font-weight:600}
    .sub{margin-top:2px;color:var(--muted);font-size:12px;display:flex;gap:6px;align-items:center;flex-wrap:wrap}
    .sub code{background:#eef1f4;padding:1px 5px;border-radius:4px;color:#3b434d}
    .tag{border:1px solid var(--line);border-radius:6px;padding:0 6px;font-size:11px}
    .tag.sev-critical{border-color:var(--fail);color:var(--fail)}
    .tag.sev-warning{border-color:var(--warn);color:var(--warn)}
    .tag.stale{border-color:var(--warn);color:var(--warn);cursor:help}
    .tag.dom{border-color:var(--accent);color:var(--accent)}
    .info{display:inline-flex;align-items:center;justify-content:center;width:14px;height:14px;border-radius:50%;border:1px solid currentColor;font-size:9px;font-weight:700;font-style:normal;line-height:1;cursor:help;opacity:.5;vertical-align:middle;flex:0 0 auto}
    .info:hover,.info:focus,[data-tip]:hover .info,[data-tip]:focus .info{opacity:1;outline:none}
    .detail{max-width:640px}
    .bk{padding:1px 0}
    .bk .dot{display:inline-block;width:8px;height:8px;border-radius:50%;margin-right:7px;vertical-align:middle}
    .dot.Pass{background:var(--pass)}.dot.Warn{background:var(--warn)}.dot.Fail{background:var(--fail)}.dot.Error{background:var(--error)}.dot.Skipped{background:#8c959f}
    .bkmsg{color:var(--muted)}
    .bkrule{display:inline-block;margin:0 0 4px;padding:1px 7px;font-size:11px;line-height:16px;color:var(--muted);background:#eef1f4;border-radius:6px}
    .muted{color:var(--muted)}
    .hide{display:none}
    #tip{position:fixed;z-index:50;max-width:340px;background:#0d1117;color:#e6edf3;border:1px solid #30363d;border-radius:8px;padding:8px 11px;font-size:12px;line-height:1.45;box-shadow:0 6px 22px rgba(0,0,0,.3);pointer-events:none;opacity:0;transition:opacity .1s}
    #tip.show{opacity:1}
    </style></head><body>
    """;

    private const string Script = """
    <script>
    (function(){
      const $$=s=>[...document.querySelectorAll(s)];
      const q=document.getElementById('q');
      const kpis=$$('.kpi[data-filter]');
      const doms=$$('.dom[data-domain]');
      let sf=null,df=null;

      function apply(){
        const t=(q.value||'').toLowerCase().trim();
        $$('tr.check').forEach(r=>{
          const okS=!sf||r.dataset.status===sf;
          const okT=!t||(r.dataset.text||'').includes(t);
          const okD=!df||r.dataset.domain===df;
          r.classList.toggle('hide',!(okS&&okT&&okD));
        });
        $$('tbody.group').forEach(g=>{
          const vis=g.querySelector('tr.check:not(.hide)');
          g.classList.toggle('hide',!vis);
          if((sf||t||df)&&vis)g.classList.remove('collapsed','gcollapsed');
        });
        $$('tbody.cat').forEach(c=>{
          const any=$$('tbody.group[data-cat="'+c.dataset.cat+'"]').some(g=>!g.classList.contains('hide'));
          c.classList.toggle('hide',!any);
          if((sf||t||df)&&any)c.classList.remove('catcollapsed');
        });
        $$('.tfam').forEach(a=>{
          const row=document.getElementById(a.dataset.target);
          const g=row&&row.closest('tbody.group');
          a.classList.toggle('dim',!g||g.classList.contains('hide'));
        });
        $$('.tcat').forEach(a=>{
          const c=document.querySelector('tbody.cat[data-cat="'+a.dataset.cat+'"]');
          a.classList.toggle('dim',!c||c.classList.contains('hide'));
        });
      }

      // status filter (sidebar cards)
      kpis.forEach(k=>k.addEventListener('click',()=>{
        sf=sf===k.dataset.filter?null:k.dataset.filter;
        kpis.forEach(x=>x.classList.toggle('active',x.dataset.filter===sf));
        apply();
      }));
      q.addEventListener('input',apply);
      doms.forEach(d=>d.addEventListener('click',()=>{
        df=df===d.dataset.domain?null:d.dataset.domain;
        doms.forEach(x=>x.classList.toggle('active',x.dataset.domain===df));
        apply();
      }));

      // collapse / expand: a family group (its header), or a whole category incl. its loose checks (the category header)
      $$('tr.grp').forEach(h=>h.addEventListener('click',()=>h.parentElement.classList.toggle('collapsed')));
      $$('tr.cathdr').forEach(h=>h.addEventListener('click',()=>{
        const cat=h.closest('tbody.cat'),slug=cat.dataset.cat;
        const collapsed=cat.classList.toggle('catcollapsed');
        $$('tbody.group[data-cat="'+slug+'"]').forEach(g=>g.classList.toggle('gcollapsed',collapsed));
      }));
      const ex=document.getElementById('expand'),co=document.getElementById('collapse');
      ex&&ex.addEventListener('click',()=>{$$('tbody.group').forEach(g=>g.classList.remove('collapsed','gcollapsed'));$$('tbody.cat').forEach(c=>c.classList.remove('catcollapsed'));});
      co&&co.addEventListener('click',()=>{$$('tbody.cat').forEach(c=>c.classList.add('catcollapsed'));$$('tbody.group').forEach(g=>g.classList.add('gcollapsed'));});

      // sidebar: fold a category's family list
      $$('.tfold').forEach(b=>b.addEventListener('click',e=>{e.preventDefault();b.closest('.toccat').classList.toggle('folded');}));
      // sidebar: a family jump also expands its group (so the anchor lands on visible rows)
      $$('.tfam').forEach(a=>a.addEventListener('click',()=>{
        const row=document.getElementById(a.dataset.target);
        const g=row&&row.closest('tbody.group'); if(g)g.classList.remove('collapsed');
      }));

      // scroll-spy: highlight the section nearest the top
      function setActive(row){
        const isCat=row.classList.contains('cathdr');
        const famId=isCat?null:row.id;
        const cat=isCat?row.id.slice(4):(row.closest('tbody.group')||{}).dataset?.cat;
        $$('.tcat').forEach(a=>a.classList.toggle('active',a.dataset.cat===cat));
        $$('.tfam').forEach(a=>a.classList.toggle('active',!!famId&&a.dataset.target===famId));
      }
      const seen=new Map();
      const io=new IntersectionObserver(es=>{
        es.forEach(e=>{ if(e.isIntersecting)seen.set(e.target,e.boundingClientRect.top); else seen.delete(e.target); });
        let best=null,bt=1e9;
        seen.forEach((top,el)=>{ if(top<bt){bt=top;best=el;} });
        if(best)setActive(best);
      },{rootMargin:'-96px 0px -65% 0px',threshold:0});
      $$('tr.cathdr, tr.grp').forEach(r=>io.observe(r));

      // tooltips: one shared fixed-position box, placed in JS so the scrolling sidebar can't clip it
      const tip=document.createElement('div');tip.id='tip';tip.setAttribute('role','tooltip');document.body.appendChild(tip);
      function showTip(el){
        const txt=el.getAttribute('data-tip');if(!txt)return;
        tip.textContent=txt;tip.classList.add('show');
        const r=el.getBoundingClientRect(),tw=tip.offsetWidth,th=tip.offsetHeight;
        const left=Math.min(Math.max(8,r.left+r.width/2-tw/2),innerWidth-tw-8);
        let top=r.bottom+8;if(top+th>innerHeight-8)top=r.top-th-8;
        tip.style.left=left+'px';tip.style.top=Math.max(8,top)+'px';
      }
      const hideTip=()=>tip.classList.remove('show');
      $$('[data-tip]').forEach(el=>{
        el.addEventListener('mouseenter',()=>showTip(el));
        el.addEventListener('mouseleave',hideTip);
        el.addEventListener('focus',()=>showTip(el));
        el.addEventListener('blur',hideTip);
      });
      addEventListener('scroll',hideTip,true);

      // default expanded => native Ctrl+F finds everything on load. Flip for big-picture-first.
      const COLLAPSE_ON_LOAD=false;
      if(COLLAPSE_ON_LOAD)$$('tbody.group').forEach(g=>g.classList.add('collapsed'));
    })();
    </script></body></html>
    """;
}
