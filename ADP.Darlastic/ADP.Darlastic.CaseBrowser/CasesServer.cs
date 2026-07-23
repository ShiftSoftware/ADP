using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using ShiftSoftware.ADP.Darlastic.Engine;

namespace ShiftSoftware.ADP.Darlastic.CaseBrowser;

/// <summary>
/// The case browser server (`dotnet run cases`): localhost JSON API + the cases.html UI over the
/// in-memory CaseIndex. Phase 5 steward-queue prototype + the adjudication vehicle for the
/// deferred phone+given increment.
///
/// Efficient-loading contract with the UI:
///   - list endpoints page server-side (the browser never holds more than one page),
///   - a pair's full trace is computed on demand by the LIVE scorer (RealMatcher.Explain),
///   - search runs against in-memory records and stops at 50 hits,
///   - nothing below the index bar is stored; arbitrary pairs are still explainable on request.
///
/// PII posture: data flows only at runtime on localhost; the UI file is committable (unlike the
/// gitignored explainer.html, which embeds verbatim records). Audit verdicts append to
/// out/label_audits.csv (gitignored) with the same provenance columns the 06-12 human audit used.
/// </summary>
public static class CasesServer
{
    private sealed record GoldInfo(string PairId, string Label, string Votes);
    private sealed record AuditRow(string PairId, string OldLabel, string NewLabel, string AuditedBy, string Date, string PanelVotes, string Rationale, string Status);

    private static readonly object AuditLock = new();

