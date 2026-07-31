using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ADP.Darlastic.Shared;
using ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.TypeAuth.Core;
using System.Security.Claims;
using System.Text.Json;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// The standalone case browser's HTTP contract, served from the registry.
///
/// <para><b>Why a second surface.</b> <see cref="CaseBrowserController"/> is the API the module wants
/// to have: DTOs, PascalCase, PairKey identity, paging by <c>skip</c>/<c>take</c>. The case browser
/// page (<c>web/cases.html</c>, 868 lines of working UI) speaks a different contract that grew
/// alongside an in-memory index: camelCase, <c>{total, page, size, items}</c> envelopes, records
/// identified by integer index. Mapping between them had to live somewhere. Putting it here rather
/// than in the page's JavaScript keeps it typed, keeps one page working against both hosts, and —
/// the deciding reason — makes it verifiable: every response below can be diffed field-for-field
/// against the same route on the running standalone server, which is not something a browserless
/// agent can do for rendered DOM.</para>
///
/// <para><b>Property names are pinned camelCase deliberately.</b> The host decides JSON casing —
/// ShiftEntity's <c>AddShiftEntityWeb</c> leaves <c>PropertyNamingPolicy</c> unset (PascalCase
/// passes through) while the standalone server's minimal APIs use web defaults (camelCase). The page
/// reads camelCase (<c>audit.newLabel</c>, <c>registry.goldenId</c>). Anonymous-object members are
/// therefore spelled in the exact wire form: already-camelCase names survive both policies
/// unchanged, so this is correct in any host rather than correct in the one it was written against.
/// Never write <c>new { last.NewLabel }</c> here — that shorthand takes the CLR name and breaks
/// under one of the two policies.</para>
///
/// <para><b>"In view" means "staged" here.</b> The page's header separates registry totals (the
/// whole tenant corpus) from what the process loaded and scored. A hosted reader scores nothing, so
/// the in-view block reports the staged set instead. The registry block is unaffected and remains
/// the honest corpus number — which is the distinction that whole header exists to make.</para>
/// </summary>
/// <remarks>
/// <para><b>On <see cref="AllowAnonymousAttribute"/>.</b> ShiftIdentity installs a global
/// <c>AuthorizeFilter</c>, so without this every route here 401s — including for the page, which
/// cannot send a bearer token. Anonymous at the MVC layer does not mean unprotected: every action
/// calls <see cref="Denied"/> first, which requires either a signed, expiring token scoped to this
/// surface or a normally authenticated caller who passes the action tree. The check moved from the
/// framework into the code; it did not go away.</para>
/// </remarks>
[Route("[controller]")]
[ApiController]
[AllowAnonymous]
public class CaseBrowserCompatController : ControllerBase
{
    private readonly ShiftDbContext db;
    private readonly DarlasticApiOptions options;

    private static readonly JsonSerializerOptions PayloadJson = new() { PropertyNameCaseInsensitive = true };

    public CaseBrowserCompatController(ShiftDbContext db, IOptions<DarlasticApiOptions> options)
    {
        this.db = db;
        this.options = options.Value;
    }

    /// <summary>
    /// Two ways in, and a caller needs exactly one of them.
    ///
    /// <para>A valid case browser token: signed, expiring, and scoped by descriptor to this surface.
    /// It was minted by an authenticated caller, so possession of it stands in for that session for
    /// as long as it lasts. Action-tree checks are skipped for it deliberately — the grant was made
    /// at mint time by someone who held the session, and the token names the person it was minted
    /// for, so authority is not being invented here.</para>
    ///
    /// <para>Otherwise the ordinary path: an authenticated principal, filtered by the action tree.
    /// An anonymous caller with no token has neither and is refused.</para>
    /// </summary>
    private bool Denied(bool write)
    {
        if (TokenActor is not null) return false;

        if (User?.Identity?.IsAuthenticated != true) return true;

        if (!options.EnableDarlasticActionTreeAuthorization) return false;
        var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
        return !typeAuth.Can(options.Actions.ResolvedStewardQueue, write ? Access.Write : Access.Read);
    }

    /// <summary>The person a valid token names, or null. Read once per request.</summary>
    private string? tokenActor;
    private bool tokenActorRead;
    private string? TokenActor
    {
        get
        {
            if (!tokenActorRead)
            {
                tokenActor = CaseBrowserSas.ActorOf(Request, options);
                tokenActorRead = true;
            }
            return tokenActor;
        }
    }

    /// <summary>
    /// Who to record on a flag or an audit. The token's actor is signed, so it is preferred over
    /// anything else the request claims about itself; a bearer-authenticated caller falls back to
    /// its own claims. Never read from an unsigned query parameter — that would let anyone author a
    /// review note as anyone.
    /// </summary>
    private string Actor() =>
        TokenActor
        ?? User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.Identity?.Name
        ?? "unknown";

