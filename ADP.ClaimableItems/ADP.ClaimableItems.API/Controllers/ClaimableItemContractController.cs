using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.ClaimableItems.API.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Data.Services;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
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
        if (!CanAccessContracts(write: false))
            return Forbid();

        var hashIdService = HttpContext.RequestServices.GetRequiredService<IHashIdService>();
        var menuService = HttpContext.RequestServices.GetRequiredService<ClaimableItemContractMenuService>();

        var lookup = await menuService.LookupAsync(
            vin,
            mileage,
            string.IsNullOrWhiteSpace(countryId) ? null : hashIdService.Decode<CountryDTO>(countryId),
            transferRate,
            language,
            string.IsNullOrWhiteSpace(contractId) ? null : hashIdService.Decode<ClaimableItemContractDTO>(contractId),
            menuService.Today);

        return Ok(ClaimableItemContractMenuService.ForForm(lookup));
    }

    /// <summary>Issues a saved draft: the lines and prices are frozen and each line gets its expiry.</summary>
    [HttpPost("{key}/issue")]
    public Task<ActionResult<ShiftEntityResponse<ClaimableItemContractDTO>>> Issue(string key) =>
        RunAsync(key, (repository, id, userId) => repository.IssueAsync(id, userId));

    /// <summary>Cancels an issued contract and all its live lines.</summary>
    [HttpPost("{key}/cancel")]
    public Task<ActionResult<ShiftEntityResponse<ClaimableItemContractDTO>>> Cancel(string key, [FromBody] ClaimableItemContractCancelDTO body) =>
        RunAsync(key, (repository, id, userId) => repository.CancelAsync(id, body?.Reason, userId));

    /// <summary>Cancels one line of an issued contract.</summary>
    [HttpPost("{key}/lines/{lineId:long}/cancel")]
    public Task<ActionResult<ShiftEntityResponse<ClaimableItemContractDTO>>> CancelLine(string key, long lineId, [FromBody] ClaimableItemContractCancelDTO body) =>
        RunAsync(key, (repository, id, userId) => repository.CancelLineAsync(id, lineId, body?.Reason, userId));

    /// <summary>
    /// The framework's print token (its route is inherited), issued only for an issued contract and, when the host
    /// sets <see cref="ClaimableItemsApiOptions.PrintContractAction"/>, only to a user who has it. The print itself is
    /// <see cref="ClaimableItemContractRepository.PrintAsync"/>.
    /// </summary>
    public override async Task<ActionResult> PrintToken(string key)
    {
        if (options.PrintContractAction is { } printAction
            && !HttpContext.RequestServices.GetRequiredService<ITypeAuthService>().CanAccess(printAction))
            return Forbid();

        var repository = HttpContext.RequestServices.GetRequiredService<ClaimableItemContractRepository>();
        var hashIdService = HttpContext.RequestServices.GetRequiredService<IHashIdService>();

        try
        {
            // Read access applies here, so a user cannot get a token for a contract outside their scope.
            var contract = await repository.FindAsync(hashIdService.Decode<ClaimableItemContractDTO>(key));

            if (contract is null)
                return NotFound();

            if (contract.IsDraft)
                return BadRequest(new ShiftEntityResponse<ClaimableItemContractDTO>
                {
                    Message = new Message("Error", "Only an issued contract can be printed."),
                });
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse<ClaimableItemContractDTO> { Message = ex.Message });
        }

        return await base.PrintToken(key);
    }

    private async Task<ActionResult<ShiftEntityResponse<ClaimableItemContractDTO>>> RunAsync(
        string key,
        Func<ClaimableItemContractRepository, long, long?, Task<ClaimableItemContract>> action)
    {
        if (!CanAccessContracts(write: true))
            return Forbid();

        var repository = HttpContext.RequestServices.GetRequiredService<ClaimableItemContractRepository>();
        var hashIdService = HttpContext.RequestServices.GetRequiredService<IHashIdService>();

        try
        {
            var contract = await action(repository, hashIdService.Decode<ClaimableItemContractDTO>(key), HttpContext.GetUserID());
            await repository.SaveChangesAsync();

            return Ok(new ShiftEntityResponse<ClaimableItemContractDTO>(await repository.ViewAsync(contract)));
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse<ClaimableItemContractDTO>
            {
                Message = ex.Message,
                Additional = ex.AdditionalData,
            });
        }
    }

    private bool CanAccessContracts(bool write)
    {
        if (!options.EnableClaimableItemsActionTreeAuthorization || options.ClaimableItemContractsAction is not { } action)
            return true;

        var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();

        return write ? typeAuth.CanWrite(action) : typeAuth.CanRead(action);
    }
}
