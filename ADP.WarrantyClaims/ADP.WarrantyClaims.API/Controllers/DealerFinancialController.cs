using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.WarrantyClaims.API.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Financial;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

/// <summary>
/// The dealer-side financial analytics list. Moved from the original host application (Phase 3
/// Slice 3.5, D23) at its exact original route. The original controller passed <c>base(null)</c> —
/// authentication-only at the controller level, with only the Get override's Financial read check —
/// and the host keeps that behavior by leaving <see cref="WarrantyClaimsApiOptions.DealerFinancialAction"/>
/// null. Lax-auth quirk logged for the standing security pass; do not "fix" it in a move slice.
/// </summary>
[Route("[controller]")]
public class DealerFinancialController : ShiftEntitySecureControllerAsync<DealerFinancialRepository, Data.Entities.WarrantyClaim, DealerFinancialListDTO, WarrantyClaimDTO>
{
    private readonly ITypeAuthService typeAuthService;
    private readonly IOptions<WarrantyClaimsApiOptions> options;

    public DealerFinancialController(ITypeAuthService typeAuthService, IOptions<WarrantyClaimsApiOptions> options)
        : base(options.Value.EnableWarrantyClaimsActionTreeAuthorization ? options.Value.DealerFinancialAction : null)
    {
        this.typeAuthService = typeAuthService;
        this.options = options;
    }

    public override async Task<ActionResult<ODataDTO<DealerFinancialListDTO>>> Get(ODataQueryOptions<DealerFinancialListDTO> oDataQueryOptions)
    {
        if (options.Value.EnableWarrantyClaimsActionTreeAuthorization &&
            options.Value.WarrantyClaimFinancialAction is not null &&
            !this.typeAuthService.CanRead(options.Value.WarrantyClaimFinancialAction))
            return Unauthorized();

        return await base.Get(oDataQueryOptions);
    }
}
