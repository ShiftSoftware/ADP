using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// Renders the run <i>history</i> (many runs over a time window) as a self-contained trends page — the
/// time-axis companion to <see cref="DashboardRenderer"/>, which only shows the latest run. Same JSONL
/// source (<see cref="JsonlResultSink.ReadSince"/>); here every retained run becomes a sample so each check
/// can be plotted over time. Same shell as the dashboard, with the page switch at the top of the rail, so
/// moving between "what is broken now" and "how did it get that way" costs one click and no re-orientation.
/// <para>
/// A check is ONE row — pill, name, day strip — and its chart slides out underneath it, which is what lets
/// every chart share the page's single day axis: a spike on the 19th lines up vertically across the whole
/// page. Charts are folded on load; the strips already answer "when did it break" for every check in one
/// screen, and the chart answers "by how much", which you ask about one check at a time. Nothing leaves the
/// DOM — folded content here is graphical, and every title, name and status stays visible — so native
/// Ctrl+F still finds everything. "Expand all" or <c>?expand=1</c> restores the full wall.
/// </para>
/// <para>
/// Everything is bucketed by UTC day, and a day is represented by its <b>last run per domain</b> — so the
/// hero, the strips and the charts all describe the same "end-of-day state". Per domain, not globally: in
/// the federated model each pack runs on its own schedule with its own RunId, so a single global "last run
/// of the day" filter keeps one pack and silently discards every other. Daily bucketing also keeps the page
/// light no matter the run cadence (hourly runs collapse to one point per day). The metric a check
/// contributes is inferred from which keys its <see cref="CheckResult.Metrics"/> carries
/// (<c>ageHours</c> → freshness age, <c>value</c> → threshold count/ratio, <c>diff</c> → reconciliation
/// gap), so no per-check wiring is needed and new checks light up automatically.
/// </para>
/// <para>
/// Layout is HTML/CSS; <b>all text is HTML</b> (real pixels, page font) so type size is identical across the
/// hero, strips and charts at any width. SVG is used only for the line marks, with
/// <c>preserveAspectRatio="none"</c> + <c>vector-effect="non-scaling-stroke"</c> so the plot fills its box
/// responsively without distorting stroke width. No external assets and no charting library, same as the
/// dashboard.
/// </para>
/// </summary>
public static class TrendsRenderer
{
    /// <summary>
    /// Lines on one grouped chart; the rest collapse to a "+N more" note. The palette is exactly this long
    /// on purpose — indexing a shorter one with <c>% Length</c> draws two lines in the same colour on a
    /// chart whose entire job is telling lines apart.
    /// </summary>
    private const int MaxSeries = 10;

    /// <summary>
    /// Series colours for grouped charts: custom properties, not hexes, because this design language ships
    /// light and dark and a single set of mid-ramp hexes goes muddy on one of them. Values per theme live
    /// in the stylesheet. Deliberately NOT semantic — a line's colour says which breakdown key it is,
    /// nothing more; status lives in the strips and the threshold lines.
    /// </summary>
    private static readonly string[] Palette =
    [
        "var(--series-1)", "var(--series-2)", "var(--series-3)", "var(--series-4)", "var(--series-5)",
        "var(--series-6)", "var(--series-7)", "var(--series-8)", "var(--series-9)", "var(--series-10)",
    ];

    private const int HeroHeight = 96;    // px — tall enough to read a composition, short enough to keep the strips above the fold
    private const int PlotHeight = 104;   // px

    /// <summary>The grid the ruler, the hero and every check row share, so the columns line up down the page.</summary>
    private const string Cols = "grid-cols-[3.5rem_minmax(0,20rem)_minmax(0,1fr)]";

    public static string Render(IReadOnlyList<CheckResult> all, DateTimeOffset nowUtc, TimeSpan window, DashboardOptions? options = null)
    {
        var opt = options ?? DashboardOptions.Default;
        var model = CheckModel.Build(all, opt);
        var L = model.Labels;

        // ---- day buckets over the window -------------------------------------
        var startDay = (nowUtc - window).UtcDateTime.Date;
        var endDay = nowUtc.UtcDateTime.Date;
        var days = new List<DateTime>();
        for (var d = startDay; d <= endDay; d = d.AddDays(1)) days.Add(d);
        var n = days.Count;
        int DayIndex(DateTimeOffset t) => (t.UtcDateTime.Date - startDay).Days;
        string DayLabel(int i) => days[i].ToString("MMM d", CultureInfo.InvariantCulture);

        var byDay = new List<CheckResult>[n];
        for (var i = 0; i < n; i++) byDay[i] = [];
        foreach (var r in all)
        {
            var i = DayIndex(r.StartedAtUtc);
            if (i >= 0 && i < n) byDay[i].Add(r);
        }

        // The representative snapshot for a day: that day's last run PER DOMAIN. Taking one global "last
        // run of the day" and discarding every row that does not carry its RunId throws away all but one
        // pack on any day more than one of them ran — most of the page, on a federated deployment.
        var snap = new List<CheckResult>[n];
        var ranOn = new HashSet<string>[n];
        for (var i = 0; i < n; i++)
        {
            snap[i] = [];
            ranOn[i] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in byDay[i].GroupBy(DomainOf, StringComparer.OrdinalIgnoreCase))
            {
                var lastRunId = g.MaxBy(r => r.StartedAtUtc)!.RunId;
                snap[i].AddRange(g.Where(r => r.RunId == lastRunId));
                ranOn[i].Add(g.Key);
            }
        }

        // Per-day, per-check rolled-up status — what the hero and the strips are built from.
        var statusOf = new Dictionary<string, HealthStatus>[n];
        for (var i = 0; i < n; i++)
            statusOf[i] = snap[i].GroupBy(r => r.CheckName)
                                 .ToDictionary(g => g.Key, g => CheckRunner.Rollup(g.Select(x => x.Status)), StringComparer.OrdinalIgnoreCase);

        var allDomains = all.Select(DomainOf).Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToList();

        // ---- per-check: strip, readout messages, chart -----------------------
        var trends = model.Checks.ToDictionary(c => c.Name, c => BuildTrend(c, n, snap, statusOf, ranOn, L), StringComparer.OrdinalIgnoreCase);

