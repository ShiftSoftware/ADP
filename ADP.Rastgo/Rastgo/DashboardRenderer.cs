using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// Renders the latest run per check into a single self-contained HTML page — no external assets, so it
/// works opened straight off a file share. Layout is "big picture → drill down": a fixed left rail gives
/// an at-a-glance health summary plus jump-navigation (Category → Family), while the detail pane on the
/// right keeps EVERY row in the DOM, expanded by default. Drill-down is navigation, not view-swapping, so
/// native Ctrl+F still finds everything on load. JS only adds opt-in filtering, collapsible groups, the
/// floating tooltips and the theme toggle.
/// <para>
/// The transform is <see cref="CheckModel"/>, shared with <see cref="TrendsRenderer"/>; so is the chrome
/// (stylesheet, head, lockup, tooltip / theme / fold script) in <see cref="PageChrome"/>. The two
/// renderers share no markup.
/// </para>
/// </summary>
public static partial class DashboardRenderer
{
    public static string Render(IReadOnlyList<CheckResult> all, DateTimeOffset generatedAtUtc, DashboardOptions? options = null)
    {
        var model = CheckModel.Build(all, options ?? DashboardOptions.Default);
        var L = model.Labels;

        var sb = new StringBuilder();
        sb.Append(PageChrome.Head("Rastgo"));
        sb.Append("<body class=\"bg-base-200 text-base-content flex h-screen flex-col overflow-hidden\">");
        sb.Append("<div class=\"flex min-h-0 flex-1\">");

        // ---- rail: health summary + jump nav ----------------------------------
        sb.Append("<aside class=\"bg-base-100 border-base-300 hidden w-[248px] shrink-0 flex-col overflow-y-auto border-r lg:flex\">");

        sb.Append("<div class=\"border-base-300 border-b px-3 py-3\">")
          .Append("<div data-brand>").Append(PageChrome.Logo).Append("</div>")
          .Append("<p class=\"text-base-content/50 mt-1.5 text-[10px] leading-relaxed\" data-meta>")
          .Append($"{generatedAtUtc.UtcDateTime:yyyy-MM-dd HH:mm} UTC · {model.Checks.Count} checks · {model.Domains.Count} domains")
          .Append("</p></div>");

        sb.Append("<div class=\"grid grid-cols-4 gap-1 px-3 py-2.5\" data-kpis>");
        foreach (var status in PageChrome.KpiOrder)
            sb.Append($"<button type=\"button\" data-filter=\"{status}\" class=\"border-base-300 rounded-field hover:border-accent/40 border px-1 py-1.5 text-center transition-colors\">")
              .Append($"<span class=\"block text-sm leading-none font-bold {PageChrome.Tone(status).Text}\">{model.Count(status)}</span>")
              .Append($"<span class=\"eyebrow text-base-content/45 mt-0.5 block text-[8px]\">{status}</span></button>");
        sb.Append("</div>");

        // Domain chips only earn their space in a federated deployment.
        sb.Append("<div class=\"px-3 pb-2.5\" data-domains>");
        if (model.MultiDomain)
        {
            sb.Append("<p class=\"eyebrow text-base-content/40 mb-1 text-[8px]\">Domains</p><div class=\"flex flex-wrap gap-1\">");
            foreach (var d in model.Domains)
                sb.Append($"<button type=\"button\" data-domain=\"{Esc(d.Id)}\" class=\"border-base-300 rounded-field hover:border-accent/40 border px-1.5 py-0.5 text-[10px] transition-colors\">")
                  .Append($"{Esc(d.Id)} <span class=\"text-base-content/45\">{d.Count}</span></button>");
            sb.Append("</div>");
        }
        sb.Append("</div>");

        // The rail's own fold is independent of the pane's — it shortens the jump list, it does not hide
        // content, so "Collapse all" deliberately leaves it alone.
        sb.Append("<nav class=\"px-1.5 pb-8\" data-toc>");
        foreach (var cat in model.Categories)
        {
            var families = cat.Families.Where(f => f.Tail is not null).ToList();

            sb.Append("<div class=\"mb-0.5\"><div class=\"flex items-center\">")
              .Append(families.Count > 0
                  ? $"<button type=\"button\" data-tocfold class=\"hover:text-base-content grid h-6 w-4 shrink-0 place-items-center\">{PageChrome.Caret()}</button>"
                  : "<span class=\"w-4 shrink-0\"></span>")
              .Append($"<a href=\"#cat-{cat.Slug}\" data-jump=\"cat-{cat.Slug}\" class=\"hover:bg-base-200 rounded-field flex min-w-0 flex-1 items-center gap-1.5 px-1.5 py-1 text-xs font-semibold\">")
              .Append("<span class=\"flex min-w-0 flex-1 items-center gap-1\">")
              .Append($"<span class=\"truncate\">{Esc(cat.Label)}</span>{PageChrome.InfoCue(cat.Blurb)}</span>")
              .Append($"<span class=\"flex shrink-0 gap-0.5\">{CheckModel.Chips(cat.Checks)}</span></a></div>");

            var inner = new StringBuilder("<div class=\"border-base-300 mt-0.5 ml-4 flex flex-col border-l\">");
            foreach (var fam in families)
                inner.Append($"<a href=\"#fam-{fam.Slug}\" data-jump=\"fam-{fam.Slug}\" class=\"text-base-content/60 hover:bg-base-200 hover:text-base-content flex items-center gap-1.5 py-0.5 pr-1.5 pl-2 text-[11px]\">")
                     .Append($"<span class=\"min-w-0 flex-1 truncate\">{Esc(fam.Tail)}</span>")
                     .Append($"<span class=\"flex shrink-0 gap-0.5\">{CheckModel.Chips(fam.Checks)}</span></a>");
            sb.Append(PageChrome.Slide(inner.Append("</div>").ToString())).Append("</div>");
        }
        sb.Append("</nav></aside>");

        // ---- main: toolbar + detail pane --------------------------------------
        sb.Append("<main class=\"min-w-0 flex-1 overflow-y-auto\">");
        sb.Append("<div class=\"bg-base-200/95 border-base-300 sticky top-0 z-20 flex h-10 items-center gap-1.5 border-b px-4 backdrop-blur\">")
          .Append("<input type=\"search\" data-q placeholder=\"Filter checks…  (native Ctrl+F still works)\" autocomplete=\"off\" class=\"input input-xs bg-base-100 border-base-300 rounded-field h-7 w-full max-w-[320px]\">")
          .Append("<button type=\"button\" data-expand class=\"btn btn-xs btn-ghost border-base-300 h-7 border font-normal\">Expand all</button>")
          .Append("<button type=\"button\" data-collapse class=\"btn btn-xs btn-ghost border-base-300 h-7 border font-normal\">Collapse all</button>")
          // Hint and toggle share one right-docked group: the hint drops out on narrow screens and the
          // toggle must not drift left when it does.
          .Append("<div class=\"ml-auto flex items-center gap-2\">")
          .Append("<span class=\"text-base-content/45 hidden text-[11px] xl:inline\">Click a card to filter by status · click a heading to fold</span>")
          .Append(PageChrome.ThemeToggleButton)
          .Append("</div></div>");

        sb.Append("<div class=\"px-4 pt-3 pb-16\" data-table>");
        foreach (var cat in model.Categories)
        {
            // `overflow-clip`, NOT `overflow-hidden`. The section has to clip its children to its own
            // rounded corners — otherwise the body's square top corners show through the notches of the
            // sticky header once rows scroll under it — but `hidden` would make the section a scroll
            // container and kill the sticky header outright. `clip` does not. With the rounding owned by
            // the section the header carries none, so it looks right stuck to the toolbar and a folded
            // category is a clean rounded pill with no missing bottom edge.
            sb.Append($"<section class=\"bg-base-100 border-base-300 rounded-box mb-2.5 overflow-clip border\" data-cat=\"{cat.Slug}\">")
              // Hover is a SOLID base-200: a translucent one let the rows scrolling beneath the sticky
              // header bleed through it.
              .Append($"<button type=\"button\" id=\"cat-{cat.Slug}\" data-catfold class=\"bg-base-100 hover:bg-base-200 sticky top-10 z-10 flex w-full scroll-mt-[44px] items-center gap-2 px-3 py-1.5 text-left transition-colors\">")
              .Append(PageChrome.Caret())
              .Append("<span class=\"bg-accent h-3 w-0.5 shrink-0 rounded-full\"></span>")
              .Append("<span class=\"flex min-w-0 items-center gap-1\">")
              .Append($"<span class=\"truncate text-xs font-bold tracking-wide uppercase\">{Esc(cat.Label)}</span>{PageChrome.InfoCue(cat.Blurb)}</span>")
              .Append($"<span class=\"ml-auto flex gap-0.5\">{CheckModel.Chips(cat.Checks)}</span></button>");

            var body = new StringBuilder("<div class=\"border-base-300 border-t\">");
            foreach (var fam in cat.Families)
            {
                if (fam.Tail is null)
                {
                    // Family == category: no tail to label it with, so these rows sit directly under the
                    // category and fold only with it.
                    body.Append("<div class=\"border-base-300 border-b last:border-b-0\">");
                    foreach (var c in fam.Checks) AppendCheckRow(body, c, model.MultiDomain, L);
                    body.Append("</div>");
                    continue;
                }

                body.Append("<div data-family class=\"border-base-300 border-b last:border-b-0\">")
                    .Append($"<button type=\"button\" id=\"fam-{fam.Slug}\" data-famfold class=\"bg-base-200/50 hover:bg-base-200 flex w-full scroll-mt-[68px] items-center gap-1.5 px-3 py-1 text-left\">")
                    .Append(PageChrome.Caret())
                    .Append($"<span class=\"text-[11px] font-semibold\">{Esc(fam.Tail)}</span>")
                    .Append($"<span class=\"ml-auto flex gap-0.5\">{CheckModel.Chips(fam.Checks)}</span></button>");

                var rows = new StringBuilder();
                foreach (var c in fam.Checks) AppendCheckRow(rows, c, model.MultiDomain, L);
                body.Append(PageChrome.Slide(rows.ToString())).Append("</div>");
            }

            sb.Append(PageChrome.Slide(body.Append("</div>").ToString())).Append("</section>");
        }

        sb.Append("</div></main></div>");
        sb.Append("<script>(function(){'use strict';").Append(PageChrome.SharedScript).Append(Script).Append("})();</script></body></html>");
        return sb.ToString();
    }

