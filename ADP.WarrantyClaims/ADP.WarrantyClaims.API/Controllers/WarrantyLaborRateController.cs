using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.WarrantyClaims.API.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyLaborRate;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

/// <summary>
/// The labor rates, edited on the Labor Rates page. Gated by the same node as the warranty rates
/// (<see cref="WarrantyClaimsApiOptions.WarrantyRatesAction"/>), and distributor-only whatever that node grants:
/// <see cref="WarrantyLaborRateRepository"/>.
/// </summary>
[Route("[controller]")]
public class WarrantyLaborRateController : ShiftEntitySecureControllerAsync<WarrantyLaborRateRepository, Data.Entities.WarrantyLaborRate, WarrantyLaborRateListDTO, WarrantyLaborRateDTO>
{
    public WarrantyLaborRateController(IOptions<WarrantyClaimsApiOptions> options)
        : base(options.Value.EnableWarrantyClaimsActionTreeAuthorization ? options.Value.WarrantyRatesAction : null)
    {
    }
}