        // ---- hero: what reported each day ------------------------------------
        var hero = new HeroDay[n];
        for (var i = 0; i < n; i++)
        {
            var counts = new Dictionary<HealthStatus, int>();
            foreach (var s in statusOf[i].Values) counts[s] = counts.GetValueOrDefault(s) + 1;
            var total = counts.Values.Sum();

            // How many checks SHOULD have reported: those inside their own lifetime on this day. Counting
            // the whole inventory would draw a check that did not exist yet as missing; counting only what
            // reported would let an outage look like a small but healthy day. The difference is the bar's
            // unmeasured cap.
            var day = i;
            var expected = trends.Values.Count(t => t.FirstDay is { } f && day >= f && day <= t.LastDay);
            var missing = allDomains.Where(d => !ranOn[i].Contains(d)).ToList();
            hero[i] = new HeroDay(counts, total, expected, Math.Max(0, expected - total), missing);
        }

        // One denominator for every column, so bar heights stay comparable across the window even as the
        // check inventory grows.
        var heroMax = Math.Max(1, hero.Max(h => Math.Max(h.Expected, h.Total)));

        // ---- window-level facts ----------------------------------------------
        var latest = -1;
        for (var i = n - 1; i >= 0; i--) if (snap[i].Count > 0) { latest = i; break; }
        var asOf = latest >= 0 ? snap[latest].Max(r => r.StartedAtUtc) : (DateTimeOffset?)null;
        var runDays = snap.Count(s => s.Count > 0);
        var windowDays = (int)Math.Round(window.TotalDays);
        int CountLatest(HealthStatus s) => latest < 0 ? 0 : statusOf[latest].Values.Count(v => v == s);

        // ≈5 evenly spaced day labels, shared by the ruler.
        var tickCount = Math.Min(5, n);
        var ticks = Enumerable.Range(0, tickCount)
            .Select(k => tickCount == 1 ? 0 : (int)Math.Round(k * (n - 1) / (double)(tickCount - 1), MidpointRounding.AwayFromZero))
            .ToList();

        var sb = new StringBuilder();
        sb.Append(PageChrome.Head("Rastgo · Trends"));
        sb.Append("<body class=\"bg-base-200 text-base-content flex h-screen flex-col overflow-hidden\">");
        sb.Append("<div class=\"flex min-h-0 flex-1\">");

        // ---- rail: app chrome, shared with the dashboard ---------------------
        sb.Append("<aside class=\"bg-base-100 border-base-300 hidden w-[248px] shrink-0 flex-col overflow-y-auto border-r lg:flex\">");

        sb.Append("<div class=\"border-base-300 border-b px-3 py-3\">")
          .Append("<div data-brand>").Append(PageChrome.Logo).Append("</div>")
          .Append("<p class=\"text-base-content/50 mt-1.5 text-[10px] leading-relaxed\" data-meta>")
          .Append($"{windowDays}d window · {runDays} run-days · latest {(asOf is { } a ? $"{a.UtcDateTime:yyyy-MM-dd HH:mm} UTC" : "no runs yet")}")
          .Append("</p></div>");

        // As a segmented control at the top of the rail this reads as what it is — two views of one
        // dataset — rather than a hyperlink to somewhere else.
        sb.Append("<div class=\"px-3 pt-2.5\"><div class=\"border-base-300 rounded-field grid grid-cols-2 gap-0.5 border p-0.5\">")
          .Append("<a href=\"dashboard\" class=\"hover:bg-base-200 rounded-[4px] py-1 text-center text-[11px] font-medium transition-colors\">Current</a>")
          .Append("<span class=\"bg-primary text-primary-content rounded-[4px] py-1 text-center text-[11px] font-semibold\">Trends</span>")
          .Append("</div></div>");

        sb.Append("<div class=\"grid grid-cols-4 gap-1 px-3 pt-2.5\" data-kpis>");
        foreach (var status in PageChrome.KpiOrder)
            sb.Append($"<button type=\"button\" data-filter=\"{status}\" class=\"border-base-300 rounded-field hover:border-accent/40 border px-1 py-1.5 text-center transition-colors\">")
              .Append($"<span class=\"block text-sm leading-none font-bold {PageChrome.Tone(status).Text}\">{CountLatest(status)}</span>")
              .Append($"<span class=\"eyebrow text-base-content/45 mt-0.5 block text-[8px]\">{status}</span></button>");
        sb.Append("</div>");

        // The four cards are the dashboard's four, which omit Skipped. The hero below counts it, so leaving
        // it out silently here would make the two disagree by one.
        var skipped = CountLatest(HealthStatus.Skipped);
        sb.Append("<p class=\"text-base-content/40 px-3 pt-1 pb-2.5 text-[9px]\" data-kpi-note>")
          .Append(skipped > 0 ? $"+{skipped} skipped — not counted above" : "")
          .Append("</p>");

        // Each domain gets its own run-recency strip. "Did they all even run?" is a question only this page
        // can answer, and a single "N run-days" number says nothing about WHICH pack went quiet.
        sb.Append("<div class=\"px-3 pb-2.5\" data-domains>");
        if (model.MultiDomain)
        {
            sb.Append("<p class=\"eyebrow text-base-content/40 mb-1 text-[8px]\">Domains</p><div class=\"flex flex-col gap-1\">");
            foreach (var d in model.Domains)
            {
                var lastRun = all.Where(r => string.Equals(DomainOf(r), d.Id, StringComparison.OrdinalIgnoreCase))
                                 .Max(r => (DateTimeOffset?)r.StartedAtUtc);
                sb.Append($"<button type=\"button\" data-domain=\"{Esc(d.Id)}\" class=\"border-base-300 rounded-field hover:border-accent/40 w-full border px-1.5 py-1 text-left transition-colors\">")
                  .Append("<span class=\"flex items-baseline gap-1 text-[10px]\">")
                  .Append($"<span class=\"font-semibold\">{Esc(d.Id)}</span>")
                  .Append($"<span class=\"text-base-content/45\">{d.Count}</span>")
                  .Append($"<span class=\"text-base-content/45 ml-auto\">{(lastRun is { } lr ? Since(lr, nowUtc) : "never")}</span></span>")
                  .Append("<span class=\"mt-1 flex h-1.5 gap-px\">");
                for (var i = 0; i < n; i++)
                {
                    var ran = ranOn[i].Contains(d.Id);
                    sb.Append($"<span data-tip=\"{Esc($"{DayLabel(i)} — {(ran ? $"{d.Id} reported" : $"no run from {d.Id}")}")}\" ")
                      .Append($"class=\"min-w-0 flex-1 rounded-[1px] {(ran ? "bg-success/60" : "cell-norun")}\"></span>");
                }
                sb.Append("</span></button>");
            }
            sb.Append("</div>");
        }
        sb.Append("</div>");