    private static void AppendCheckRow(StringBuilder sb, CheckView c, bool showDomain, Labeler L)
    {
        var searchText = Esc((string.Join(' ',
            new[] { c.Name, c.Title, c.Category, c.Severity, c.Domain, c.Description ?? "" }
            .Concat(c.Rows.Select(r => $"{L.BreakdownLabel(r.BreakdownKey ?? "")} {r.Message}")))).ToLowerInvariant());

        // 21rem for the name column is measured, not guessed: the longest check name plus its domain and
        // severity tags. Any narrower and the tag line wraps, which costs every row a third line.
        sb.Append("<div class=\"border-base-300 grid grid-cols-[4rem_minmax(0,21rem)_minmax(0,1fr)] gap-3 border-b px-3 py-1.5 last:border-b-0\" ")
          .Append($"data-check data-status=\"{c.Status}\" data-domain=\"{Esc(c.Domain)}\" data-text=\"{searchText}\">");

        sb.Append($"<div><span class=\"rounded-selector {PageChrome.Tone(c.Status).Solid} inline-block px-1.5 text-[10px] leading-[17px] font-bold\">{c.Status}</span></div>");

        sb.Append("<div class=\"min-w-0\"><div class=\"flex items-center gap-1 text-[13px] leading-tight font-semibold\">")
          .Append($"<span class=\"truncate\">{Esc(c.Title)}</span>{PageChrome.InfoCue(c.Description)}</div>")
          .Append("<div class=\"mt-0.5 flex flex-wrap items-center gap-1\">")
          .Append($"<code class=\"ident bg-base-200 text-base-content/55 rounded px-1 text-[10px]\">{Esc(c.Name)}</code>");
        if (showDomain)
            sb.Append($"<span class=\"border-base-300 text-base-content/55 rounded border px-1 text-[9px]\">{Esc(c.Domain)}</span>");
        sb.Append($"<span class=\"rounded border px-1 text-[9px] {(string.Equals(c.Severity, "critical", StringComparison.OrdinalIgnoreCase) ? "border-error/35 text-error" : "border-base-300 text-base-content/50")}\">{Esc(c.Severity)}</span>");
        if (c.Stale)
            sb.Append($"<span tabindex=\"0\" data-tip=\"From an earlier run ({c.When.UtcDateTime:yyyy-MM-dd HH:mm} UTC) — not in the latest run, so likely renamed or removed.\" class=\"border-warning/40 text-warning cursor-help rounded border px-1 text-[9px]\">stale</span>");
        sb.Append("</div></div>");

        sb.Append("<div class=\"min-w-0 text-xs\">");
        if (!c.Grouped)
        {
            sb.Append($"<span class=\"text-base-content/75\">{Esc(c.Rows[0].Message)}</span>");
        }
        else
        {
            const int cap = 60;
            // The threshold ("max 2d") belongs to the whole check, yet every breached breakdown row repeats
            // it. Group the rows by their rule and lift each distinct rule to a single chip, dropping it
            // from the rows beneath. The common case (every row shares one rule) collapses to a single
            // chip; a mixed check (some "(warn 1.5d)", some "(max 2d)") gets one chip per rule,
            // worst-severity group first (RuleGroups preserves c.Rows' status-ranked order). Rows carrying
            // no rule render chip-less, verbatim.
            var shown = 0;
            foreach (var (rule, rows) in RuleGroups(c.Rows))
            {
                if (shown >= cap) break;
                if (rule is not null)
                    sb.Append($"<div class=\"border-base-300 text-base-content/60 rounded-selector bg-base-200 mt-1 mb-0.5 inline-block border px-1.5 text-[9px] leading-4 font-semibold first:mt-0\">{Esc(rule)}</div>");
                foreach (var r in rows)
                {
                    if (shown >= cap) break;
                    var msg = rule is null ? r.Message : StripRule(r.Message);
                    sb.Append("<div class=\"flex items-baseline gap-1.5 leading-5\">")
                      .Append($"<span class=\"dot {PageChrome.Tone(r.Status).Text} translate-y-[-1px]\"></span>")
                      .Append($"<span class=\"min-w-[7rem] shrink-0 text-[11px] font-semibold\">{Esc(L.BreakdownLabel(r.BreakdownKey ?? "—"))}</span>")
                      .Append($"<span class=\"text-base-content/65 text-[11px]\">{Esc(msg)}</span></div>");
                    shown++;
                }
            }
            if (c.Rows.Count > shown)
                sb.Append($"<div class=\"text-base-content/40 text-[11px] leading-5\">+{c.Rows.Count - shown} more…</div>");
        }
        sb.Append("</div></div>");
    }

