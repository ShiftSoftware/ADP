using System.Globalization;
using System.Net;
using System.Text;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// Renders the run <i>history</i> (many runs over a time window) as a self-contained trends page — the
/// time-axis companion to <see cref="DashboardRenderer"/>, which only shows the latest run. Same JSONL
/// source (<see cref="JsonlResultSink.ReadSince"/>); here every retained run becomes a sample so each check
/// can be plotted over time.
/// <para>
/// Everything is bucketed by UTC day, and a day is represented by its <b>last</b> run — so the hero, the
/// status strips, and the metric charts all describe the same "end-of-day state" and stay consistent. Daily
/// bucketing also keeps the page light no matter the run cadence (hourly runs collapse to one point/day).
/// The metric a check contributes is inferred from which keys its <see cref="CheckResult.Metrics"/> carries
/// (<c>ageHours</c> → freshness age, <c>value</c> → threshold count/ratio, <c>diff</c> → reconciliation
/// gap), so no per-check wiring is needed and new checks light up automatically.
/// </para>
/// <para>
/// Layout is HTML/CSS; <b>all text is HTML</b> (real pixels, page font) so type size is identical across the
/// hero, strips and charts at any width. SVG is used only for the line marks, with
/// <c>preserveAspectRatio="none"</c> + <c>vector-effect="non-scaling-stroke"</c> so the plot fills its box
/// responsively without distorting stroke width. No external assets or JS charting lib, same as the dashboard.
/// </para>
/// </summary>
public static class TrendsRenderer
{
    // Distinct line colours for grouped (per-breakdown) series — mid-ramp hexes that read on the light page.
    private static readonly string[] Palette =
        { "#378ADD", "#1D9E75", "#D85A30", "#7F77DD", "#BA7517", "#D4537E", "#639922", "#888780" };

    private const int MaxSeries = 10;   // cap lines on a grouped chart; the rest collapse to a "+N more" note
    private const int HeroHeight = 140; // px
    private const int PlotHeight = 128; // px
    private const int YAxisWidth = 38;  // px — shared by the hero + every chart so baselines line up
    private const int LabelWidth = 240; // px — the strip check-name column