        sb.Append("<nav class=\"px-1.5 pb-8\" data-toc>");
        foreach (var cat in model.Categories)
            sb.Append($"<a href=\"#cat-{cat.Slug}\" data-jump=\"cat-{cat.Slug}\" class=\"hover:bg-base-200 rounded-field mb-0.5 flex min-w-0 items-center gap-1.5 px-2 py-1 text-xs font-semibold\">")
              .Append("<span class=\"flex min-w-0 flex-1 items-center gap-1\">")
              .Append($"<span class=\"truncate\">{Esc(cat.Label)}</span>{PageChrome.InfoCue(cat.Blurb)}</span>")
              .Append($"<span class=\"flex shrink-0 gap-0.5\">{CheckModel.Chips(cat.Checks)}</span></a>");
        sb.Append("</nav></aside>");

        // ---- main: toolbar + shared day ruler + content ----------------------
        sb.Append("<main class=\"min-w-0 flex-1 overflow-y-auto\">");
        sb.Append("<div class=\"bg-base-200/95 border-base-300 sticky top-0 z-30 flex h-10 items-center gap-1.5 border-b px-4 backdrop-blur\">")
          .Append("<input type=\"search\" data-q placeholder=\"Filter checks…  (native Ctrl+F still works)\" autocomplete=\"off\" class=\"input input-xs bg-base-100 border-base-300 rounded-field h-7 w-full max-w-[300px]\">")
          .Append("<button type=\"button\" data-expand class=\"btn btn-xs btn-ghost border-base-300 h-7 border font-normal\">Expand all</button>")
          .Append("<button type=\"button\" data-collapse class=\"btn btn-xs btn-ghost border-base-300 h-7 border font-normal\">Collapse all</button>")
          .Append("<div class=\"ml-auto flex items-center gap-2\">")
          .Append("<span class=\"text-base-content/45 hidden text-[11px] xl:inline\">Click a row for its chart · hover a cell for that day</span>")
          .Append(PageChrome.ThemeToggleButton)
          .Append("</div></div>");

        // One ruler for the whole page. It shares the check rows' grid template and their padding chain
        // (the transparent border stands in for the section's 1px), so the ticks sit exactly over the cells.
        sb.Append("<div class=\"bg-base-200/95 border-base-300 sticky top-10 z-20 border-b px-4 backdrop-blur\">")
          .Append("<div class=\"border-x border-transparent px-3\">")
          .Append($"<div class=\"grid {Cols} items-center gap-2\">")
          .Append($"<span class=\"eyebrow text-base-content/35 col-span-2 text-[8px]\" data-window>{Esc($"{DayLabel(0)} → {DayLabel(n - 1)}")}</span>")
          .Append("<div class=\"relative h-6\" data-ruler>");
        for (var k = 0; k < ticks.Count; k++)
        {
            var shift = k == 0 ? "" : k == ticks.Count - 1 ? " -translate-x-full" : " -translate-x-1/2";
            sb.Append($"<span class=\"text-base-content/40 absolute top-1/2 -translate-y-1/2 text-[10px] whitespace-nowrap{shift}\" style=\"left:{F(Pct(ticks[k], n))}%\">{Esc(DayLabel(ticks[k]))}</span>");
        }
        sb.Append("</div></div></div></div>");

        sb.Append("<div class=\"px-4 pt-3 pb-16\" data-content>");
        AppendHero(sb, hero, heroMax, n, DayLabel, model.Checks.Count);

        foreach (var cat in model.Categories)
        {
            // Same section shell as the dashboard: the radius and border belong to the SECTION with
            // `overflow-clip`, so the sticky header can be square and opaque without the body's corners
            // showing through it. `clip`, not `hidden` — `hidden` would make the section a scroll container
            // and kill the sticky header outright.
            sb.Append($"<section class=\"bg-base-100 border-base-300 rounded-box mb-2.5 overflow-clip border\" data-cat=\"{cat.Slug}\">")
              .Append($"<button type=\"button\" id=\"cat-{cat.Slug}\" data-catfold class=\"bg-base-100 hover:bg-base-200 sticky top-16 z-10 flex w-full scroll-mt-[68px] items-center gap-2 px-3 py-1.5 text-left transition-colors\">")
              .Append(PageChrome.Caret())
              .Append("<span class=\"bg-accent h-3 w-0.5 shrink-0 rounded-full\"></span>")
              .Append("<span class=\"flex min-w-0 items-center gap-1\">")
              .Append($"<span class=\"truncate text-xs font-bold tracking-wide uppercase\">{Esc(cat.Label)}</span>{PageChrome.InfoCue(cat.Blurb)}</span>")
              .Append($"<span class=\"ml-auto flex gap-0.5\">{CheckModel.Chips(cat.Checks)}</span></button>");

            var rows = new StringBuilder("<div class=\"border-base-300 border-t\">");
            foreach (var c in cat.Checks) AppendCheckRow(rows, trends[c.Name], cat, n, DayLabel, L);
            sb.Append(PageChrome.Slide(rows.Append("</div>").ToString())).Append("</section>");
        }
        sb.Append("</div></main></div>");

        // The crosshair readout's data. Values are pre-formatted server-side so the readout and the y-axis
        // can never disagree about how a number reads.
        var charts = trends.Values.Where(t => t.Chart is not null).ToDictionary(
            t => t.Check.Name,
            t => new { s = t.Chart!.Series.Select(s => new { l = s.Label, v = s.At.Select(v => v is { } x ? t.Chart.Scale.Fmt(x) : null).ToArray() }).ToArray() });