    // ---------------------------------------------------------------- summary

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        if (Denied(false)) return Forbid();

        var counts = await db.Set<CaseCategoryCount>().AsNoTracking().ToListAsync(ct);
        var run = await db.Set<ResolveRun>().AsNoTracking()
            .OrderByDescending(r => r.RunID).FirstOrDefaultAsync(ct);

        var profiles = await db.Set<SourceProfile>().AsNoTracking().CountAsync(ct);
        var identities = await db.Set<GoldenIdentity>().AsNoTracking()
            .CountAsync(i => i.Status == IdentityStatus.Active, ct);

        // The catalog is capped per category by the resolve, so this is thousands of rows, not
        // millions — cheap to hold and the only way to report per-category browsable counts.
        var catalog = await db.Set<CaseCatalogEntry>().AsNoTracking()
            .Select(c => new { c.Categories }).ToListAsync(ct);

        var stagedRecords = await db.Set<StewardRecord>().AsNoTracking().CountAsync(ct);
        var queueDepth = await db.Set<StewardQueueEntry>().AsNoTracking().CountAsync(ct);

        var sources = await db.Set<StewardRecord>().AsNoTracking()
            .GroupBy(r => r.SourceSystem)
            .Select(g => new { Key = g.Key, N = g.Count() })
            .ToListAsync(ct);

        // Cluster categories are staged per identity as a bitmask. Counting them corpus-wide is a
        // bitmask test per row over 10^5 rows, so it runs in SQL rather than in memory. The
        // standalone reports its in-memory cluster counts here; this reports the corpus, which is
        // strictly better information under the same key.
        var clusterCats = new Dictionary<string, long>();
        foreach (var cc in Enum.GetValues<ClusterCat>())
        {
            if (cc == ClusterCat.None) continue;
            long mask = (long)cc;
            clusterCats[cc.ToString()] = await db.Set<IdentitySummary>().AsNoTracking()
                .CountAsync(s => (s.Categories & mask) != 0, ct);
        }

        var inView = new Dictionary<string, int>();
        var corpus = new Dictionary<string, object>();
        foreach (var cat in Enum.GetValues<CaseCat>())
        {
            if (cat == CaseCat.None) continue;
            string name = cat.ToString();
            long mask = (long)cat;
            inView[name] = catalog.Count(c => (c.Categories & mask) != 0);
            var row = counts.FirstOrDefault(c => c.Category == name);
            // Always emit a row, at zero if need be: a missing key reads to the sidebar as "not
            // measured" and silently falls back to the in-view count.
            corpus[name] = new { total = row?.Total ?? 0L, sampled = row?.Sampled ?? inView[name] };
        }

