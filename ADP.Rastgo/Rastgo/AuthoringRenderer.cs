using System.Net;
using System.Text;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>Renders the loaded pack, validation, source inventory, and bounded metadata as one safe HTML page.</summary>
public static class AuthoringRenderer
{
    private const string Css = """
    .authoring-main{max-width:92rem;margin:0 auto;padding:1rem 1rem 5rem}.authoring-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(18rem,1fr));gap:.65rem}.authoring-stack{display:grid;gap:.65rem}.authoring-card{background:var(--color-base-100);border:1px solid var(--color-base-300);border-radius:var(--radius-box);padding:.85rem}.authoring-section{scroll-margin-top:3.5rem;margin-bottom:1.25rem}.authoring-title{font-size:1rem;font-weight:750;letter-spacing:.02em}.authoring-muted{color:color-mix(in oklab,var(--color-base-content) 55%,transparent);font-size:.72rem}.authoring-table{width:100%;border-collapse:collapse;font-size:.74rem}.authoring-table th,.authoring-table td{padding:.45rem .55rem;border-bottom:1px solid var(--color-base-300);text-align:left;vertical-align:top}.authoring-table th{position:sticky;top:2.5rem;background:var(--color-base-100);font-size:.65rem;text-transform:uppercase;letter-spacing:.08em}.authoring-code{display:block;overflow:auto;white-space:pre-wrap;overflow-wrap:anywhere;background:var(--color-base-200);border-radius:var(--radius-field);padding:.7rem;font:11px/1.55 var(--font-mono)}.authoring-pill{display:inline-flex;align-items:center;border:1px solid var(--color-base-300);border-radius:999px;padding:.12rem .45rem;font-size:.65rem}.authoring-error{border-color:color-mix(in oklab,var(--color-error) 42%,var(--color-base-300));}.authoring-ok{border-color:color-mix(in oklab,var(--color-success) 42%,var(--color-base-300));}.authoring-group{background:var(--color-base-100);border:1px solid var(--color-base-300);border-radius:var(--radius-box);overflow:hidden}.authoring-group>summary{display:flex;align-items:center;gap:.5rem;padding:.7rem .85rem}.authoring-group[open]>summary{border-bottom:1px solid var(--color-base-300)}.authoring-group-body{padding:.7rem}.authoring-family{border:1px solid var(--color-base-300);border-radius:var(--radius-field);overflow:hidden}.authoring-family>summary{display:flex;align-items:center;gap:.5rem;padding:.5rem .65rem;background:var(--color-base-200)}.authoring-family-body{padding:.6rem}.authoring-dataset{border-top:1px solid var(--color-base-300);padding:.55rem 0}.authoring-dataset:first-child{border-top:0}.authoring-fields{display:flex;flex-wrap:wrap;gap:.25rem;margin-top:.35rem}.authoring-field{background:var(--color-base-200);border-radius:.25rem;padding:.12rem .35rem;font:10px/1.4 var(--font-mono)}.authoring-agent{border-left:.2rem solid var(--color-accent)}details>summary{cursor:pointer;list-style:none}details>summary::-webkit-details-marker{display:none}details>summary .authoring-caret{transition:transform .15s ease}details[open]>summary .authoring-caret{transform:rotate(180deg)}@media(max-width:1023px){.authoring-main{padding-top:.75rem}.authoring-table th{top:2.5rem}}
    """;