    public static string Render(IReadOnlyList<CheckResult> all, DateTimeOffset nowUtc, TimeSpan window, DashboardOptions? options = null)
    {
        var opt = options ?? DashboardOptions.Default;
        var L = new Labeler(opt);

        // Category display order: the consumer's CategoryOrder wins, then the built-in order, then name.
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

        // ---- day buckets over the window -------------------------------------
        var startDay = (nowUtc - window).UtcDateTime.Date;
        var endDay = nowUtc.UtcDateTime.Date;
        var days = new List<DateTime>();
        for (var d = startDay; d <= endDay; d = d.AddDays(1)) days.Add(d);
        var n = days.Count;
        int DayIndex(DateTimeOffset t) => (t.UtcDateTime.Date - startDay).Days;

        // The representative snapshot for each day = the rows of that day's latest run (by StartedAtUtc).
        var snap = new List<CheckResult>[n];
        for (var i = 0; i < n; i++) snap[i] = [];
        var byDay = new List<CheckResult>[n];
        for (var i = 0; i < n; i++) byDay[i] = [];
        foreach (var r in all)
        {
            var i = DayIndex(r.StartedAtUtc);
            if (i >= 0 && i < n) byDay[i].Add(r);
        }
        for (var i = 0; i < n; i++)
        {
            if (byDay[i].Count == 0) continue;
            var lastRunId = byDay[i].MaxBy(r => r.StartedAtUtc)!.RunId;
            snap[i] = byDay[i].Where(r => r.RunId == lastRunId).ToList();
        }

        // ---- checks present across the window, ordered category → name -------
        var checks = all
            .GroupBy(r => r.CheckName)
            .Select(g =>
            {
                var any = g.First();
                return new CheckMeta(
                    Name: g.Key,
                    Category: string.IsNullOrWhiteSpace(any.Category) ? "other" : any.Category.Trim().ToLowerInvariant(),
                    Severity: any.Severity,
                    Description: any.Description,
                    Order: any.Order,
                    Grouped: g.Any(r => r.BreakdownKey is not null));
            })
            .OrderBy(c => c.Order ?? int.MaxValue).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Latest day that actually has a run → KPIs + the "as of" stamp.
        var latest = -1;
        for (var i = n - 1; i >= 0; i--) if (snap[i].Count > 0) { latest = i; break; }
        var asOf = latest >= 0 ? snap[latest].Max(r => r.StartedAtUtc) : (DateTimeOffset?)null;
        var runDays = snap.Count(s => s.Count > 0);

        // Per-day, per-check rolled-up status (used by the hero + strips). [dayIndex][checkName] -> status.
        var statusOf = new Dictionary<string, HealthStatus>[n];
        for (var i = 0; i < n; i++)
        {
            statusOf[i] = snap[i]
                .GroupBy(r => r.CheckName)
                .ToDictionary(g => g.Key, g => CheckRunner.Rollup(g.Select(x => x.Status)), StringComparer.OrdinalIgnoreCase);
        }

        // ---- x-axis ticks (≈5 evenly spaced day labels) ----------------------
        var ticks = new List<(int idx, string lab)>();
        var tcount = Math.Min(5, n);
        for (var k = 0; k < tcount; k++)
        {
            var idx = tcount == 1 ? 0 : (int)Math.Round(k * (n - 1) / (double)(tcount - 1));
            ticks.Add((idx, days[idx].ToString("MMM d", CultureInfo.InvariantCulture)));
        }

        var sb = new StringBuilder();
        sb.Append(Head);

        // ---- top bar ---------------------------------------------------------
        var asOfLabel = asOf is { } a ? $"{a:yyyy-MM-dd HH:mm} UTC" : "no runs yet";
        sb.Append("<div class=\"bar\"><span class=\"logo\">").Append(Logo).Append("</span><span class=\"sub\">Trends</span>")
          .Append($"<span class=\"meta\">last {(int)Math.Round(window.TotalDays)}d · {runDays} run-days · latest run {asOfLabel}</span>")
          .Append("<a class=\"link\" href=\"dashboard\">Current state →</a></div>");

        sb.Append("<div class=\"wrap\">");

        // ---- KPI row (latest day) -------------------------------------------
        int CountLatest(HealthStatus s) => latest < 0 ? 0 : statusOf[latest].Values.Count(v => v == s);
        sb.Append("<div class=\"kpis\">");
        foreach (var (st, lab) in new[] { (HealthStatus.Fail, "Fail"), (HealthStatus.Error, "Error"), (HealthStatus.Warn, "Warn"), (HealthStatus.Pass, "Pass") })
            sb.Append($"<div class=\"kpi {st}\"><div class=\"n\">{CountLatest(st)}</div><div class=\"l\">{lab}</div></div>");
        sb.Append("</div>");

        // ---- hero: daily health composition ---------------------------------
        sb.Append("<div class=\"card\"><h2>Overall health over time</h2>");
        sb.Append(Hero(snap, statusOf, checks.Count, n, ticks));
        sb.Append("<div class=\"lg\">")
          .Append("<span><i class=\"sw\" style=\"background:var(--pass)\"></i>Pass</span>")
          .Append("<span><i class=\"sw\" style=\"background:var(--warn)\"></i>Warn</span>")
          .Append("<span><i class=\"sw\" style=\"background:var(--fail)\"></i>Fail</span>")
          .Append("<span><i class=\"sw\" style=\"background:var(--error)\"></i>Error</span>")
          .Append("</div></div>");

        // ---- per category: status strips + metric charts --------------------
        foreach (var cat in checks.GroupBy(c => c.Category).OrderBy(g => CatRank(g.Key)).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var list = cat.ToList();
            sb.Append($"<div class=\"sec\"><h2>{Esc(L.Pretty(cat.Key))}</h2>");

            sb.Append("<div class=\"card\">").Append(Strips(list, statusOf, n, ticks, L)).Append("</div>");

            var panels = list.Select(c => MetricPanel(c, snap, n, ticks, L)).Where(p => p is not null).ToList();
            if (panels.Count > 0)
            {
                sb.Append("<div class=\"grid\">");
                foreach (var p in panels) sb.Append(p);
                sb.Append("</div>");
            }
            sb.Append("</div>");
        }

        sb.Append("</div>");   // .wrap
        return sb.ToString();
    }

