using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Shared;
using ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;
using ShiftSoftware.ADP.Darlastic.Shared.DTOs.Steward;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.TypeAuth.Core;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// The stewardship queue (CC6 / Phase 5 v0) — the candidate pairs the engine scored inside the
/// steward band and refused to decide, plus the verdict endpoint that records a human's answer.
///
/// <para>Reads are key seeks over the registry's own <c>StewardQueue</c> / <c>StewardRecord</c>
/// tables, which every resolve stages. Nothing here re-scores anything: the queue is a derived
/// projection of the last run, so the surface stays stateless and cheap.</para>
///
/// <para>Writes go through EF against the HOST's context, not the engine's
/// <c>Registry.RecordStewardVerdict</c> — that path resolves its connection from environment
/// configuration, which is right for the batch runner and wrong for a hosted API. The two paths
/// share the verdict vocabulary and the row shapes, so a decision recorded here replays identically.</para>
/// </summary>
[Route("[controller]")]
[ApiController]
public class StewardQueueController : ControllerBase
{
    private readonly ShiftDbContext db;
    private readonly DarlasticApiOptions options;

    /// <summary>The engine serializes <c>RealRecord</c> with default options, so the staged payload
    /// carries CLR property names verbatim.</summary>
    private static readonly JsonSerializerOptions PayloadJson = new() { PropertyNameCaseInsensitive = true };

    public StewardQueueController(ShiftDbContext db, IOptions<DarlasticApiOptions> options)
    {
        this.db = db;
        this.options = options.Value;
    }

    private bool Denied(bool write)
    {
        if (!options.EnableDarlasticActionTreeAuthorization) return false;
        var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
        return !typeAuth.Can(options.Actions.ResolvedStewardQueue, write ? Access.Write : Access.Read);
    }

    /// <summary>
    /// One page of open cases, hardest-first (highest score = closest to the auto-merge line, where
    /// a wrong call costs most). Paged rather than OData-queryable because each row is a join plus
    /// two JSON payloads — a filterable projection over that is a later refinement, not v0.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<StewardQueuePageDTO>> Get(int skip = 0, int top = 25)
    {
        if (Denied(write: false)) return Forbid();

        top = Math.Clamp(top, 1, 200);
        skip = Math.Max(skip, 0);

        var queue = db.Set<StewardQueueEntry>().AsNoTracking();
        int total = await queue.CountAsync();

        var page = await queue
            .OrderByDescending(x => x.Score).ThenBy(x => x.PairKey)
            .Skip(skip).Take(top)
            .ToListAsync();

        if (page.Count == 0)
            return Ok(new StewardQueuePageDTO { Total = total, Skip = skip, Cases = [] });

        // Fetch this page's source records and identity assignments in two set-based reads rather
        // than per-case round-trips.
        var wanted = page
            .SelectMany(p => new[] { (p.SourceSystemA, p.SourceRecordIdA), (p.SourceSystemB, p.SourceRecordIdB) })
            .Distinct().ToList();
        var systems = wanted.Select(w => w.Item1).Distinct().ToList();
        var recordIds = wanted.Select(w => w.Item2).Distinct().ToList();

        var records = await db.Set<StewardRecord>().AsNoTracking()
            .Where(x => systems.Contains(x.SourceSystem) && recordIds.Contains(x.SourceRecordId))
            .ToDictionaryAsync(x => (x.SourceSystem, x.SourceRecordId), x => x.Payload);

        var identities = await db.Set<SourceProfile>().AsNoTracking()
            .Where(x => systems.Contains(x.SourceSystem) && recordIds.Contains(x.SourceRecordId) && !x.Removed)
            .ToDictionaryAsync(x => (x.SourceSystem, x.SourceRecordId), x => x.IdentityID);

        var pairKeys = page.Select(p => p.PairKey).ToList();
        var standing = await db.Set<StewardDecision>().AsNoTracking()
            .Where(x => x.Active && x.Value != null && pairKeys.Contains(x.Value))
            .ToDictionaryAsync(x => x.Value!, x => x.Kind);

        return Ok(new StewardQueuePageDTO
        {
            Total = total,
            Skip = skip,
            Cases = page.Select(p => new StewardCaseDTO
            {
                PairKey = p.PairKey,
                Score = p.Score,
                RunID = p.RunID,
                A = Side(p.SourceSystemA, p.SourceRecordIdA, records, identities),
                B = Side(p.SourceSystemB, p.SourceRecordIdB, records, identities),
                StandingVerdict = standing.GetValueOrDefault(p.PairKey),
            }).ToList(),
        });
    }

