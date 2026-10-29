using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.ClaimableItems.API.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Data.Services;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Country;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.ClaimableItems.API.Controllers;

[Route("[controller]")]
public class ClaimableItemContractController : ShiftEntitySecureControllerAsync<ClaimableItemContractRepository, ClaimableItemContract, ClaimableItemContractListDTO, ClaimableItemContractDTO>
{
    private readonly ClaimableItemsApiOptions options;

    public ClaimableItemContractController(IOptions<ClaimableItemsApiOptions> options)
        : base(options.Value.EnableClaimableItemsActionTreeAuthorization ? options.Value.ClaimableItemContractsAction : null)
    {
        this.options = options.Value;
    }

    /// <summary>
    /// The vehicle and the contract-type menu lines the contract form may offer for it.
    /// </summary>
    /// <param name="mileage">The vehicle's km now. Periodic services at or below it are left out.</param>
    /// <param name="countryId">Needed only when several countries are configured.</param>
    /// <param name="transferRate">Used only when several countries are configured.</param>
    /// <param name="contractId">The contract being edited, so its own lines do not count as taken.</param>
    [HttpGet("vin-lookup/{vin}")]
    public async Task<ActionResult<ClaimableItemContractVehicleLookupDTO>> VinLookup(
        string vin,
        [FromQuery] long? mileage = null,
        [FromQuery] string? countryId = null,
        [FromQuery] decimal? transferRate = null,
        [FromQuery] string? language = null,
        [FromQuery] string? contractId = null)
    {
        if (options.EnableClaimableItemsActionTreeAuthorization && options.ClaimableItemContractsAction is { } action)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanRead(action))
                return Forbid();
        }

        var hashIdService = HttpContext.RequestServices.GetRequiredService<IHashIdService>();
        var menuService = HttpContext.RequestServices.GetRequiredService<ClaimableItemContractMenuService>();

        var lookup = await menuService.LookupAsync(
            vin,
            mileage,
            string.IsNullOrWhiteSpace(countryId) ? null : hashIdService.Decode<CountryDTO>(countryId),
            transferRate,
            language,
            string.IsNullOrWhiteSpace(contractId) ? null : hashIdService.Decode<ClaimableItemContractDTO>(contractId),
            DateTime.Today);

        return Ok(ClaimableItemContractMenuService.ForForm(lookup));
    }
}