    public static async Task RunAsync(CaseIndex.Index ix, string outDir, string webRoot, int port)
    {
        SharedStore.EnsureDir();

        // ---- gold set + panel votes + existing audits (provenance shown inline on cases) ----
        var goldByKey = new Dictionary<string, GoldInfo>();
        var keyByGoldId = new Dictionary<string, string>();
        string goldPath = SharedStore.GoldSet;
        var votesById = new Dictionary<string, string>();
        string labeledPath = SharedStore.LabeledPairs;
        if (File.Exists(labeledPath))
            foreach (var r in Csv.ReadRows(labeledPath).Skip(1))
                if (r.Count >= 8) votesById[r[0]] = $"{r[5]}/{r[6]}/{r[7]}";
        if (File.Exists(goldPath))
            foreach (var r in Csv.ReadRows(goldPath).Skip(1))
            {
                if (r.Count < 14) continue;
                string key = KeyOf(r[2], r[3], r[8], r[9]);
                goldByKey[key] = new GoldInfo(r[0], r[1], votesById.GetValueOrDefault(r[0], ""));
                keyByGoldId[r[0]] = key;
            }

        var auditsByKey = new Dictionary<string, List<AuditRow>>();
        string auditPath = SharedStore.LabelAudits;
        if (File.Exists(auditPath))
            foreach (var r in Csv.ReadRows(auditPath).Skip(1))
            {
                if (r.Count < 8) continue;
                // Tolerate unquoted commas in historical rationale text: first 6 and last column
                // are fixed; everything between joins back into the rationale.
                var row = new AuditRow(r[0], r[1], r[2], r[3], r[4], r[5],
                    string.Join(",", r.Skip(6).Take(r.Count - 7)), r[^1]);
                string key = keyByGoldId.GetValueOrDefault(row.PairId, row.PairId); // numeric gold id or already a src-key
                if (!auditsByKey.TryGetValue(key, out var list)) auditsByKey[key] = list = [];
                list.Add(row);
            }

        // record idx by (src, id) — resolves gold/audit keys back to live records
        var idxByKey = new Dictionary<(string, string), int>(ix.Records.Count);
        foreach (var r in ix.Records) idxByKey[(r.SourceSystem, r.SourceRecordId)] = r.Idx;

        // ---- registry join: STABLE golden IDs from the [Darlastic] SQL registry ----
        // The browser's clusters are the LIVE engine's view (recomputed in memory each launch); the
        // registry holds the last `resolve` run's assignments. Joining the two shows each cluster's
        // golden ID — and surfaces drift ("registrySplit") when the engine has changed since the
        // last resolve and a live cluster now spans records the registry assigned to >1 identity.
        // No registry (resolve never ran / SQL down) → degrade to memory-only, exactly as before.
        RegistrySnap = null;
        try
        {
            RegistrySnap = Registry.LoadSnapshot();
            Console.WriteLine($"Registry join: {RegistrySnap.ProfileToIdentity.Count:N0} profiles -> " +
                              $"{RegistrySnap.ActiveIdentities:N0} active identities (resolve run {RegistrySnap.LastRunId})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registry join unavailable — memory-only, no golden IDs. ({ex.Message.Split('\n')[0]})");
        }

        // ---- review flags: "mark this for Claude with a note" (DISTINCT from adjudication) ----
        // Each flag persists a self-contained snapshot (the full case/cluster/record detail incl.
        // the engine trace) + the steward's comment to the shared corpus's review_notes.jsonl, so a
        // later Claude session reads the evidence directly — no copy-paste, server need not be
        // running. One flag per target (re-flag edits; unflag removes). PII lives in the mounted
        // shared corpus (alongside the source data), never in git.
        var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        string flagsPath = SharedStore.ReviewNotes;
        var flags = new Dictionary<string, JsonObject>();
        var flagLock = new object();
        if (File.Exists(flagsPath))
            foreach (var line in File.ReadAllLines(flagsPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try { var n = JsonNode.Parse(line)!.AsObject(); var t = (string?)n["target"]; if (t is not null) flags[t] = n; }
                catch { /* skip a corrupt line rather than refuse to start */ }
            }
        void SaveFlags()
        {
            using var w = new StreamWriter(flagsPath, false, new UTF8Encoding(false));
            foreach (var n in flags.Values) w.WriteLine(n.ToJsonString(jsonOpts));
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://localhost:{port}");
        var app = builder.Build();

        // UI: a local web/cases.html (hot-editable dev override) wins; otherwise the copy embedded
        // in this package serves — hosts need no files on disk.
        string htmlPath = Path.Combine(webRoot, "cases.html");
        app.MapGet("/", () => Results.Content(
            File.Exists(htmlPath) ? File.ReadAllText(htmlPath, Encoding.UTF8) : EmbeddedHtml(),
            "text/html; charset=utf-8"));

        // ---- summary: category counts (computed once per request — a linear pass over ~10⁵ entries) ----
        app.MapGet("/api/summary", () =>
        {
            var catCounts = Enum.GetValues<CaseIndex.Cat>().Where(c => c != CaseIndex.Cat.None)
                .ToDictionary(c => c.ToString(), c => ix.Entries.Count(e => (e.Cats & c) != 0));
            var clusterCatCounts = Enum.GetValues<CaseIndex.ClusterCat>().Where(c => c != CaseIndex.ClusterCat.None)
                .ToDictionary(c => c.ToString(), c => ix.Clusters.Count(s => (s.Cats & c) != 0));
            return Results.Json(new
            {
                records = ix.Records.Count,
                totalPairs = ix.TotalPairs,
                identities = ix.IdentityCount,
                multiRecordIdentities = ix.ClusterMembers.Count,
                indexedCases = ix.Entries.Count,
                categories = catCounts,
                clusterCategories = clusterCatCounts,
                sources = ix.Records.GroupBy(r => r.SourceSystem).ToDictionary(g => g.Key, g => g.Count()),
                audits = auditsByKey.Values.Sum(l => l.Count),
                goldPairs = goldByKey.Count,
                flags = flags.Count,
            });
        });

        // ---- paged, filtered case list ----
        app.MapGet("/api/cases", (string? cat, string? flag, string? dealer, string? dob, string? q,
                                  double? minScore, double? maxScore, string? sort, int page = 0, int size = 50) =>
        {
            var filtered = Filter(ix, goldByKey, auditsByKey, cat, flag, dealer, dob, q, minScore, maxScore);
            if (sort == "gap")
                filtered = filtered.OrderByDescending(e => DobGap(ix.Records[e.A], ix.Records[e.B]) ?? -1);
            else if (sort == "score-asc")
                filtered = filtered.OrderBy(e => e.Score);
            // default: index order = score desc
            var total = 0;
            var pageItems = new List<object>(size);
            foreach (var e in filtered)
            {
                if (total >= page * size && total < (page + 1) * size)
                    pageItems.Add(CaseSummary(ix, goldByKey, auditsByKey, e));
                total++;
            }
            return Results.Json(new { total, page, size, items = pageItems });
        });

        // ---- one case, fully explained by the live engine ----
        app.MapGet("/api/case", (int a, int b) =>
        {
            if (a < 0 || b < 0 || a >= ix.Records.Count || b >= ix.Records.Count || a == b)
                return Results.BadRequest(new { error = "bad record idx" });
            return Results.Json(CaseDetail(ix, goldByKey, auditsByKey, a, b));
        });

        // ---- search any record (name / phone / src:id), then browse its operations ----
        app.MapGet("/api/search", (string q) =>
        {
            var hits = SearchRecords(ix, q, 50);
            return Results.Json(hits.Select(i => RecordSummary(ix, i)).ToList());
        });

        // ---- a record's candidates, scored on demand (bounded by block caps: ≤ a few hundred) ----
        app.MapGet("/api/record/{idx:int}", (int idx) =>
        {
            var det = RecordDetail(ix, idx);
            return det is null ? Results.BadRequest(new { error = "bad record idx" }) : Results.Json(det);
        });

        // ---- paged cluster (identity) list ----
        app.MapGet("/api/clusters", (string? cat, string? q, int page = 0, int size = 50) =>
        {
            CaseIndex.ClusterCat catMask = CaseIndex.ClusterCat.None;
            if (!string.IsNullOrWhiteSpace(cat))
                foreach (var c in cat.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    if (Enum.TryParse<CaseIndex.ClusterCat>(c, true, out var pc)) catMask |= pc;
            string? qn = string.IsNullOrWhiteSpace(q) ? null : Norm.Name(q);
            int total = 0;
            var items = new List<object>(size);
            foreach (var s in ix.Clusters)
            {
                if (catMask != CaseIndex.ClusterCat.None && (s.Cats & catMask) == 0) continue;
                if (qn is { Length: > 0 } && !s.GoldenName.Contains(qn)) continue;
                if (total >= page * size && total < (page + 1) * size)
                    items.Add(new { s.Root, s.Size, s.Sources, s.GoldenName, cats = ClusterCatNames(s.Cats), highlight = Highlight(ix, s), registry = RegistryInfo(ix, s.Root) });
                total++;
            }
            return Results.Json(new { total, page, size, items });
        });

        // ---- cluster view: how an identity assembled + survivorship explanation ----
        app.MapGet("/api/cluster/{root:int}", (int root) =>
        {
            var det = ClusterDetail(ix, root);
            return det is null ? Results.BadRequest(new { error = "not a multi-record cluster root" }) : Results.Json(det);
        });

        // ---- audit actions: append provenance rows; the corpus-growth loop ----
        app.MapPost("/api/audit", async (HttpRequest req) =>
        {
            var body = await System.Text.Json.JsonSerializer.DeserializeAsync<AuditRequest>(req.Body,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            if (body is null || body.A < 0 || body.B < 0 || body.A >= ix.Records.Count || body.B >= ix.Records.Count)
                return Results.BadRequest(new { error = "bad audit body" });
            string verdict = body.Verdict?.ToLowerInvariant() switch
            {
                "same" => "same", "different" => "different", "ambiguous" or "unsure" => "ambiguous",
                _ => "",
            };
            if (verdict == "") return Results.BadRequest(new { error = "verdict must be same|different|unsure" });

            string key = ix.PairKey(body.A, body.B);
            var gold = goldByKey.GetValueOrDefault(key);
            var prior = auditsByKey.GetValueOrDefault(key);
            string oldLabel = prior?.LastOrDefault()?.NewLabel ?? gold?.Label ?? "";
            var row = new AuditRow(
                PairId: gold?.PairId ?? key,
                OldLabel: oldLabel,
                NewLabel: verdict,
                AuditedBy: string.IsNullOrWhiteSpace(body.Judge) ? "case-browser" : body.Judge.Trim(),
                Date: DateTime.Now.ToString("yyyy-MM-dd"),
                PanelVotes: gold?.Votes ?? "",
                Rationale: (body.Rationale ?? "").Trim(),
                Status: "pending");
            lock (AuditLock)
            {
                bool fresh = !File.Exists(auditPath);
                using var w = new StreamWriter(auditPath, append: true, new UTF8Encoding(false));
                if (fresh) w.WriteLine("pair_id,old_label,new_label,audited_by,audit_date,panel_votes,rationale,status");
                w.WriteLine($"{row.PairId},{row.OldLabel},{row.NewLabel},{Q(row.AuditedBy)},{row.Date},{Q(row.PanelVotes)},{Q(row.Rationale)},{row.Status}");
                if (!auditsByKey.TryGetValue(key, out var list)) auditsByKey[key] = list = [];
                list.Add(row);
            }
            // Dual-write into the registry's immutable audit log (Phase 5: every steward action is
            // audited). The CSV remains the corpus-loop artifact; SQL is the operational record.
            // Registry down must never block adjudication — the CSV write above already succeeded.
            try { Registry.AppendAudit(row.AuditedBy, "verdict", key, JsonSerializer.Serialize(row, jsonOpts)); }
            catch (Exception ex) { Console.WriteLine($"  (registry audit write failed: {ex.Message.Split('\n')[0]})"); }
            return Results.Json(new { ok = true, key, row });
        });

        app.MapGet("/api/audits", () => Results.Json(
            auditsByKey.SelectMany(kv => kv.Value.Select(r => new { key = kv.Key, r.PairId, r.OldLabel, r.NewLabel, r.AuditedBy, r.Date, r.Rationale, r.Status })).ToList()));

        // ---- review flags: mark a case/cluster/record for Claude with a note ----
        app.MapPost("/api/flag", async (HttpRequest req) =>
        {
            var body = await JsonSerializer.DeserializeAsync<FlagRequest>(req.Body, jsonOpts);
            if (body is null) return Results.BadRequest(new { error = "bad body" });
            string kind = (body.Kind ?? "pair").ToLowerInvariant();

            string target, label;
            object? snapshot;
            var node = new JsonObject();
            if (kind == "pair")
            {
                if (body.A < 0 || body.B < 0 || body.A >= ix.Records.Count || body.B >= ix.Records.Count || body.A == body.B)
                    return Results.BadRequest(new { error = "bad pair idx" });
                var ra = ix.Records[body.A]; var rb = ix.Records[body.B];
                double sc = RealMatcher.Score(ra, rb);
                target = "pair:" + ix.PairKey(body.A, body.B);
                label = $"{(ra.RawName.Length > 0 ? ra.RawName : "(no name)")} ~ {(rb.RawName.Length > 0 ? rb.RawName : "(no name)")} ({sc:F3})";
                snapshot = CaseDetail(ix, goldByKey, auditsByKey, body.A, body.B);
                node["a"] = body.A; node["b"] = body.B;
            }
            else if (kind == "cluster")
            {
                snapshot = ClusterDetail(ix, body.Root);
                if (snapshot is null) return Results.BadRequest(new { error = "bad cluster root" });
                var idxs = ix.ClusterMembers[body.Root];
                var name = Merge.SurviveGolden(idxs.Select(i => ix.Records[i]).ToList()).FirstOrDefault(g => g.AttrType == "full_name")?.Value ?? "(no name)";
                target = "cluster:" + body.Root;
                label = $"{name} ×{idxs.Count}";
                node["root"] = body.Root;
            }
            else if (kind == "record")
            {
                snapshot = RecordDetail(ix, body.Idx);
                if (snapshot is null) return Results.BadRequest(new { error = "bad record idx" });
                var r = ix.Records[body.Idx];
                target = $"record:{r.SourceSystem}:{r.SourceRecordId}";
                label = $"{(r.RawName.Length > 0 ? r.RawName : "(no name)")} · {r.SourceSystem}:{r.SourceRecordId}";
                node["idx"] = body.Idx;
            }
            else return Results.BadRequest(new { error = "kind must be pair|cluster|record" });

            // Editing an existing flag preserves its createdAt and any prior Claude resolution
            // (kept visible, timestamped) but re-opens it — the steward changed the question.
            flags.TryGetValue(target, out var prev);
            node["target"] = target;
            node["kind"] = kind;
            node["label"] = label;
            node["topic"] = string.IsNullOrWhiteSpace(body.Topic) ? "other" : body.Topic!.Trim();
            node["comment"] = (body.Comment ?? "").Trim();
            node["author"] = string.IsNullOrWhiteSpace(body.Author) ? "steward" : body.Author!.Trim();
            node["createdAt"] = (string?)prev?["createdAt"] ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            node["updatedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            node["status"] = "open";
            node["resolution"] = (string?)prev?["resolution"] ?? "";
            node["resolvedAt"] = (string?)prev?["resolvedAt"];
            node["resolvedBy"] = (string?)prev?["resolvedBy"];
            node["snapshot"] = JsonSerializer.SerializeToNode(snapshot, jsonOpts);
            lock (flagLock) { flags[target] = node; SaveFlags(); }
            try { Registry.AppendAudit((string)node["author"]!, "flag", target, node.ToJsonString(jsonOpts)); }
            catch (Exception ex) { Console.WriteLine($"  (registry audit write failed: {ex.Message.Split('\n')[0]})"); }
            return Results.Json(new { ok = true, target, label });
        });

        // Claude's channel: record what was done about a flag + flip it to addressed, so the steward
        // who revisits the case sees their note, the response, and (via the snapshot) what changed.
        app.MapPost("/api/flag/resolve", async (HttpRequest req) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ResolveRequest>(req.Body, jsonOpts);
            if (string.IsNullOrWhiteSpace(body?.Target)) return Results.BadRequest(new { error = "target required" });
            bool found;
            lock (flagLock)
            {
                found = flags.TryGetValue(body.Target, out var n);
                if (found)
                {
                    n!["resolution"] = (body.Resolution ?? "").Trim();
                    n["status"] = string.IsNullOrWhiteSpace(body.Status) ? "addressed" : body.Status!.Trim();
                    n["resolvedBy"] = string.IsNullOrWhiteSpace(body.ResolvedBy) ? "Claude" : body.ResolvedBy!.Trim();
                    n["resolvedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    SaveFlags();
                }
            }
            return found ? Results.Json(new { ok = true }) : Results.NotFound(new { error = "no such flag" });
        });

        app.MapPost("/api/unflag", async (HttpRequest req) =>
        {
            var body = await JsonSerializer.DeserializeAsync<UnflagRequest>(req.Body, jsonOpts);
            if (string.IsNullOrWhiteSpace(body?.Target)) return Results.BadRequest(new { error = "target required" });
            bool removed; lock (flagLock) { removed = flags.Remove(body.Target); if (removed) SaveFlags(); }
            return Results.Json(new { ok = removed });
        });

        app.MapGet("/api/flags", () => Results.Json(new
        {
            total = flags.Count,
            byTopic = flags.Values.GroupBy(n => (string?)n["topic"] ?? "other").ToDictionary(g => g.Key, g => g.Count()),
            open = flags.Values.Count(n => (string?)n["status"] != "addressed"),
            items = flags.Values
                .OrderBy(n => (string?)n["status"] == "addressed" ? 1 : 0)   // open first
                .ThenByDescending(n => (string?)n["updatedAt"] ?? "")
                .Select(n => new
                {
                    target = (string?)n["target"],
                    kind = (string?)n["kind"],
                    label = (string?)n["label"],
                    topic = (string?)n["topic"],
                    comment = (string?)n["comment"],
                    author = (string?)n["author"],
                    createdAt = (string?)n["createdAt"],
                    updatedAt = (string?)n["updatedAt"],
                    status = (string?)n["status"],
                    resolution = (string?)n["resolution"],
                    resolvedAt = (string?)n["resolvedAt"],
                    resolvedBy = (string?)n["resolvedBy"],
                    snapScore = SnapScore(n),
                    snapDecision = SnapField(n, "decision"),
                    a = (int?)n["a"], b = (int?)n["b"], root = (int?)n["root"], idx = (int?)n["idx"],
                }).ToList(),
        }));

        // ---- batch export for bulk-LLM adjudication (JSONL; same evidence a human sees) ----
        app.MapGet("/api/export", (string? cat, string? flag, string? dealer, string? dob, string? q,
                                   double? minScore, double? maxScore, int limit = 500, int stride = 1) =>
        {
            var filtered = Filter(ix, goldByKey, auditsByKey, cat, flag, dealer, dob, q, minScore, maxScore).ToList();
            if (stride > 1) filtered = filtered.Where((_, i) => i % stride == 0).ToList();
            var sb = new StringBuilder();
            var opts = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
            foreach (var e in filtered.Take(limit))
                sb.AppendLine(System.Text.Json.JsonSerializer.Serialize(CaseDetail(ix, goldByKey, auditsByKey, e.A, e.B, includeCluster: false), opts));
            return Results.File(Encoding.UTF8.GetBytes(sb.ToString()), "application/x-ndjson", "cases_export.jsonl");
        });

        Console.WriteLine($"\nCase browser ready: http://localhost:{port}   (Ctrl+C stops it)");
        Console.WriteLine($"  {ix.Entries.Count:N0} indexed cases over {ix.TotalPairs:N0} scored pairs · {ix.ClusterMembers.Count:N0} multi-record identities · {goldByKey.Count} gold pairs · {auditsByKey.Values.Sum(l => l.Count)} audit rows");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = $"http://localhost:{port}", UseShellExecute = true });
        }
        catch { /* headless/CI — fine */ }
        await app.RunAsync();
    }

    private sealed record AuditRequest(int A, int B, string? Verdict, string? Rationale, string? Judge);
    private sealed record FlagRequest(string? Kind, int A, int B, int Root, int Idx, string? Comment, string? Topic, string? Author);
    private sealed record UnflagRequest(string? Target);
    private sealed record ResolveRequest(string? Target, string? Resolution, string? Status, string? ResolvedBy);

    private static double? SnapScore(JsonObject n) => (n["snapshot"] as JsonObject)?["score"] is JsonNode v ? (double)v : null;
    private static string? SnapField(JsonObject n, string field) => (string?)((n["snapshot"] as JsonObject)?[field]);

    // ---------- filtering ----------

    private static IEnumerable<CaseIndex.Entry> Filter(CaseIndex.Index ix,
        Dictionary<string, GoldInfo> gold, Dictionary<string, List<AuditRow>> audits,
        string? cat, string? flag, string? dealer, string? dob, string? q, double? minScore, double? maxScore)
    {
        CaseIndex.Cat catMask = CaseIndex.Cat.None;
        if (!string.IsNullOrWhiteSpace(cat))
            foreach (var c in cat.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (Enum.TryParse<CaseIndex.Cat>(c, true, out var pc)) catMask |= pc;
        MatchFlags flagMask = MatchFlags.None;
        if (!string.IsNullOrWhiteSpace(flag))
            foreach (var f in flag.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (Enum.TryParse<MatchFlags>(f, true, out var pf)) flagMask |= pf;
        string? qn = string.IsNullOrWhiteSpace(q) ? null : Norm.Name(q);
        string? qDigits = string.IsNullOrWhiteSpace(q) ? null : new string(q!.Where(char.IsDigit).ToArray());
        if (qDigits is { Length: < 4 }) qDigits = null;

        foreach (var e in ix.Entries)
        {
            if (catMask != CaseIndex.Cat.None && (e.Cats & catMask) == 0) continue;
            if (flagMask != MatchFlags.None && (e.Flags & flagMask) != flagMask) continue;
            if (minScore is not null && e.Score < minScore) continue;
            if (maxScore is not null && e.Score >= maxScore) continue;
            var ra = ix.Records[e.A];
            var rb = ix.Records[e.B];
            if (dealer is not null && ra.SourceSystem != dealer && rb.SourceSystem != dealer) continue;
            if (dob is not null && !DobFilter(ra, rb, e.Flags, dob)) continue;
            if (qn is not null || qDigits is not null)
            {
                bool hit = qn is { Length: > 0 } && (ra.NormName.Contains(qn) || rb.NormName.Contains(qn));
                if (!hit && qDigits is not null)
                    hit = ra.Phones.Concat(ra.WeakPhones).Concat(rb.Phones).Concat(rb.WeakPhones).Any(p => p.Contains(qDigits));
                if (!hit) continue;
            }
            yield return e;
        }
    }

    private static bool DobFilter(RealRecord a, RealRecord b, MatchFlags f, string dob) => dob switch
    {
        "conflict" => (f & MatchFlags.DobConflict) != 0,
        "equal" => (f & MatchFlags.DobEqual) != 0,
        "july1" => (a.Dob is { } da && IsJuly1(da)) || (b.Dob is { } db && IsJuly1(db)),
        "gap10" => DobGap(a, b) is >= 10,
        "none" => a.Dob is null || b.Dob is null,
        _ => true,
    };

    private static bool IsJuly1(DobInfo d) => (d.P1 == 1 && d.P2 == 7) || (d.P1 == 7 && d.P2 == 1);
    private static int? DobGap(RealRecord a, RealRecord b) =>
        a.Dob is { } da && b.Dob is { } db ? Math.Abs(da.Year - db.Year) : null;

    // ---------- JSON shapes ----------

    private static object CaseSummary(CaseIndex.Index ix, Dictionary<string, GoldInfo> gold, Dictionary<string, List<AuditRow>> audits, CaseIndex.Entry e)
    {
        string key = ix.PairKey(e.A, e.B);
        var g = gold.GetValueOrDefault(key);
        var au = audits.GetValueOrDefault(key);
        return new
        {
            a = RecordSummary(ix, e.A),
            b = RecordSummary(ix, e.B),
            score = Math.Round((double)e.Score, 3),
            flags = FlagNames(e.Flags),
            cats = CatNames(e.Cats),
            key,
            dobGap = DobGap(ix.Records[e.A], ix.Records[e.B]),
            gold = g is null ? null : new { pairId = g.PairId, label = g.Label, votes = g.Votes },
            audit = au?.LastOrDefault() is { } last ? new { last.NewLabel, last.AuditedBy, last.Date, last.Status } : null,
        };
    }

    private static object CaseDetail(CaseIndex.Index ix, Dictionary<string, GoldInfo> gold, Dictionary<string, List<AuditRow>> audits, int a, int b, bool includeCluster = true)
    {
        var ra = ix.Records[a];
        var rb = ix.Records[b];
        var trace = new MatchTrace();
        double score = RealMatcher.Explain(ra, rb, trace);
        RealMatcher.Score(ra, rb, out var flags);
        var sharedKeys = RealMatcher.BlockKeysOf(ra).Intersect(RealMatcher.BlockKeysOf(rb)).ToList();
        var cappedShared = sharedKeys.Where(k => ix.Blocking.CappedKeys!.ContainsKey(k)).ToList();
        string key = ix.PairKey(a, b);
        var g = gold.GetValueOrDefault(key);
        int rootA = ix.Uf.Find(a), rootB = ix.Uf.Find(b);
        return new
        {
            key,
            a = RecordSummary(ix, a),
            b = RecordSummary(ix, b),
            normalizeA = NormalizeSteps(ra),
            normalizeB = NormalizeSteps(rb),
            blockKeys = sharedKeys,
            cappedBlockKeys = cappedShared,
            score = Math.Round(score, 3),
            band = RealMatcher.BandLabel(RealMatcher.Band(score)),
            decision = score >= 0.90 ? "auto-merge" : score >= 0.80 ? "steward-queue" : "kept-separate",
            flags = FlagNames(flags),
            trace = trace.Steps.Select(s => new { s.Stage, s.Title, s.Detail, conf = s.ConfAfter is { } c ? Math.Round(c, 3) : (double?)null }).ToList(),
            dobGap = DobGap(ra, rb),
            gold = g is null ? null : new { pairId = g.PairId, label = g.Label, votes = g.Votes },
            audits = audits.GetValueOrDefault(key)?.Select(r => new { r.NewLabel, r.OldLabel, r.AuditedBy, r.Date, r.Rationale, r.Status }).ToList(),
            sameIdentity = rootA == rootB,
            cluster = includeCluster && rootA == rootB && ix.ClusterMembers.ContainsKey(rootA) ? ClusterSummaryJson(ix, rootA) : null,
            clusterA = includeCluster && rootA != rootB && ix.ClusterMembers.ContainsKey(rootA) ? ClusterSummaryJson(ix, rootA) : null,
            clusterB = includeCluster && rootA != rootB && ix.ClusterMembers.ContainsKey(rootB) ? ClusterSummaryJson(ix, rootB) : null,
        };
    }

    /// <summary>Last loaded registry snapshot; null = memory-only mode. Set once at startup.</summary>
    private static Registry.Snapshot? RegistrySnap;

    /// <summary>The cluster's stable golden ID from the registry, with drift surfaced: if the LIVE
    /// cluster spans records the registry assigned to more than one identity (the engine changed
    /// since the last resolve), report the majority ID + split=true so stewards see the pending
    /// re-resolve instead of a silently wrong ID.</summary>
    private static object? RegistryInfo(CaseIndex.Index ix, int root)
    {
        if (RegistrySnap is null || !ix.ClusterMembers.TryGetValue(root, out var memberIdxs)) return null;
        Dictionary<long, int>? counts = null;
        int unregistered = 0;
        foreach (var i in memberIdxs)
        {
            var r = ix.Records[i];
            if (RegistrySnap.ProfileToIdentity.TryGetValue((r.SourceSystem, r.SourceRecordId), out var id))
            { counts ??= []; counts[id] = counts.GetValueOrDefault(id) + 1; }
            else unregistered++;
        }
        if (counts is null) return new { goldenId = (long?)null, split = false, unregistered };
        var top = counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First();
        return new
        {
            goldenId = (long?)top.Key,
            status = RegistrySnap.IdentityStatus.GetValueOrDefault(top.Key, (byte)0),
            split = counts.Count > 1,
            identities = counts.Count,
            unregistered,
            run = RegistrySnap.LastRunId,
        };
    }

    /// <summary>The packaged cases.html (EmbeddedResource) — served when no local override exists.</summary>
    private static string EmbeddedHtml()
    {
        var asm = typeof(CasesServer).Assembly;
        var name = asm.GetManifestResourceNames().First(n => n.EndsWith("cases.html", StringComparison.OrdinalIgnoreCase));
        using var s = asm.GetManifestResourceStream(name)!;
        using var r = new StreamReader(s, Encoding.UTF8);
        return r.ReadToEnd();
    }

    private static object? ClusterDetail(CaseIndex.Index ix, int root)
    {
        if (!ix.ClusterMembers.TryGetValue(root, out var memberIdxs)) return null;
        var recs = memberIdxs.Select(i => ix.Records[i]).ToList();
        var explain = new Merge.NameSurvivalExplain();
        var golden = Merge.SurviveGolden(recs, explain);
        var edges = ix.ClusterEdges.GetValueOrDefault(root, []);
        return new
        {
            root,
            cats = ClusterCatNames(ix.ClusterCatsByRoot.GetValueOrDefault(root)),
            registry = RegistryInfo(ix, root),
            members = memberIdxs.Select(i => RecordSummary(ix, i)).ToList(),
            edges = edges.Select(e => new
            {
                a = e.A, b = e.B,
                score = Math.Round((double)e.Score, 3),
                noVin = Math.Round(RealMatcher.Score(ix.Records[e.A], ix.Records[e.B], useAddress: true, useVin: false), 3),
                crossDealer = ix.Records[e.A].SourceSystem != ix.Records[e.B].SourceSystem,
                flags = FlagNames(e.Flags),
            }).ToList(),
            golden = golden.Select(g => new { attrType = g.AttrType, g.Value, g.WonBy }).ToList(),
            nameSurvival = new
            {
                anchor = explain.Anchor,
                anchorCount = explain.AnchorCount,
                chosen = explain.Chosen,
                extensions = explain.Extensions.Select(e => new { name = e.Name, count = e.Count, givenAligns = e.GivenAligns, bridge = e.Bridge }).ToList(),
            },
        };
    }

    /// <summary>A one-line "why this identity is interesting" caption for the demo/showcase list — computed
    /// only for the current page's clusters (≤ size). Leads with the strongest signal: a sold-VIN bridge that
    /// name+phone alone could NOT have made (cross-dealer + VIN-decisive), then weaker VIN/cross-dealer notes.</summary>
    private static string? Highlight(CaseIndex.Index ix, CaseIndex.ClusterSummary s)
    {
        if ((s.Cats & (CaseIndex.ClusterCat.VinBridged | CaseIndex.ClusterCat.CrossDealer)) == 0) return null;
        var vinEdges = ix.ClusterEdges.GetValueOrDefault(s.Root, [])
            .Where(e => (e.Flags & MatchFlags.VinSoldMerge) != 0)
            .Select(e =>
            {
                var a = ix.Records[e.A]; var b = ix.Records[e.B];
                double noVin = RealMatcher.Score(a, b, useAddress: true, useVin: false);
                return (a, b, score: (double)e.Score, noVin, xDealer: a.SourceSystem != b.SourceSystem);
            })
            .OrderByDescending(x => (x.xDealer ? 2 : 0) + (x.noVin < 0.90 ? 1 : 0))   // most illustrative edge first
            .ToList();

        if (vinEdges.Count > 0)
        {
            var v = vinEdges[0];
            string vin = SharedVin(v.a, v.b);
            if (v.noVin < 0.90)
                return v.xDealer
                    ? $"VIN bridged {v.a.SourceSystem}↔{v.b.SourceSystem}: a shared sold VIN ({vin}) merges them at {v.score:F2}, only {v.noVin:F2} without VIN — different phones, so name+phone alone would have kept them apart."
                    : $"VIN-decisive: a shared sold VIN ({vin}) merges these at {v.score:F2} vs {v.noVin:F2} without VIN — name+phone alone wouldn't have.";
            if (v.xDealer)
                return $"Cross-dealer identity ({v.a.SourceSystem}↔{v.b.SourceSystem}) corroborated by a shared sold VIN ({vin}).";
        }
        if ((s.Cats & CaseIndex.ClusterCat.CrossDealer) != 0)
            return $"Cross-dealer identity — one customer known to {s.Sources} dealers.";
        return null;
    }

    /// <summary>The first VIN both profiles carry (the bridge evidence for the caption).</summary>
    private static string SharedVin(RealRecord a, RealRecord b)
    {
        if (a.VinLinks is null || b.VinLinks is null) return "—";
        foreach (var v in a.VinLinks.Select(l => l.Vin).Distinct())
            if (b.VinLinks.Any(l => l.Vin == v)) return v;
        return "—";
    }

    private static object? RecordDetail(CaseIndex.Index ix, int idx)
    {
        if (idx < 0 || idx >= ix.Records.Count) return null;
        var r = ix.Records[idx];
        var candIdxs = new HashSet<int>();
        var cappedKeys = new List<object>();
        foreach (var key in RealMatcher.BlockKeysOf(r))
        {
            if (ix.Blocking.KeyBlocks!.TryGetValue(key, out var block))
                foreach (var i in block) { if (i != idx) candIdxs.Add(i); }
            else if (ix.Blocking.CappedKeys!.TryGetValue(key, out var would))
                cappedKeys.Add(new { key, size = would });
        }
        var cands = candIdxs
            .Select(i => { double s = RealMatcher.Score(r, ix.Records[i], out var f); return (i, s, f); })
            .OrderByDescending(x => x.s).Take(50)
            .Select(x => new { record = RecordSummary(ix, x.i), score = Math.Round(x.s, 3), flags = FlagNames(x.f) })
            .ToList();
        int root = ix.Uf.Find(idx);
        return new
        {
            record = RecordSummary(ix, idx),
            normalize = NormalizeSteps(r),
            blockKeys = RealMatcher.BlockKeysOf(r).ToList(),
            cappedKeys,
            cluster = ix.ClusterMembers.ContainsKey(root) ? ClusterSummaryJson(ix, root) : null,
            candidates = cands,
        };
    }

    private static object RecordSummary(CaseIndex.Index ix, int idx)
    {
        var r = ix.Records[idx];
        int root = ix.Uf.Find(idx);
        return new
        {
            idx,
            src = r.SourceSystem,
            id = r.SourceRecordId,
            rawName = r.RawName,
            normName = r.NormName,
            phones = r.Phones,
            weakPhones = r.WeakPhones,
            natId = r.NationalId,
            dob = r.Dob is { } d ? $"{d.P1}/{d.P2}/{d.Year}" : null,
            rawAddress = r.RawAddress,
            normAddress = r.NormAddress,
            city = r.NormCity,
            mojibake = r.NameWasMojibake,
            arabizi = r.NameHadArabizi,
            vins = r.VinLinks is { Length: > 0 }
                ? r.VinLinks.Select(l => new { vin = l.Vin, src = l.Source == VinSource.Sale ? "sale" : "service", first = l.First?.ToString("yyyy-MM-dd"), last = l.Last?.ToString("yyyy-MM-dd") }).ToList<object>()
                : null,
            clusterRoot = root,
            clusterSize = ix.ClusterMembers.TryGetValue(root, out var m) ? m.Count : 1,
        };
    }

    private static object ClusterSummaryJson(CaseIndex.Index ix, int root)
    {
        var idxs = ix.ClusterMembers[root];
        var golden = Merge.SurviveGolden(idxs.Select(i => ix.Records[i]).ToList());
        return new
        {
            root,
            size = idxs.Count,
            sources = idxs.Select(i => ix.Records[i].SourceSystem).Distinct().Count(),
            goldenName = golden.FirstOrDefault(g => g.AttrType == "full_name")?.Value,
            cats = ClusterCatNames(ix.ClusterCatsByRoot.GetValueOrDefault(root)),
        };
    }

    /// <summary>The record's normalize-station story, composed from ingest-known facts —
    /// including the CP1256 repair intermediate (recomputed on demand, never stored).</summary>
    private static List<object> NormalizeSteps(RealRecord r)
    {
        var steps = new List<object>();
        if (r.RawName.Length > 0)
        {
            if (r.NameWasMojibake)
                steps.Add(new { stage = "repair", detail = $"CP1256 mojibake repaired: \"{r.RawName}\" → \"{Mojibake.TryRepair(r.RawName)}\" → transliterated" });
            if (r.NameHadArabizi)
                steps.Add(new { stage = "arabizi", detail = "chat-numeral letters folded (3→a · 7→h · 5→kh …) before transliteration" });
            steps.Add(new { stage = "name", detail = r.NormName.Length > 0 ? $"\"{r.RawName}\" → \"{r.NormName}\"" : $"\"{r.RawName}\" → dropped (junk/blocklist)" });
        }
        else steps.Add(new { stage = "name", detail = "no name in source" });
        if (r.Phones.Length + r.WeakPhones.Length > 0)
            steps.Add(new { stage = "phone", detail = string.Join(" · ", r.Phones.Select(p => p + " (strong)").Concat(r.WeakPhones.Select(p => p + " (weak 9-digit)"))) });
        if (r.NormAddress.Length > 0)
            steps.Add(new { stage = "address", detail = $"\"{r.RawAddress.Replace('|', ' ')}\" → \"{r.NormAddress}\" · city slot \"{r.NormCity}\"" });
        return steps;
    }

    private static List<string> FlagNames(MatchFlags f) =>
        Enum.GetValues<MatchFlags>().Where(v => v != MatchFlags.None && (f & v) != 0).Select(v => v.ToString()).ToList();
    private static List<string> CatNames(CaseIndex.Cat c) =>
        Enum.GetValues<CaseIndex.Cat>().Where(v => v != CaseIndex.Cat.None && (c & v) != 0).Select(v => v.ToString()).ToList();
    private static List<string> ClusterCatNames(CaseIndex.ClusterCat c) =>
        Enum.GetValues<CaseIndex.ClusterCat>().Where(v => v != CaseIndex.ClusterCat.None && (c & v) != 0).Select(v => v.ToString()).ToList();

    // ---------- search ----------

    private static List<int> SearchRecords(CaseIndex.Index ix, string q, int cap)
    {
        var hits = new List<int>();
        q = q.Trim();
        if (q.Length == 0) return hits;

        // src:id exact
        int colon = q.IndexOf(':');
        if (colon > 0)
        {
            string src = q[..colon].ToLowerInvariant();
            string id = q[(colon + 1)..];
            foreach (var r in ix.Records)
                if (r.SourceSystem == src && r.SourceRecordId == id) { hits.Add(r.Idx); return hits; }
        }

        string digits = new(q.Where(char.IsDigit).ToArray());
        if (digits.Length >= 5)
        {
            // phone path: normalize like ingest does, suffix-match so partials work
            string p = digits;
            if (p.StartsWith("00964")) p = p[5..];
            else if (p.StartsWith("964")) p = p[3..];
            if (p.StartsWith('0')) p = p[1..];
            foreach (var r in ix.Records)
            {
                if (r.Phones.Any(x => x.Contains(p)) || r.WeakPhones.Any(x => x.Contains(p)))
                {
                    hits.Add(r.Idx);
                    if (hits.Count >= cap) return hits;
                }
            }
            if (hits.Count > 0) return hits;
        }

        string qn = Norm.Name(q);
        if (qn.Length >= 2)
            foreach (var r in ix.Records)
            {
                if (r.NormName.Contains(qn))
                {
                    hits.Add(r.Idx);
                    if (hits.Count >= cap) break;
                }
            }
        return hits;
    }

    private static string KeyOf(string srcA, string idA, string srcB, string idB)
    {
        int c = string.CompareOrdinal(srcA, srcB);
        if (c == 0) c = string.CompareOrdinal(idA, idB);
        return c <= 0 ? $"{srcA}:{idA}~{srcB}:{idB}" : $"{srcB}:{idB}~{srcA}:{idA}";
    }

    private static string Q(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
}