    /// <summary>
    /// Record a steward's verdict. <c>merge</c> / <c>separate</c> write an Active
    /// <c>StewardDecision</c> the next resolve replays as a hard constraint; <c>defer</c> and
    /// <c>release</c> are audited and clear any standing constraint instead of adding one.
    ///
    /// <para>The decision row and its audit row are written in one transaction: an audit entry
    /// without its constraint claims a steward decided something the engine will ignore, and a
    /// constraint without its audit entry is an unattributed change to identity resolution.</para>
    /// </summary>
    [HttpPost("verdict")]
    [Authorize]
    public async Task<ActionResult<StewardVerdictResultDTO>> Verdict([FromBody] StewardVerdictDTO input)
    {
        if (Denied(write: true)) return Forbid();

        if (input is null || string.IsNullOrWhiteSpace(input.PairKey))
            return BadRequest("A verdict needs the case's pair key.");
        if (string.IsNullOrWhiteSpace(input.Verdict) || !StewardVerdict.IsKnown(input.Verdict))
            return BadRequest($"Unknown verdict '{input.Verdict}'.");

        // The pair must be a real queued case. Without this check the endpoint would accept
        // constraints on arbitrary record pairs the engine never asked about — a write surface onto
        // clustering that bypasses the queue entirely.
        var queued = await db.Set<StewardQueueEntry>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.PairKey == input.PairKey);
        if (queued is null) return NotFound("That pair is not on the current steward queue.");

        var actor = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User?.Identity?.Name
                    ?? "unknown";

        var payload = JsonSerializer.Serialize(new
        {
            pairKey = input.PairKey,
            verdict = input.Verdict,
            score = queued.Score,
            note = input.Note,
        });

        await using var tx = await db.Database.BeginTransactionAsync();

        // Supersede rather than stack, so replay reads Active rows without tie-breaking on time.
        var prior = await db.Set<StewardDecision>()
            .Where(x => x.Active && x.Value == input.PairKey && (x.Kind == StewardVerdict.Merge || x.Kind == StewardVerdict.Separate))
            .ToListAsync();
        foreach (var p in prior) p.Active = false;

        if (StewardVerdict.IsConstraint(input.Verdict))
        {
            db.Set<StewardDecision>().Add(new StewardDecision
            {
                AtUtc = DateTime.UtcNow,
                Actor = actor,
                Kind = input.Verdict,
                // Side A in the indexed source columns; Value carries the authoritative pair key.
                SourceSystem = queued.SourceSystemA,
                SourceRecordId = queued.SourceRecordIdA,
                Value = input.PairKey,
                Active = true,
                Payload = payload,
            });
        }

        db.Set<AuditEntry>().Add(new AuditEntry
        {
            AtUtc = DateTime.UtcNow,
            Actor = actor,
            Action = input.Verdict,
            TargetKey = input.PairKey,
            Payload = payload,
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new StewardVerdictResultDTO
        {
            PairKey = input.PairKey,
            Verdict = input.Verdict,
            SupersededDecisions = prior.Count,
            IsConstraint = StewardVerdict.IsConstraint(input.Verdict),
        });
    }

    private static StewardCaseRecordDTO? Side(string system, string recordId,
        Dictionary<(string, string), string> records, Dictionary<(string, string), long> identities)
    {
        var dto = new StewardCaseRecordDTO { SourceSystem = system, SourceRecordId = recordId };

        if (identities.TryGetValue((system, recordId), out var identityId))
            dto.IdentityID = identityId.ToString(CultureInfo.InvariantCulture);

        // A queued pair whose staged record is missing means the queue and the record table were
        // written by different runs — surface the pair with what we know rather than dropping it.
        if (!records.TryGetValue((system, recordId), out var payload) || string.IsNullOrWhiteSpace(payload))
            return dto;

        var parsed = JsonSerializer.Deserialize<StagedRecord>(payload, PayloadJson);
        if (parsed is null) return dto;

        dto.RawName = parsed.RawName;
        dto.NormName = parsed.NormName;
        dto.Phones = [.. (parsed.Phones ?? []), .. (parsed.WeakPhones ?? [])];
        dto.City = string.IsNullOrWhiteSpace(parsed.NormCity) ? null : parsed.NormCity;
        dto.Address = string.IsNullOrWhiteSpace(parsed.RawAddress) ? parsed.NormAddress : parsed.RawAddress;
        dto.NationalId = parsed.NationalId;
        return dto;
    }

    /// <summary>The slice of the engine's staged <c>RealRecord</c> a steward needs. Declared here
    /// rather than referencing the Engine package: this API deliberately does not depend on the
    /// matching engine (it would pull the Cosmos client in to read two tables).</summary>
    private sealed class StagedRecord
    {
        [JsonPropertyName("RawName")] public string? RawName { get; set; }
        [JsonPropertyName("NormName")] public string? NormName { get; set; }
        [JsonPropertyName("Phones")] public string[]? Phones { get; set; }
        [JsonPropertyName("WeakPhones")] public string[]? WeakPhones { get; set; }
        [JsonPropertyName("NationalId")] public string? NationalId { get; set; }
        [JsonPropertyName("RawAddress")] public string? RawAddress { get; set; }
        [JsonPropertyName("NormAddress")] public string? NormAddress { get; set; }
        [JsonPropertyName("NormCity")] public string? NormCity { get; set; }
    }
}
