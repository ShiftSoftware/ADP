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
}