    // ---------------------------------------------------------------- hero ----
    // Stacked health composition per day, in pure HTML: each day is a flex column whose segments (pass at the
    // bottom, then warn/fail/error) are sized in px against the total check count.
    private static string Hero(List<CheckResult>[] snap, Dictionary<string, HealthStatus>[] statusOf, int totalChecks, int n, List<(int idx, string lab)> ticks)
    {
        var max = Math.Max(1, totalChecks);
        var sb = new StringBuilder("<div class=\"hero\">");
        sb.Append(YAxis(max.ToString(), (max / 2).ToString(), "0"));
        sb.Append($"<div class=\"hbars\" style=\"height:{HeroHeight}px\">");
        for (var i = 0; i < n; i++)
        {
            sb.Append("<div class=\"hbar\">");
            if (snap[i].Count > 0)
            {
                var counts = new Dictionary<HealthStatus, int>();
                foreach (var s in statusOf[i].Values) counts[s] = counts.GetValueOrDefault(s) + 1;
                // document order top→bottom; flex-end packs them at the baseline so Pass ends up lowest
                foreach (var (st, cv) in new[] { (HealthStatus.Error, "--error"), (HealthStatus.Fail, "--fail"), (HealthStatus.Warn, "--warn"), (HealthStatus.Pass, "--pass") })
                {
                    var c = counts.GetValueOrDefault(st);
                    if (c > 0) sb.Append($"<i style=\"height:{F(c / (double)max * HeroHeight)}px;background:var({cv})\"></i>");
                }
            }
            sb.Append("</div>");
        }
        sb.Append("</div></div>");
        return sb.Append(XAxis(ticks, n, YAxisWidth)).ToString();
    }

    // -------------------------------------------------------------- strips ----
    // One HTML row per check: a fixed label column + a flex row of equal day cells. No SVG, no scaling.
    private static string Strips(List<CheckMeta> list, Dictionary<string, HealthStatus>[] statusOf, int n, List<(int idx, string lab)> ticks, Labeler L)
    {
        var sb = new StringBuilder();
        foreach (var c in list)
        {
            sb.Append($"<div class=\"strip\"><div class=\"slabel\">{Esc(L.Title(c.Name))}</div><div class=\"cells\">");
            for (var i = 0; i < n; i++)
            {
                var cls = statusOf[i].TryGetValue(c.Name, out var st) ? StatusClass(st) : "n";
                sb.Append($"<i class=\"cell {cls}\"></i>");
            }
            sb.Append("</div></div>");
        }
        return sb.Append(XAxis(ticks, n, LabelWidth + 8)).ToString();   // +8 = the strip's label→cells gap
    }

    // --------------------------------------------------------- metric chart ---

    private static string? MetricPanel(CheckMeta c, List<CheckResult>[] snap, int n, List<(int idx, string lab)> ticks, Labeler L)
    {
        var rowsPerDay = new List<CheckResult>[n];
        for (var i = 0; i < n; i++)
            rowsPerDay[i] = snap[i].Where(r => string.Equals(r.CheckName, c.Name, StringComparison.OrdinalIgnoreCase)).ToList();

        // Infer the metric kind from the most recent row that carries one.
        var kind = MetricKind.None;
        double? lo = null, hi = null;
        for (var i = n - 1; i >= 0 && kind == MetricKind.None; i--)
            foreach (var r in rowsPerDay[i])
            {
                var m = Extract(r);
                if (m.Kind != MetricKind.None) { kind = m.Kind; lo = m.Lo; hi = m.Hi; break; }
            }
        if (kind == MetricKind.None) return null;   // e.g. a check that only ever errored — strips still cover it

        var series = new List<Series>();
        if (c.Grouped)
        {
            var keys = new List<string>();
            foreach (var rs in rowsPerDay)
                foreach (var r in rs)
                    if (r.BreakdownKey is { } k && !keys.Contains(k)) keys.Add(k);

            double Latest(string k)
            {
                for (var i = n - 1; i >= 0; i--)
                {
                    var r = rowsPerDay[i].FirstOrDefault(x => x.BreakdownKey == k);
                    if (r is not null && Extract(r).Value is { } v) return v;
                }
                return double.NegativeInfinity;
            }
            var shown = keys.OrderByDescending(Latest).Take(MaxSeries).ToList();
            var hidden = keys.Count - shown.Count;

            for (var s = 0; s < shown.Count; s++)
            {
                var key = shown[s];
                var pts = new List<(int day, double v)>();
                for (var i = 0; i < n; i++)
                {
                    var r = rowsPerDay[i].FirstOrDefault(x => x.BreakdownKey == key);
                    if (r is not null && Extract(r).Value is { } v) pts.Add((i, v));
                }
                if (pts.Count > 0) series.Add(new Series(L.Breakdown(key), Palette[s % Palette.Length], pts));
            }
            if (series.Count == 0) return null;
            return ChartPanel(c, series, kind, lo, hi, n, ticks, L, hidden);
        }
        else
        {
            var pts = new List<(int day, double v)>();
            for (var i = 0; i < n; i++)
            {
                var r = rowsPerDay[i].FirstOrDefault();
                if (r is not null && Extract(r).Value is { } v) pts.Add((i, v));
            }
            if (pts.Count == 0) return null;
            series.Add(new Series("", Palette[0], pts));
            return ChartPanel(c, series, kind, lo, hi, n, ticks, L, 0);
        }
    }

