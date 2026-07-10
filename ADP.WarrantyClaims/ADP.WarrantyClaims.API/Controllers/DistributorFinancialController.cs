using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.WarrantyClaims.API.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Financial;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

/// <summary>
/// The distributor-side financial analytics list + PDF export. Moved from the original host
/// application (Phase 3 Slice 3.5, D23) at its exact original route.
/// </summary>
[Route("[controller]")]
public class DistributorFinancialController : ShiftEntitySecureControllerAsync<DistributorFinancialRepository, Data.Entities.WarrantyClaim, DistributorFinancialListDTO, WarrantyClaimDTO>
{
    private readonly IWarrantyClaimsCapabilityProvider capabilityProvider;
    private readonly ITypeAuthService typeAuthService;
    private readonly DistributorFinancialRepository distributorFinancialRepository;
    private readonly IOptions<WarrantyClaimsApiOptions> options;

    public DistributorFinancialController(
        IWarrantyClaimsCapabilityProvider capabilityProvider,
        ITypeAuthService typeAuthService,
        DistributorFinancialRepository distributorFinancialRepository,
        IOptions<WarrantyClaimsApiOptions> options
    )
        : base(options.Value.EnableWarrantyClaimsActionTreeAuthorization ? options.Value.DistributorFinancialAction : null)
    {
        this.capabilityProvider = capabilityProvider;
        this.typeAuthService = typeAuthService;
        this.distributorFinancialRepository = distributorFinancialRepository;
        this.options = options;
    }

    public override async Task<ActionResult<ODataDTO<DistributorFinancialListDTO>>> Get(ODataQueryOptions<DistributorFinancialListDTO> oDataQueryOptions)
    {
        if (options.Value.EnableWarrantyClaimsActionTreeAuthorization &&
            options.Value.WarrantyClaimFinancialAction is not null &&
            !this.typeAuthService.CanRead(options.Value.WarrantyClaimFinancialAction))
            return Unauthorized();

        if (!this.capabilityProvider.IsDistributor)
            return Unauthorized();

        return await base.Get(oDataQueryOptions);
    }


    [HttpPost("Export")]

#if DEBUG
    [HttpGet("Export")]
    [AllowAnonymous]
#endif
    public async Task<IActionResult> Export([FromQuery] ODataQueryOptions<DistributorFinancialListDTO> oDataQueryOptions)
    {
        var skipAuthentication = false;
        var disableDefaultDataLevelAccess = false;
        var disableGlobalFilters = false;

        //Print in browser tab without auth during development
#if DEBUG
        if (Request.Method == HttpMethods.Get)
        {
            skipAuthentication = true;
            disableDefaultDataLevelAccess = true;
            disableGlobalFilters = true;
        }
#endif

        var selectedItems = new List<DistributorFinancialListDTO>();

        if (Request.Method == HttpMethods.Post)
        {
            var selectState = await this.HttpContext.Request.ReadFromJsonAsync<SelectStateDTO<DistributorFinancialListDTO>>();

            selectedItems = await this.GetSelectedListDTOsAsync(
                selectState!,
                skipAuthentication: skipAuthentication,
                disableDefaultDataLevelAccess: disableDefaultDataLevelAccess,
                disableGlobalFilters: disableGlobalFilters
            );
        }
        else
        {
            selectedItems = await this.GetSelectedListDTOsAsync(
                oDataQueryOptions,
                skipAuthentication: skipAuthentication,
                disableDefaultDataLevelAccess: disableDefaultDataLevelAccess,
                disableGlobalFilters: disableGlobalFilters
            );
        }

        var export = await distributorFinancialRepository.PrintAsync(selectedItems);

        // Reset the stream position
        export.Position = 0;

        if (Request.Method == HttpMethods.Get)
            return new FileStreamResult(export, "application/pdf");

        return File(export, "application/pdf", "FinancialReport.pdf");
    }
}