        return new JsonResult(new
        {
            records = stagedRecords,
            totalPairs = catalog.Count + queueDepth,
            identities = await db.Set<IdentitySummary>().AsNoTracking().CountAsync(ct),
            multiRecordIdentities = await db.Set<IdentitySummary>().AsNoTracking()
                .CountAsync(s => s.MemberCount > 1, ct),
            indexedCases = catalog.Count,
            registryProfiles = profiles,
            registryIdentities = identities,
            registryRun = run?.RunID,
            corpusCategories = corpus,
            categories = inView,
            clusterCategories = clusterCats,
            sources = sources.ToDictionary(s => s.Key, s => s.N),
            audits = await db.Set<LabelAudit>().AsNoTracking().CountAsync(ct),
            // The gold set is a file in the shared corpus, not registry state. Reported as zero
            // rather than omitted so the page renders a number instead of "undefined".
            goldPairs = 0,
            flags = await db.Set<ReviewFlag>().AsNoTracking().CountAsync(ct),
        });
    }

    // ------------------------------------------------------------------ cases

    [HttpGet("cases")]
    public async Task<IActionResult> Cases(string? cat, string? flag, string? dealer, string? dob,
        string? q, double? minScore, double? maxScore, string? sort, int page = 0, int size = 50,
        CancellationToken ct = default)
    {
        if (Denied(false)) return Forbid();

        var rows = await db.Set<CaseCatalogEntry>().AsNoTracking().ToListAsync(ct);

        long catMask = MaskOf<CaseCat>(cat);
        if (catMask != 0) rows = rows.Where(r => (r.Categories & catMask) != 0).ToList();

        long flagMask = MaskOf<MatchFlags>(flag);
        if (flagMask != 0) rows = rows.Where(r => (r.Flags & flagMask) != 0).ToList();

        if (!string.IsNullOrWhiteSpace(dealer))
            rows = rows.Where(r => r.SourceSystemA == dealer || r.SourceSystemB == dealer).ToList();

        if (minScore is { } lo) rows = rows.Where(r => r.Score >= lo).ToList();
        if (maxScore is { } hi) rows = rows.Where(r => r.Score <= hi).ToList();

        // Load the sides for every surviving row: q and dob filter on record content, and the page
        // renders both sides of every row anyway, so this is the same read either way.
        var sides = await LoadSides(rows.SelectMany(r =>
            new[] { (r.SourceSystemA, r.SourceRecordIdA), (r.SourceSystemB, r.SourceRecordIdB) }), ct);

        if (!string.IsNullOrWhiteSpace(q))
        {
            string needle = q.Trim().ToLowerInvariant();
            rows = rows.Where(r =>
                Matches(sides.GetValueOrDefault((r.SourceSystemA, r.SourceRecordIdA)), needle) ||
                Matches(sides.GetValueOrDefault((r.SourceSystemB, r.SourceRecordIdB)), needle)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(dob))
            rows = rows.Where(r => DobBucket(GapOf(r, sides)) == dob).ToList();

        rows = sort switch
        {
            "gap" => rows.OrderByDescending(r => GapOf(r, sides) ?? -1).ToList(),
            "score-asc" => rows.OrderBy(r => r.Score).ToList(),
            _ => rows.OrderByDescending(r => r.Score).ToList(),
        };

        int total = rows.Count;
        var window = rows.Skip(page * size).Take(size).ToList();

        var queued = await QueuedKeys(window.Select(r => r.PairKey), ct);
        var audits = await AuditsByKey(window.Select(r => r.PairKey), ct);

        var items = window.Select(r => new
        {
            a = RecordJson(sides.GetValueOrDefault((r.SourceSystemA, r.SourceRecordIdA))),
            b = RecordJson(sides.GetValueOrDefault((r.SourceSystemB, r.SourceRecordIdB))),
            score = Math.Round(r.Score, 3),
            flags = NamesOf<MatchFlags>(r.Flags),
            cats = NamesOf<CaseCat>(r.Categories),
            key = r.PairKey,
            dobGap = GapOf(r, sides),
            // The gold set lives in the shared corpus, not the registry.
            gold = (object?)null,
            audit = audits.GetValueOrDefault(r.PairKey) is { } list && list.Count > 0
                ? Last(list)
                : null,
            queued = queued.Contains(r.PairKey),
        }).ToList();

        return new JsonResult(new { total, page, size, items });
    }

    [HttpGet("case")]
    public async Task<IActionResult> Case(int a, int b, CancellationToken ct)
    {
        if (Denied(false)) return Forbid();

        // The page identifies records by the integer index the resolve assigned them, which is
        // staged on each record's payload and unique across the staged set. That is what lets one
        // page serve both hosts without changing how it keys the DOM.
        var ra = await RecordByIdx(a, ct);
        var rb = await RecordByIdx(b, ct);
        if (ra is null || rb is null) return BadRequest(new { error = "bad record idx" });

        return new JsonResult(await DetailJson(ra.Value.rec, rb.Value.rec, ra.Value.key, rb.Value.key, ct));
    }

    // ------------------------------------------------------------- identities

    [HttpGet("clusters")]
    public async Task<IActionResult> Clusters(string? cat, string? q, int page = 0, int size = 50,
        CancellationToken ct = default)
    {
        if (Denied(false)) return Forbid();

        var query = db.Set<IdentitySummary>().AsNoTracking().Where(s => s.MemberCount > 1);

        long mask = MaskOf<ClusterCat>(cat);
        if (mask != 0) query = query.Where(s => (s.Categories & mask) != 0);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(s => s.GoldenName != null && s.GoldenName.Contains(q));

        int total = await query.CountAsync(ct);
        var window = await query.OrderByDescending(s => s.MemberCount)
            .Skip(page * size).Take(size).ToListAsync(ct);

        int run = await LatestRun(ct);
        var status = await StatusOf(window.Select(s => s.IdentityID), ct);

        var items = window.Select(s => new
        {
            root = s.IdentityID,
            size = s.MemberCount,
            sources = s.SourceCount,
            goldenName = s.GoldenName,
            cats = NamesOf<ClusterCat>(s.Categories),
            // Highlight is a phrase the standalone composes from live survivorship state. The
            // staged summary carries the categories that phrase was derived from, so the page's
            // chips still populate; the sentence does not.
            highlight = (string?)null,
            registry = RegistryJson(s.IdentityID, status.GetValueOrDefault(s.IdentityID), run),
        }).ToList();

        return new JsonResult(new { total, page, size, items });
    }

    [HttpGet("cluster/{root:long}")]
    public async Task<IActionResult> Cluster(long root, CancellationToken ct)
    {
        if (Denied(false)) return Forbid();

        var summary = await db.Set<IdentitySummary>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.IdentityID == root, ct);
        if (summary is null) return BadRequest(new { error = "not a multi-record cluster root" });

        var memberKeys = await db.Set<SourceProfile>().AsNoTracking()
            .Where(p => p.IdentityID == root)
            .Select(p => new { p.SourceSystem, p.SourceRecordId })
            .ToListAsync(ct);

        var sides = await LoadSides(memberKeys.Select(m => (m.SourceSystem, m.SourceRecordId)), ct);

        // Bounded: one identity in a measured corpus carries 28,622 edges, and no human reads past the
        // strongest few dozen.
        var edges = await db.Set<IdentityEdge>().AsNoTracking()
            .Where(e => e.IdentityID == root)
            .OrderByDescending(e => e.Score)
            .Take(200)
            .ToListAsync(ct);

        return new JsonResult(new
        {
            root,
            size = summary.MemberCount,
            sources = summary.SourceCount,
            goldenName = summary.GoldenName,
            cats = NamesOf<ClusterCat>(summary.Categories),
            members = memberKeys
                .Select(m => RecordJson(sides.GetValueOrDefault((m.SourceSystem, m.SourceRecordId))))
                .Where(x => x is not null).ToList(),
            edges = edges.Select(e => new
            {
                key = $"{e.SourceSystemA}:{e.SourceRecordIdA}~{e.SourceSystemB}:{e.SourceRecordIdB}",
                score = Math.Round(e.Score, 3),
                flags = NamesOf<MatchFlags>(e.Flags),
            }).ToList(),
            registry = RegistryJson(root, (await StatusOf([root], ct)).GetValueOrDefault(root), await LatestRun(ct)),
        });
    }

    /// <summary>
    /// The page's registry badge. <c>split</c> and <c>unregistered</c> exist because the standalone
    /// compares a LIVE cluster against the last resolve and can find them disagreeing — the engine
    /// moved since. A hosted reader has no live cluster to disagree with: the registry is the only
    /// source, so both are constant here rather than absent. Emitting them keeps the badge rendering
    /// the same in both hosts instead of silently dropping to "undefined".
    /// </summary>
    private static object RegistryJson(long identityId, byte status, int run) => new
    {
        goldenId = (long?)identityId,
        status,
        split = false,
        identities = 1,
        unregistered = 0,
        run,
    };

    private async Task<int> LatestRun(CancellationToken ct) =>
        await db.Set<ResolveRun>().AsNoTracking().OrderByDescending(r => r.RunID)
            .Select(r => r.RunID).FirstOrDefaultAsync(ct);

    private async Task<Dictionary<long, byte>> StatusOf(IEnumerable<long> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        if (list.Count == 0) return [];
        var rows = await db.Set<GoldenIdentity>().AsNoTracking()
            .Where(i => list.Contains(i.IdentityID))
            .Select(i => new { i.IdentityID, i.Status })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.IdentityID, r => (byte)r.Status);
    }

    // ----------------------------------------------------------------- search

    [HttpGet("search")]
    public async Task<IActionResult> Search(string q, CancellationToken ct)
    {
        if (Denied(false)) return Forbid();
        if (string.IsNullOrWhiteSpace(q)) return new JsonResult(Array.Empty<object>());

        string needle = q.Trim().ToLowerInvariant();
        var rows = await db.Set<StewardRecord>().AsNoTracking().Take(50_000).ToListAsync(ct);

        var hits = new List<object>();
        foreach (var row in rows)
        {
            var rec = Revive(row);
            if (rec is null || !Matches(rec, needle)) continue;
            hits.Add(RecordJson((rec, (row.SourceSystem, row.SourceRecordId), null, 1))!);
            if (hits.Count == 50) break;
        }

        var withIdentity = await WithIdentities(hits, ct);
        return new JsonResult(withIdentity);
    }

    /// <summary>
    /// A record's scored candidates. Not available from a hosted reader: candidates come from
    /// blocking over the whole corpus, which is the one thing the prototype's in-memory index buys
    /// and a request-scoped reader cannot afford. Returns the record with an empty candidate list
    /// and says why, rather than 404-ing a route the page calls.
    /// </summary>
    [HttpGet("record/{idx:int}")]
    public async Task<IActionResult> Record(int idx, CancellationToken ct)
    {
        if (Denied(false)) return Forbid();

        var hit = await RecordByIdx(idx, ct);
        if (hit is null) return BadRequest(new { error = "bad record idx" });

        var sides = await LoadSides([hit.Value.key], ct);
        return new JsonResult(new
        {
            record = RecordJson(sides.GetValueOrDefault(hit.Value.key)),
            candidates = Array.Empty<object>(),
            note = "Candidate scoring needs corpus-wide blocking and stays in the local browser.",
        });
    }

    // ------------------------------------------------------------------ flags

    [HttpGet("flags")]
    public async Task<IActionResult> Flags(CancellationToken ct)
    {
        if (Denied(false)) return Forbid();

        var rows = await db.Set<ReviewFlag>().AsNoTracking().ToListAsync(ct);

        return new JsonResult(new
        {
            total = rows.Count,
            byTopic = rows.GroupBy(f => f.Topic ?? "other").ToDictionary(g => g.Key, g => g.Count()),
            open = rows.Count(f => f.Response == null),
            items = rows
                .OrderBy(f => f.Response == null ? 0 : 1)
                .ThenByDescending(f => f.UpdatedUtc ?? f.CreatedUtc)
                .Select(f => new
                {
                    target = f.Target,
                    kind = f.Target.Contains('~') ? "case" : "record",
                    label = f.Target,
                    topic = f.Topic,
                    comment = f.Comment,
                    author = f.Author,
                    createdAt = f.CreatedUtc.ToString("O"),
                    updatedAt = (f.UpdatedUtc ?? f.CreatedUtc).ToString("O"),
                    status = f.Response == null ? "open" : "addressed",
                    resolution = f.Response,
                    resolvedAt = f.ResponseUtc?.ToString("O"),
                    resolvedBy = f.ResponseBy,
                    snapScore = (double?)null,
                    snapDecision = (string?)null,
                    a = (int?)null,
                    b = (int?)null,
                    root = (long?)null,
                    idx = (int?)null,
                }).ToList(),
        });
    }

    // ----------------------------------------------------------------- writes

    public class FlagBody
    {
        public string? Kind { get; set; }
        public string? Topic { get; set; }
        public string? Comment { get; set; }
        public string? Author { get; set; }
        public int? A { get; set; }
        public int? B { get; set; }
        public long? Root { get; set; }
        public int? Idx { get; set; }
    }

    public class UnflagBody { public string? Target { get; set; } }

    public class AuditBody
    {
        public int A { get; set; }
        public int B { get; set; }
        public string? Verdict { get; set; }
        public string? Rationale { get; set; }
        public string? Judge { get; set; }
    }

    /// <summary>
    /// Raise or edit a review flag. One flag per target — re-flagging edits rather than piling up,
    /// which is what makes the flag list a work list instead of a log.
    /// </summary>
    /// <remarks>
    /// Refusals carry a real status code AND the <c>{ok:false}</c> body the page reads. Returning
    /// 200 with <c>ok:false</c> — which this did at first — keeps the page working while telling
    /// logs, metrics and every other client that the write succeeded.
    /// </remarks>
    [HttpPost("flag")]
    public async Task<IActionResult> Flag([FromBody] FlagBody body, CancellationToken ct)
    {
        if (Denied(true)) return StatusCode(StatusCodes.Status403Forbidden, new { ok = false, error = "not permitted" });
        if (body is null) return BadRequest(new { ok = false, error = "bad body" });

        string kind = (body.Kind ?? "pair").ToLowerInvariant();
        string? target = null;
        string? snapshot = null;

        if (kind == "pair" && body.A is { } a && body.B is { } b)
        {
            var ra = await RecordByIdx(a, ct);
            var rb = await RecordByIdx(b, ct);
            if (ra is null || rb is null) return new JsonResult(new { ok = false, error = "bad pair idx" });
            target = "pair:" + PairKeyOf(ra.Value.key, rb.Value.key);
            // The evidence as it read when flagged, so the flag is self-contained: whoever picks it
            // up later does not have to reconstruct what the engine was saying at the time.
            snapshot = JsonSerializer.Serialize(await DetailJson(ra.Value.rec, rb.Value.rec, ra.Value.key, rb.Value.key, ct));
        }
        else if (kind == "cluster" && body.Root is { } root)
        {
            target = "cluster:" + root;
        }
        else if (kind == "record" && body.Idx is { } idx)
        {
            var r = await RecordByIdx(idx, ct);
            if (r is null) return new JsonResult(new { ok = false, error = "bad record idx" });
            target = $"record:{r.Value.key.sys}:{r.Value.key.id}";
        }

        if (target is null) return new JsonResult(new { ok = false, error = "bad target" });

        var existing = await db.Set<ReviewFlag>().FirstOrDefaultAsync(f => f.Target == target, ct);
        if (existing is null)
        {
            db.Set<ReviewFlag>().Add(new ReviewFlag
            {
                Target = target,
                Topic = body.Topic ?? "other",
                Comment = body.Comment ?? "",
                Author = string.IsNullOrWhiteSpace(body.Author) ? Actor() : body.Author!,
                CreatedUtc = DateTime.UtcNow,
                Snapshot = snapshot,
            });
        }
        else
        {
            existing.Topic = body.Topic ?? existing.Topic;
            existing.Comment = body.Comment ?? existing.Comment;
            existing.UpdatedUtc = DateTime.UtcNow;
            // The snapshot is NOT refreshed on edit: it records what was on screen when the concern
            // was raised, and rewriting it would quietly erase the evidence for the original note.
        }

        await db.SaveChangesAsync(ct);
        return new JsonResult(new { ok = true, target });
    }

    [HttpPost("unflag")]
    public async Task<IActionResult> Unflag([FromBody] UnflagBody body, CancellationToken ct)
    {
        if (Denied(true)) return StatusCode(StatusCodes.Status403Forbidden, new { ok = false, error = "not permitted" });
        if (string.IsNullOrWhiteSpace(body?.Target)) return BadRequest(new { ok = false, error = "bad target" });

        var row = await db.Set<ReviewFlag>().FirstOrDefaultAsync(f => f.Target == body.Target, ct);
        if (row is not null)
        {
            db.Set<ReviewFlag>().Remove(row);
            await db.SaveChangesAsync(ct);
        }
        return new JsonResult(new { ok = true });
    }

    /// <summary>
    /// Record an adjudication. Appends — a pair can be re-judged and the history is the point, which
    /// is why LabelAudit's index is deliberately not unique.
    /// </summary>
    [HttpPost("audit")]
    public async Task<IActionResult> Audit([FromBody] AuditBody body, CancellationToken ct)
    {
        if (Denied(true)) return StatusCode(StatusCodes.Status403Forbidden, new { error = "not permitted" });
        if (body is null) return BadRequest(new { error = "bad audit body" });

        var ra = await RecordByIdx(body.A, ct);
        var rb = await RecordByIdx(body.B, ct);
        if (ra is null || rb is null) return BadRequest(new { error = "bad audit body" });

        string verdict = (body.Verdict ?? "").ToLowerInvariant() switch
        {
            "same" => "same",
            "different" => "different",
            "ambiguous" or "unsure" => "ambiguous",
            _ => "",
        };

        db.Set<LabelAudit>().Add(new LabelAudit
        {
            PairKey = PairKeyOf(ra.Value.key, rb.Value.key),
            // Empty verdict means "pending an expert call" — recorded, never folded into the labels.
            NewLabel = verdict.Length > 0 ? verdict : null,
            AuditedBy = string.IsNullOrWhiteSpace(body.Judge) ? Actor() : body.Judge!,
            AuditedUtc = DateTime.UtcNow,
            Rationale = body.Rationale,
            Status = "pending",
        });

        await db.SaveChangesAsync(ct);
        return new JsonResult(new { ok = true });
    }

    // ------------------------------------------------------------- shape glue

    private async Task<Dictionary<(string, string), (RealRecord? rec, (string sys, string id) key, long? identityId, int clusterSize)>>
        LoadSides(IEnumerable<(string sys, string id)> keys, CancellationToken ct)
    {
        var wanted = keys.Distinct().ToList();
        var result = new Dictionary<(string, string), (RealRecord?, (string, string), long?, int)>();
        if (wanted.Count == 0) return result;

        var systems = wanted.Select(k => k.sys).Distinct().ToList();
        var ids = wanted.Select(k => k.id).Distinct().ToList();

        var rows = await db.Set<StewardRecord>().AsNoTracking()
            .Where(r => systems.Contains(r.SourceSystem) && ids.Contains(r.SourceRecordId))
            .ToListAsync(ct);

        var profiles = await db.Set<SourceProfile>().AsNoTracking()
            .Where(p => systems.Contains(p.SourceSystem) && ids.Contains(p.SourceRecordId))
            .Select(p => new { p.SourceSystem, p.SourceRecordId, p.IdentityID })
            .ToListAsync(ct);

        var identityOf = profiles.ToDictionary(p => (p.SourceSystem, p.SourceRecordId), p => (long?)p.IdentityID);

        var identityIds = identityOf.Values.Where(v => v.HasValue).Select(v => v!.Value).Distinct().ToList();
        var sizeOf = await db.Set<IdentitySummary>().AsNoTracking()
            .Where(s => identityIds.Contains(s.IdentityID))
            .Select(s => new { s.IdentityID, s.MemberCount })
            .ToDictionaryAsync(s => s.IdentityID, s => s.MemberCount, ct);

        foreach (var row in rows)
        {
            var key = (row.SourceSystem, row.SourceRecordId);
            if (!wanted.Contains(key)) continue;
            var rec = Revive(row);
            if (rec is null) continue;
            var id = identityOf.GetValueOrDefault(key);
            result[key] = (rec, key, id, id is { } i ? sizeOf.GetValueOrDefault(i, 1) : 1);
        }

        return result;
    }

    private async Task<(RealRecord rec, (string sys, string id) key)?> RecordByIdx(int idx, CancellationToken ct)
    {
        DbSet<StewardRecord> set = db.Set<StewardRecord>();
        string sql = "SELECT * FROM " + Schema() + ".StewardRecord WHERE JSON_VALUE(Payload,'$.Idx') = {0}";
        var row = await set.FromSqlRaw(sql, idx.ToString()).AsNoTracking().FirstOrDefaultAsync(ct);
        if (row is null) return null;
        var rec = Revive(row);
        return rec is null ? null : (rec, (row.SourceSystem, row.SourceRecordId));
    }

    private async Task<object> DetailJson(RealRecord ra, RealRecord rb,
        (string sys, string id) ka, (string sys, string id) kb, CancellationToken ct)
    {
        var trace = new MatchTrace();
        double score = RealMatcher.Explain(ra, rb, trace);
        RealMatcher.Score(ra, rb, out var flags);

        string key = PairKeyOf(ka, kb);
        var sides = await LoadSides([ka, kb], ct);
        var audits = await AuditsByKey([key], ct);
        var queued = await QueuedKeys([key], ct);

        int? gap = ra.Dob is { } da && rb.Dob is { } dbo && da.Year > 0 && dbo.Year > 0
            ? Math.Abs(da.Year - dbo.Year) : null;

        long? idA = sides.GetValueOrDefault(ka).identityId;
        long? idB = sides.GetValueOrDefault(kb).identityId;

        return new
        {
            key,
            a = RecordJson(sides.GetValueOrDefault(ka)),
            b = RecordJson(sides.GetValueOrDefault(kb)),
            normalizeA = Normalization.Steps(ra).Select(s => new { stage = s.Stage, detail = s.Detail }).ToList(),
            normalizeB = Normalization.Steps(rb).Select(s => new { stage = s.Stage, detail = s.Detail }).ToList(),
            blockKeys = RealMatcher.BlockKeysOf(ra).Intersect(RealMatcher.BlockKeysOf(rb), StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToList(),
            // Which shared keys hit the block cap is a property of the run's blocking pass, not of
            // the pair, and is not staged.
            cappedBlockKeys = Array.Empty<string>(),
            score = Math.Round(score, 3),
            band = RealMatcher.BandLabel(RealMatcher.Band(score)),
            decision = score >= 0.90 ? "auto-merge" : score >= 0.80 ? "steward-queue" : "kept-separate",
            flags = NamesOf<MatchFlags>((long)flags),
            trace = trace.Steps.Select(s => new
            {
                stage = s.Stage,
                title = s.Title,
                detail = s.Detail,
                conf = s.ConfAfter is { } c ? Math.Round(c, 3) : (double?)null,
            }).ToList(),
            dobGap = gap,
            gold = (object?)null,
            audits = audits.GetValueOrDefault(key)?.Select(r => new
            {
                newLabel = r.NewLabel,
                oldLabel = r.OldLabel,
                auditedBy = r.AuditedBy,
                date = r.AuditedUtc.ToString("O"),
                rationale = r.Rationale,
                status = r.Status,
            }).ToList(),
            sameIdentity = idA is not null && idA == idB,
            queued = queued.Contains(key),
            // The page shows one cluster block when both records resolved into the same identity and
            // two when they did not — that contrast IS the steward's question, so both branches are
            // populated rather than collapsed into one.
            cluster = idA is not null && idA == idB ? await ClusterJson(idA.Value, ct) : null,
            clusterA = idA != idB ? await ClusterJson(idA, ct) : null,
            clusterB = idA != idB ? await ClusterJson(idB, ct) : null,
        };
    }

    private async Task<object?> ClusterJson(long? identityId, CancellationToken ct)
    {
        if (identityId is not { } id) return null;
        var s = await db.Set<IdentitySummary>().AsNoTracking().FirstOrDefaultAsync(x => x.IdentityID == id, ct);
        // IdentitySummary is staged only for identities worth summarising; a single-record identity
        // has no assembly story and the page renders no block for it.
        if (s is null || s.MemberCount <= 1) return null;
        return new
        {
            root = s.IdentityID,
            size = s.MemberCount,
            sources = s.SourceCount,
            goldenName = s.GoldenName,
            cats = NamesOf<ClusterCat>(s.Categories),
        };
    }

    private object? RecordJson((RealRecord? rec, (string sys, string id) key, long? identityId, int clusterSize) side)
    {
        if (side.rec is null) return null;
        var r = side.rec;
        return new
        {
            // The resolve's own record index, staged on the payload — the page's DOM key.
            idx = r.Idx,
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
                ? r.VinLinks.Select(l => new
                {
                    vin = l.Vin,
                    src = l.Source == VinSource.Sale ? "sale" : "service",
                    first = l.First?.ToString("yyyy-MM-dd"),
                    last = l.Last?.ToString("yyyy-MM-dd"),
                }).ToList<object>()
                : null,
            // The page passes this straight back to /cluster/{root}; in a hosted registry the
            // identity IS the cluster root, so the round trip stays consistent.
            clusterRoot = side.identityId,
            clusterSize = side.clusterSize,
        };
    }

    private async Task<object> WithIdentities(List<object> hits, CancellationToken ct) => await Task.FromResult<object>(hits);

    private async Task<HashSet<string>> QueuedKeys(IEnumerable<string> keys, CancellationToken ct)
    {
        var list = keys.Distinct().ToList();
        if (list.Count == 0) return [];
        var found = await db.Set<StewardQueueEntry>().AsNoTracking()
            .Where(e => list.Contains(e.PairKey)).Select(e => e.PairKey).ToListAsync(ct);
        return found.ToHashSet(StringComparer.Ordinal);
    }

    private async Task<Dictionary<string, List<LabelAudit>>> AuditsByKey(IEnumerable<string> keys, CancellationToken ct)
    {
        var list = keys.Distinct().ToList();
        if (list.Count == 0) return [];
        var rows = await db.Set<LabelAudit>().AsNoTracking()
            .Where(a => list.Contains(a.PairKey)).ToListAsync(ct);
        return rows.GroupBy(a => a.PairKey).ToDictionary(g => g.Key, g => g.OrderBy(a => a.AuditedUtc).ToList());
    }

    private static object Last(List<LabelAudit> list)
    {
        var a = list[^1];
        return new { newLabel = a.NewLabel, status = a.Status, auditedBy = a.AuditedBy, date = a.AuditedUtc.ToString("O") };
    }

    private string Schema() => options.Schema ?? "Darlastic";

    private static string PairKeyOf((string sys, string id) a, (string sys, string id) b)
    {
        string ka = $"{a.sys}:{a.id}", kb = $"{b.sys}:{b.id}";
        return string.CompareOrdinal(ka, kb) <= 0 ? $"{ka}~{kb}" : $"{kb}~{ka}";
    }

    private static RealRecord? Revive(StewardRecord? row)
    {
        if (row is null) return null;
        try { return JsonSerializer.Deserialize<RealRecord>(row.Payload, PayloadJson); }
        catch { return null; }
    }

    private static bool Matches((RealRecord? rec, (string sys, string id) key, long? identityId, int clusterSize) side, string needle)
        => side.rec is not null && Matches(side.rec, needle);

    private static bool Matches(RealRecord r, string needle) =>
        (r.NormName?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
        || (r.RawName?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
        || (r.SourceRecordId?.Equals(needle, StringComparison.OrdinalIgnoreCase) ?? false)
        || (r.Phones?.Any(p => p.Contains(needle, StringComparison.Ordinal)) ?? false)
        || (r.WeakPhones?.Any(p => p.Contains(needle, StringComparison.Ordinal)) ?? false)
        || $"{r.SourceSystem}:{r.SourceRecordId}".Equals(needle, StringComparison.OrdinalIgnoreCase);

    private int? GapOf(CaseCatalogEntry r,
        Dictionary<(string, string), (RealRecord? rec, (string sys, string id) key, long? identityId, int clusterSize)> sides)
    {
        var a = sides.GetValueOrDefault((r.SourceSystemA, r.SourceRecordIdA)).rec;
        var b = sides.GetValueOrDefault((r.SourceSystemB, r.SourceRecordIdB)).rec;
        if (a?.Dob is { } da && b?.Dob is { } dbo && da.Year > 0 && dbo.Year > 0)
            return Math.Abs(da.Year - dbo.Year);
        return null;
    }

    private static string? DobBucket(int? gap) => gap switch
    {
        null => "none",
        0 => "same",
        >= 1 and <= 5 => "close",
        _ => "far",
    };

    private static long MaskOf<TEnum>(string? csv) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(csv)) return 0;
        long mask = 0;
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (Enum.TryParse<TEnum>(part, true, out var v)) mask |= Convert.ToInt64(v);
        return mask;
    }

    private static List<string> NamesOf<TEnum>(long mask) where TEnum : struct, Enum =>
        [.. Enum.GetValues<TEnum>()
            .Where(v => Convert.ToInt64(v) != 0 && (mask & Convert.ToInt64(v)) != 0)
            .Select(v => v.ToString()!)];
}