    private static string ChartPanel(CheckMeta c, List<Series> series, MetricKind kind, double? lo, double? hi, int n, List<(int idx, string lab)> ticks, Labeler L, int hidden)
    {
        var vals = series.SelectMany(s => s.Points.Select(p => p.v)).ToList();
        var dataMax = vals.Count > 0 ? vals.Max() : 1;
        var dataMin = vals.Count > 0 ? vals.Min() : 0;

        double yMin, yMax;
        var thresholds = new List<(double v, string cv, string lab)>();
        Func<double, string> yfmt;
        switch (kind)
        {
            case MetricKind.Age:
                yMin = 0;
                yMax = Math.Max(dataMax, hi ?? 0) * 1.15 + 0.001;
                if (hi is { } mh) thresholds.Add((mh, "--fail", $"max {FmtHours(mh)}"));
                yfmt = FmtHours;
                break;
            case MetricKind.Diff:
                var mm = Math.Max(Math.Max(Math.Abs(dataMax), Math.Abs(dataMin)), 1) * 1.2;
                yMin = -mm; yMax = mm;
                thresholds.Add((0, "--line-strong", "0"));
                yfmt = Kfmt;
                break;
            default:   // Threshold
                yMin = Math.Min(0, dataMin);
                yMax = Math.Max(dataMax, hi ?? 0) * 1.1 + 0.001;
                if (lo is { } mn && mn > yMin) thresholds.Add((mn, "--warn", $"min {Kfmt(mn)}"));
                if (hi is { } mx) thresholds.Add((mx, "--fail", $"max {Kfmt(mx)}"));
                yfmt = Kfmt;
                break;
        }
        if (yMax <= yMin) yMax = yMin + 1;

        double Frac(double v) => 1 - (v - yMin) / (yMax - yMin);   // 0 = top, 1 = bottom
        double Xpc(int day) => n <= 1 ? 0 : day / (double)(n - 1) * 100;

        var sb = new StringBuilder("<div class=\"pan\"><div class=\"ph\">")
            .Append($"<span class=\"pt\">{Esc(L.Title(c.Name))}</span>")
            .Append($"<span class=\"pn\">{Esc(c.Name)}</span></div>");

        sb.Append("<div class=\"chartrow\">")
          .Append(YAxis(yfmt(yMax), yfmt((yMin + yMax) / 2), yfmt(yMin)))
          .Append($"<div class=\"plot\" role=\"img\" aria-label=\"{Esc(L.Title(c.Name))} over time\">");

        // SVG = marks only; stretched to the box, strokes kept constant.
        sb.Append("<svg viewBox=\"0 0 1000 1000\" preserveAspectRatio=\"none\" aria-hidden=\"true\">");
        sb.Append("<line x1=\"0\" y1=\"500\" x2=\"1000\" y2=\"500\" stroke=\"var(--line)\" stroke-width=\"1\" vector-effect=\"non-scaling-stroke\"/>");
        foreach (var (v, cv, _) in thresholds)
        {
            if (v < yMin || v > yMax) continue;
            var y = F(Frac(v) * 1000);
            var dash = cv == "--line-strong" ? "" : " stroke-dasharray=\"4 3\"";
            sb.Append($"<line x1=\"0\" y1=\"{y}\" x2=\"1000\" y2=\"{y}\" stroke=\"var({cv})\" stroke-width=\"1\"{dash} vector-effect=\"non-scaling-stroke\"/>");
        }
        foreach (var s in series)
        {
            var d = new StringBuilder();
            for (var i = 0; i < s.Points.Count; i++)
                d.Append(i == 0 ? "M" : "L").Append(F(Xpc(s.Points[i].day) * 10)).Append(' ').Append(F(Frac(s.Points[i].v) * 1000)).Append(' ');
            sb.Append($"<path d=\"{d}\" fill=\"none\" stroke=\"{s.Color}\" stroke-width=\"1.6\" vector-effect=\"non-scaling-stroke\" stroke-linejoin=\"round\"/>");
        }
        sb.Append("</svg>");

        // HTML overlays: threshold labels + a dot for any single-point series (otherwise invisible).
        foreach (var (v, cv, lab) in thresholds)
            if (v >= yMin && v <= yMax)
                sb.Append($"<span class=\"thlab\" style=\"top:{F(Frac(v) * 100)}%;color:var({cv})\">{Esc(lab)}</span>");
        foreach (var s in series)
            if (s.Points.Count == 1)
                sb.Append($"<i class=\"dot\" style=\"left:{F(Xpc(s.Points[0].day))}%;top:{F(Frac(s.Points[0].v) * 100)}%;background:{s.Color}\"></i>");

        sb.Append("</div></div>");   // .plot, .chartrow
        sb.Append(XAxis(ticks, n, YAxisWidth));

        if (series.Count > 1 || (series.Count == 1 && series[0].Label.Length > 0))
        {
            sb.Append("<div class=\"lg\">");
            foreach (var s in series)
                sb.Append($"<span><i class=\"sw\" style=\"background:{s.Color}\"></i>{Esc(s.Label)}</span>");
            if (hidden > 0) sb.Append($"<span class=\"more\">+{hidden} more</span>");
            sb.Append("</div>");
        }
        return sb.Append("</div>").ToString();
    }

