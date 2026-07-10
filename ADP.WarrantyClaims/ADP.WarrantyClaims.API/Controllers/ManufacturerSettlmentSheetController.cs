using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.WarrantyClaims.API.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.ManufacturerSettlmentSheet;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

[Route("[controller]")]
public class ManufacturerSettlmentSheetController : ShiftEntitySecureControllerAsync<ManufacturerSettlmentSheetRepository, ShiftSoftware.ADP.WarrantyClaims.Data.Entities.ManufacturerSettlmentSheet, ManufacturerSettlmentSheetListDTO, ManufacturerSettlmentSheetDTO>
{
    private readonly IOptions<WarrantyClaimsApiOptions> options;

    public ManufacturerSettlmentSheetController(IOptions<WarrantyClaimsApiOptions> options)
        : base(options.Value.EnableWarrantyClaimsActionTreeAuthorization ? options.Value.ManufacturerSettlmentsAction : null)
    {
        this.options = options;
    }

    [HttpGet("GenerateFromClaims")]
#if DEBUG
    [AllowAnonymous]
#endif
    public async Task<IActionResult> GenerateFromClaims(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? invoiceNumber,
        [FromQuery] bool showInBrowser,
        [FromServices] WarrantyClaimRepository warrantyClaimRepository,
        [FromServices] ITypeAuthService typeAuthService,
        [FromServices] ManufacturerSettlmentSheetRepository manufacturerSettlmentSheetRepository
    )
    {
        var skipAuthentication = false;
        var disableDefaultDataLevelAccess = false;
        var disableGlobalFilters = false;

        //Print in browser tab without auth during development
#if DEBUG
        skipAuthentication = true;
        disableDefaultDataLevelAccess = true;
        disableGlobalFilters = true;
#endif

        if (!skipAuthentication)
        {
            if (options.Value.EnableWarrantyClaimsActionTreeAuthorization &&
                options.Value.ManufacturerSettlmentsAction is not null &&
                !typeAuthService.CanWrite(options.Value.ManufacturerSettlmentsAction))
                return Unauthorized();
        }

        var q = await warrantyClaimRepository.GetIQueryable(
            disableDefaultDataLevelAccess: disableDefaultDataLevelAccess,
            disableGlobalFilters: disableGlobalFilters
        );

        var export = await manufacturerSettlmentSheetRepository.GenerateFromClaimsAsync(q, startDate, endDate, invoiceNumber);

        await warrantyClaimRepository.SaveChangesAsync();
        export.Position = 0;

        if (showInBrowser)
            return Ok(export);

        return File(export, "text/csv", "Manufacturer Settlement.csv");
    }
}
