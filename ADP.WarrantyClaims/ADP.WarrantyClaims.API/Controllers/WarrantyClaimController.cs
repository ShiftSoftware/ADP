using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.WarrantyClaims.API.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ADP.WarrantyClaims.Data.Services;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Services;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using ShiftSoftware.ShiftEntity.Print;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

/// <summary>
/// The warranty claim controller: CRUD (action from options) + UpdateStatus, UpdateDeliveryDate
/// (DeliveryDateOverwrite Maximum), the VIN lookup, and the SAS print-invoice endpoints. Moved from the original host application
/// (Phase 2b) with routes/EntitySet name preserved via the controller name + route-prefix convention.
/// The former extras endpoints (ExportManufacturerCSV / flat-rate / part-lookup) moved
/// here too (Phase 3 Slice 3.3) at their byte-identical routes, with the consumer-only dependencies
/// behind the <see cref="IWarrantyRatesStore"/> /
/// <see cref="IFlatRateProvider"/> / <see cref="IPartLookupProvider"/> seams — resolved lazily per
/// action so consumers that never call them need no registrations. The new <c>current-rates</c>
/// endpoint closes the last module-Web-to-host hidden dependency (the list/form pages previously read
/// a current-rates route only the original host's own Setting controller served).
/// </summary>
[Route("[controller]")]
public class WarrantyClaimController : ShiftEntitySecureControllerAsync<WarrantyClaimRepository, ShiftSoftware.ADP.WarrantyClaims.Data.Entities.WarrantyClaim, WarrantyClaimListDTO, WarrantyClaimDTO>
{
    private readonly WarrantyClaimRepository repository;
    private readonly DeliveryDateService deliveryDateService;
    private readonly IHashIdService hashIdService;
    private readonly ITypeAuthService typeAuthService;
    private readonly IOptions<WarrantyClaimsApiOptions> options;

    public WarrantyClaimController(
        WarrantyClaimRepository repository,
        DeliveryDateService deliveryDateService,
        IHashIdService hashIdService,
        ITypeAuthService typeAuthService,
        IOptions<WarrantyClaimsApiOptions> options
    ) : base(options.Value.EnableWarrantyClaimsActionTreeAuthorization ? options.Value.WarrantyClaimAction : null)
    {
        this.repository = repository;
        this.deliveryDateService = deliveryDateService;
        this.hashIdService = hashIdService;
        this.typeAuthService = typeAuthService;
        this.options = options;
    }