    // ----------------------------------------------------------- axis HTML ----

    private static string YAxis(params string[] labels)
    {
        var sb = new StringBuilder("<div class=\"yax3\">");
        foreach (var l in labels) sb.Append($"<span>{Esc(l)}</span>");
        return sb.Append("</div>").ToString();
    }

    private static string XAxis(List<(int idx, string lab)> ticks, int n, int spacerPx)
    {
        var sb = new StringBuilder($"<div class=\"xaxis\"><div class=\"yspacer\" style=\"flex:0 0 {spacerPx}px\"></div><div class=\"xline\">");
        foreach (var (idx, lab) in ticks)
        {
            var f = n <= 1 ? 0 : idx / (double)(n - 1) * 100;
            sb.Append($"<span style=\"left:{F(f)}%\">{Esc(lab)}</span>");
        }
        return sb.Append("</div></div>").ToString();
    }

    // ------------------------------------------------------------- metrics ----

    private enum MetricKind { None, Age, Threshold, Diff }

    private readonly record struct Metric(MetricKind Kind, double? Value, double? Lo, double? Hi);

    /// <summary>Infers what to plot from which metric keys the result carries (see class summary).</summary>
    private static Metric Extract(CheckResult r)
    {
        var m = r.Metrics;
        if (m.TryGetValue("ageHours", out var age) && age is not null)
            return new Metric(MetricKind.Age, age, null, m.GetValueOrDefault("maxHours"));
        if (m.TryGetValue("value", out var val))
            return new Metric(MetricKind.Threshold, val, m.GetValueOrDefault("min"), m.GetValueOrDefault("max"));
        if (m.TryGetValue("diff", out var diff))
            return new Metric(MetricKind.Diff, diff, null, null);
        return new Metric(MetricKind.None, null, null, null);
    }

    private sealed record Series(string Label, string Color, List<(int day, double v)> Points);

    private sealed record CheckMeta(string Name, string Category, string Severity, string? Description, int? Order, bool Grouped);

    // --------------------------------------------------------------- utils ----

