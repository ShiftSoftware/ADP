using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.WarrantyClaims.API.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

/// <summary>
/// The warranty-rates admin CRUD controller. Moved from the original host application's
/// SettingController and renamed with its entity (Phase 3 Slice 3.6, D24 — the
/// <c>api/Setting</c> → <c>api/WarrantyRates</c> route change is an authorized wire break; the
/// host's admin pages moved lock-step). The host controller's <c>current-rates</c> endpoint is NOT
/// reproduced here — <c>WarrantyClaim/current-rates</c> (Phase 3 Slice 3.3) already serves it off
/// <see cref="Shared.IWarrantyRatesStore"/>.
/// </summary>
[Route("[controller]")]
public class WarrantyRatesController : ShiftEntitySecureControllerAsync<WarrantyRatesRepository, Data.Entities.WarrantyRates, WarrantyRatesListDTO, WarrantyRatesDTO>
{
    public WarrantyRatesController(IOptions<WarrantyClaimsApiOptions> options)
        : base(options.Value.EnableWarrantyClaimsActionTreeAuthorization ? options.Value.WarrantyRatesAction : null)
    {
    }

    /// <summary>
    /// Preserved host quirk: rates rows are create-only from the admin form (each save is a new
    /// audit row); the update verb was never implemented.
    /// </summary>
    public override Task<ActionResult<ShiftEntityResponse<WarrantyRatesDTO>>> Put(string key, [FromBody] WarrantyRatesDTO dto)
    {
        throw new NotImplementedException();
    }
}