        sb.Append("<script>(function(){'use strict';")
          .Append("const DAYS=").Append(JsonSerializer.Serialize(Enumerable.Range(0, n).Select(DayLabel).ToArray())).Append(';')
          .Append("const CHARTS=").Append(JsonSerializer.Serialize(charts)).Append(';')
          .Append(PageChrome.SharedScript).Append(Script)
          .Append("})();</script></body></html>");
        return sb.ToString();
    }

    // ------------------------------------------------------------------ hero --

    /// <summary>
    /// Stacked daily composition, in plain HTML (real pixels, page font, no SVG). Skipped is a segment
    /// rather than a silent hole — otherwise a skipped check contributes to nothing and the day's bar is
    /// simply short, which is indistinguishable from a day it did not run — and the checks that SHOULD have
    /// reported but did not are drawn as a hatched cap, so an outage is a visible gap in the column rather
    /// than a shorter healthy-looking bar.
    /// </summary>
    private static void AppendHero(StringBuilder sb, HeroDay[] hero, int heroMax, int n, Func<int, string> dayLabel, int totalChecks)
    {
        sb.Append("<section class=\"bg-base-100 border-base-300 rounded-box mb-2.5 overflow-clip border\">")
          .Append("<div class=\"border-base-300 flex items-center gap-2 border-b px-3 py-1.5\">")
          .Append("<span class=\"bg-accent h-3 w-0.5 shrink-0 rounded-full\"></span>")
          .Append("<span class=\"text-xs font-bold tracking-wide uppercase\">Overall health</span>")
          .Append(PageChrome.InfoCue("Every check, every day: one column per day, stacked by status. The hatched cap is checks that existed but did not report — an outage, not a pass."))
          .Append("<span class=\"text-base-content/45 ml-auto flex flex-wrap gap-2 text-[10px]\">");
        foreach (var (label, cls) in new[]
                 {
                     ("Pass", PageChrome.Tone(HealthStatus.Pass).Bar), ("Warn", PageChrome.Tone(HealthStatus.Warn).Bar),
                     ("Fail", PageChrome.Tone(HealthStatus.Fail).Bar), ("Error", PageChrome.Tone(HealthStatus.Error).Bar),
                     ("Skipped", PageChrome.Tone(HealthStatus.Skipped).Bar), ("not measured", "cell-norun"),
                 })
            sb.Append($"<span class=\"flex items-center gap-1\"><i class=\"h-0.5 w-3 rounded-full {cls}\"></i>{label}</span>");
        sb.Append("</span></div>");

        // The coverage line states the two facts the bars encode but do not spell out — a page that draws an
        // outage and then does not name it makes the reader count columns.
        var blankDays = hero.Count(d => d.Total == 0);
        var partialDays = hero.Count(d => d.Total > 0 && d.Missing.Count > 0);
        var coverage = string.Join(" · ", new[]
        {
            $"{totalChecks} checks tracked",
            blankDays > 0 ? $"{blankDays} day{(blankDays > 1 ? "s" : "")} with no run at all" : null,
            partialDays > 0 ? $"{partialDays} day{(partialDays > 1 ? "s" : "")} missing a pack" : null,
        }.Where(x => x is not null));

        sb.Append($"<div class=\"grid {Cols} items-end gap-2 px-3 py-2\">")
          .Append($"<div class=\"col-span-2 flex gap-2\" style=\"height:{HeroHeight}px\">")
          .Append("<div class=\"min-w-0 flex-1 self-end pb-1\">")
          .Append("<p class=\"text-base-content/70 text-[11px] leading-snug\">Checks reporting each day, stacked by status.</p>")
          .Append($"<p class=\"text-base-content/45 mt-1 text-[10px] leading-snug\">{Esc(coverage)}</p></div>")
          .Append("<div class=\"text-base-content/40 flex w-12 shrink-0 flex-col justify-between pr-1 text-right text-[10px] tabular-nums\">")
          .Append($"<span>{heroMax}</span><span>{(int)Math.Round(heroMax / 2.0, MidpointRounding.AwayFromZero)}</span><span>0</span></div></div>")
          .Append($"<div class=\"border-base-300 flex items-end gap-px border-b\" style=\"height:{HeroHeight}px\">");

        for (var i = 0; i < n; i++)
        {
            var d = hero[i];
            var summary = d.Total == 0
                ? $"{dayLabel(i)} — nothing ran"
                : $"{dayLabel(i)} · {string.Join(", ", PageChrome.KpiOrder.Where(s => d.Counts.GetValueOrDefault(s) > 0).Select(s => $"{d.Counts[s]} {s}"))}"
                  + (d.Counts.GetValueOrDefault(HealthStatus.Skipped) > 0 ? $", {d.Counts[HealthStatus.Skipped]} Skipped" : "")
                  + (d.Missing.Count > 0 ? $" · no run from {string.Join(", ", d.Missing)}" : "");

            sb.Append($"<div data-tip=\"{Esc(summary)}\" class=\"flex min-w-0 flex-1 flex-col justify-end\">");
            foreach (var (count, cls) in new (int, string)[]
                     {
                         (d.Unmeasured, "cell-norun"),
                         (d.Counts.GetValueOrDefault(HealthStatus.Error), PageChrome.Tone(HealthStatus.Error).Bar),
                         (d.Counts.GetValueOrDefault(HealthStatus.Fail), PageChrome.Tone(HealthStatus.Fail).Bar),
                         (d.Counts.GetValueOrDefault(HealthStatus.Warn), PageChrome.Tone(HealthStatus.Warn).Bar),
                         (d.Counts.GetValueOrDefault(HealthStatus.Skipped), PageChrome.Tone(HealthStatus.Skipped).Bar),
                         (d.Counts.GetValueOrDefault(HealthStatus.Pass), PageChrome.Tone(HealthStatus.Pass).Bar),
                     })
                if (count > 0)
                    sb.Append($"<i class=\"block w-full {cls}\" style=\"height:{F(count / (double)heroMax * HeroHeight)}px\"></i>");
            sb.Append("</div>");
        }
        sb.Append("</div></div></section>");
    }

    // ------------------------------------------------------------ check rows --

    private static void AppendCheckRow(StringBuilder sb, TrendCheck t, CategoryView cat, int n, Func<int, string> dayLabel, Labeler L)
    {
        var c = t.Check;
        var searchText = Esc(string.Join(' ', c.Name, c.Title, c.Category, c.Severity, c.Domain, c.Description ?? "").ToLowerInvariant());

        // A check with no plottable metric (only ever skipped, or only ever errored) gets no chart and so
        // no fold — a caret that opens nothing is worse than no caret.
        var foldable = t.Chart is not null;

        sb.Append($"<div data-check data-status=\"{c.Status}\" data-domain=\"{Esc(c.Domain)}\" data-text=\"{searchText}\" class=\"border-base-300 border-b last:border-b-0\">");
        sb.Append(foldable
            ? $"<button type=\"button\" data-chartfold class=\"hover:bg-base-200/70 grid w-full {Cols} items-center gap-2 px-3 py-1 text-left transition-colors\">"
            : $"<div class=\"grid {Cols} items-center gap-2 px-3 py-1\">");

        sb.Append($"<div><span class=\"rounded-selector {PageChrome.Tone(c.Status).Solid} inline-block px-1.5 text-[10px] leading-[17px] font-bold\">{c.Status}</span></div>");

        // Label is FAMILY › TITLE, not the bare title: `Title` takes only the last dotted token, so in a
        // flat per-category list "Vin", "Vehicle" and "Vehicles" sit together with nothing to tell them
        // apart. The full dotted name moves into the tooltip, one hover away.
        sb.Append("<div class=\"flex min-w-0 items-center gap-1\">")
          .Append(foldable ? PageChrome.Caret(folded: true) : "<span class=\"w-2.5 shrink-0\"></span>")
          .Append($"<span class=\"truncate text-[12px] leading-tight font-medium\">{Esc(t.Label)}</span>")
          .Append(PageChrome.InfoCue(string.Join(" — ", new[] { c.Name, c.Description }.Where(x => !string.IsNullOrWhiteSpace(x)))));
        if (c.Stale)
            sb.Append($"<span tabindex=\"0\" data-tip=\"Last reported {c.When.UtcDateTime:yyyy-MM-dd HH:mm} UTC — not in the latest run, so likely renamed or removed.\" class=\"border-warning/40 text-warning shrink-0 cursor-help rounded border px-1 text-[9px]\">retired</span>");
        sb.Append("</div>");

        sb.Append("<div class=\"flex gap-px\">");
        for (var i = 0; i < n; i++)
            sb.Append($"<span data-tip=\"{Esc(CellTip(t, i, dayLabel))}\" class=\"h-3.5 min-w-0 flex-1 rounded-[2px] {CellClass(t.Cells[i])}\"></span>");
        sb.Append("</div>");

        sb.Append(foldable ? "</button>" : "</div>");
        if (foldable) sb.Append(PageChrome.Slide(Chart(t, n, dayLabel, L), folded: true));
        sb.Append("</div>");
    }

    /// <summary>
    /// A cell's readout. The two no-data states are worded apart on purpose: "nothing was measured" and
    /// "this check was not in the run" lead to completely different follow-ups.
    /// </summary>
    private static string CellTip(TrendCheck t, int i, Func<int, string> dayLabel)
    {
        var cell = t.Cells[i];
        if (cell.Status is { } s) return $"{dayLabel(i)} · {s} — {t.Messages[i] ?? ""}";
        return cell.DomainRan
            ? $"{dayLabel(i)} — the {t.Check.Domain} pack ran, but this check was not in it."
            : $"{dayLabel(i)} — no run: the {t.Check.Domain} pack did not report.";
    }

    /// <summary>A day nothing ran is a hole in the record and gets a hatch; a day the pack ran without this
    /// check is simply blank. Collapsing those two is the one thing a page that answers "did they all even
    /// run?" must not do.</summary>
    private static string CellClass(DayCell cell) =>
        cell.Status is { } s ? PageChrome.Tone(s).Bar : cell.DomainRan ? "bg-base-200" : "cell-norun";

    // ---------------------------------------------------------------- charts --

    private static string Chart(TrendCheck t, int n, Func<int, string> dayLabel, Labeler L)
    {
        var c = t.Chart!;
        var sc = c.Scale;
        var shown = sc.Thresholds.Where(x => x.V >= sc.Min && x.V <= sc.Max).ToList();

        var sb = new StringBuilder($"<div class=\"grid {Cols} gap-2 px-3 pt-1 pb-2.5\">");

        sb.Append("<div class=\"col-span-2 flex min-w-0 gap-2\"><div class=\"min-w-0 flex-1\">")
          .Append($"<p class=\"text-base-content/45 text-[10px] leading-tight\">{Esc(c.Kind switch
          {
              MetricKind.Age => "Age at each run",
              MetricKind.Diff => "Difference, source − loaded",
              _ => "Measured value",
          })}</p>");

        // The numbers the shape does not give you. On a scalar chart the left column is otherwise empty —
        // the price of pinning the plot to the shared day axis — and "where it is now, where it started,
        // how far it moved" is exactly what a reader would otherwise hover for. The arrow states DIRECTION,
        // not judgement: up is bad for an age and for a max, good for a min, and meaningless for a diff.
        // Status is already carried by the pill and the strip.
        if (c.Series.Count == 1 && c.Series[0].Label.Length == 0)
        {
            var pts = c.Series[0].Points.ToList();
            if (pts.Count >= 2)
            {
                var first = pts[0].V;
                var last = pts[^1].V;
                var delta = last - first;
                var arrow = delta > 0 ? "▲" : delta < 0 ? "▼" : "–";
                sb.Append("<p class=\"text-base-content/60 mt-1 text-[11px] tabular-nums\">")
                  .Append($"now <b class=\"text-base-content/85\">{Esc(sc.Fmt(last))}</b> ")
                  .Append($"<span class=\"text-base-content/40\">· {Esc(dayLabel(pts[0].Day))} {Esc(sc.Fmt(first))} · {arrow} {Esc(sc.Fmt(Math.Abs(delta)))}</span></p>");
            }
        }
        else
        {
            sb.Append("<div class=\"text-base-content/60 mt-1 flex flex-wrap gap-x-2.5 gap-y-0.5 text-[10px]\">");
            foreach (var s in c.Series)
                sb.Append($"<span class=\"flex items-center gap-1\"><i class=\"h-0.5 w-3 rounded-full\" style=\"background:{s.Color}\"></i>{Esc(s.Label)}</span>");
            if (c.Hidden > 0) sb.Append($"<span class=\"text-base-content/40 italic\">+{c.Hidden} more</span>");
            sb.Append("</div>");
        }

        sb.Append("</div><div class=\"text-base-content/40 flex w-12 shrink-0 flex-col justify-between text-right text-[10px] tabular-nums\" ")
          .Append($"style=\"height:{PlotHeight}px\">")
          .Append($"<span>{Esc(sc.Fmt(sc.Max))}</span><span>{Esc(sc.Fmt((sc.Min + sc.Max) / 2))}</span><span>{Esc(sc.Fmt(sc.Min))}</span></div></div>");

        sb.Append($"<div data-plot data-for=\"{Esc(t.Check.Name)}\" role=\"img\" aria-label=\"{Esc(t.Label)} over time\" ")
          .Append($"class=\"border-base-300 relative cursor-crosshair border-y\" style=\"height:{PlotHeight}px\">")
          // SVG = marks only; stretched to the box, strokes kept constant, so the plot is responsive with
          // no measuring and no charting library.
          .Append("<svg viewBox=\"0 0 1000 1000\" preserveAspectRatio=\"none\" aria-hidden=\"true\" class=\"absolute inset-0 block h-full w-full\">");

        foreach (var th in shown)
            sb.Append($"<line x1=\"0\" y1=\"{F(sc.Frac(th.V) * 1000)}\" x2=\"1000\" y2=\"{F(sc.Frac(th.V) * 1000)}\" stroke=\"currentColor\" stroke-width=\"1\"")
              .Append(th.Dashed ? " stroke-dasharray=\"4 3\"" : "")
              .Append($" vector-effect=\"non-scaling-stroke\" class=\"{th.Cls}\"/>");

        foreach (var s in c.Series)
        {
            // The path BREAKS at a gap. Joining every consecutive pair draws a three-day outage as a clean
            // straight line across it: invented data, invented precisely where the reader most needs to see
            // that there is none.
            var d = new StringBuilder();
            var pen = false;
            for (var i = 0; i < n; i++)
            {
                if (s.At[i] is not { } v) { pen = false; continue; }
                d.Append(pen ? 'L' : 'M').Append(F(Pct(i, n) * 10)).Append(' ').Append(F(sc.Frac(v) * 1000)).Append(' ');
                pen = true;
            }
            sb.Append($"<path d=\"{d.ToString().TrimEnd()}\" fill=\"none\" stroke=\"{s.Color}\" stroke-width=\"1.6\" vector-effect=\"non-scaling-stroke\" stroke-linejoin=\"round\" stroke-linecap=\"round\"/>");

            // A lone sample has no segment to draw, so it needs a mark of its own or it is simply invisible.
            for (var i = 0; i < n; i++)
                if (s.At[i] is { } v && (i == 0 || s.At[i - 1] is null) && (i == n - 1 || s.At[i + 1] is null))
                    sb.Append($"<circle cx=\"{F(Pct(i, n) * 10)}\" cy=\"{F(sc.Frac(v) * 1000)}\" r=\"9\" fill=\"{s.Color}\"/>");
        }
        sb.Append("</svg>");

        // Flush right, not inset: the line runs under the label to the plot's edge, so any inset leaves a
        // stray stub of dashes past the text.
        foreach (var th in shown)
            sb.Append($"<span class=\"bg-base-100 absolute right-0 -translate-y-1/2 pl-1 text-[10px] leading-none {th.Cls}\" style=\"top:{F(sc.Frac(th.V) * 100)}%\">{Esc(th.Label)}</span>");

        sb.Append("<span data-cross class=\"bg-base-content/25 pointer-events-none absolute inset-y-0 hidden w-px\"></span>")
          .Append("<span data-readout class=\"rounded-field bg-secondary text-secondary-content pointer-events-none absolute top-1 z-10 hidden max-w-[15rem] px-2 py-1 text-[10px] leading-snug whitespace-nowrap shadow-lg\"></span>")
          .Append("</div></div>");

        return sb.ToString();
    }

    // ----------------------------------------------------------- the per-check model --

    private static TrendCheck BuildTrend(
        CheckView c, int n, List<CheckResult>[] snap, Dictionary<string, HealthStatus>[] statusOf,
        HashSet<string>[] ranOn, Labeler L)
    {
        var rowsPerDay = new List<CheckResult>[n];
        var cells = new DayCell[n];
        var messages = new string?[n];

        for (var i = 0; i < n; i++)
        {
            rowsPerDay[i] = snap[i].Where(r => string.Equals(r.CheckName, c.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            cells[i] = statusOf[i].TryGetValue(c.Name, out var st)
                ? new DayCell(st, true)
                : new DayCell(null, ranOn[i].Contains(c.Domain));
            messages[i] = Message(rowsPerDay[i]);
        }

        var reported = Enumerable.Range(0, n).Where(i => cells[i].Status is not null).ToList();

        return new TrendCheck(
            Check: c,
            Label: Label(c, L),
            Cells: cells,
            Messages: messages,
            FirstDay: reported.Count > 0 ? reported[0] : null,
            LastDay: reported.Count > 0 ? reported[^1] : -1,
            Chart: BuildChart(c, rowsPerDay, n, L));
    }

    /// <summary>The strip readout's answer to "what happened that day?".</summary>
    private static string? Message(List<CheckResult> rows)
    {
        if (rows.Count == 0) return null;
        if (rows.Count == 1) return rows[0].Message;

        // Worst first, then by key: the example the tooltip names should be the most serious one, and the
        // same one on every render.
        var bad = rows.Where(r => r.Status != HealthStatus.Pass)
                      .OrderByDescending(r => CheckRunner.Rank(r.Status))
                      .ThenBy(r => r.BreakdownKey, StringComparer.OrdinalIgnoreCase)
                      .ToList();
        if (bad.Count == 0) return $"{rows.Count} groups, all within range.";
        return $"{bad.Count} of {rows.Count} groups breaching · {bad[0].BreakdownKey ?? ""} {bad[0].Message}".Trim();
    }

    private static string Label(CheckView c, Labeler L)
    {
        var tail = L.FamilyTail(string.IsNullOrWhiteSpace(c.Category) ? "other" : c.Category.Trim().ToLowerInvariant(), c.Family);
        return tail is null ? c.Title : $"{tail} › {c.Title}";
    }

    private static ChartData? BuildChart(CheckView c, List<CheckResult>[] rowsPerDay, int n, Labeler L)
    {
        // Kind is inferred from the most recent day that carries a metric at all, so a check that errors
        // today still plots from its history.
        var kind = MetricKind.None;
        double? lo = null, hi = null;
        for (var i = n - 1; i >= 0 && kind == MetricKind.None; i--)
            foreach (var r in rowsPerDay[i])
            {
                var m = Extract(r);
                if (m.Kind != MetricKind.None) { kind = m.Kind; lo = m.Lo; hi = m.Hi; break; }
            }
        if (kind == MetricKind.None) return null;   // e.g. a check that is only ever skipped — the strip still covers it

        double?[] Sample(Func<List<CheckResult>, CheckResult?> pick)
        {
            var at = new double?[n];
            for (var i = 0; i < n; i++)
            {
                var r = pick(rowsPerDay[i]);
                if (r is not null && Extract(r).Value is { } v) at[i] = v;
            }
            return at;
        }

        var series = new List<Series>();
        var hidden = 0;

        if (c.Grouped)
        {
            var keys = new List<string>();
            foreach (var rows in rowsPerDay)
                foreach (var r in rows)
                    if (r.BreakdownKey is { } k && !keys.Contains(k)) keys.Add(k);

            // Ranked by their most recent value so the ten lines you get are the ten that matter now, not
            // the ten that happen to sort first.
            double Latest(string key)
            {
                for (var i = n - 1; i >= 0; i--)
                {
                    var r = rowsPerDay[i].FirstOrDefault(x => x.BreakdownKey == key);
                    if (r is not null && Extract(r).Value is { } v) return v;
                }
                return double.NegativeInfinity;
            }

            var pickedKeys = keys.OrderByDescending(Latest).Take(MaxSeries).ToList();
            hidden = keys.Count - pickedKeys.Count;

            for (var s = 0; s < pickedKeys.Count; s++)
            {
                var key = pickedKeys[s];
                var at = Sample(rows => rows.FirstOrDefault(x => x.BreakdownKey == key));
                if (at.Any(v => v is not null)) series.Add(new Series(L.BreakdownLabel(key), Palette[s % Palette.Length], at));
            }
        }
        else
        {
            var at = Sample(rows => rows.FirstOrDefault());
            if (at.Any(v => v is not null)) series.Add(new Series("", Palette[0], at));
        }

        if (series.Count == 0) return null;

        var values = series.SelectMany(s => s.At).Where(v => v is not null).Select(v => v!.Value).ToList();
        return new ChartData(kind, series, hidden, Scale.For(kind, values, lo, hi));
    }

    /// <summary>The y-scale and threshold marks for one chart.</summary>
    private readonly record struct Scale(double Min, double Max, List<Threshold> Thresholds, Func<double, string> Fmt)
    {
        /// <summary>0 = top of the plot, 1 = bottom.</summary>
        public double Frac(double v) => 1 - (v - Min) / (Max - Min);

        public static Scale For(MetricKind kind, List<double> values, double? lo, double? hi)
        {
            var dataMax = values.Count > 0 ? values.Max() : 1;
            var dataMin = values.Count > 0 ? values.Min() : 0;
            var thresholds = new List<Threshold>();
            double min, max;
            Func<double, string> fmt;

            switch (kind)
            {
                case MetricKind.Age:
                    // NOT floored at zero. A future-dated source produces a NEGATIVE age (the evaluator's
                    // "…in the future" warn), and on a floor-at-zero axis that line is drawn off the bottom
                    // of the plot: the one check whose value is genuinely alarming is the one you cannot see.
                    min = Math.Min(0, dataMin * 1.15);
                    max = Math.Max(dataMax, hi ?? 0) * 1.15 + 0.001;
                    if (hi is { } ah) thresholds.Add(new Threshold(ah, "text-error", true, $"max {FmtHours(ah)}"));
                    if (min < 0) thresholds.Add(new Threshold(0, "text-base-content/45", false, "0"));
                    fmt = FmtHours;
                    break;

                case MetricKind.Diff:
                    var mm = Math.Max(Math.Max(Math.Abs(dataMax), Math.Abs(dataMin)), 1) * 1.2;
                    min = -mm;
                    max = mm;
                    thresholds.Add(new Threshold(0, "text-base-content/45", false, "0"));
                    fmt = Kfmt;
                    break;

                default:   // Threshold
                    min = Math.Min(0, dataMin);
                    max = Math.Max(dataMax, hi ?? 0) * 1.1 + 0.001;
                    if (lo is { } mn && mn > min) thresholds.Add(new Threshold(mn, "text-warning", true, $"min {Kfmt(mn)}"));
                    if (hi is { } mx) thresholds.Add(new Threshold(mx, "text-error", true, $"max {Kfmt(mx)}"));
                    fmt = Kfmt;
                    break;
            }

            if (max <= min) max = min + 1;
            return new Scale(min, max, thresholds, fmt);
        }
    }

    private readonly record struct Threshold(double V, string Cls, bool Dashed, string Label);

    private sealed record Series(string Label, string Color, double?[] At)
    {
        public IEnumerable<(int Day, double V)> Points =>
            At.Select((v, i) => (Day: i, V: v)).Where(p => p.V is not null).Select(p => (p.Day, p.V!.Value));
    }

    private sealed record ChartData(MetricKind Kind, List<Series> Series, int Hidden, Scale Scale);

    private sealed record TrendCheck(
        CheckView Check, string Label, DayCell[] Cells, string?[] Messages, int? FirstDay, int LastDay, ChartData? Chart);

    /// <summary>
    /// One day of one check's strip. A null <paramref name="Status"/> is "no data", which is TWO states:
    /// the pack ran without this check (<paramref name="DomainRan"/> true — introduced later, or retired),
    /// or the pack did not run at all (false — an outage, nothing was measured).
    /// </summary>
    private readonly record struct DayCell(HealthStatus? Status, bool DomainRan);

    private sealed record HeroDay(Dictionary<HealthStatus, int> Counts, int Total, int Expected, int Unmeasured, List<string> Missing);

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

    // --------------------------------------------------------------- utils ----

    private static string DomainOf(CheckResult r) => string.IsNullOrWhiteSpace(r.Domain) ? "unknown" : r.Domain;

    private static double Pct(int day, int n) => n <= 1 ? 0 : day / (double)(n - 1) * 100;

    /// <summary>One decimal, trailing zero dropped. A value that rounds to zero loses its sign — an axis
    /// labelled "-0" reads as a bug.</summary>
    private static string F(double v)
    {
        var s = v.ToString("0.#", CultureInfo.InvariantCulture);
        return s == "-0" ? "0" : s;
    }

    /// <summary>Hours, switching to days past 24 — on MAGNITUDE, so a future-dated "-30h" reads "-1.3d"
    /// rather than staying in hours only because it is negative.</summary>
    private static string FmtHours(double h) => Math.Abs(h) >= 24 ? $"{F(h / 24)}d" : $"{F(h)}h";

    private static string Kfmt(double v)
    {
        var sign = v < 0 ? "-" : "";
        var a = Math.Abs(v);
        return a >= 1000 ? $"{sign}{F(a / 1000)}k" : $"{sign}{F(a)}";
    }

    /// <summary>Relative age of a run, for the rail's "is this pack still reporting?" line.</summary>
    private static string Since(DateTimeOffset then, DateTimeOffset now)
    {
        var h = (now - then).TotalHours;
        if (h < 1) return $"{Math.Max(1, (int)Math.Round(h * 60, MidpointRounding.AwayFromZero))}m ago";
        if (h < 48) return $"{h.ToString(h < 10 ? "0.0" : "0", CultureInfo.InvariantCulture)}h ago";
        return $"{(h / 24).ToString("0.0", CultureInfo.InvariantCulture)}d ago";
    }

    private static string Esc(string? s) => WebUtility.HtmlEncode(s ?? "");

    // ------------------------------------------------------- page behaviour ---

    private const string Script = """

    const N = DAYS.length;
    const pct = i => (N <= 1 ? 0 : (i / (N - 1)) * 100);
    const num = v => Number(v.toFixed(1));
    const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

    /* ---- chart readout ----------------------------------------------------
       The shipped charts had no interaction at all: you could see that a line
       went up, never by how much or on which day. One crosshair fixes both, and
       it costs a pointermove handler — no library, nothing per-point in the
       DOM. Values arrive pre-formatted, so the readout and the y-axis can never
       disagree about how a number reads. */
    const READOUT_LINES = 6;

    function readout(plot, event) {
      const chart = CHARTS[plot.dataset.for];
      if (!chart) return;

      const box = plot.getBoundingClientRect();
      const day = Math.max(0, Math.min(N - 1, Math.round(((event.clientX - box.left) / box.width) * (N - 1))));
      const values = chart.s.map(s => ({ label: s.l, v: s.v[day] })).filter(x => x.v !== null && x.v !== undefined);

      const cross = plot.querySelector('[data-cross]');
      const tip = plot.querySelector('[data-readout]');

      cross.style.left = `${num(pct(day))}%`;
      cross.classList.remove('hidden');

      const lines = values.slice(0, READOUT_LINES).map(x => `${x.label ? esc(x.label) + ' ' : ''}<b>${esc(x.v)}</b>`).join(' · ');
      const more = values.length > READOUT_LINES ? ` +${values.length - READOUT_LINES}` : '';
      tip.innerHTML = `${esc(DAYS[day])} — ${values.length === 0 ? 'no run' : lines + more}`;
      tip.classList.remove('hidden');

      /* Flip to the other side near the right edge rather than letting the
         readout run out of the plot. */
      const flip = pct(day) > 55;
      tip.style.left = flip ? 'auto' : `calc(${num(pct(day))}% + 6px)`;
      tip.style.right = flip ? `calc(${num(100 - pct(day))}% + 6px)` : 'auto';
    }

    const content = document.querySelector('[data-content]');

    content.addEventListener('pointermove', e => {
      const plot = e.target.closest?.('[data-plot]');
      if (plot) readout(plot, e);
    });

    content.addEventListener('pointerout', e => {
      const plot = e.target.closest?.('[data-plot]');
      if (!plot || plot.contains(e.relatedTarget)) return;
      plot.querySelector('[data-cross]').classList.add('hidden');
      plot.querySelector('[data-readout]').classList.add('hidden');
    });

    /* ---- folding ----------------------------------------------------------
       Categories open, charts closed. 23 charts is about eight screens; the
       strips already answer "when did it break" for every check in one screen,
       and the chart answers "by how much", which you ask about one check at a
       time. */
    for (const header of document.querySelectorAll('[data-catfold], [data-chartfold]')) {
      const body = bodyOf(header);
      header.setAttribute('aria-expanded', String(!isFolded(body)));
      header.addEventListener('click', () => setFold(header, body, !isFolded(body)));
    }

    /* Both buttons act on both levels, so expanding after a collapse is a
       drill-down rather than every chart arriving at once. */
    const setAll = folded =>
      document.querySelectorAll('[data-catfold], [data-chartfold]').forEach(h => setFold(h, bodyOf(h), folded));

    document.querySelector('[data-collapse]').addEventListener('click', () => setAll(true));
    document.querySelector('[data-expand]').addEventListener('click', () => setAll(false));

    /* `?expand=1` opens on the full chart wall — the same role as `?theme=`: it
       makes the expanded view a link somebody can send. */
    if (new URLSearchParams(location.search).get('expand') === '1') setAll(false);

    /* ---- filtering --------------------------------------------------------- */
    const state = { status: null, domain: null, q: '' };
    const rows = [...document.querySelectorAll('[data-check]')];

    function apply() {
      for (const row of rows) {
        const ok =
          (!state.status || row.dataset.status === state.status) &&
          (!state.domain || row.dataset.domain === state.domain) &&
          (!state.q || row.dataset.text.includes(state.q));
        row.classList.toggle('hidden', !ok);
      }

      /* A filter must never leave a match hidden behind a fold. Only the
         categories are opened — opening every chart too would bury the matches
         it just found. */
      if (state.status || state.domain || state.q)
        document.querySelectorAll('[data-catfold]').forEach(h => setFold(h, bodyOf(h), false));

      for (const section of document.querySelectorAll('[data-cat]')) {
        const any = [...section.querySelectorAll('[data-check]')].some(r => !r.classList.contains('hidden'));
        section.classList.toggle('opacity-35', !any);
      }
    }

    /* Scoped to the RAIL. `data-domain` is also on every check row — that is
       how apply() reads a row's domain — so an unscoped query would bind a
       filter handler to every row: click any row and the page silently filters
       to that row's domain. */
    const rail = document.querySelector('aside');

    for (const kind of ['filter', 'domain']) {
      const key = kind === 'filter' ? 'status' : 'domain';
      rail.querySelectorAll(`[data-${kind}]`).forEach(button =>
        button.addEventListener('click', () => {
          state[key] = state[key] === button.dataset[kind] ? null : button.dataset[kind];
          rail.querySelectorAll(`[data-${kind}]`).forEach(b => {
            const on = b.dataset[kind] === state[key];
            b.classList.toggle('ring-2', on);
            b.classList.toggle('ring-accent', on);
          });
          apply();
        }),
      );
    }

    document.querySelector('[data-q]').addEventListener('input', e => {
      state.q = e.target.value.trim().toLowerCase();
      apply();
    });

    /* A jump into a folded category opens it first, or it goes nowhere. */
    document.querySelectorAll('[data-jump]').forEach(link =>
      link.addEventListener('click', e => {
        e.preventDefault();
        const target = document.getElementById(link.dataset.jump);
        if (!target) return;
        setFold(target, bodyOf(target), false);
        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }),
    );
    """;
}