    private static string StatusClass(HealthStatus s) => s switch
    {
        HealthStatus.Pass => "p",
        HealthStatus.Warn => "w",
        HealthStatus.Fail => "f",
        HealthStatus.Error => "e",
        _ => "n",
    };

    private static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    private static string FmtHours(double h)
        => h >= 24 ? $"{(h / 24).ToString("0.#", CultureInfo.InvariantCulture)}d" : $"{h.ToString("0.#", CultureInfo.InvariantCulture)}h";

    private static string Kfmt(double v)
    {
        var sign = v < 0 ? "-" : "";
        var a = Math.Abs(v);
        if (a >= 1000) return $"{sign}{(a / 1000).ToString("0.#", CultureInfo.InvariantCulture)}k";
        return $"{sign}{a.ToString("0.#", CultureInfo.InvariantCulture)}";
    }

    private static string Esc(string? s) => WebUtility.HtmlEncode(s ?? "");

    private static readonly string[] CatOrder = { "freshness", "reconciliation", "quality", "volume", "flow" };
    private static int CategoryRank(string key) { var i = Array.IndexOf(CatOrder, key); return i < 0 ? 99 : i; }

    /// <summary>Minimal label helper — friendly titles for check/category tokens and the consumer's
    /// breakdown-key formatting. (A trimmed echo of the dashboard's Labeler; kept local so the trends page
    /// doesn't depend on the dashboard's internals.)</summary>
    private sealed class Labeler
    {
        private readonly Dictionary<string, string> _tokens;
        private readonly IReadOnlySet<string> _acronyms;
        private readonly Func<string, string> _breakdown;

        public Labeler(DashboardOptions o)
        {
            _tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["freshness"] = "Freshness", ["reconciliation"] = "Reconciliation", ["recon"] = "Reconciliation",
                ["quality"] = "Quality", ["volume"] = "Volume", ["flow"] = "Flow", ["other"] = "Other",
            };
            if (o.TokenLabels is not null)
                foreach (var (k, v) in o.TokenLabels) _tokens[k] = v;
            _acronyms = o.Acronyms ?? new HashSet<string>();
            _breakdown = o.BreakdownKeyFormatter ?? (k => k);
        }

        public string Pretty(string key) => Token(key);

        /// <summary>Last dotted segment of a check name, prettified ("freshness.source.sales" → "Sales (VSData)").</summary>
        public string Title(string name)
        {
            var i = name.LastIndexOf('.');
            return Token(i < 0 ? name : name[(i + 1)..]);
        }

        public string Breakdown(string key) => _breakdown(key);

