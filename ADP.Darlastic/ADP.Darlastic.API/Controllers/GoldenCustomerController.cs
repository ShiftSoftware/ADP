using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;
using ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using System.Globalization;
using System.Text.Json;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// Golden-customer list — served from the <c>[schema].[GoldenCustomer]</c> view in the HOST's
/// own database (no Cosmos round-trip; fresh as of the last resolve run). List-only by design:
/// goldens are minted and survived by the resolve engine, merged/split by stewards — consumers
/// never write them, so this is a plain authorized OData read-through, not ShiftEntity CRUD
/// (there is no ShiftEntity behind the view, and no upsert surface to generate).
/// </summary>
[Route("[controller]")]
[ApiController]
public class GoldenCustomerController : ControllerBase
{
    /// <summary>Artifact family the golden documents are staged under in <c>ProjectionState</c>.</summary>
    private const string GoldenArtifactType = "golden";

    /// <summary>Sentinel content hash meaning "delete this downstream" — a redirected-away identity.</summary>
    private const string TombstoneHash = "TOMBSTONE";

    private readonly ShiftDbContext db;
    private readonly DarlasticApiOptions options;

    public GoldenCustomerController(ShiftDbContext db, IOptions<DarlasticApiOptions> options)
    {
        this.db = db;
        this.options = options.Value;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ODataDTO<GoldenCustomerListDTO>>> Get(ODataQueryOptions<GoldenCustomerListDTO> oDataQueryOptions)
    {
        if (options.EnableDarlasticActionTreeAuthorization)
        {
            var typeAuthService = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuthService.CanRead(DarlasticActionTree.GoldenCustomers))
                return Forbid();
        }

        var query = db.Set<GoldenCustomer>().AsNoTracking().Select(x => new GoldenCustomerListDTO
        {
            ID = x.ID.ToString(),
            FullName = x.FullName,
            Phone = x.Phone,
            City = x.City,
            IDNumber = x.IDNumber,
            Email = x.Email,
            SourceCount = x.SourceCount,
        });

        // applySoftDeleteFilter off: the view already excludes tombstoned identities, and the
        // DTO's IsDeleted is a base-class constant here, not a real column.
        return Ok(await query.ToOdataDTO(oDataQueryOptions, HttpContext.Request, applySoftDeleteFilter: false));
    }

    /// <summary>
    /// One identity by ID, redirect-chased, with its survived attributes — the read a customer-detail
    /// surface opens a profile with.
    ///
    /// <para>Reads <c>ProjectionState</c> on its clustered PK <c>(ArtifactType, ArtifactKey)</c>
    /// rather than filtering the <c>GoldenCustomer</c> view, which would need
    /// <c>CAST(ArtifactKey AS bigint)</c> — non-SARGable, and OPENJSON over the whole golden slice
    /// to return one row.</para>
    ///
    /// <para>An identity minted interactively since the last resolve has no staged golden at all
    /// (see <see cref="GoldenCustomerDetailDTO.AwaitingResolve"/>). That is a 200 with the flag set,
    /// not a 404 — the identity exists, it simply has no survived attributes yet, and the caller
    /// renders its own record's values meanwhile. 404 means no such identity in the registry.</para>
    /// </summary>
    [HttpGet("{id:long}")]
    [Authorize]
    public async Task<ActionResult<GoldenCustomerDetailDTO>> Detail(long id)
    {
        if (options.EnableDarlasticActionTreeAuthorization)
        {
            var typeAuthService = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuthService.CanRead(DarlasticActionTree.GoldenCustomers))
                return Forbid();
        }

        // Chase redirects first: a merge never deletes, so a caller holding a merged-away ID must
        // still land on the survivor. Bounded — a corrupt cycle must not spin a request forever.
        long live = id;
        for (int hop = 0; hop < 64; hop++)
        {
            var next = await db.Set<IdentityRedirect>().AsNoTracking()
                .Where(x => x.OldIdentityID == live)
                .Select(x => (long?)x.NewIdentityID)
                .FirstOrDefaultAsync();
            if (next is null) break;
            live = next.Value;
        }

