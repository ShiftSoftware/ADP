using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ADP.Darlastic.Shared;
using ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;
using ShiftSoftware.ADP.Darlastic.Shared.DTOs.Cases;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.TypeAuth.Core;
using System.Security.Claims;
using System.Text.Json;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// The case browser, hosted. Serves the categorised corpus view, per-case evidence with the live
/// engine's derivation, identity assembly, and the human-decision surfaces (review flags, label
/// audits) that used to be files on one laptop.
///
/// <para><b>Reads tables, never an index.</b> The prototype built an in-memory index of the whole
/// corpus at startup — minutes and gigabytes, fine for a laptop and impossible for a hosted app
/// that recycles. Everything here is a key seek or a bounded page over what the resolve staged:
/// <c>CaseCategoryCount</c> (exact corpus totals), <c>CaseCatalog</c> (the browsable sample),
/// <c>StewardRecord</c> (the evidence), <c>IdentityEdge</c> (assembly).</para>
///
/// <para><b>Recomputes the derivation, never stages it.</b> A pair's trace is a pure function of
/// its two staged records, so it is computed per request through <c>ADP.Darlastic.Engine.Core</c>.
/// Staging it instead would let it go stale against an engine change with nothing to signal that —
/// and because it is recomputed, the surface can compare live against staged and SAY when the
/// engine has moved (<see cref="CaseDetailDTO.EngineDrift"/>).</para>
///
/// <para>Writes go through the host's EF context, matching <c>StewardQueueController</c>: the
/// engine's own registry path resolves its connection from environment configuration, which is
/// right for the batch runner and wrong here.</para>
/// </summary>
[Route("[controller]")]
[ApiController]
public class CaseBrowserController : ControllerBase
{
    private readonly ShiftDbContext db;
    private readonly DarlasticApiOptions options;

    /// <summary>The engine serializes <c>RealRecord</c> with default options, so the staged payload
    /// carries CLR property names verbatim.</summary>
    private static readonly JsonSerializerOptions PayloadJson = new() { PropertyNameCaseInsensitive = true };

    public CaseBrowserController(ShiftDbContext db, IOptions<DarlasticApiOptions> options)
    {
        this.db = db;
        this.options = options.Value;
    }

    // Seeing how the engine decided the whole corpus and being trusted to change how it resolves
    // are different permissions, so this rides the StewardQueue node's read grant and its own
    // write grant — never GoldenCustomers.
    private bool Denied(bool write)
    {
        if (!options.EnableDarlasticActionTreeAuthorization) return false;
        var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
        return !typeAuth.Can(options.Actions.ResolvedStewardQueue, write ? Access.Write : Access.Read);
    }

    private string Actor() =>
        User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.Identity?.Name
        ?? "unknown";

    // ------------------------------------------------------------------ reads

    /// <summary>Sidebar + header. Corpus totals and browsable counts are returned separately and
    /// deliberately — see <see cref="CaseSummaryDTO"/>.</summary>
    [HttpGet("Summary")]
    [Authorize]
    public async Task<ActionResult<CaseSummaryDTO>> Summary(CancellationToken ct)
    {
        if (Denied(write: false)) return Forbid();

        var counts = await db.Set<CaseCategoryCount>().AsNoTracking().ToListAsync(ct);
        var run = counts.Count > 0 ? counts.Max(c => c.RunID) : 0;

        var dto = new CaseSummaryDTO
        {
            RunID = run,
            RegistryProfiles = await db.Set<SourceProfile>().AsNoTracking().CountAsync(ct),
            RegistryIdentities = await db.Set<GoldenIdentity>().AsNoTracking()
                .CountAsync(i => i.Status == IdentityStatus.Active, ct),
            QueueDepth = await db.Set<StewardQueueEntry>().AsNoTracking().CountAsync(ct),
            OpenFlags = await db.Set<ReviewFlag>().AsNoTracking().CountAsync(f => f.Response == null, ct),
            Audits = await db.Set<LabelAudit>().AsNoTracking().CountAsync(ct),
            Categories = [.. counts
                .OrderByDescending(c => c.Total)
                .Select(c => new CaseCategoryDTO { Category = c.Category, Total = c.Total, Browsable = c.Sampled })],
        };

        dto.Sources = await db.Set<StewardRecord>().AsNoTracking()
            .GroupBy(r => r.SourceSystem)
            .Select(g => new { g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.N, ct);

        return dto;
    }

    /// <summary>
    /// A page of cases, optionally filtered to one category. Ordered by score descending, which is
    /// stable because the catalog is written in a deterministic order.
    /// </summary>
    [HttpGet("Cases")]
    [Authorize]
    public async Task<ActionResult<CasePageDTO>> Cases(
        string? category, string? source, float? minScore, float? maxScore,
        int skip = 0, int take = 50, CancellationToken ct = default)
    {
        if (Denied(write: false)) return Forbid();
        take = Math.Clamp(take, 1, 200);

        long catMask = 0;
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.TryParse<CaseCat>(category, ignoreCase: true, out var parsed))
                return BadRequest(new { error = $"unknown category '{category}'" });
            catMask = (long)parsed;
        }

