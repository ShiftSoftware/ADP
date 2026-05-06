using ShiftSoftware.ADP.Lookup.Services.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ShiftSoftware.ADP.Lookup.Services.Diagnostics;

/// <summary>
/// Renders <see cref="ServiceItemTrace"/> in formats suitable for humans rather than
/// library developers: a vertical Mermaid pipeline labelled in dealer-facing language,
/// a Mermaid Gantt timeline of free-service expiry, a Markdown report, and a fully
/// self-contained HTML page (Mermaid loaded from CDN).
/// </summary>
public static class ServiceItemTraceRenderer
{
    public static string ToMermaid(ServiceItemTrace t) => RenderPipeline(t);
    public static string ToTimelineMermaid(ServiceItemTrace t) => RenderTimeline(t);
    public static string ToFunnelMermaid(ServiceItemTrace t) => RenderFunnel(t);
    public static string ToMarkdown(ServiceItemTrace t) => RenderMarkdown(t);
    public static string ToHtml(ServiceItemTrace t) => RenderHtml(t);

    // ---- Pipeline (vertical) ----

    static string RenderPipeline(ServiceItemTrace t)
    {
        if (t is null) return string.Empty;

        var inputs = t.Inputs ?? new ServiceItemTraceInputs();
        var elig = t.Eligibility ?? new ServiceItemTraceEligibility();
        var pp = t.PostProcessing ?? new ServiceItemTracePostProcessing();
        var fr = t.FinalResult ?? new ServiceItemTraceFinalResult();
        var w = t.WarrantyRollingExpansion ?? new ServiceItemTraceWarrantyRollingExpansion();
        var paidLines = inputs.AggregateCounts?.PaidServiceInvoiceLines ?? 0;

        var rejByStage = (elig.Decisions ?? new List<ServiceItemEligibilityDecision>())
            .Where(d => d.Verdict == EligibilityVerdict.Rejected)
            .GroupBy(d => d.RejectionStage)
            .ToDictionary(g => g.Key, g => g.Count());

        var statusBreakdown = (t.Statuses ?? new List<ServiceItemTraceStatus>())
            .GroupBy(s => s.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var sb = new StringBuilder();
        sb.AppendLine("flowchart TB");

        // Vehicle
        var brand = NameOrId("Brand", inputs.BrandID, t.ResolvedNames?.Brands);
        var dealer = NameOrId("Dealer", inputs.CompanyID, t.ResolvedNames?.Companies);
        var country = NameOrId("Country", inputs.SaleCountryID, t.ResolvedNames?.Countries);
        sb.AppendLine($"  Vehicle[\"🚗 <b>Vehicle</b><br/>VIN: {Esc(inputs.Vin)}<br/>{brand}<br/>{dealer}<br/>{country}<br/>Free service starts: {Date(inputs.FreeServiceStartDate)}\"]:::vehicle");

        // Catalog
        sb.AppendLine($"  Catalog[\"📋 <b>Service-item catalog</b><br/>{elig.InputCount} items defined\"]:::stage");
        sb.AppendLine("  Vehicle --> Catalog");

        // Filter (collapsed into one node) → split eligible / not eligible
        sb.AppendLine($"  Filter[\"🔍 <b>Checking eligibility</b><br/>brand · dealer · country · campaign window · vehicle model\"]:::stage");
        sb.AppendLine("  Catalog --> Filter");

        sb.AppendLine($"  Eligible[\"✅ <b>Eligible for this vehicle</b><br/>{elig.AcceptedCount} items\"]:::ok");
        sb.AppendLine($"  Skipped[\"⏭ <b>Not eligible</b><br/>{elig.RejectedCount} items skipped\"]:::skipped");
        sb.AppendLine("  Filter --> Eligible");
        sb.AppendLine("  Filter -.-> Skipped");

        if (rejByStage.Count > 0)
        {
            var lines = string.Join("<br/>", rejByStage.OrderByDescending(x => x.Value)
                .Select(kv => $"{FriendlyRejection(kv.Key)}: {kv.Value}"));
            sb.AppendLine($"  Why[\"📊 <b>Why items were skipped</b><br/>{lines}\"]:::note");
            sb.AppendLine("  Skipped -.-> Why");
        }

        // Free + paid feed Prepared
        var freeBuilt = t.FreeBuilds?.Count ?? 0;
        sb.AppendLine($"  Prepared[\"🛠 <b>Items prepared</b><br/>{freeBuilt} free · {(t.PaidBuilds?.Count ?? 0)} paid\"]:::stage");
        sb.AppendLine("  Eligible --> Prepared");
        if (paidLines > 0)
        {
            sb.AppendLine($"  Paid[\"💳 <b>Paid services from invoices</b><br/>{inputs.AggregateCounts?.PaidServiceInvoices} invoice(s) · {paidLines} line(s)\"]:::stage");
            sb.AppendLine("  Paid --> Prepared");
        }

        // Expiry
        var wseq = w.SequentialItems?.Count ?? 0;
        var wnon = w.NonSequentialItems?.Count ?? 0;
        var anchor = w.Skipped
            ? $"skipped — {Esc(w.SkippedReason)}"
            : $"anchor: {Date(w.AnchorDate)}";
        sb.AppendLine($"  Expiry[\"⏰ <b>Calculating activation &amp; expiry</b><br/>{anchor}<br/>sequential: {wseq} · non-sequential: {wnon}\"]:::stage");
        sb.AppendLine("  Prepared --> Expiry");

        // Vehicle inspection
        var viItems = t.VehicleInspectionExpansions?.Count ?? 0;
        var viOut = t.VehicleInspectionExpansions?.Sum(x => x.SelectedTriggerCount) ?? 0;
        if (viItems > 0)
        {
            sb.AppendLine($"  Insp[\"🔧 <b>Adding items per vehicle inspection</b><br/>{viItems} item(s) · {viOut} occurrence(s) added\"]:::stage");
            sb.AppendLine("  Expiry --> Insp");
            sb.AppendLine("  Insp --> Manual");
        }
        else
        {
            sb.AppendLine("  Expiry --> Manual");
        }

        // Manual VIN entry
        var mvItems = t.ManualVinEntryExpansions?.Count ?? 0;
        var mvOut = t.ManualVinEntryExpansions?.Sum(x => x.SelectedTriggerCount) ?? 0;
        if (mvItems > 0)
            sb.AppendLine($"  Manual[\"📝 <b>Adding items per manual VIN entry</b><br/>{mvItems} item(s) · {mvOut} occurrence(s) added\"]:::stage");
        else
            sb.AppendLine("  Manual[\"📝 <b>Manual VIN entries</b><br/>none for this vehicle\"]:::stage");

        // Status breakdown
        var statusLines = statusBreakdown.Count > 0
            ? string.Join(" · ", statusBreakdown.Select(kv => $"{FriendlyStatus(kv.Key)}: {kv.Value}"))
            : "—";
        sb.AppendLine($"  Status[\"🏷 <b>Determining each item&#39;s status</b><br/>{statusLines}\"]:::stage");
        sb.AppendLine("  Manual --> Status");

        // Cleanup
        var cancelLine = (pp.DynamicallyCancelled?.Count ?? 0) > 0
            ? $"<br/>cancelled (superseded by claimed): {pp.DynamicallyCancelled.Count}"
            : "";
        var pickedLine = pp.IneligibleItemsPickedUp > 0
            ? $"<br/>already-claimed items added back: {pp.IneligibleItemsPickedUp}"
            : "";
        var vinExclLine = pp.VinExclusionApplied
            ? $"this VIN is excluded — removed {pp.RemovedByVinExclusion} warranty-activated item(s)"
            : "no VIN exclusions applied";
        sb.AppendLine($"  Cleanup[\"🧹 <b>Final cleanup</b><br/>{vinExclLine}{pickedLine}{cancelLine}\"]:::stage");
        sb.AppendLine("  Status --> Cleanup");

        // Final
        var actLine = fr.ActivationRequired ? "yes — warranty activation needed" : "no — ready to claim";
        sb.AppendLine($"  Final[\"🎯 <b>Final list shown to dealer</b><br/>{fr.Count} service item(s)<br/>activation required: {actLine}\"]:::final");
        sb.AppendLine("  Cleanup --> Final");

        // Notes (issue callouts) shown beside Cleanup
        if (t.Notes != null && t.Notes.Count > 0)
        {
            var noteText = string.Join("<br/>", t.Notes.Select(n => "⚠ " + Esc(n)));
            sb.AppendLine($"  Notes[\"<b>Things to check</b><br/>{noteText}\"]:::note");
            sb.AppendLine("  Cleanup -.-> Notes");
        }

        sb.AppendLine("  classDef vehicle fill:#eef6ff,stroke:#3a6f9a,stroke-width:2px");
        sb.AppendLine("  classDef stage fill:#ffffff,stroke:#666,stroke-width:1.5px");
        sb.AppendLine("  classDef ok fill:#cfe9c4,stroke:#5a8f4a,stroke-width:2px");
        sb.AppendLine("  classDef skipped fill:#f7c4c4,stroke:#a84444,stroke-width:1.5px");
        sb.AppendLine("  classDef note fill:#fff3b0,stroke:#b89a1f,stroke-dasharray: 5 3");
        sb.AppendLine("  classDef final fill:#a8d0e6,stroke:#3a6f9a,stroke-width:3px");

        return sb.ToString();
    }

    // ---- Gantt timeline of free-service expiry ----

    static string RenderTimeline(ServiceItemTrace t)
    {
        if (t is null) return string.Empty;
        var w = t.WarrantyRollingExpansion;
        var insp = t.VehicleInspectionExpansions ?? new List<ServiceItemTraceTriggerExpansion>();
        var mv = t.ManualVinEntryExpansions ?? new List<ServiceItemTraceTriggerExpansion>();

        var rows = new List<(string Section, string Label, DateTime Start, DateTime End)>();

        if (w?.SequentialItems != null)
            foreach (var r in w.SequentialItems)
                if (r.ExpiresAt is DateTime end)
                    rows.Add(("Warranty (sequential)", IdName(r.ServiceItemID, Truncate(r.Name, 40)), r.ActivatedAt, end));

        if (w?.NonSequentialItems != null)
            foreach (var r in w.NonSequentialItems)
                if (r.ExpiresAt is DateTime end)
                    rows.Add(("Warranty (non-sequential)", IdName(r.ServiceItemID, Truncate(r.Name, 40)), r.ActivatedAt, end));

        foreach (var e in insp)
            foreach (var o in e.Outputs ?? new List<ServiceItemTraceTriggerOutput>())
                if (o.ActivatedAt is DateTime act && o.ExpiresAt is DateTime end)
                    rows.Add(("Vehicle inspection", IdName(e.ServiceItemID, Truncate(e.Name, 40)), act, end));

        foreach (var e in mv)
            foreach (var o in e.Outputs ?? new List<ServiceItemTraceTriggerOutput>())
                if (o.ActivatedAt is DateTime act && o.ExpiresAt is DateTime end)
                    rows.Add(("Manual VIN entry", IdName(e.ServiceItemID, Truncate(e.Name, 40)), act, end));

        if (rows.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("gantt");
        sb.AppendLine("    title Free-service activation & expiry timeline");
        sb.AppendLine("    dateFormat YYYY-MM-DD");
        sb.AppendLine("    axisFormat %Y-%m");
        foreach (var section in rows.GroupBy(r => r.Section))
        {
            sb.AppendLine($"    section {section.Key}");
            foreach (var (_, label, start, end) in section)
            {
                var safeStart = start == default ? end : start;
                if (safeStart > end) safeStart = end; // Mermaid rejects reversed ranges
                if (safeStart == end) safeStart = end.AddDays(-1); // Mermaid rejects zero-duration; flag elsewhere via the issue #14 note
                sb.AppendLine($"    {GanttSafe(label)} :{safeStart:yyyy-MM-dd}, {end:yyyy-MM-dd}");
            }
        }
        return sb.ToString();
    }

    // ---- Sankey funnel ----

    /// <summary>
    /// Mermaid sankey-beta diagram showing where catalog items flow (or get dropped) on
    /// their way to the final list. Edge widths are proportional, so a wide red edge
    /// from "Catalog" to "Different country" reveals the actual bottleneck visually.
    /// </summary>
    static string RenderFunnel(ServiceItemTrace t)
    {
        if (t is null) return string.Empty;

        var elig = t.Eligibility ?? new ServiceItemTraceEligibility();
        var pp = t.PostProcessing ?? new ServiceItemTracePostProcessing();
        var paid = t.PaidBuilds?.Count ?? 0;

        var rejByStage = (elig.Decisions ?? new List<ServiceItemEligibilityDecision>())
            .Where(d => d.Verdict == EligibilityVerdict.Rejected)
            .GroupBy(d => d.RejectionStage)
            .ToDictionary(g => g.Key, g => g.Count());

        var statusBreakdown = (t.Statuses ?? new List<ServiceItemTraceStatus>())
            .GroupBy(s => s.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // sankey-beta drops zero-value edges; skip them explicitly so Mermaid layout stays clean.
        var lines = new List<string>();
        void Edge(string from, string to, int value) { if (value > 0) lines.Add($"{Csv(from)},{Csv(to)},{value}"); }

        // Stage 1: catalog fans out
        Edge("Catalog", "Eligible", elig.AcceptedCount);
        foreach (var kv in rejByStage.OrderByDescending(x => x.Value))
            Edge("Catalog", FriendlyRejection(kv.Key), kv.Value);

        // Stage 2: eligible flows into status buckets. Inspection / VIN-entry expansion can
        // multiply the count here (one eligible item → several activated DTOs), so the
        // total status flow may exceed Eligible's incoming flow. sankey-beta sizes nodes by
        // max(in,out) and tolerates this; we accept the visual non-conservation in exchange
        // for a flatter, less crowded diagram. Cancelled is terminal (cleanup sink).
        foreach (var kv in statusBreakdown.OrderByDescending(x => x.Value))
            Edge("Eligible", StatusFunnelLabel(kv.Key), kv.Value);

        // Stage 3: non-cancelled status buckets → Final list
        foreach (var kv in statusBreakdown)
        {
            if (kv.Key == VehcileServiceItemStatuses.Cancelled) continue;
            Edge(StatusFunnelLabel(kv.Key), "Final list", kv.Value);
        }

        // Side injections that bypass the catalog/eligibility funnel
        Edge("Paid invoices", "Final list", paid);
        Edge("Re-added (already claimed)", "Final list", pp.IneligibleItemsPickedUp);

        // Cleanup sink
        Edge("Final list", "Removed by VIN exclusion", pp.RemovedByVinExclusion);

        if (lines.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("sankey-beta");
        sb.AppendLine();
        foreach (var l in lines) sb.AppendLine(l);
        return sb.ToString();
    }

    static string StatusFunnelLabel(VehcileServiceItemStatuses s) => s switch
    {
        VehcileServiceItemStatuses.Pending => "Pending",
        VehcileServiceItemStatuses.Processed => "Processed",
        VehcileServiceItemStatuses.Expired => "Expired",
        VehcileServiceItemStatuses.ActivationRequired => "Activation required",
        VehcileServiceItemStatuses.Cancelled => "Cancelled",
        _ => s.ToString(),
    };

    static string Csv(string s) =>
        s.Contains(',') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

    // ---- Markdown ----

    static string RenderMarkdown(ServiceItemTrace t)
    {
        if (t is null) return string.Empty;
        var sb = new StringBuilder();
        sb.AppendLine($"# Service-item lookup trace — VIN {t.Vin}");
        sb.AppendLine();
        sb.AppendLine($"_Started {t.StartedUtc:O}, finished {t.FinishedUtc:O}, elapsed {t.Elapsed.TotalMilliseconds:F1} ms_");
        sb.AppendLine();

        if (t.Notes?.Count > 0)
        {
            sb.AppendLine("## ⚠ Things to check");
            foreach (var n in t.Notes) sb.AppendLine($"- {n}");
            sb.AppendLine();
        }

        AppendInputs(sb, t);
        AppendEligibility(sb, t);
        AppendBuilds(sb, t.FreeBuilds, t.PaidBuilds);
        AppendExpansions(sb, t);
        AppendStatuses(sb, t.Statuses);
        AppendPostProcessing(sb, t.PostProcessing);
        AppendFinal(sb, t.FinalResult);
        AppendStageTimings(sb, t.StageTimings);

        return sb.ToString();
    }

    // ---- HTML ----

    static string RenderHtml(ServiceItemTrace t)
    {
        if (t is null) return "<html><body><p>No trace.</p></body></html>";

        var pipeline = RenderPipeline(t);
        var funnel = RenderFunnel(t);
        var timeline = RenderTimeline(t);
        var markdown = RenderMarkdown(t);

        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html><head><meta charset=\"utf-8\">");
        sb.AppendLine($"<title>Service-item lookup trace — {HtmlEsc(t.Vin)}</title>");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js\"></script>");
        sb.AppendLine("<script>mermaid.initialize({ startOnLoad: true, securityLevel: 'loose', maxTextSize: 500000, flowchart: { htmlLabels: true, useMaxWidth: false } });</script>");
        sb.AppendLine("<style>");
        sb.AppendLine("  :root { --ok: #4a8f4a; --bad: #a84444; --warn: #b89a1f; --ink: #1d2735; --muted: #5a6b7a; }");
        sb.AppendLine("  body { font-family: -apple-system, Segoe UI, sans-serif; max-width: 1500px; margin: 0 auto; padding: 1.5rem; color: var(--ink); background: #fafbfc; }");
        sb.AppendLine("  h1 { font-size: 1.5rem; margin: 0 0 0.25rem; }");
        sb.AppendLine("  h2 { font-size: 1.1rem; margin-top: 2.2rem; border-bottom: 1px solid #d6dde4; padding-bottom: 0.3rem; }");
        sb.AppendLine("  .summary { display: flex; gap: 0.75rem; flex-wrap: wrap; margin: 0.75rem 0 1.5rem; }");
        sb.AppendLine("  .pill { background: #fff; border: 1px solid #d6dde4; border-radius: 6px; padding: 0.5rem 0.75rem; font-size: 0.9rem; }");
        sb.AppendLine("  .pill b { display: block; font-size: 1.4rem; line-height: 1.2; }");
        sb.AppendLine("  .pill.ok b { color: var(--ok); }");
        sb.AppendLine("  .pill.bad b { color: var(--bad); }");
        sb.AppendLine("  .pill.warn b { color: var(--warn); }");
        sb.AppendLine("  .mermaid { background: #fff; border: 1px solid #d6dde4; border-radius: 8px; padding: 1rem; margin: 1rem 0; text-align: center; overflow-x: auto; }");
        sb.AppendLine("  .note { background: #fff8d6; border-left: 4px solid var(--warn); padding: 0.6rem 0.9rem; margin: 0.4rem 0; border-radius: 4px; }");
        sb.AppendLine("  table { border-collapse: collapse; width: 100%; font-size: 0.88rem; margin: 0.5rem 0; background: #fff; }");
        sb.AppendLine("  th, td { border: 1px solid #d6dde4; padding: 6px 10px; text-align: left; vertical-align: top; }");
        sb.AppendLine("  th { background: #eef2f5; font-weight: 600; }");
        sb.AppendLine("  tr.rejected { background: #fdf0f0; }");
        sb.AppendLine("  tr.accepted { background: #f1f8ec; }");
        sb.AppendLine("  details { margin: 0.5rem 0; }");
        sb.AppendLine("  summary { cursor: pointer; font-weight: 600; padding: 0.4rem 0; }");
        sb.AppendLine("  pre { background: #f5f5f5; padding: 0.6rem; overflow-x: auto; font-size: 0.8rem; border-radius: 4px; }");
        sb.AppendLine("  .muted { color: var(--muted); font-size: 0.85rem; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");

        sb.AppendLine($"<h1>Service-item lookup — VIN {HtmlEsc(t.Vin)}</h1>");
        sb.AppendLine($"<div class=\"muted\">Traced {t.StartedUtc:yyyy-MM-dd HH:mm:ss} UTC · elapsed {t.Elapsed.TotalMilliseconds:F1} ms</div>");

        AppendHtmlSummaryPills(sb, t);

        if (t.Notes?.Count > 0)
        {
            sb.AppendLine("<h2>⚠ Things to check</h2>");
            foreach (var n in t.Notes)
                sb.AppendLine($"<div class=\"note\">{HtmlEsc(n)}</div>");
        }

        sb.AppendLine("<h2>Lookup pipeline</h2>");
        sb.AppendLine("<div class=\"mermaid\">");
        sb.AppendLine(HtmlEsc(pipeline));
        sb.AppendLine("</div>");

        if (!string.IsNullOrWhiteSpace(funnel))
        {
            sb.AppendLine("<h2>Where items go (proportional flow)</h2>");
            sb.AppendLine("<p class=\"muted\">Edge width is proportional to item count. Wide red edges from the catalog show where most items get filtered out; the narrower the path to <em>Final list</em>, the more attrition along the way.</p>");
            sb.AppendLine("<div class=\"mermaid\">");
            sb.AppendLine(HtmlEsc(funnel));
            sb.AppendLine("</div>");
        }

        if (!string.IsNullOrWhiteSpace(timeline))
        {
            sb.AppendLine("<h2>Activation &amp; expiry timeline</h2>");
            sb.AppendLine("<div class=\"mermaid\">");
            sb.AppendLine(HtmlEsc(timeline));
            sb.AppendLine("</div>");
        }

        sb.AppendLine("<h2>Eligibility decisions</h2>");
        sb.AppendLine(EligibilityHtmlTable(t.Eligibility, t.ResolvedNames));

        if (t.WarrantyRollingExpansion != null)
        {
            sb.AppendLine("<h2>Warranty-activated free items</h2>");
            sb.AppendLine(WarrantyRollingHtmlTable(t.WarrantyRollingExpansion));
        }

        if (t.VehicleInspectionExpansions?.Count > 0)
        {
            sb.AppendLine("<h2>Items added per vehicle inspection</h2>");
            sb.AppendLine(TriggerExpansionHtmlTable(t.VehicleInspectionExpansions));
        }

        if (t.ManualVinEntryExpansions?.Count > 0)
        {
            sb.AppendLine("<h2>Items added per manual VIN entry</h2>");
            sb.AppendLine(TriggerExpansionHtmlTable(t.ManualVinEntryExpansions));
        }

        sb.AppendLine("<h2>Status &amp; claimability</h2>");
        sb.AppendLine(StatusHtmlTable(t.Statuses));

        sb.AppendLine("<h2>Final cleanup</h2>");
        sb.AppendLine(PostProcessingHtml(t.PostProcessing));

        sb.AppendLine("<h2>Final list</h2>");
        sb.AppendLine(FinalHtmlTable(t.FinalResult));

        sb.AppendLine("<details><summary>Plain-text report</summary>");
        sb.AppendLine($"<pre>{HtmlEsc(markdown)}</pre>");
        sb.AppendLine("</details>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    static void AppendHtmlSummaryPills(StringBuilder sb, ServiceItemTrace t)
    {
        var fr = t.FinalResult ?? new ServiceItemTraceFinalResult();
        var elig = t.Eligibility ?? new ServiceItemTraceEligibility();
        var pp = t.PostProcessing ?? new ServiceItemTracePostProcessing();
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"  <div class=\"pill\"><b>{elig.InputCount}</b>Catalog items</div>");
        sb.AppendLine($"  <div class=\"pill ok\"><b>{elig.AcceptedCount}</b>Eligible</div>");
        sb.AppendLine($"  <div class=\"pill bad\"><b>{elig.RejectedCount}</b>Skipped</div>");
        sb.AppendLine($"  <div class=\"pill\"><b>{fr.Count}</b>Final list</div>");
        sb.AppendLine($"  <div class=\"pill {(fr.ActivationRequired ? "warn" : "ok")}\"><b>{(fr.ActivationRequired ? "yes" : "no")}</b>Activation needed</div>");
        if (pp.VinExclusionApplied)
            sb.AppendLine($"  <div class=\"pill warn\"><b>{pp.RemovedByVinExclusion}</b>Removed by VIN exclusion</div>");
        if ((pp.DynamicallyCancelled?.Count ?? 0) > 0)
            sb.AppendLine($"  <div class=\"pill warn\"><b>{pp.DynamicallyCancelled.Count}</b>Cancelled (superseded)</div>");
        sb.AppendLine("</div>");
    }

    // ---- Markdown helpers ----

    static void AppendInputs(StringBuilder sb, ServiceItemTrace t)
    {
        var i = t.Inputs;
        sb.AppendLine("## Vehicle being looked up");
        if (i is null) { sb.AppendLine("_(none)_"); sb.AppendLine(); return; }
        sb.AppendLine($"- VIN: **{i.Vin}**");
        sb.AppendLine($"- {NameOrIdPlain("Brand", i.BrandID, t.ResolvedNames?.Brands)}");
        sb.AppendLine($"- {NameOrIdPlain("Dealer", i.CompanyID, t.ResolvedNames?.Companies)}");
        sb.AppendLine($"- {NameOrIdPlain("Country (sale)", i.SaleCountryID, t.ResolvedNames?.Countries)}");
        sb.AppendLine($"- Katashiki: {i.Katashiki} · Variant: {i.VariantCode}");
        sb.AppendLine($"- Free service start date: {Date(i.FreeServiceStartDate)}{(i.FreeServiceStartDateOverriddenByDateShift ? $" (shifted from {Date(i.FreeServiceStartDateBeforeDateShift)})" : "")}");
        var c = i.AggregateCounts ?? new ServiceItemTraceAggregateCounts();
        sb.AppendLine($"- Data loaded: {c.CosmosServiceItems} catalog items · {c.PaidServiceInvoices} paid invoices ({c.PaidServiceInvoiceLines} lines) · {c.ItemClaims} previous claims · {c.VehicleInspections} inspections · {c.CampaignVinEntries} manual VIN entries");
        sb.AppendLine();
    }

    static void AppendEligibility(StringBuilder sb, ServiceItemTrace t)
    {
        var e = t.Eligibility;
        sb.AppendLine("## Eligibility decisions");
        if (e is null) { sb.AppendLine("_(none)_"); sb.AppendLine(); return; }
        sb.AppendLine($"Catalog total: **{e.InputCount}** · Eligible: **{e.AcceptedCount}** · Skipped: **{e.RejectedCount}**");
        sb.AppendLine();
        if (e.Decisions != null && e.Decisions.Count > 0)
        {
            sb.AppendLine("| Verdict | Service item | Why skipped |");
            sb.AppendLine("|---|---|---|");
            foreach (var d in e.Decisions)
            {
                var verdict = d.Verdict == EligibilityVerdict.Accepted ? "✅ Eligible" : $"❌ {FriendlyRejection(d.RejectionStage)}";
                sb.AppendLine($"| {verdict} | {IdNameMd(d.ServiceItemID, d.Name)} | {Esc(d.Reason)} |");
            }
            sb.AppendLine();
        }
    }

    static void AppendBuilds(StringBuilder sb, List<ServiceItemTraceBuild> free, List<ServiceItemTracePaidBuild> paid)
    {
        sb.AppendLine("## Items prepared");
        sb.AppendLine($"- Free items: {free?.Count ?? 0}");
        sb.AppendLine($"- Paid items: {paid?.Count ?? 0}");
        sb.AppendLine();
    }

    static void AppendExpansions(StringBuilder sb, ServiceItemTrace t)
    {
        sb.AppendLine("## Activation & expiry calculations");
        var w = t.WarrantyRollingExpansion;
        if (w != null)
        {
            sb.AppendLine($"### Warranty-activated — anchor {Date(w.AnchorDate)}{(w.Skipped ? $" (skipped: {w.SkippedReason})" : "")}");
            if (w.SequentialItems?.Count > 0)
            {
                sb.AppendLine("Mileage-based (rolling):");
                foreach (var r in w.SequentialItems)
                    sb.AppendLine($"  - {IdNameMd(r.ServiceItemID, r.Name)} · mileage: {r.MaximumMileage} · active for: {r.ActiveFor}{r.ActiveForDurationType} · {Date(r.ActivatedAt)} → {Date(r.ExpiresAt)}{(string.IsNullOrEmpty(r.Note) ? "" : "   " + r.Note)}");
            }
            if (w.NonSequentialItems?.Count > 0)
            {
                sb.AppendLine("Non-mileage:");
                foreach (var r in w.NonSequentialItems)
                    sb.AppendLine($"  - {IdNameMd(r.ServiceItemID, r.Name)} · {Date(r.ActivatedAt)} → {Date(r.ExpiresAt)}{(string.IsNullOrEmpty(r.Note) ? "" : "   " + r.Note)}");
            }
        }
        AppendTriggerExpansion(sb, "Vehicle inspection", t.VehicleInspectionExpansions);
        AppendTriggerExpansion(sb, "Manual VIN entry", t.ManualVinEntryExpansions);
        sb.AppendLine();
    }

    static void AppendTriggerExpansion(StringBuilder sb, string label, List<ServiceItemTraceTriggerExpansion> list)
    {
        if (list == null || list.Count == 0) return;
        sb.AppendLine($"### {label}");
        foreach (var e in list)
        {
            sb.AppendLine($"  - {IdNameMd(e.ServiceItemID, e.Name)} · activation rule: {FriendlyActivationType(e.ActivationType)} · candidates: {e.CandidateTriggerCount} → selected: {e.SelectedTriggerCount}{(string.IsNullOrEmpty(e.Note) ? "" : "   ⚠ " + e.Note)}");
            foreach (var o in e.Outputs ?? new List<ServiceItemTraceTriggerOutput>())
                sb.AppendLine($"      → trigger {o.TriggerID} on {Date(o.TriggerDate)} · activated {Date(o.ActivatedAt)} · expires {Date(o.ExpiresAt)}{(string.IsNullOrEmpty(o.Note) ? "" : "   " + o.Note)}");
        }
    }

    static void AppendStatuses(StringBuilder sb, List<ServiceItemTraceStatus> ss)
    {
        sb.AppendLine("## Status of each item");
        if (ss == null || ss.Count == 0) { sb.AppendLine("_(none)_"); sb.AppendLine(); return; }
        sb.AppendLine("| Service item | Status | Claimable | Why |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var s in ss)
            sb.AppendLine($"| {IdNameMd(s.ServiceItemID, s.Name)}{(string.IsNullOrEmpty(s.VehicleInspectionID) ? "" : $" · insp {s.VehicleInspectionID}")}{(string.IsNullOrEmpty(s.CampaignVinEntryID) ? "" : $" · entry {s.CampaignVinEntryID}")} | {FriendlyStatus(s.Status)} | {(s.Claimable ? "yes" : "no")} | {Esc(s.ClaimabilityReason)} |");
        sb.AppendLine();
    }

    static void AppendPostProcessing(StringBuilder sb, ServiceItemTracePostProcessing pp)
    {
        sb.AppendLine("## Final cleanup");
        if (pp is null) { sb.AppendLine("_(none)_"); sb.AppendLine(); return; }
        sb.AppendLine($"- VIN exclusion applied: {(pp.VinExclusionApplied ? $"yes — removed {pp.RemovedByVinExclusion}" : "no")}");
        sb.AppendLine($"- Already-claimed items added back: {pp.IneligibleItemsPickedUp}");
        if (pp.DynamicallyCancelled?.Count > 0)
        {
            sb.AppendLine("- Cancelled because a higher-mileage item was already claimed:");
            foreach (var c in pp.DynamicallyCancelled)
                sb.AppendLine($"  - {IdNameMd(c.CancelledServiceItemID, null)} at {c.CancelledMaximumMileage} km — superseded by {IdNameMd(c.SupersededByServiceItemID, null)} at {c.SupersededByMaximumMileage} km");
        }
        sb.AppendLine();
    }

    static void AppendFinal(StringBuilder sb, ServiceItemTraceFinalResult fr)
    {
        sb.AppendLine("## Final list shown to dealer");
        if (fr is null) { sb.AppendLine("_(none)_"); sb.AppendLine(); return; }
        sb.AppendLine($"Count: **{fr.Count}** · Activation required: **{(fr.ActivationRequired ? "yes" : "no")}**");
        if (fr.Items != null && fr.Items.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("| Service item | Type | Status | Claimable | Activated | Expires | Mileage |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var i in fr.Items)
                sb.AppendLine($"| {IdNameMd(i.ServiceItemID, i.Name)} | {i.Type} | {FriendlyStatus(ParseStatus(i.Status))} | {(i.Claimable ? "yes" : "no")} | {Date(i.ActivatedAt)} | {Date(i.ExpiresAt)} | {i.MaximumMileage} |");
        }
        sb.AppendLine();
    }

    static void AppendStageTimings(StringBuilder sb, List<ServiceItemTraceStageTiming> ts)
    {
        if (ts == null || ts.Count == 0) return;
        sb.AppendLine("## Stage timings (ms)");
        foreach (var t in ts.OrderByDescending(x => x.Elapsed))
            sb.AppendLine($"- {t.Stage}: {t.Elapsed.TotalMilliseconds:F2}");
    }

    // ---- HTML table helpers ----

    static string EligibilityHtmlTable(ServiceItemTraceEligibility e, ServiceItemTraceNameResolutions names)
    {
        if (e?.Decisions == null || e.Decisions.Count == 0) return "<p><em>No decisions recorded.</em></p>";
        var sb = new StringBuilder();
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Verdict</th><th>Service item</th><th>Why skipped</th><th>Item targets</th></tr></thead><tbody>");
        foreach (var d in e.Decisions)
        {
            var cls = d.Verdict == EligibilityVerdict.Accepted ? "accepted" : "rejected";
            var verdict = d.Verdict == EligibilityVerdict.Accepted ? "✅ Eligible" : $"❌ {FriendlyRejection(d.RejectionStage)}";
            sb.AppendLine($"<tr class=\"{cls}\"><td>{verdict}</td><td>{IdNameHtml(d.ServiceItemID, d.Name)}</td><td>{HtmlEsc(d.Reason)}</td><td>{SnapshotHtml(d.Item, names)}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    static string SnapshotHtml(ServiceItemSnapshot s, ServiceItemTraceNameResolutions names)
    {
        if (s is null) return "";
        var parts = new List<string>();
        if (s.BrandIDs?.Count > 0) parts.Add($"brands: {RenderIdList(s.BrandIDs, names?.Brands)}");
        if (s.CompanyIDs?.Count > 0) parts.Add($"dealers: {RenderIdList(s.CompanyIDs, names?.Companies)}");
        if (s.CountryIDs?.Count > 0) parts.Add($"countries: {RenderIdList(s.CountryIDs, names?.Countries)}");
        parts.Add($"trigger: {FriendlyTrigger(s.CampaignActivationTrigger)}");
        if (s.CampaignActivationType != default) parts.Add($"rule: {FriendlyActivationType(s.CampaignActivationType)}");
        parts.Add($"validity: {FriendlyValidity(s.ValidityMode)}");
        parts.Add($"campaign window: {Date(s.CampaignStartDate)} → {Date(s.CampaignEndDate)}");
        if (s.ValidFrom.HasValue || s.ValidTo.HasValue) parts.Add($"valid: {Date(s.ValidFrom)} → {Date(s.ValidTo)}");
        if (s.MaximumMileage.HasValue) parts.Add($"max mileage: {s.MaximumMileage}");
        if (s.ModelCostCount > 0) parts.Add($"model costs: {s.ModelCostCount}");
        if (s.VehicleInspectionTypeID.HasValue) parts.Add($"inspection type: {s.VehicleInspectionTypeID}");
        if (s.CampaignID.HasValue) parts.Add($"campaign ID: {s.CampaignID}");
        return HtmlEsc(string.Join(" · ", parts));
    }

    static string RenderIdList(List<long?> ids, Dictionary<long, string> names)
    {
        if (ids == null || ids.Count == 0) return "—";
        return string.Join(", ", ids.Select(id =>
        {
            if (id is null) return "?";
            if (names != null && names.TryGetValue(id.Value, out var name) && !string.IsNullOrWhiteSpace(name))
                return $"{name} (#{id})";
            return $"#{id}";
        }));
    }

    static string WarrantyRollingHtmlTable(ServiceItemTraceWarrantyRollingExpansion w)
    {
        if (w.Skipped) return $"<p><em>Skipped: {HtmlEsc(w.SkippedReason)}</em></p>";
        if ((w.SequentialItems?.Count ?? 0) == 0 && (w.NonSequentialItems?.Count ?? 0) == 0)
            return "<p><em>No warranty-activated free items.</em></p>";
        var sb = new StringBuilder();
        sb.AppendLine($"<p>Anchor date (free service starts): <strong>{Date(w.AnchorDate)}</strong></p>");
        sb.AppendLine("<table><thead><tr><th>Group</th><th>Service item</th><th>Mileage</th><th>Active for</th><th>Activated</th><th>Expires</th><th>Note</th></tr></thead><tbody>");
        foreach (var r in w.SequentialItems ?? new List<ServiceItemTraceRollingItem>())
            sb.AppendLine($"<tr><td>mileage-based</td><td>{IdNameHtml(r.ServiceItemID, r.Name)}</td><td>{r.MaximumMileage}</td><td>{r.ActiveFor}{r.ActiveForDurationType}</td><td>{Date(r.ActivatedAt)}</td><td>{Date(r.ExpiresAt)}</td><td>{HtmlEsc(r.Note)}</td></tr>");
        foreach (var r in w.NonSequentialItems ?? new List<ServiceItemTraceRollingItem>())
            sb.AppendLine($"<tr><td>non-mileage</td><td>{IdNameHtml(r.ServiceItemID, r.Name)}</td><td>—</td><td>{r.ActiveFor}{r.ActiveForDurationType}</td><td>{Date(r.ActivatedAt)}</td><td>{Date(r.ExpiresAt)}</td><td>{HtmlEsc(r.Note)}</td></tr>");
        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    static string TriggerExpansionHtmlTable(List<ServiceItemTraceTriggerExpansion> list)
    {
        var sb = new StringBuilder();
        foreach (var e in list)
        {
            sb.AppendLine($"<p>{IdNameHtml(e.ServiceItemID, e.Name)} · rule: {FriendlyActivationType(e.ActivationType)} · candidates: {e.CandidateTriggerCount} · selected: {e.SelectedTriggerCount}{(string.IsNullOrEmpty(e.Note) ? "" : $" · ⚠ {HtmlEsc(e.Note)}")}</p>");
            if (e.Outputs?.Count > 0)
            {
                sb.AppendLine("<table><thead><tr><th>Trigger</th><th>Trigger date</th><th>Activated</th><th>Expires</th><th>Note</th></tr></thead><tbody>");
                foreach (var o in e.Outputs)
                    sb.AppendLine($"<tr><td>{HtmlEsc(o.TriggerID)}</td><td>{Date(o.TriggerDate)}</td><td>{Date(o.ActivatedAt)}</td><td>{Date(o.ExpiresAt)}</td><td>{HtmlEsc(o.Note)}</td></tr>");
                sb.AppendLine("</tbody></table>");
            }
        }
        return sb.ToString();
    }

    static string StatusHtmlTable(List<ServiceItemTraceStatus> ss)
    {
        if (ss == null || ss.Count == 0) return "<p><em>No status decisions.</em></p>";
        var sb = new StringBuilder();
        sb.AppendLine("<table><thead><tr><th>Service item</th><th>Status</th><th>Matched claim</th><th>Claimable</th><th>Why</th><th>Activated</th><th>Expires</th></tr></thead><tbody>");
        foreach (var s in ss)
        {
            var subId = string.IsNullOrEmpty(s.VehicleInspectionID) && string.IsNullOrEmpty(s.CampaignVinEntryID)
                ? ""
                : $" <span class=\"muted\">{(string.IsNullOrEmpty(s.VehicleInspectionID) ? "" : $" · insp {HtmlEsc(s.VehicleInspectionID)}")}{(string.IsNullOrEmpty(s.CampaignVinEntryID) ? "" : $" · entry {HtmlEsc(s.CampaignVinEntryID)}")}</span>";
            sb.AppendLine($"<tr><td>{IdNameHtml(s.ServiceItemID, s.Name)}{subId}</td><td>{FriendlyStatus(s.Status)}</td><td>{(s.ClaimMatched ? $"WIP {HtmlEsc(s.ClaimMatchedJobNumber)} / INV {HtmlEsc(s.ClaimMatchedInvoiceNumber)}" : "—")}</td><td>{(s.Claimable ? "yes" : "no")}</td><td>{HtmlEsc(s.ClaimabilityReason)}</td><td>{Date(s.ActivatedAt)}</td><td>{Date(s.ExpiresAt)}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    static string PostProcessingHtml(ServiceItemTracePostProcessing pp)
    {
        if (pp is null) return "<p><em>None.</em></p>";
        var sb = new StringBuilder();
        sb.AppendLine($"<ul><li>VIN exclusion applied: <strong>{(pp.VinExclusionApplied ? "yes" : "no")}</strong>{(pp.VinExclusionApplied ? $" — removed {pp.RemovedByVinExclusion}" : "")}</li>");
        sb.AppendLine($"<li>Already-claimed items added back: {pp.IneligibleItemsPickedUp}</li></ul>");
        if (pp.DynamicallyCancelled?.Count > 0)
        {
            sb.AppendLine("<p>Cancelled because a higher-mileage item was already claimed:</p>");
            sb.AppendLine("<table><thead><tr><th>Cancelled</th><th>Mileage</th><th>Superseded by</th><th>Mileage</th></tr></thead><tbody>");
            foreach (var c in pp.DynamicallyCancelled)
                sb.AppendLine($"<tr><td>{IdNameHtml(c.CancelledServiceItemID, null)}</td><td>{c.CancelledMaximumMileage}</td><td>{IdNameHtml(c.SupersededByServiceItemID, null)}</td><td>{c.SupersededByMaximumMileage}</td></tr>");
            sb.AppendLine("</tbody></table>");
        }
        return sb.ToString();
    }

    static string FinalHtmlTable(ServiceItemTraceFinalResult fr)
    {
        if (fr is null) return "<p><em>None.</em></p>";
        var sb = new StringBuilder();
        sb.AppendLine($"<p>Items: <strong>{fr.Count}</strong> · Activation needed: <strong>{(fr.ActivationRequired ? "yes" : "no")}</strong></p>");
        if (fr.Items?.Count > 0)
        {
            sb.AppendLine("<table><thead><tr><th>Service item</th><th>Type</th><th>Status</th><th>Claimable</th><th>Activated</th><th>Expires</th><th>Mileage</th></tr></thead><tbody>");
            foreach (var i in fr.Items)
                sb.AppendLine($"<tr><td>{IdNameHtml(i.ServiceItemID, i.Name)}{(string.IsNullOrEmpty(i.VehicleInspectionID) ? "" : $" <span class=\"muted\">· insp {HtmlEsc(i.VehicleInspectionID)}</span>")}{(string.IsNullOrEmpty(i.CampaignVinEntryID) ? "" : $" <span class=\"muted\">· entry {HtmlEsc(i.CampaignVinEntryID)}</span>")}</td><td>{HtmlEsc(i.Type)}</td><td>{FriendlyStatus(ParseStatus(i.Status))}</td><td>{(i.Claimable ? "yes" : "no")}</td><td>{Date(i.ActivatedAt)}</td><td>{Date(i.ExpiresAt)}</td><td>{i.MaximumMileage}</td></tr>");
            sb.AppendLine("</tbody></table>");
        }
        return sb.ToString();
    }

    // ---- Friendly label helpers ----

    static string FriendlyRejection(EligibilityRejectionStage s) => s switch
    {
        EligibilityRejectionStage.IsDeleted => "Item deleted in catalog",
        EligibilityRejectionStage.Brand => "Different brand",
        EligibilityRejectionStage.Company => "Different dealer",
        EligibilityRejectionStage.Country => "Different country",
        EligibilityRejectionStage.CampaignWindow => "Outside campaign window / no matching trigger",
        EligibilityRejectionStage.VehicleApplicability => "Vehicle model not covered",
        _ => s.ToString(),
    };

    static string FriendlyStatus(VehcileServiceItemStatuses s) => s switch
    {
        VehcileServiceItemStatuses.Pending => "🟡 Pending",
        VehcileServiceItemStatuses.Processed => "🟢 Processed",
        VehcileServiceItemStatuses.Expired => "⚫ Expired",
        VehcileServiceItemStatuses.Cancelled => "⚪ Cancelled (superseded)",
        VehcileServiceItemStatuses.ActivationRequired => "🟠 Activation required",
        _ => s.ToString(),
    };

    static string FriendlyTrigger(ShiftSoftware.ADP.Models.Enums.ClaimableItemCampaignActivationTrigger t) => t switch
    {
        ShiftSoftware.ADP.Models.Enums.ClaimableItemCampaignActivationTrigger.WarrantyActivation => "warranty activation",
        ShiftSoftware.ADP.Models.Enums.ClaimableItemCampaignActivationTrigger.VehicleInspection => "vehicle inspection",
        ShiftSoftware.ADP.Models.Enums.ClaimableItemCampaignActivationTrigger.ManualVinEntry => "manual VIN entry",
        _ => t.ToString(),
    };

    static string FriendlyActivationType(ShiftSoftware.ADP.Models.Enums.ClaimableItemCampaignActivationTypes t) => t switch
    {
        ShiftSoftware.ADP.Models.Enums.ClaimableItemCampaignActivationTypes.EveryTrigger => "every trigger",
        ShiftSoftware.ADP.Models.Enums.ClaimableItemCampaignActivationTypes.FirstTriggerOnly => "first trigger only",
        ShiftSoftware.ADP.Models.Enums.ClaimableItemCampaignActivationTypes.ExtendOnEachTrigger => "extends on each trigger",
        _ => t.ToString(),
    };

    static string FriendlyValidity(ShiftSoftware.ADP.Models.Enums.ClaimableItemValidityMode v) => v switch
    {
        ShiftSoftware.ADP.Models.Enums.ClaimableItemValidityMode.FixedDateRange => "fixed date range",
        ShiftSoftware.ADP.Models.Enums.ClaimableItemValidityMode.RelativeToActivation => "relative to activation",
        _ => v.ToString(),
    };

    static VehcileServiceItemStatuses ParseStatus(string s) =>
        Enum.TryParse<VehcileServiceItemStatuses>(s, true, out var v) ? v : default;

    // ---- ID/name helpers ----

    static string NameOrId(string label, long? id, Dictionary<long, string> names)
    {
        if (id is null) return $"{label}: —";
        if (names != null && names.TryGetValue(id.Value, out var name) && !string.IsNullOrWhiteSpace(name))
            return $"{label}: {Esc(name)} (#{id})";
        return $"{label}: #{id}";
    }

    static string NameOrIdPlain(string label, long? id, Dictionary<long, string> names)
    {
        if (id is null) return $"{label}: —";
        if (names != null && names.TryGetValue(id.Value, out var name) && !string.IsNullOrWhiteSpace(name))
            return $"{label}: **{name}** (#{id})";
        return $"{label}: #{id}";
    }

    // ---- Misc ----

    static string Date(DateTime? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—";
    static string Date(DateTime d) => d == default ? "—" : d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Format service-item identity consistently: "Name (#ID)", or just one if the other is missing.</summary>
    static string IdName(string id, string name)
    {
        var hasName = !string.IsNullOrWhiteSpace(name);
        var hasId = !string.IsNullOrWhiteSpace(id);
        if (hasName && hasId) return $"{name} (#{id})";
        if (hasName) return name;
        if (hasId) return $"#{id}";
        return "";
    }
    static string IdNameMd(string id, string name) => Esc(IdName(id, name));
    static string IdNameHtml(string id, string name) => HtmlEsc(IdName(id, name));
    static string Truncate(string s, int max) => string.IsNullOrEmpty(s) ? "" : (s.Length > max ? s.Substring(0, max - 1) + "…" : s);
    // Mermaid Gantt uses ':' to separate task name from dates, so it must be stripped from labels.
    // '#' is fine in task names; do not strip it (would mangle the "(#ID)" suffix).
    static string GanttSafe(string s) => string.IsNullOrEmpty(s) ? "(item)" : s.Replace(":", " ");
    static string Esc(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    static string HtmlEsc(string s) =>
        string.IsNullOrEmpty(s) ? "" :
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