    private static string Esc(string? s) => WebUtility.HtmlEncode(s ?? "");

    // A check's threshold parenthetical — "(max 2d)", "(warn 6h)", "(min 100)" — is a property of the whole
    // check, not of each breakdown row, so a grouped check repeats it on every line. The helpers below lift it
    // off the rows into per-rule chips: matched only when it's a real rule keyword (so a diff's "(Δ 3)" value,
    // which legitimately varies per row, is never mistaken for one).
    [GeneratedRegex(@"\s*\((?:max|min|warn)\s+[^()]*\)(?=\.?\s*$)", RegexOptions.IgnoreCase)]
    private static partial Regex RuleSuffixRegex();

    /// <summary>A grouped check's rows clustered by their rule chip, ready to render as one chip per distinct
    /// rule (e.g. "max 2d") with the matching rows beneath. Groups are ordered worst first: by the harshest
    /// status they contain, then — so a "(max 2d)" breach leads its "(warn 1.5d)" sibling even when a
    /// warning-severity check maps both Stale and Aging to Warn — by rule hardness (max/min before warn). Rows
    /// keep their (status-ranked, then alphabetical) order within a group; rows with no rule cluster under a
    /// null key and render chip-less, last. A single-rule check collapses to today's one-chip layout.</summary>
    private static List<(string? Rule, List<CheckResult> Rows)> RuleGroups(IReadOnlyList<CheckResult> rows) =>
        rows.GroupBy(RowRule)
            .Select(g => (Rule: g.Key, Rows: g.ToList()))
            .OrderByDescending(g => g.Rows.Max(r => CheckRunner.Rank(r.Status)))
            .ThenBy(g => RuleHardness(g.Rule))
            .ToList();