        var q = db.Set<CaseCatalogEntry>().AsNoTracking().AsQueryable();
        // Bitmask test in SQL: the categories column is a CaseCat flags value.
        if (catMask != 0) q = q.Where(c => (c.Categories & catMask) != 0);
        if (minScore is not null) q = q.Where(c => c.Score >= minScore);
        if (maxScore is not null) q = q.Where(c => c.Score < maxScore);
        if (!string.IsNullOrWhiteSpace(source))
            q = q.Where(c => c.SourceSystemA == source || c.SourceSystemB == source);

        int total = await q.CountAsync(ct);
        var page = await q.OrderByDescending(c => c.Score).ThenBy(c => c.PairKey)
            .Skip(skip).Take(take).ToListAsync(ct);

        var result = new CasePageDTO { Total = total, Skip = skip };

        if (catMask != 0)
        {
            var cc = await db.Set<CaseCategoryCount>().AsNoTracking()
                .FirstOrDefaultAsync(c => c.Category == category, ct);
            if (cc is not null)
            {
                result.CategoryTotal = cc.Total;
                // The catalog is capped per category; say so rather than let a full-looking page
                // imply the population is this small.
                result.Capped = cc.Total > cc.Sampled;
            }
        }

        if (page.Count == 0) return result;

        var sides = await LoadSides(page.SelectMany(p =>
            new[] { (p.SourceSystemA, p.SourceRecordIdA), (p.SourceSystemB, p.SourceRecordIdB) }), ct);
        var queued = await QueuedKeys(page.Select(p => p.PairKey), ct);
        var flagged = await FlaggedTargets(page.Select(p => p.PairKey), ct);