    public static string Render(
        AuthoringCatalog catalog,
        DashboardOptions? dashboardOptions = null,
        RastgoPageLinks? links = null)
    {
        var displayOptions = dashboardOptions ?? DashboardOptions.Default;
        links ??= RastgoPageLinks.Default;
        var errors = catalog.Pack.Diagnostics.Count(d => d.Severity == CheckDiagnosticSeverity.Error);
        var warnings = catalog.Pack.Diagnostics.Count(d => d.Severity == CheckDiagnosticSeverity.Warning);
        var datasets = catalog.Sources.Sum(s => s.Datasets.Count);
        var sb = new StringBuilder(PageChrome.Head("Rastgo · Authoring", Css));
        sb.Append("<body class=\"bg-base-200 text-base-content flex h-screen flex-col overflow-hidden\"><div class=\"flex min-h-0 flex-1\">");

        sb.Append("<aside class=\"bg-base-100 border-base-300 hidden w-[248px] shrink-0 flex-col overflow-y-auto border-r lg:flex\">")
          .Append("<div class=\"border-base-300 border-b px-3 py-3\"><div data-brand>").Append(PageChrome.Logo).Append("</div>")
          .Append("<p class=\"text-base-content/50 mt-1.5 text-[10px] leading-relaxed\" data-meta>")
          .Append(E(catalog.PackName)).Append(" · ").Append(catalog.Pack.Checks.Count).Append(" checks · ")
          .Append(catalog.Sources.Count).Append(" sources</p></div>")
          .Append(PageChrome.Navigation("authoring", links))
          .Append("<nav class=\"px-3 py-3 text-xs leading-7\">")
          .Append("<a href=\"#validation\" class=\"hover:text-base-content text-base-content/60 block\">Validation</a>")
          .Append("<a href=\"#agent-workflow\" class=\"hover:text-base-content text-base-content/60 block\">Agent workflow</a>")
          .Append("<a href=\"#pack\" class=\"hover:text-base-content text-base-content/60 block\">Loaded check pack</a>")
          .Append("<a href=\"#sources\" class=\"hover:text-base-content text-base-content/60 block\">Registered sources</a>")
          .Append("<a href=\"#language\" class=\"hover:text-base-content text-base-content/60 block\">YAML language</a>")
          .Append("</nav></aside>");

        sb.Append("<main class=\"min-w-0 flex-1 overflow-y-auto\">")
          .Append("<div class=\"bg-base-200/95 border-base-300 sticky top-0 z-20 flex h-10 items-center gap-1.5 border-b px-4 backdrop-blur\">")
          .Append("<input type=\"search\" data-authoring-q placeholder=\"Filter checks, sources, tables, columns…\" autocomplete=\"off\" class=\"input input-xs bg-base-100 border-base-300 rounded-field h-7 w-full max-w-[360px]\">")
          .Append("<a data-rastgo-link href=\"").Append(E(WithRefresh(links.Authoring))).Append("\" class=\"btn btn-xs btn-ghost border-base-300 h-7 border font-normal\">Refresh metadata</a>")
          .Append("<span class=\"text-base-content/45 ml-auto hidden text-[11px] xl:inline\">Metadata only · no credentials or records</span>")
          .Append(PageChrome.ThemeToggleButton).Append("</div><div class=\"authoring-main\">");

        sb.Append("<header class=\"mb-4\"><p class=\"eyebrow text-accent mb-1 text-[9px]\">Live developer reference</p>")
          .Append("<h1 class=\"text-xl font-bold\">Rastgo authoring catalog</h1><p class=\"authoring-muted mt-1\">Generated ")
          .Append(catalog.GeneratedAtUtc.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss")).Append(" UTC from the loaded pack and this host's registered sources. Discovery is cached for ")
          .Append(E(FormatDuration(catalog.CacheDuration))).Append(" unless refreshed.</p></header>");

        sb.Append("<div class=\"authoring-grid mb-5\">")
          .Append(Summary("Checks", catalog.Pack.Checks.Count.ToString(), "Definitions loaded"))
          .Append(Summary("Validation", errors == 0 ? "Valid" : $"{errors} error(s)", warnings == 0 ? "No warnings" : $"{warnings} warning(s)"))
          .Append(Summary("Sources", catalog.Sources.Count.ToString(), $"{catalog.Sources.Count(s => s.Error is null)} available"))
          .Append(Summary("Datasets", datasets.ToString(), "Bounded metadata inventory"))
          .Append("</div>");

        AppendValidation(sb, catalog.Pack.Diagnostics);
        AppendAgentWorkflow(sb, catalog);
        AppendPack(sb, catalog.Pack.Checks, displayOptions);
        AppendSources(sb, catalog.Sources);
        AppendLanguage(sb);

        sb.Append("</div></main></div><textarea data-agent-context hidden>")
          .Append(E(RenderAgentContext(catalog)))
          .Append("</textarea><script>(function(){'use strict';")
          .Append(PageChrome.SharedScript)
          .Append("""
          const q=document.querySelector('[data-authoring-q]');
          q.addEventListener('input',()=>{
            const needle=q.value.trim().toLowerCase();
            const groups=[...document.querySelectorAll('[data-authoring-group]')];
            groups.forEach(group=>group.hidden=false);
            document.querySelectorAll('[data-authoring-item]').forEach(item=>item.hidden=!!needle&&!item.dataset.authoringSearch.includes(needle));
            groups.reverse().forEach(group=>{
              const match=group.querySelector('[data-authoring-item]:not([hidden])');
              group.hidden=!match;
              if(needle&&match)group.open=true;
            });
          });
          const download=document.querySelector('[data-download-agent-context]');
          if(download)download.addEventListener('click',()=>{
            const context=document.querySelector('[data-agent-context]').value.trim()+'\n';
            const url=URL.createObjectURL(new Blob([context],{type:'application/json'}));
            const link=document.createElement('a');
            link.href=url;link.download='rastgo-authoring-context.json';
            document.body.appendChild(link);link.click();link.remove();
            setTimeout(()=>URL.revokeObjectURL(url),0);
            download.textContent='Downloaded';setTimeout(()=>download.textContent='Download agent context',1400);
          });
          """)
          .Append("})();</script></body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Produces a safe, machine-readable handoff for coding agents. It contains the same loaded checks,
    /// diagnostics, language contract, and bounded source metadata as the HTML page—never credentials or rows.
    /// </summary>
    public static string RenderAgentContext(AuthoringCatalog catalog) =>
        JsonSerializer.Serialize(new
        {
            schema = "rastgo-authoring-context/v2",
            agent = new
            {
                task = $"Author minimal, valid changes to the Rastgo check pack '{catalog.PackName}' using only evidence in this attachment.",
                evidenceMode = "closed-world",
                tableEncoding = "Objects with columns and rows are positional tables; each row value corresponds to the column at the same index.",
                rules = new[]
                {
                    "Ground every source, dataset/container/path, column, and document field in sources or an existing pack check. Copy paths verbatim, including drive or mount prefix, separators, and casing; never translate paths between operating systems.",
                    "Resolve the request to exactly one grounded interpretation before authoring; missing evidence is BLOCKED and multiple grounded meanings require CLARIFY.",
                    "Use only properties declared in language.fields. SQL belongs in measures[].sql; root query and error_threshold are invalid.",
                    "Return the smallest check change plus an identifier evidence list. Never claim validation or execution that was not performed.",
                    "Never request or expose connection strings, credentials, record values, or sample documents.",
                },
                responses = new
                {
                    authorable = "Return minimal Rastgo YAML, then list the attachment evidence for every identifier.",
                    clarify = "Return no YAML. Start with CLARIFY: and list only the grounded choices that change meaning.",
                    blocked = "Return no YAML. Start with BLOCKED: and state the exact missing metadata or developer confirmation.",
                },
                sourceRules = new
                {
                    fileshare = "Supports only path-based existence, modification time, and file count; it cannot query file rows with SQL. declaredSchemas ground column names for downstream-source reasoning, not file-share SQL.",
                    duckdb = "Supports read-only measures[].sql; every table, column, and external-file path must be grounded.",
                    cosmos = "Supports read-only measures[].sql with database and container; document fields must be grounded in catalog metadata or existing check SQL.",
                },
            },
            pack = new
            {
                name = catalog.PackName,
                generatedAtUtc = catalog.GeneratedAtUtc,
                valid = catalog.Pack.IsValid,
                diagnostics = catalog.Pack.Diagnostics,
                checks = AgentChecks(catalog.Pack.Checks),
            },
            language = new
            {
                queryShape = "Scalar SQL returns v; grouped SQL returns k and v. Durations use a positive decimal plus s, m, h, or d.",
                severities = CheckLanguage.Severities.OrderBy(x => x),
                valueKinds = CheckLanguage.ValueKinds.OrderBy(x => x),
                assertTypes = CheckLanguage.AssertTypes.OrderBy(x => x),
                fields = new AgentTable(
                    ["scope", "name", "type", "default", "requiredWhen", "description"],
                    CheckLanguage.Fields.Select(field => new object?[]
                    {
                        field.Scope,
                        field.Name,
                        field.Type,
                        field.Default,
                        field.RequiredWhen,
                        field.Description,
                    }).ToList()),
            },
            sources = AgentSources(catalog.Sources),
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        });

    private static IReadOnlyList<object> AgentChecks(IReadOnlyList<CheckDefinition> checks) =>
        checks.Select(check => (object)new
        {
            check.Name,
            check.Domain,
            check.Category,
            check.Severity,
            check.Description,
            check.Order,
            check.Breakdown,
            measures = check.Measures.Select(measure => new
            {
                measure.Key,
                measure.Source,
                measure.Sql,
                measure.Path,
                measure.Database,
                measure.Container,
                measure.ValueKind,
            }).ToList(),
            assert = new
            {
                check.Assert.Type,
                check.Assert.Of,
                check.Assert.Left,
                check.Assert.Right,
                check.Assert.Max,
                check.Assert.Min,
                check.Assert.Warn,
                check.Assert.Tolerance,
                check.Assert.TolerancePct,
            },
        }).ToList();

    private static IReadOnlyList<object> AgentSources(IReadOnlyList<SourceCatalogSnapshot> sources) =>
        sources.Select(source =>
        {
            var isFileShare = string.Equals(source.SourceName, "fileshare", StringComparison.OrdinalIgnoreCase);
            if (isFileShare)
            {
                var files = source.Datasets.Where(dataset => dataset.Fields.Count == 0).ToList();
                var schemas = source.Datasets.Where(dataset => dataset.Fields.Count > 0).ToList();
                var referencedFiles = files.Where(dataset => dataset.Referenced)
                    .OrderBy(CatalogPath, StringComparer.OrdinalIgnoreCase)
                    .Select(dataset => new object?[]
                    {
                        CatalogPath(dataset),
                        dataset.SizeBytes,
                        dataset.ModifiedAtUtc?.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture),
                    }).ToList();
                var otherFilesByDirectory = files.Where(dataset => !dataset.Referenced)
                    .GroupBy(dataset => string.IsNullOrWhiteSpace(dataset.Namespace) ? "Root" : dataset.Namespace!, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new object?[]
                    {
                        group.Key,
                        group.Count(),
                        group.OrderBy(dataset => dataset.Name, StringComparer.OrdinalIgnoreCase)
                            .Take(5)
                            .Select(dataset => dataset.Name)
                            .ToList(),
                    }).ToList();
                var declaredSchemas = schemas.OrderBy(CatalogPath, StringComparer.OrdinalIgnoreCase)
                    .Select(dataset => new object?[]
                    {
                        CatalogPath(dataset),
                        dataset.Detail,
                        dataset.Fields.Select(FieldRow).ToList(),
                    }).ToList();

                return (object)new
                {
                    source = source.SourceName,
                    source.Provider,
                    source.DiscoveredAtUtc,
                    source.Truncated,
                    source.Error,
                    catalog = new
                    {
                        discoveredFileCount = files.Count,
                        declaredSchemaCount = schemas.Count,
                        excludedTopLevelFolders = source.ExcludedTopLevelFolders,
                        referencedFiles = new AgentTable(["path", "bytes", "modifiedAtUtc"], referencedFiles),
                        declaredSchemas = new AgentTable(["path", "detail", "fields[name,type,nullable]"], declaredSchemas),
                        otherFilesByDirectory = new AgentTable(["directory", "fileCount", "examples"], otherFilesByDirectory),
                    },
                };
            }

            var datasets = source.Datasets
                .OrderBy(dataset => dataset.Namespace, StringComparer.OrdinalIgnoreCase)
                .ThenBy(dataset => dataset.Name, StringComparer.OrdinalIgnoreCase)
                .Select(dataset => new object?[]
                {
                    dataset.Namespace,
                    dataset.Name,
                    dataset.Kind,
                    dataset.Detail,
                    dataset.Referenced,
                    dataset.Fields.Select(FieldRow).ToList(),
                }).ToList();
            return (object)new
            {
                source = source.SourceName,
                source.Provider,
                source.DiscoveredAtUtc,
                source.Truncated,
                source.Error,
                catalog = new
                {
                    datasetCount = source.Datasets.Count,
                    datasets = new AgentTable(
                        ["namespace", "name", "kind", "detail", "referenced", "fields[name,type,nullable]"],
                        datasets),
                },
            };
        }).ToList();

    private static object?[] FieldRow(CatalogField field) => [field.Name, field.Type, field.Nullable];

    private static string CatalogPath(CatalogDataset dataset)
    {
        var name = dataset.Name.Replace('\\', '/');
        if (name.Contains('/')) return name;
        var directory = dataset.Namespace?.Replace('\\', '/').Trim('/');
        return string.IsNullOrWhiteSpace(directory) || string.Equals(directory, "referenced", StringComparison.OrdinalIgnoreCase)
            ? name
            : $"{directory}/{name}";
    }

    private sealed record AgentTable(IReadOnlyList<string> Columns, IReadOnlyList<object?[]> Rows);

    private static void AppendValidation(StringBuilder sb, IReadOnlyList<CheckDiagnostic> diagnostics)
    {
        sb.Append("<section id=\"validation\" class=\"authoring-section\"><h2 class=\"authoring-title\">Validation</h2><p class=\"authoring-muted mb-2\">Strict authoring checks run separately from the backward-compatible runtime loader.</p>");
        if (diagnostics.Count == 0)
        {
            sb.Append("<div class=\"authoring-card authoring-ok\"><span class=\"text-success font-semibold\">Valid</span> — no structural or semantic problems found.</div>");
        }
        else
        {
            foreach (var diagnostic in diagnostics)
            {
                var location = diagnostic.Line is null ? "" : $" · line {diagnostic.Line}:{diagnostic.Column}";
                sb.Append("<div class=\"authoring-card authoring-error mb-2\"><div class=\"flex flex-wrap items-center gap-2\"><span class=\"bg-error text-error-content rounded-selector px-1.5 text-[10px] font-bold\">")
                  .Append(diagnostic.Severity).Append("</span><code class=\"ident text-[11px]\">").Append(E(diagnostic.Path)).Append("</code><span class=\"authoring-muted\">")
                  .Append(E(diagnostic.Code + location)).Append("</span></div><p class=\"mt-1 text-xs\">").Append(E(diagnostic.Message)).Append("</p></div>");
            }
        }
        sb.Append("</section>");
    }

    private static void AppendAgentWorkflow(StringBuilder sb, AuthoringCatalog catalog)
    {
        sb.Append("<section id=\"agent-workflow\" class=\"authoring-section\"><h2 class=\"authoring-title\">Agent-assisted authoring</h2>")
          .Append("<p class=\"authoring-muted mb-2\">Download one self-contained context file, attach it to a fresh agent, then describe the check change you want.</p>")
          .Append("<div class=\"authoring-card authoring-agent\"><div class=\"flex flex-wrap items-center gap-2\"><strong class=\"text-sm\">Agent handoff</strong>")
          .Append("<button type=\"button\" data-download-agent-context class=\"btn btn-xs btn-ghost border-base-300 ml-auto border font-normal\">Download agent context</button></div>")
          .Append("<p class=\"authoring-muted mt-2\">The compact JSON file includes one agent contract, validation, loaded checks, the language definition, and generated metadata context. It contains no connection strings, credentials, or record values.</p></div></section>");
    }

    private static void AppendPack(
        StringBuilder sb,
        IReadOnlyList<CheckDefinition> checks,
        DashboardOptions options)
    {
        var labels = new Labeler(options);
        var categories = checks
            .GroupBy(c => CategoryKey(c.Category), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => CategoryRank(g.Key, options))
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        sb.Append("<section id=\"pack\" class=\"authoring-section\"><h2 class=\"authoring-title\">Loaded check pack</h2><p class=\"authoring-muted mb-2\">Drill down by the same category and name-family used by the dashboard. Each leaf contains the exact current YAML.</p><div class=\"authoring-stack\">");
        foreach (var category in categories)
        {
            var families = category
                .GroupBy(c => FamilyKey(c.Name), StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Min(c => c.Order ?? int.MaxValue))
                .ThenBy(g => labels.FamilyTail(category.Key, g.Key) ?? labels.CategoryName(category.Key), StringComparer.OrdinalIgnoreCase)
                .ToList();
            sb.Append("<details class=\"authoring-group\" data-authoring-group><summary><span class=\"bg-accent h-3 w-0.5 shrink-0 rounded-full\"></span><strong class=\"text-sm\">")
              .Append(E(labels.CategoryName(category.Key))).Append("</strong><span class=\"authoring-pill\">").Append(category.Count()).Append(" checks</span><span class=\"authoring-caret ml-auto text-base-content/40\">▾</span></summary><div class=\"authoring-group-body authoring-stack\">");

            foreach (var family in families)
            {
                var familyLabel = labels.FamilyTail(category.Key, family.Key) ?? labels.CategoryName(category.Key);
                sb.Append("<details class=\"authoring-family\" data-authoring-group><summary><strong class=\"text-xs\">")
                  .Append(E(familyLabel)).Append("</strong><span class=\"authoring-pill\">").Append(family.Count()).Append("</span><span class=\"authoring-caret ml-auto text-base-content/40\">▾</span></summary><div class=\"authoring-family-body authoring-grid\">");
                foreach (var check in family.OrderBy(c => c.Order ?? int.MaxValue).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                    AppendCheck(sb, check);
                sb.Append("</div></details>");
            }
            sb.Append("</div></details>");
        }
        sb.Append("</div></section>");
    }

    private static void AppendCheck(StringBuilder sb, CheckDefinition check)
    {
        var search = CheckSearch(check);
        sb.Append("<details class=\"authoring-card\" data-authoring-item data-authoring-search=\"").Append(E(search)).Append("\"><summary><div class=\"flex items-start gap-2\"><span class=\"bg-accent mt-1 h-3 w-0.5 shrink-0 rounded-full\"></span><div class=\"min-w-0\"><div class=\"text-xs font-semibold\">")
          .Append(E(check.Name)).Append("</div><div class=\"authoring-muted mt-1\">").Append(E($"{check.Domain} · {check.Category} · {check.Severity} · {check.Measures.Count} measure(s)"))
          .Append("</div></div><span class=\"authoring-caret ml-auto text-base-content/40\">▾</span></div></summary>")
          .Append("<p class=\"authoring-muted my-2\">").Append(E(check.Description ?? "No description.")).Append("</p><code class=\"authoring-code\">")
          .Append(E(ToYaml(check))).Append("</code></details>");
    }

    private static void AppendSources(StringBuilder sb, IReadOnlyList<SourceCatalogSnapshot> sources)
    {
        sb.Append("<section id=\"sources\" class=\"authoring-section\"><h2 class=\"authoring-title\">Registered sources</h2><p class=\"authoring-muted mb-2\">Open a source, then a schema, database, or directory. Only bounded metadata is shown.</p><div class=\"authoring-stack\">");
        foreach (var source in sources)
        {
            var sourceSearch = string.Join(' ', source.SourceName, source.Provider, source.Note, source.Error,
                string.Join(' ', source.ExcludedTopLevelFolders)).ToLowerInvariant();
            sb.Append("<details class=\"authoring-group").Append(source.Error is null ? "" : " authoring-error").Append("\" data-authoring-group").Append(source.Error is null ? "" : " open").Append("><summary><h3 class=\"text-sm font-semibold\">").Append(E(source.SourceName)).Append("</h3><span class=\"authoring-pill\">").Append(E(source.Provider)).Append("</span>")
              .Append(source.Error is not null ? (source.FromCache ? "<span class=\"authoring-pill\">cached failure</span>" : "<span class=\"authoring-pill\">failed</span>") : source.FromCache ? "<span class=\"authoring-pill\">cached</span>" : "<span class=\"authoring-pill\">fresh</span>")
              .Append(source.Truncated ? "<span class=\"authoring-pill\">bounded</span>" : "")
              .Append("<span class=\"authoring-pill\">").Append(source.Datasets.Count).Append(" datasets</span><span class=\"authoring-caret ml-auto text-base-content/40\">▾</span></summary><div class=\"authoring-group-body\">")
              .Append("<div data-authoring-item data-authoring-search=\"").Append(E(sourceSearch)).Append("\"><p class=\"authoring-muted\">Discovered ").Append(source.DiscoveredAtUtc.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss")).Append(" UTC</p>");
            if (source.Error is not null)
                sb.Append("<p class=\"text-error mt-2 text-xs\">").Append(E(source.Error)).Append("</p>");
            if (source.Note is not null)
                sb.Append("<p class=\"authoring-muted mt-2\">").Append(E(source.Note)).Append("</p>");
            if (source.ExcludedTopLevelFolders.Count > 0)
            {
                sb.Append("<p class=\"authoring-muted mt-2\">Excluded from broad inventory (explicit pack references still win): ");
                foreach (var folder in source.ExcludedTopLevelFolders)
                    sb.Append("<code class=\"authoring-field ml-1\">").Append(E(folder)).Append("</code>");
                sb.Append("</p>");
            }
            sb.Append("</div>");

            if (source.Datasets.Count > 0)
            {
                sb.Append("<div class=\"authoring-stack mt-2\">");
                foreach (var group in source.Datasets.GroupBy(DatasetGroup, StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
                {
                    sb.Append("<details class=\"authoring-family\" data-authoring-group><summary><strong class=\"text-xs\">").Append(E(group.Key)).Append("</strong><span class=\"authoring-pill\">").Append(group.Count()).Append("</span><span class=\"authoring-caret ml-auto text-base-content/40\">▾</span></summary><div class=\"authoring-family-body\">");
                    foreach (var dataset in group.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        var search = string.Join(' ', source.SourceName, source.Provider, group.Key, dataset.Name, dataset.Kind, dataset.Detail,
                            string.Join(' ', dataset.Fields.Select(f => f.Name + " " + f.Type))).ToLowerInvariant();
                        sb.Append("<div class=\"authoring-dataset\" data-authoring-item data-authoring-search=\"").Append(E(search)).Append("\"><div class=\"flex flex-wrap items-baseline gap-1\"><code class=\"ident text-[11px] font-semibold\">")
                          .Append(E(dataset.Name)).Append("</code><span class=\"authoring-pill\">").Append(E(dataset.Kind)).Append("</span>")
                          .Append(dataset.Referenced ? "<span class=\"authoring-pill\">used by pack</span>" : "").Append("</div>");
                        if (dataset.Detail is not null) sb.Append("<p class=\"authoring-muted mt-1\">").Append(E(dataset.Detail)).Append("</p>");
                        if (dataset.Fields.Count > 0)
                        {
                            sb.Append("<div class=\"authoring-fields\">");
                            foreach (var field in dataset.Fields)
                                sb.Append("<span class=\"authoring-field\">").Append(E(field.Name)).Append(" <span class=\"text-base-content/45\">").Append(E(field.Type)).Append(field.Nullable == true ? "?" : "").Append("</span></span>");
                            sb.Append("</div>");
                        }
                        sb.Append("</div>");
                    }
                    sb.Append("</div></details>");
                }
                sb.Append("</div>");
            }
            if (source.Datasets.Count == 0 && source.Error is null)
                sb.Append("<p class=\"authoring-muted mt-2\">No datasets were returned.</p>");
            sb.Append("</div></details>");
        }
        sb.Append("</div></section>");
    }

    private static string CategoryKey(string? category) =>
        string.IsNullOrWhiteSpace(category) ? "other" : category.Trim().ToLowerInvariant();

    private static int CategoryRank(string category, DashboardOptions options)
    {
        if (options.CategoryOrder is { } configured)
        {
            for (var i = 0; i < configured.Count; i++)
                if (string.Equals(configured[i], category, StringComparison.OrdinalIgnoreCase)) return i;
            return configured.Count + BuiltInCategoryRank(category);
        }
        return BuiltInCategoryRank(category);
    }

    private static int BuiltInCategoryRank(string category) =>
        Array.FindIndex(new[] { "freshness", "reconciliation", "quality", "volume", "flow" },
            value => string.Equals(value, category, StringComparison.OrdinalIgnoreCase)) is var index && index >= 0 ? index : 99;

    private static string FamilyKey(string name)
    {
        var separator = name.LastIndexOf('.');
        return separator <= 0 ? "" : name[..separator];
    }

    private static string CheckSearch(CheckDefinition check) =>
        string.Join(' ', check.Name, check.Domain, check.Category, check.Severity, check.Description,
            string.Join(' ', check.Measures.Select(m => $"{m.Key} {m.Source} {m.Database} {m.Container} {m.Path} {m.Sql}"))).ToLowerInvariant();

    private static string DatasetGroup(CatalogDataset dataset) =>
        !string.IsNullOrWhiteSpace(dataset.Namespace) ? dataset.Namespace! :
        string.Equals(dataset.Kind, "database", StringComparison.OrdinalIgnoreCase) ? "Databases" : "Root";

    private static void AppendLanguage(StringBuilder sb)
    {
        sb.Append("<section id=\"language\" class=\"authoring-section\"><h2 class=\"authoring-title\">YAML language</h2><p class=\"authoring-muted mb-2\">Scalar queries return a column named <code>v</code>. When <code>breakdown</code> is set, every measure returns <code>k</code> and <code>v</code>. Durations accept positive decimals plus s, m, h, or d (for example 30s, 90m, 26h, 2d).</p><div class=\"authoring-card overflow-x-auto\"><table class=\"authoring-table\"><thead><tr><th>Scope</th><th>Field</th><th>Type</th><th>Default</th><th>Required</th><th>Meaning</th></tr></thead><tbody>");
        foreach (var field in CheckLanguage.Fields)
            sb.Append("<tr><td>").Append(E(field.Scope)).Append("</td><td><code>").Append(E(field.Name)).Append("</code></td><td>").Append(E(field.Type)).Append("</td><td>").Append(E(field.Default)).Append("</td><td>").Append(E(field.RequiredWhen)).Append("</td><td>").Append(E(field.Description)).Append("</td></tr>");
        sb.Append("</tbody></table></div></section>");
    }

    private static string Summary(string label, string value, string note) =>
        $"<div class=\"authoring-card\"><p class=\"eyebrow text-base-content/45 text-[8px]\">{E(label)}</p><p class=\"mt-1 text-lg font-bold\">{E(value)}</p><p class=\"authoring-muted\">{E(note)}</p></div>";

    private static string ToYaml(CheckDefinition check)
    {
        var sb = new StringBuilder();
        Line("name", check.Name); Line("domain", check.Domain); Line("category", check.Category);
        if (!string.Equals(check.Severity, "warning", StringComparison.OrdinalIgnoreCase)) Line("severity", check.Severity);
        if (!string.IsNullOrWhiteSpace(check.Description)) Line("description", check.Description!);
        if (check.Order is not null) sb.Append("order: ").Append(check.Order).AppendLine();
        if (!string.IsNullOrWhiteSpace(check.Breakdown)) Line("breakdown", check.Breakdown!);
        sb.AppendLine("measures:");
        foreach (var measure in check.Measures)
        {
            sb.Append("  - key: ").AppendLine(Quote(measure.Key));
            sb.Append("    source: ").AppendLine(Quote(measure.Source));
            if (!string.Equals(measure.ValueKind, "number", StringComparison.OrdinalIgnoreCase)) sb.Append("    valueKind: ").AppendLine(Quote(measure.ValueKind));
            Property("sql", measure.Sql); Property("path", measure.Path); Property("database", measure.Database); Property("container", measure.Container);
        }
        sb.Append("assert:").AppendLine();
        sb.Append("  type: ").AppendLine(Quote(check.Assert.Type));
        AssertProperty("of", check.Assert.Of); AssertProperty("left", check.Assert.Left); AssertProperty("right", check.Assert.Right);
        AssertProperty("max", check.Assert.Max); AssertProperty("min", check.Assert.Min); AssertProperty("warn", check.Assert.Warn);
        if (check.Assert.Tolerance is not null) sb.Append("  tolerance: ").Append(check.Assert.Tolerance.Value.ToString(CultureInfo.InvariantCulture)).AppendLine();
        if (check.Assert.TolerancePct is not null) sb.Append("  tolerancePct: ").Append(check.Assert.TolerancePct.Value.ToString(CultureInfo.InvariantCulture)).AppendLine();
        return sb.ToString().TrimEnd();

        void Line(string key, string value) => sb.Append(key).Append(": ").AppendLine(Quote(value));
        void Property(string key, string? value) { if (value is not null) sb.Append("    ").Append(key).Append(": ").AppendLine(Quote(value)); }
        void AssertProperty(string key, string? value) { if (value is not null) sb.Append("  ").Append(key).Append(": ").AppendLine(Quote(value)); }
    }

    private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
    private static string WithRefresh(string href) => href + (href.Contains('?') ? "&" : "?") + "refresh=true";
    private static string FormatDuration(TimeSpan value) => value.TotalMinutes >= 1 ? $"{value.TotalMinutes:0.#} minutes" : $"{value.TotalSeconds:0.#} seconds";
    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
}
