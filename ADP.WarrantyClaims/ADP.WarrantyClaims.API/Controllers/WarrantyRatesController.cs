using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.WarrantyClaims.API.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

/// <summary>
/// The warranty-rates admin CRUD controller. There is no <c>current-rates</c> endpoint here:
/// <c>WarrantyClaim/current-rates</c> serves it off <see cref="Shared.IWarrantyRatesStore"/>.
/// </summary>
[Route("[controller]")]
public class WarrantyRatesController : ShiftEntitySecureControllerAsync<WarrantyRatesRepository, Data.Entities.WarrantyRates, WarrantyRatesListDTO, WarrantyRatesDTO>
{
    public WarrantyRatesController(IOptions<WarrantyClaimsApiOptions> options)
        : base(options.Value.EnableWarrantyClaimsActionTreeAuthorization ? options.Value.WarrantyRatesAction : null)
    {
    }

    /// <summary>
    /// Rates rows are create-only from the admin form (each save is a new
    /// audit row); the update verb was never implemented.
    /// </summary>
    public override Task<ActionResult<ShiftEntityResponse<WarrantyRatesDTO>>> Put(string key, [FromBody] WarrantyRatesDTO dto)
    {
        throw new NotImplementedException();
    }
}