    /// <summary>Orders rule chips within a check: hard breaches (max/min — "stale", out-of-range) before soft
    /// ones (warn — "aging") when they share a status, so the more serious rule leads; no-rule groups sort last.</summary>
    private static int RuleHardness(string? rule) =>
        rule is null ? 2 : rule.StartsWith("warn", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

    /// <summary>The rule suffix of a single row, unwrapped ("Stale: 4.5d old (max 2d)." → "max 2d"), or null
    /// when the row carries none.</summary>
    private static string? RowRule(CheckResult r)
    {
        var m = RuleSuffixRegex().Match(r.Message ?? "");
        return m.Success ? m.Value.Trim().Trim('(', ')') : null;   // "(max 2d)" -> "max 2d"
    }

    /// <summary>Drops the rule suffix from a row message, keeping it terminated: "Stale: 4.5d old (max 2d)." → "Stale: 4.5d old.".</summary>
    private static string StripRule(string? msg)
    {
        var s = RuleSuffixRegex().Replace(msg ?? "", "").TrimEnd();
        if (s.Length > 0 && s[^1] is not ('.' or '!' or '?')) s += ".";
        return s;
    }

    // ---- page behaviour ----------------------------------------------------

    private const string Script = """

    /* ---- fold binds -------------------------------------------------------
       Two levels, matching the structure. A check whose family IS its category
       renders with no family header — there is no tail left to label it with —
       so it has no fold control of its own and belongs to the category
       directly. That only works if the CATEGORY folds too: without it,
       "Collapse all" hid those rows with nothing but "Expand all" able to bring
       them back. */
    function bindFold(selector) {
      document.querySelectorAll(selector).forEach(header => {
        const body = bodyOf(header);
        header.setAttribute('aria-expanded', 'true');
        header.addEventListener('click', () => setFold(header, body, !isFolded(body)));
      });
    }

    bindFold('[data-catfold]');
    bindFold('[data-famfold]');
    bindFold('[data-tocfold]');

    /* Both buttons act on both levels. Collapsing only categories looked
       identical while collapsed but behaved wrongly on the way back out:
       opening one category dumped every check in it on screen at once. Folding
       the families too makes the expand a real drill-down.

       The rail is deliberately untouched. It is navigation, not content, and
       collapsing it would take the jump list away exactly when the pane is
       folded down and you most need it. */
    const setAll = folded =>
      document.querySelectorAll('[data-catfold], [data-famfold]').forEach(h => setFold(h, bodyOf(h), folded));

    document.querySelector('[data-collapse]').addEventListener('click', () => setAll(true));
    document.querySelector('[data-expand]').addEventListener('click', () => setAll(false));

    /* ---- filtering --------------------------------------------------------
       Opt-in only: the page loads fully expanded and unfiltered, so native
       Ctrl+F finds everything before any of this runs. */
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

      /* A filter must never leave a match hidden behind a fold. */
      if (state.status || state.domain || state.q) setAll(false);

      for (const section of document.querySelectorAll('[data-cat]')) {
        const any = [...section.querySelectorAll('[data-check]')].some(r => !r.classList.contains('hidden'));
        section.classList.toggle('opacity-35', !any);
      }
    }

    /* Scoped to the RAIL. `data-domain` is also on every check row — that is
       how apply() reads a row's domain — so an unscoped query would bind a
       filter handler to every row and put the selected ring on it: click any
       row and the page silently filters to that row's domain. */
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

    /* The rail links live outside the scrolling pane, so let the anchor scroll
       it. A jump into a folded category opens it first, or it goes nowhere. */
    document.querySelectorAll('[data-jump]').forEach(link =>
      link.addEventListener('click', e => {
        e.preventDefault();
        const target = document.getElementById(link.dataset.jump);
        if (!target) return;
        const catHeader = target.closest('[data-cat]')?.querySelector('[data-catfold]');
        if (catHeader) setFold(catHeader, bodyOf(catHeader), false);
        if (target.matches('[data-famfold]')) setFold(target, bodyOf(target), false);
        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }),
    );
    """;
}
