using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.WarrantyClaims.API.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyCurrencyRate;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

/// <summary>
/// The currency rates to USD, edited on the Warranty Rates page. Gated by the same node as the
/// warranty rates (<see cref="WarrantyClaimsApiOptions.WarrantyRatesAction"/>).
/// </summary>
[Route("[controller]")]
public class WarrantyCurrencyRateController : ShiftEntitySecureControllerAsync<WarrantyCurrencyRateRepository, Data.Entities.WarrantyCurrencyRate, WarrantyCurrencyRateListDTO, WarrantyCurrencyRateDTO>
{
    private readonly WarrantyCurrencyRateRepository repository;

    public WarrantyCurrencyRateController(WarrantyCurrencyRateRepository repository, IOptions<WarrantyClaimsApiOptions> options)
        : base(options.Value.EnableWarrantyClaimsActionTreeAuthorization ? options.Value.WarrantyRatesAction : null)
    {
        this.repository = repository;
    }

    /// <summary>
    /// Today's rate of a currency, or null when none is set. Authenticated-only, like
    /// <c>WarrantyClaim/current-rates</c>: the claim form reads it for users who cannot edit rates.
    /// </summary>
    [HttpGet("current/{currency}")]
    public async Task<IActionResult> GetCurrent(string currency)
    {
        var rate = await this.repository.GetCurrentAsync(currency.Trim().ToUpperInvariant());

        return Ok(rate is null ? null : new WarrantyCurrencyRateListDTO
        {
            Currency = rate.Currency,
            Rate = rate.Rate,
            EffectiveDate = rate.EffectiveDate,
        });
    }
}