        private string Token(string t)
        {
            if (_tokens.TryGetValue(t, out var v)) return v;
            if (_acronyms.Contains(t)) return t.ToUpperInvariant();
            var s = t.Replace('_', ' ').Trim();
            return s.Length == 0 ? s : char.ToUpper(s[0]) + s[1..];
        }
    }

    // Rastgo horizontal lockup (on-dark: white letters, yellow star + "A" accent) — same artwork the
    // dashboard inlines in its sidebar, reused here on the dark top bar. Inlined so the page ships no assets.
    private const string Logo = """
    <svg viewBox="48 24 412 128" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Rastgo">
    <g transform="translate(64,40) scale(0.8)"><path fill-rule="evenodd" fill="#F2C230" d="M60,0 75,45 120,60 75,75 60,120 45,75 0,60 45,45 Z M60,28 68,52 92,60 68,68 60,92 52,68 28,60 52,52 Z"/></g>
    <text x="184" y="109" textLength="260" lengthAdjust="spacing" font-family="'Sora','Exo 2','Rajdhani','Segoe UI',sans-serif" font-size="58" font-weight="600"><tspan fill="#ffffff">R</tspan><tspan fill="#F2C230">A</tspan><tspan fill="#ffffff">STGO</tspan></text>
    </svg>
    """;

    private const string Head = """
    <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Rastgo · Trends</title>
    <style>
    :root{--bg:#f6f8fa;--fg:#1c2128;--muted:#656d76;--line:#d0d7de;--line-strong:#9aa4af;--card:#fff;
          --pass:#1a7f37;--warn:#9a6700;--fail:#cf222e;--error:#6e7781;--none:#e7ebf0;--side:#0d1117}
    *{box-sizing:border-box}
    body{margin:0;font-family:system-ui,'Segoe UI',Arial,sans-serif;color:var(--fg);background:var(--bg);font-size:13px}
    .bar{background:var(--side);color:#fff;padding:10px 20px;display:flex;align-items:center;gap:12px}
    .bar .logo svg{height:22px;width:auto;display:block}
    .bar .sub{font-size:13px;font-weight:600;color:#fff}
    .bar .meta{font-size:12px;color:#8b949e}
    .bar .link{margin-left:auto;color:#58a6ff;text-decoration:none;font-size:12px}
    .wrap{max-width:1100px;margin:0 auto;padding:16px 20px 48px}
    .kpis{display:flex;gap:10px;margin:0 0 16px}
    .kpi{flex:1;background:var(--card);border:1px solid var(--line);border-radius:10px;padding:9px 14px}
    .kpi .n{font-size:22px;font-weight:700;line-height:1}
    .kpi .l{font-size:10px;letter-spacing:.05em;text-transform:uppercase;color:var(--muted);margin-top:3px}
    .kpi.Fail .n{color:var(--fail)}.kpi.Warn .n{color:var(--warn)}.kpi.Pass .n{color:var(--pass)}.kpi.Error .n{color:var(--error)}
    .card{background:var(--card);border:1px solid var(--line);border-radius:12px;padding:14px 16px;margin:0 0 14px}
    .card h2{font-size:13px;margin:0 0 12px;font-weight:600}
    .sec{margin:22px 0 0}
    .sec>h2{font-size:11px;text-transform:uppercase;letter-spacing:.06em;color:var(--muted);margin:0 0 10px;border-bottom:1px solid var(--line);padding-bottom:6px}
    .grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(300px,1fr));gap:14px}
    .pan{background:var(--card);border:1px solid var(--line);border-radius:10px;padding:10px 12px}
    .ph{display:flex;justify-content:space-between;align-items:baseline;gap:8px;margin-bottom:10px}
    .pt{font-size:13px;font-weight:600}
    .pn{font-size:11px;color:var(--muted);font-family:ui-monospace,Consolas,monospace}
    .yax3{display:flex;flex-direction:column;justify-content:space-between;flex:0 0 38px;text-align:right;padding-right:6px;font-size:11px;color:var(--muted);line-height:1}
    .xaxis{display:flex;margin-top:4px}
    .xline{position:relative;flex:1;height:13px}
    .xline span{position:absolute;font-size:11px;color:var(--muted);white-space:nowrap;transform:translateX(-50%)}
    .xline span:first-child{transform:none}
    .xline span:last-child{transform:translateX(-100%)}
    .hero{display:flex}
    .hbars{flex:1;display:flex;gap:2px;align-items:flex-end;border-bottom:1px solid var(--line)}
    .hbar{flex:1;display:flex;flex-direction:column;justify-content:flex-end;min-width:0}
    .hbar i{display:block;width:100%}
    .strip{display:flex;align-items:center;gap:8px;margin:2px 0}
    .slabel{flex:0 0 240px;font-family:ui-monospace,Consolas,monospace;font-size:12px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
    .cells{flex:1;display:flex;gap:2px}
    .cell{flex:1;height:16px;border-radius:2px;min-width:0}
    .cell.p{background:var(--pass)}.cell.w{background:var(--warn)}.cell.f{background:var(--fail)}.cell.e{background:var(--error)}.cell.n{background:var(--none)}
    .chartrow{display:flex;align-items:stretch}
    .plot{position:relative;flex:1;height:128px;border-top:1px solid var(--line);border-bottom:1px solid var(--line)}
    .plot svg{position:absolute;inset:0;width:100%;height:100%;display:block}
    .thlab{position:absolute;right:3px;font-size:11px;transform:translateY(-50%);background:var(--card);padding:0 2px;line-height:1}
    .dot{position:absolute;width:6px;height:6px;border-radius:50%;transform:translate(-50%,-50%)}
    .lg{display:flex;flex-wrap:wrap;gap:9px;font-size:11px;color:var(--muted);margin-top:8px}
    .lg span{display:inline-flex;align-items:center;gap:5px}
    .lg .sw{width:14px;height:3px;border-radius:2px;display:inline-block}
    .lg .more{font-style:italic}
    </style></head><body>
    """;
}
