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