        result.Cases = [.. page.Select(p => new CaseListItemDTO
        {
            PairKey = p.PairKey,
            Score = p.Score,
            Categories = NamesOf((CaseCat)p.Categories),
            Rules = NamesOf((MatchFlags)p.Flags),
            A = sides.GetValueOrDefault((p.SourceSystemA, p.SourceRecordIdA)),
            B = sides.GetValueOrDefault((p.SourceSystemB, p.SourceRecordIdB)),
            Queued = queued.Contains(p.PairKey),
            Flagged = flagged.Contains(p.PairKey),
        })];
        return result;
    }

    /// <summary>One case, with the live engine's derivation recomputed from the staged records.</summary>
    [HttpGet("Case")]
    [Authorize]
    public async Task<ActionResult<CaseDetailDTO>> Case(string pairKey, CancellationToken ct)
    {
        if (Denied(write: false)) return Forbid();
        if (string.IsNullOrWhiteSpace(pairKey)) return BadRequest(new { error = "pairKey required" });

        var detail = await BuildDetail(pairKey, ct);
        return detail is null ? NotFound(new { error = "pair not staged", pairKey }) : detail;
    }

    /// <summary>How an identity was assembled: members plus the edges that joined them.</summary>
    [HttpGet("Identity/{identityId:long}")]
    [Authorize]
    public async Task<ActionResult<IdentityAssemblyDTO>> Identity(long identityId, CancellationToken ct)
    {
        if (Denied(write: false)) return Forbid();

        var edges = await db.Set<IdentityEdge>().AsNoTracking()
            .Where(e => e.IdentityID == identityId)
            .OrderByDescending(e => e.Score)
            // A dense cluster can carry tens of thousands of corroborating edges (max measured:
            // 28,622). Nobody reads past the strongest few hundred, and shipping them all would
            // make this endpoint the slowest thing in the app.
            .Take(200)
            .ToListAsync(ct);

        var members = await db.Set<SourceProfile>().AsNoTracking()
            .Where(p => p.IdentityID == identityId)
            .Select(p => new { p.SourceSystem, p.SourceRecordId })
            .ToListAsync(ct);

        if (edges.Count == 0 && members.Count == 0)
            return NotFound(new { error = "identity not found", identityId });

        var sides = await LoadSides(members.Select(m => (m.SourceSystem, m.SourceRecordId)), ct);

        return new IdentityAssemblyDTO
        {
            IdentityID = identityId,
            GoldenName = await GoldenNameOf(identityId, ct),
            MemberCount = members.Count,
            SourceCount = members.Select(m => m.SourceSystem).Distinct().Count(),
            // Only members staged for browsing have evidence; the rest are named by key alone.
            Members = [.. members.Select(m => sides.GetValueOrDefault((m.SourceSystem, m.SourceRecordId))
                ?? new CaseSideDTO { SourceSystem = m.SourceSystem, SourceRecordId = m.SourceRecordId })],
            Edges = [.. edges.Select(e => new IdentityEdgeDTO
            {
                PairKey = $"{e.SourceSystemA}:{e.SourceRecordIdA}~{e.SourceSystemB}:{e.SourceRecordIdB}",
                Score = e.Score,
                Rules = NamesOf((MatchFlags)e.Flags),
            })],
        };
    }

    /// <summary>
    /// Identities, biggest first — the "what did the engine actually assemble" list. Served from
    /// the staged <c>IdentitySummary</c>: grouping <c>SourceProfile</c> per page would scan the
    /// whole corpus, and the interesting categories are not derivable by any query at all.
    /// </summary>
    [HttpGet("Identities")]
    [Authorize]
    public async Task<ActionResult<IdentityPageDTO>> Identities(
        string? category, int minMembers = 2, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        if (Denied(write: false)) return Forbid();
        take = Math.Clamp(take, 1, 200);

        long catMask = 0;
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.TryParse<ClusterCat>(category, ignoreCase: true, out var parsed))
                return BadRequest(new { error = $"unknown identity category '{category}'" });
            catMask = (long)parsed;
        }

        var q = db.Set<IdentitySummary>().AsNoTracking().Where(s => s.MemberCount >= minMembers);
        if (catMask != 0) q = q.Where(s => (s.Categories & catMask) != 0);

        int total = await q.CountAsync(ct);
        var page = await q.OrderByDescending(s => s.MemberCount).ThenBy(s => s.IdentityID)
            .Skip(skip).Take(take).ToListAsync(ct);

        return new IdentityPageDTO
        {
            Total = total,
            Skip = skip,
            Identities = [.. page.Select(s => new IdentityListItemDTO
            {
                IdentityID = s.IdentityID,
                GoldenName = s.GoldenName,
                MemberCount = s.MemberCount,
                SourceCount = s.SourceCount,
                Categories = NamesOf((ClusterCat)s.Categories),
            })],
        };
    }

    /// <summary>
    /// Find a staged record by name or phone.
    ///
    /// <para>Searches the JSON payloads directly. That is a scan, and it is the right call here:
    /// the staged set is ~10⁴ records, the alternative is denormalising name and phone into columns
    /// the engine would then have to keep in step, and this endpoint is a navigation aid rather
    /// than a hot path. If the staged set grows an order of magnitude, promote the columns.</para>
    ///
    /// <para>Note the scope: this searches what is STAGED for browsing (queue + catalog), not the
    /// corpus. Finding an arbitrary customer is what the golden list is for.</para>
    /// </summary>
    [HttpGet("Search")]
    [Authorize]
    public async Task<ActionResult<List<CaseSideDTO>>> Search(string q, int take = 50, CancellationToken ct = default)
    {
        if (Denied(write: false)) return Forbid();
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return BadRequest(new { error = "query must be at least 2 characters" });

        take = Math.Clamp(take, 1, 200);
        string term = q.Trim();
        string like = "%" + term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";

        var rows = await db.Set<StewardRecord>().FromSqlRaw("""
            SELECT TOP (@take) SourceSystem, SourceRecordId, RunID, Payload
            FROM Darlastic.StewardRecord
            WHERE JSON_VALUE(Payload, '$.NormName') LIKE @like
               OR JSON_VALUE(Payload, '$.RawName')  LIKE @like
               OR SourceRecordId = @exact
               OR EXISTS (SELECT 1 FROM OPENJSON(Payload, '$.Phones') p WHERE p.value LIKE @like)
            """,
            new Microsoft.Data.SqlClient.SqlParameter("@take", take),
            new Microsoft.Data.SqlClient.SqlParameter("@like", like),
            new Microsoft.Data.SqlClient.SqlParameter("@exact", term))
            .AsNoTracking().ToListAsync(ct);

        var ids = await IdentitiesOf(rows.Select(r => (r.SourceSystem, r.SourceRecordId)), ct);
        return rows.Select(r => Revive(r) is { } rec
                ? ToSide(rec, ids.GetValueOrDefault((r.SourceSystem, r.SourceRecordId)))
                : new CaseSideDTO { SourceSystem = r.SourceSystem, SourceRecordId = r.SourceRecordId })
            .ToList();
    }

    /// <summary>
    /// The current case filter as CSV, for adjudication outside the browser.
    ///
    /// <para>Bounded at 5,000 rows — the catalog is a capped sample anyway, so an unbounded export
    /// would promise a completeness the underlying table does not have. The corpus total travels in
    /// the header comment so a spreadsheet cannot be mistaken for the population.</para>
    /// </summary>
    [HttpGet("Export")]
    [Authorize]
    public async Task<IActionResult> Export(string? category, int take = 5000, CancellationToken ct = default)
    {
        if (Denied(write: false)) return Forbid();
        take = Math.Clamp(take, 1, 5000);

        var page = await Cases(category, source: null, minScore: null, maxScore: null, skip: 0, take: 200, ct);
        if (page.Result is not null && page.Value is null) return page.Result;

        // Re-page through the filter rather than one huge query, so the export path uses the same
        // shaping the UI does and cannot silently diverge from what the user is looking at.
        var all = new List<CaseListItemDTO>();
        long? corpusTotal = page.Value?.CategoryTotal;
        for (int skip = 0; skip < take; skip += 200)
        {
            var p = await Cases(category, null, null, null, skip, Math.Min(200, take - skip), ct);
            if (p.Value is null || p.Value.Cases.Count == 0) break;
            all.AddRange(p.Value.Cases);
            if (all.Count >= p.Value.Total) break;
        }

        var sb = new System.Text.StringBuilder();
        sb.Append("# darlastic case export");
        if (category is not null) sb.Append($" · category={category}");
        if (corpusTotal is not null) sb.Append($" · corpus total={corpusTotal} · exported={all.Count}");
        sb.AppendLine();
        sb.AppendLine("pair_key,score,categories,rules,queued,src_a,id_a,name_a,phones_a,city_a,src_b,id_b,name_b,phones_b,city_b");
        foreach (var c in all)
            sb.AppendLine(string.Join(",",
                Csv(c.PairKey), c.Score.ToString("0.000"), Csv(string.Join("|", c.Categories)),
                Csv(string.Join("|", c.Rules)), c.Queued,
                Csv(c.A?.SourceSystem), Csv(c.A?.SourceRecordId), Csv(c.A?.NormName),
                Csv(string.Join("|", c.A?.Phones ?? [])), Csv(c.A?.City),
                Csv(c.B?.SourceSystem), Csv(c.B?.SourceRecordId), Csv(c.B?.NormName),
                Csv(string.Join("|", c.B?.Phones ?? [])), Csv(c.B?.City)));

        return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv",
            $"darlastic-cases-{category ?? "all"}.csv");

        static string Csv(string? s) =>
            s is null ? "" : s.Contains(',') || s.Contains('"') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }

    /// <summary>Open review flags, newest first — the "needs a second opinion" queue.</summary>
    [HttpGet("Flags")]
    [Authorize]
    public async Task<ActionResult<List<ReviewFlagDTO>>> Flags(bool includeAnswered = false, CancellationToken ct = default)
    {
        if (Denied(write: false)) return Forbid();
        var q = db.Set<ReviewFlag>().AsNoTracking().AsQueryable();
        if (!includeAnswered) q = q.Where(f => f.Response == null);
        var rows = await q.OrderByDescending(f => f.CreatedUtc).Take(500).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    /// <summary>Every adjudication recorded against a pair, oldest first.</summary>
    [HttpGet("Audits")]
    [Authorize]
    public async Task<ActionResult<List<LabelAuditDTO>>> Audits(string? pairKey, CancellationToken ct = default)
    {
        if (Denied(write: false)) return Forbid();
        var q = db.Set<LabelAudit>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(pairKey)) q = q.Where(a => a.PairKey == pairKey);
        var rows = await q.OrderBy(a => a.AuditedUtc).Take(1000).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    // ------------------------------------------------------------------ writes

    /// <summary>Flag a case for discussion, or edit an existing flag. One open flag per target.</summary>
    [HttpPost("Flag")]
    [Authorize]
    public async Task<ActionResult<ReviewFlagDTO>> Flag([FromBody] ReviewFlagInputDTO input, CancellationToken ct)
    {
        if (Denied(write: true)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Target)) return BadRequest(new { error = "target required" });
        if (string.IsNullOrWhiteSpace(input.Comment)) return BadRequest(new { error = "comment required" });

        // A flag with no evidence is a flag nobody can action later, so build the snapshot here
        // when the caller did not supply one.
        string snapshot = input.Snapshot ?? "";
        if (snapshot.Length == 0 && input.Target.Contains('~'))
        {
            var detail = await BuildDetail(input.Target, ct);
            if (detail is not null) snapshot = JsonSerializer.Serialize(detail);
        }

        var existing = await db.Set<ReviewFlag>().FirstOrDefaultAsync(f => f.Target == input.Target, ct);
        if (existing is null)
        {
            existing = new ReviewFlag
            {
                Target = input.Target,
                Topic = input.Topic ?? "",
                Comment = input.Comment,
                Author = Actor(),
                CreatedUtc = DateTime.UtcNow,
                Snapshot = snapshot,
            };
            db.Add(existing);
        }
        else
        {
            existing.Topic = input.Topic ?? existing.Topic;
            existing.Comment = input.Comment;
            existing.UpdatedUtc = DateTime.UtcNow;
            // Re-flagging refreshes the evidence: the point of editing is usually that the case
            // has moved.
            if (snapshot.Length > 0) existing.Snapshot = snapshot;
        }

        await db.SaveChangesAsync(ct);
        return ToDto(existing);
    }

    /// <summary>Answer a flag. The flag stays, carrying both the note and the response.</summary>
    [HttpPost("Flag/Respond")]
    [Authorize]
    public async Task<ActionResult<ReviewFlagDTO>> RespondToFlag([FromBody] ReviewFlagResponseInputDTO input, CancellationToken ct)
    {
        if (Denied(write: true)) return Forbid();

        var flag = await db.Set<ReviewFlag>().FirstOrDefaultAsync(f => f.Target == input.Target, ct);
        if (flag is null) return NotFound(new { error = "no flag on that target", input.Target });

        flag.Response = input.Response;
        flag.ResponseBy = Actor();
        flag.ResponseUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(flag);
    }

    [HttpDelete("Flag")]
    [Authorize]
    public async Task<IActionResult> Unflag(string target, CancellationToken ct)
    {
        if (Denied(write: true)) return Forbid();
        var flag = await db.Set<ReviewFlag>().FirstOrDefaultAsync(f => f.Target == target, ct);
        if (flag is null) return NoContent();
        db.Remove(flag);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Record an adjudication of a pair's label — the corpus-growth row. Append-only: a pair can be
    /// re-adjudicated and the history is the point, so this never updates a prior row.
    /// </summary>
    [HttpPost("Audit")]
    [Authorize]
    public async Task<ActionResult<LabelAuditDTO>> Audit([FromBody] LabelAuditInputDTO input, CancellationToken ct)
    {
        if (Denied(write: true)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.PairKey)) return BadRequest(new { error = "pairKey required" });

        string? label = string.IsNullOrWhiteSpace(input.NewLabel) ? null : input.NewLabel.Trim().ToLowerInvariant();
        if (label is not null && label is not ("same" or "different" or "ambiguous"))
            return BadRequest(new { error = $"label must be same/different/ambiguous (or empty for pending), got '{label}'" });

        var row = new LabelAudit
        {
            PairKey = input.PairKey,
            OldLabel = input.OldLabel,
            NewLabel = label,
            AuditedBy = Actor(),
            AuditedUtc = DateTime.UtcNow,
            PanelVotes = input.PanelVotes,
            Rationale = input.Rationale,
            // An empty label is an explicit "pending an expert call" and must never be folded into
            // the gold set — the CSV format carried that convention and it survives here.
            Status = label is null ? "pending" : "pending",
        };
        db.Add(row);
        await db.SaveChangesAsync(ct);
        return ToDto(row);
    }

    // ------------------------------------------------------------------ helpers

    private async Task<CaseDetailDTO?> BuildDetail(string pairKey, CancellationToken ct)
    {
        var cat = await db.Set<CaseCatalogEntry>().AsNoTracking().FirstOrDefaultAsync(c => c.PairKey == pairKey, ct);
        var queue = await db.Set<StewardQueueEntry>().AsNoTracking().FirstOrDefaultAsync(q => q.PairKey == pairKey, ct);
        if (cat is null && queue is null) return null;

        (string sysA, string idA, string sysB, string idB, float staged) = cat is not null
            ? (cat.SourceSystemA, cat.SourceRecordIdA, cat.SourceSystemB, cat.SourceRecordIdB, cat.Score)
            : (queue!.SourceSystemA, queue.SourceRecordIdA, queue.SourceSystemB, queue.SourceRecordIdB, queue.Score);

        var payloads = await db.Set<StewardRecord>().AsNoTracking()
            .Where(r => (r.SourceSystem == sysA && r.SourceRecordId == idA)
                     || (r.SourceSystem == sysB && r.SourceRecordId == idB))
            .ToListAsync(ct);

        var recA = Revive(payloads.FirstOrDefault(p => p.SourceSystem == sysA && p.SourceRecordId == idA));
        var recB = Revive(payloads.FirstOrDefault(p => p.SourceSystem == sysB && p.SourceRecordId == idB));

        var dto = new CaseDetailDTO
        {
            PairKey = pairKey,
            StagedScore = staged,
            Queued = queue is not null,
            Categories = cat is not null ? NamesOf((CaseCat)cat.Categories) : [],
            Rules = cat is not null ? NamesOf((MatchFlags)cat.Flags) : [],
        };

        var ids = await IdentitiesOf([(sysA, idA), (sysB, idB)], ct);
        dto.A = recA is null ? null : ToSide(recA, ids.GetValueOrDefault((sysA, idA)));
        dto.B = recB is null ? null : ToSide(recB, ids.GetValueOrDefault((sysB, idB)));
        if (dto.A?.IdentityID is long ia && dto.B?.IdentityID == ia) dto.IdentityID = ia;

        // The derivation, from the live engine. This is the whole reason .API references
        // Engine.Core rather than staging a trace that could quietly go stale.
        if (recA is not null && recB is not null)
        {
            var trace = new MatchTrace();
            dto.LiveScore = RealMatcher.Explain(recA, recB, trace);
            dto.Trace = [.. trace.Steps.Select(s => new TraceStepDTO
            {
                Stage = s.Stage, Title = s.Title, Detail = s.Detail, ConfidenceAfter = s.ConfAfter,
            })];

            // The record-level half of the walkthrough, and why the two were ever compared. Both
            // are pure functions of a single record, so neither needs staging — the same reason
            // the trace does not.
            dto.NormalizeA = [.. Normalization.Steps(recA).Select(s => new NormalizeStepDTO { Stage = s.Stage, Detail = s.Detail })];
            dto.NormalizeB = [.. Normalization.Steps(recB).Select(s => new NormalizeStepDTO { Stage = s.Stage, Detail = s.Detail })];
            dto.SharedBlockKeys = [.. RealMatcher.BlockKeysOf(recA).Intersect(RealMatcher.BlockKeysOf(recB), StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            dto.Decision = dto.LiveScore >= 0.90 ? "auto-merge" : dto.LiveScore >= 0.80 ? "steward-queue" : "kept-separate";
            if (recA.Dob is { } da && recB.Dob is { } dbo && da.Year > 0 && dbo.Year > 0)
                dto.DobGapYears = Math.Abs(da.Year - dbo.Year);
            // Tolerance is well below any decision boundary — this flags a real engine change,
            // not float noise.
            dto.EngineDrift = Math.Abs(dto.LiveScore - staged) > 0.0005;
            if (dto.Rules.Count == 0)
            {
                RealMatcher.Score(recA, recB, out var flags);
                dto.Rules = NamesOf(flags);
                dto.Categories = NamesOf(CaseCategories.Categorize(recA, recB, dto.LiveScore, flags));
            }
        }

        var decision = await db.Set<StewardDecision>().AsNoTracking()
            .Where(d => d.Value == pairKey && d.Active)
            .OrderByDescending(d => d.DecisionID)
            .FirstOrDefaultAsync(ct);
        dto.StandingVerdict = decision?.Kind;

        var flag = await db.Set<ReviewFlag>().AsNoTracking().FirstOrDefaultAsync(f => f.Target == pairKey, ct);
        dto.Flag = flag is null ? null : ToDto(flag);

        dto.Audits = [.. (await db.Set<LabelAudit>().AsNoTracking()
            .Where(a => a.PairKey == pairKey).OrderBy(a => a.AuditedUtc).ToListAsync(ct)).Select(ToDto)];

        return dto;
    }

    private async Task<Dictionary<(string, string), CaseSideDTO>> LoadSides(
        IEnumerable<(string Sys, string Id)> keys, CancellationToken ct)
    {
        var wanted = keys.Distinct().ToList();
        if (wanted.Count == 0) return [];

        // EF cannot translate a tuple-set contains, so filter by the (small) distinct source list
        // and the id list, then narrow exactly in memory. Both lists are page-bounded.
        var systems = wanted.Select(k => k.Sys).Distinct().ToList();
        var recIds = wanted.Select(k => k.Id).Distinct().ToList();
        var rows = await db.Set<StewardRecord>().AsNoTracking()
            .Where(r => systems.Contains(r.SourceSystem) && recIds.Contains(r.SourceRecordId))
            .ToListAsync(ct);

        var want = wanted.ToHashSet();
        var ids = await IdentitiesOf(wanted, ct);
        var result = new Dictionary<(string, string), CaseSideDTO>();
        foreach (var r in rows)
        {
            var key = (r.SourceSystem, r.SourceRecordId);
            if (!want.Contains(key)) continue;
            var rec = Revive(r);
            if (rec is not null) result[key] = ToSide(rec, ids.GetValueOrDefault(key));
        }
        return result;
    }

    private async Task<Dictionary<(string, string), long?>> IdentitiesOf(
        IEnumerable<(string Sys, string Id)> keys, CancellationToken ct)
    {
        var wanted = keys.Distinct().ToList();
        if (wanted.Count == 0) return [];
        var systems = wanted.Select(k => k.Sys).Distinct().ToList();
        var recIds = wanted.Select(k => k.Id).Distinct().ToList();
        var rows = await db.Set<SourceProfile>().AsNoTracking()
            .Where(p => systems.Contains(p.SourceSystem) && recIds.Contains(p.SourceRecordId))
            .Select(p => new { p.SourceSystem, p.SourceRecordId, p.IdentityID })
            .ToListAsync(ct);
        var want = wanted.ToHashSet();
        return rows.Where(r => want.Contains((r.SourceSystem, r.SourceRecordId)))
            .ToDictionary(r => (r.SourceSystem, r.SourceRecordId), r => (long?)r.IdentityID);
    }

    private async Task<HashSet<string>> QueuedKeys(IEnumerable<string> keys, CancellationToken ct)
    {
        var list = keys.Distinct().ToList();
        return (await db.Set<StewardQueueEntry>().AsNoTracking()
            .Where(q => list.Contains(q.PairKey)).Select(q => q.PairKey).ToListAsync(ct)).ToHashSet();
    }

    private async Task<HashSet<string>> FlaggedTargets(IEnumerable<string> keys, CancellationToken ct)
    {
        var list = keys.Distinct().ToList();
        return (await db.Set<ReviewFlag>().AsNoTracking()
            .Where(f => list.Contains(f.Target)).Select(f => f.Target).ToListAsync(ct)).ToHashSet();
    }

    /// <summary>
    /// The identity's survived name, read from the staged golden payload.
    ///
    /// <para>Read from <c>ProjectionState</c> on its clustered PK rather than the
    /// <c>GoldenCustomer</c> view: the view is created by a HOST migration, so it is absent from a
    /// registry the engine bootstrapped for local dev, and filtering it by id needs a non-SARGable
    /// cast anyway (the same reason <c>{id}/sources</c> refuses to carry a display name).</para>
    /// </summary>
    private async Task<string?> GoldenNameOf(long identityId, CancellationToken ct)
    {
        var row = await db.Set<ProjectionState>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.ArtifactType == "golden"
                                   && p.ArtifactKey == identityId.ToString(), ct);
        if (row?.Payload is not { Length: > 0 }) return null;
        try
        {
            using var doc = JsonDocument.Parse(row.Payload);
            if (!doc.RootElement.TryGetProperty("attrs", out var attrs)) return null;
            foreach (var a in attrs.EnumerateArray())
                if (a.TryGetProperty("t", out var t) && t.GetString() == "full_name"
                    && a.TryGetProperty("v", out var v))
                    return v.GetString();
        }
        catch (JsonException) { /* a malformed payload costs a display name, not the endpoint */ }
        return null;
    }

    private static RealRecord? Revive(StewardRecord? row)
    {
        if (row is null) return null;
        try { return JsonSerializer.Deserialize<RealRecord>(row.Payload, PayloadJson); }
        catch (JsonException) { return null; }   // a corrupt payload loses one case, not the page
    }

    private static CaseSideDTO ToSide(RealRecord r, long? identityId) => new()
    {
        SourceSystem = r.SourceSystem,
        SourceRecordId = r.SourceRecordId,
        RawName = r.RawName,
        NormName = r.NormName,
        Phones = [.. r.Phones],
        WeakPhones = [.. r.WeakPhones],
        NationalId = r.NationalId,
        City = string.IsNullOrEmpty(r.NormCity) ? null : r.NormCity,
        Address = string.IsNullOrEmpty(r.RawAddress) ? null : r.RawAddress,
        Gender = r.Gender,
        Emails = r.Emails is null ? [] : [.. r.Emails],
        NameWasMojibake = r.NameWasMojibake,
        NameHadArabizi = r.NameHadArabizi,
        IdentityID = identityId,
    };

    /// <summary>Set flag members as names — the UI shows rules, not bitmasks.</summary>
    private static List<string> NamesOf<T>(T flags) where T : struct, Enum =>
        [.. Enum.GetValues<T>()
            .Where(v => Convert.ToInt64(v) != 0 && (Convert.ToInt64(flags) & Convert.ToInt64(v)) == Convert.ToInt64(v))
            .Select(v => v.ToString())];

    private static ReviewFlagDTO ToDto(ReviewFlag f) => new()
    {
        FlagID = f.FlagID, Target = f.Target, Topic = f.Topic, Comment = f.Comment,
        Author = f.Author, CreatedUtc = f.CreatedUtc, UpdatedUtc = f.UpdatedUtc,
        Response = f.Response, ResponseBy = f.ResponseBy, ResponseUtc = f.ResponseUtc,
    };

    private static LabelAuditDTO ToDto(LabelAudit a) => new()
    {
        AuditID = a.AuditID, PairKey = a.PairKey, OldLabel = a.OldLabel, NewLabel = a.NewLabel,
        AuditedBy = a.AuditedBy, AuditedUtc = a.AuditedUtc, PanelVotes = a.PanelVotes,
        Rationale = a.Rationale, Status = a.Status,
    };
}