        var identity = await db.Set<GoldenIdentity>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdentityID == live);

        if (identity is null)
            return NotFound();

        var dto = new GoldenCustomerDetailDTO
        {
            ID = live.ToString(CultureInfo.InvariantCulture),
            RequestedID = id.ToString(CultureInfo.InvariantCulture),
            WasRedirected = live != id,
            Status = identity.Status,
            CreatedRunID = identity.CreatedRunID,
            LastChangedRunID = identity.LastChangedRunID,
        };

        var key = live.ToString(CultureInfo.InvariantCulture);
        var staged = await db.Set<ProjectionState>().AsNoTracking()
            .Where(x => x.ArtifactType == GoldenArtifactType && x.ArtifactKey == key)
            .Select(x => new { x.ContentHash, x.Payload })
            .FirstOrDefaultAsync();

        // No staged golden — minted interactively, not yet resolved. Tombstoned counts the same for
        // a reader: there is nothing to render, and the redirect chase above already pointed at the
        // survivor if one exists.
        if (staged?.Payload is null || staged.ContentHash == TombstoneHash)
        {
            dto.AwaitingResolve = true;
            return Ok(dto);
        }

        ApplyStagedAttributes(dto, staged.Payload);
        return Ok(dto);
    }

    /// <summary>
    /// Unpack the staged golden payload — <c>{"goldenId":N,"attrs":[{"t","v"}...],"members":[...]}</c>
    /// — using LAST-WINS per attribute type, which is what the Cosmos drain does. The engine stages
    /// attrs sorted by (type, value), so last-wins equals the view's <c>MAX(v)</c>: this endpoint,
    /// the view and the drained documents cannot disagree about which value survived.
    /// </summary>
    private static void ApplyStagedAttributes(GoldenCustomerDetailDTO dto, string payload)
    {
        using var doc = JsonDocument.Parse(payload);

        if (doc.RootElement.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Array)
        {
            foreach (var attr in attrs.EnumerateArray())
            {
                if (!attr.TryGetProperty("t", out var t) || !attr.TryGetProperty("v", out var v)) continue;
                var value = v.GetString();
                switch (t.GetString())
                {
                    case "full_name": dto.FullName = value; break;
                    case "phone": dto.Phone = value; break;
                    case "city": dto.City = value; break;
                    case "national_id": dto.IDNumber = value; break;
                    case "email": dto.Email = value; break;
                }
            }
        }

        if (doc.RootElement.TryGetProperty("members", out var members) && members.ValueKind == JsonValueKind.Array)
            dto.SourceCount = members.GetArrayLength();
    }

    /// <summary>
    /// One identity's provenance: the source records it unifies, its lifecycle, and the identities
    /// merged into it. Every read is a key seek — the identity by PK, its profiles on
    /// <c>IX_SourceProfile_Identity</c> — so this is cheap enough to open per row.
    ///
    /// Note what this is NOT: the engine's *reasoning*. Survivorship reasons and pair scores are
    /// computed during a resolve and never staged, so no read can surface them. See
    /// <see cref="GoldenCustomerSourcesDTO"/>.
    /// </summary>
    [HttpGet("{id:long}/sources")]
    [Authorize]
    public async Task<ActionResult<GoldenCustomerSourcesDTO>> Sources(long id)
    {
        if (options.EnableDarlasticActionTreeAuthorization)
        {
            var typeAuthService = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuthService.CanRead(DarlasticActionTree.GoldenCustomers))
                return Forbid();
        }

        var identity = await db.Set<GoldenIdentity>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdentityID == id);

        if (identity is null)
            return NotFound();

        var sources = await db.Set<SourceProfile>().AsNoTracking()
            .Where(x => x.IdentityID == id)
            .OrderBy(x => x.SourceSystem).ThenBy(x => x.SourceRecordId)
            .Select(x => new GoldenCustomerSourceDTO
            {
                SourceSystem = x.SourceSystem,
                SourceRecordId = x.SourceRecordId,
                Removed = x.Removed,
                FirstRunID = x.FirstRunID,
                LastChangedRunID = x.LastChangedRunID,
            })
            .ToListAsync();

        // Which identities were absorbed into this one. IdentityRedirect is keyed on OldIdentityID,
        // so this direction is a scan — fine while merges are rare, but if a tenant's redirect table
        // grows into the millions this wants its own index on NewIdentityID.
        var absorbed = await db.Set<IdentityRedirect>().AsNoTracking()
            .Where(x => x.NewIdentityID == id)
            .OrderBy(x => x.OldIdentityID)
            .Select(x => x.OldIdentityID)
            .ToListAsync();

        return Ok(new GoldenCustomerSourcesDTO
        {
            ID = id.ToString(CultureInfo.InvariantCulture),
            Status = identity.Status,
            CreatedRunID = identity.CreatedRunID,
            LastChangedRunID = identity.LastChangedRunID,
            Sources = sources,
            AbsorbedIdentityIDs = absorbed.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
        });
    }
}