    [HttpPost("UpdateStatus/{actionType}/{inputText?}")]
    public async Task<IActionResult> ProcessBatchAction([FromBody] ShiftSoftware.ShiftEntity.Model.Dtos.SelectStateDTO<WarrantyClaimListDTO> selectedItems, [FromRoute] UpdateStatusActionTypes actionType, [FromRoute] string? inputText)
    {
        var items = await this.GetSelectedEntitiesAsync(selectedItems);

        try
        {
            await this.repository.UpdateClaimStatusAsync(items, actionType, inputText);

            return Ok(new ShiftEntityResponse<IEnumerable<WarrantyClaimListDTO>>()
            {
                Entity = items.Select(x => new WarrantyClaimListDTO { })
            });
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse<UserImportDTO>
            {
                Message = ex.Message,
                Additional = ex.AdditionalData,
            });
        }
    }


    [HttpPost("UpdateDeliveryDate/{deliveryDate}")]
    public async Task<IActionResult> UpdateDeliveryDate([FromBody] ShiftSoftware.ShiftEntity.Model.Dtos.SelectStateDTO<WarrantyClaimListDTO> selectedItems, [FromRoute] DateTime deliveryDate)
    {
        // The original host application gated this with [TypeAuth(..., DeliveryDateOverwrite, Access.Maximum)]. The
        // node is consumer-owned (D9), so the equivalent check runs imperatively against the options-
        // supplied boolean action.
        if (options.Value.EnableWarrantyClaimsActionTreeAuthorization &&
            options.Value.DeliveryDateOverwriteAction is not null &&
            !typeAuthService.Can(options.Value.DeliveryDateOverwriteAction, Access.Maximum))
        {
            return Forbid();
        }

        var items = await this.GetSelectedEntitiesAsync(selectedItems);

        try
        {
            var affected = await this.repository.OverrideDeliveryDateAsync(items, deliveryDate);

            return Ok(new ShiftEntityResponse<IEnumerable<WarrantyClaimListDTO>>()
            {
                Entity = Enumerable.Range(0, affected).Select(_ => new WarrantyClaimListDTO { })
            });
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse<UserImportDTO>
            {
                Message = ex.Message,
                Additional = ex.AdditionalData,
            });
        }
    }

    [HttpGet("print-invoice-token/{key}")]
    public ActionResult PrintInvoiceToken(string key)
    {
        ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
        ShiftEntityPrintOptions requiredService2 = base.HttpContext.RequestServices.GetRequiredService<ShiftEntityPrintOptions>();

        if (options.Value.EnableWarrantyClaimsActionTreeAuthorization &&
            options.Value.WarrantyClaimAction is not null &&
            !requiredService.CanRead(options.Value.WarrantyClaimAction))
        {
            return Forbid();
        }

        var (text, text2) = TokenService.GenerateSASToken(base.Url.Action(nameof(PrintInvoice), new { key })!, key, DateTime.UtcNow.AddSeconds(requiredService2.TokenExpirationInSeconds), requiredService2.SASTokenKey);
        return Ok("expires=" + text2 + "&token=" + text);
    }

    [HttpGet("print-invoice/{key}")]
    [AllowAnonymous]
    public async Task<ActionResult> PrintInvoice(string key, [FromQuery] string? expires = null, [FromQuery] string? token = null)
    {
        ShiftEntityPrintOptions requiredService = base.HttpContext.RequestServices.GetRequiredService<ShiftEntityPrintOptions>();

        if (!TokenService.ValidateSASToken(base.Url.Action(nameof(PrintInvoice), new { key })!, key, expires!, token!, requiredService.SASTokenKey))
        {
            return Forbid();
        }

        try
        {
            return new FileStreamResult(await repository.PrintManuufacturerInvoiceAsync(key), "application/pdf");
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse
            {
                Message = ex.Message,
                Additional = ex.AdditionalData
            });
        }
    }

    [HttpGet("vin-lookup/{vin}")]
    public async Task<IActionResult> VINLookup(string vin, [FromQuery] bool preDelivery = false, [FromQuery] string? excludeClaimId = null)
    {
        var result = new VinLookupResultDTO
        {
            Odometer = await this.deliveryDateService.GetLastOdometerAsync(vin),
        };

        if (!preDelivery)
        {
            long? excludeId = string.IsNullOrWhiteSpace(excludeClaimId)
                ? null
                : hashIdService.Decode<WarrantyClaimDTO>(excludeClaimId);

            result.DeliveryDate = await this.deliveryDateService.EvaluateAsync(vin, excludeId);
        }

        return Ok(result);
    }

    /// <summary>
    /// Resolves a Phase-3.3 consumer seam lazily from the request services. Deliberately NOT a
    /// constructor dependency: consumers that never call the CSV-export/lookup/rates
    /// endpoints must not be forced to register the seams just to construct this controller.
    /// </summary>
    private TSeam GetRequiredSeam<TSeam>() where TSeam : class
        => HttpContext.RequestServices.GetService<TSeam>()
            ?? throw new InvalidOperationException(
                $"This endpoint requires a {typeof(TSeam).FullName} registration. " +
                $"Register your implementation (services.AddScoped<{typeof(TSeam).Name}, Your{typeof(TSeam).Name.TrimStart('I')}>()) " +
                "— see the seam's XML documentation in ADP.WarrantyClaims.Shared for the contract.");

    /// <summary>
    /// Persists the export rates (consumer store), updates the selected claims (rates -> amounts,
    /// InvoiceNo, ManufacturerStatus=Exported), saves, and returns the manufacturer CSV in the response
    /// body. Nothing is stored on the server. The web client reads the file with ActionButton.OnResponse.
    /// It already knows the invoice number it sent, and it reloads the rates from <c>current-rates</c>.
    /// </summary>
    [HttpPost("ExportManufacturerCSV")]
    public async Task<IActionResult> ExportManufacturerCSV([FromBody] ShiftSoftware.ShiftEntity.Model.Dtos.SelectStateDTO<WarrantyClaimListDTO> selectedItems, [FromQuery] string invoice, [FromQuery] string rates)
    {
        // The original host gated this with [TypeAuth(..., WarrantyClaimCSV, Access.Read)]. The node is
        // consumer-owned (D9), so the equivalent READ check runs imperatively against the options-
        // supplied action. Frozen quirk, preserved deliberately: a mutating export (claims stamped
        // Exported + rates row upserted) behind a READ check — logged for the standing security pass.
        if (options.Value.EnableWarrantyClaimsActionTreeAuthorization &&
            options.Value.CsvExportAction is not null &&
            !typeAuthService.CanRead(options.Value.CsvExportAction))
        {
            return Forbid();
        }

        var ratesStore = GetRequiredSeam<IWarrantyRatesStore>();

        this.repository.shouldIncludeLaborLines = true;
        this.repository.shouldIncludeSubletLines = true;
        this.repository.shouldIncludePartLines = true;

        // The selection is resolved under the controller's own WarrantyClaim action, as the original host
        // resolved it: a user who holds the CSV export action but cannot read warranty claims gets an empty
        // selection and exports nothing, so the export never stamps claims its user cannot read. Data-level
        // access applies either way.
        var claims = await this.GetSelectedEntitiesAsync(selectedItems);

        var userId = hashIdService.Decode<UserDTO>(this.User.Claims.First(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value);

        try
        {
            // The consumer store deserializes and persists the rates row (audit-upsert) and returns the
            // rates for the export math.
            var persistedRates = await ratesStore.PersistExportRatesAsync(rates, userId);

            var exportableStream = await this.repository.ExportManufacturerCSV(invoice, persistedRates.Rates, claims);

            exportableStream.Seek(0, SeekOrigin.Begin);

            // The CSV is built before the save. If building it fails, no claim is marked as Exported.
            await this.repository.SaveChangesAsync();

            return File(exportableStream, "text/csv; charset=utf-8", $"warranty-claims-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse<UserImportDTO>
            {
                Message = ex.Message,
                Additional = ex.AdditionalData,
            });
        }
    }

    // No authorization node, matching the original host endpoint: authenticated-only (the app-wide
    // authentication policy applies; [AllowAnonymous] is deliberately absent).
    [HttpGet("flat-rate/{vds}/{wmi?}/{brandHashId}")]
    public async Task<IActionResult> GetFlatRate(string vds, string? wmi, string brandHashId)
    {
        var flatRateProvider = GetRequiredSeam<IFlatRateProvider>();

        var result = await flatRateProvider.GetFlatRatesAsync(vds, wmi, brandHashId);

        // The original host appended the header only on its stored-procedure path; the provider
        // signals that by returning a count (null = no header), so presence/absence is preserved.
        if (result.TotalCount is not null)
            Response.Headers.Append("X-Total-Count", result.TotalCount.Value.ToString());

        return Ok(result.FlatRates);
    }

    // No authorization node, matching the original host endpoint: authenticated-only.
    [HttpGet("part-lookup/{partNumber}")]
    public async Task<IActionResult> PartLookup(string partNumber)
    {
        var partLookupProvider = GetRequiredSeam<IPartLookupProvider>();

        var part = await partLookupProvider.LookupAsync(partNumber);

        return Ok(part);
    }

    /// <summary>
    /// NEW endpoint (Phase 3 Slice 3.3): the current warranty rates from the consumer's store. The
    /// module's list/form pages previously fetched these from the host's own Setting controller —
    /// the last module-Web route that only the original host served. Authenticated-only, like the
    /// host route it replaces.
    /// </summary>
    [HttpGet("current-rates")]
    public async Task<IActionResult> GetCurrentRates()
    {
        var ratesStore = GetRequiredSeam<IWarrantyRatesStore>();

        return Ok(await ratesStore.GetCurrentAsync());
    }
}
