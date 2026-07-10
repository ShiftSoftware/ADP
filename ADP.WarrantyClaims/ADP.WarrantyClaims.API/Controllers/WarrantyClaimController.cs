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
/// The former extras endpoints (ExportManufacturerCSV / DownloadCSV / flat-rate / part-lookup) moved
/// here too (Phase 3 Slice 3.3) at their byte-identical routes, with the consumer-only dependencies
/// behind the <see cref="IWarrantyRatesStore"/> / <see cref="IWarrantyCsvExportStorage"/> /
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
    /// constructor dependency: consumers that never call the CSV-export/download/lookup/rates
    /// endpoints must not be forced to register the seams just to construct this controller.
    /// </summary>
    private TSeam GetRequiredSeam<TSeam>() where TSeam : class
        => HttpContext.RequestServices.GetService<TSeam>()
            ?? throw new InvalidOperationException(
                $"This endpoint requires a {typeof(TSeam).FullName} registration. " +
                $"Register your implementation (services.AddScoped<{typeof(TSeam).Name}, Your{typeof(TSeam).Name.TrimStart('I')}>()) " +
                "— see the seam's XML documentation in ADP.WarrantyClaims.Shared for the contract.");

    /// <summary>
    /// Persists the export rates (consumer store), mutates the selected claims (rates -> amounts,
    /// InvoiceNo, ManufacturerStatus=Exported), writes the manufacturer CSV through the consumer
    /// storage and only then saves. Moved byte-identical (routes/response) from the original host's
    /// extras controller (Phase 3 Slice 3.3).
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
        var exportStorage = GetRequiredSeam<IWarrantyCsvExportStorage>();

        this.repository.shouldIncludeLaborLines = true;
        this.repository.shouldIncludeSubletLines = true;
        this.repository.shouldIncludePartLines = true;

        // skipAuthentication: the dissolved extras controller drove the CrudHandler directly (a plain
        // ControllerBase has no controller-level action), so selection resolution never re-checked the
        // WarrantyClaim CRUD node — the endpoint is gated by CsvExportAction above instead. Passing
        // false would silently return an empty selection for users who hold WarrantyClaimCSV but not
        // WarrantyClaim read — a behavior change this move must not make.
        var claims = await this.GetSelectedEntitiesAsync(selectedItems, skipAuthentication: true);

        var userId = hashIdService.Decode<UserDTO>(this.User.Claims.First(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value);

        try
        {
            // The consumer store deserializes/persists the rates row (audit-upsert) and hands back the
            // rates for the export math plus the exact echo object for the response (byte-identical
            // response shape is the consumer's contract).
            var persistedRates = await ratesStore.PersistExportRatesAsync(rates, userId);

            var exportableStream = await this.repository.ExportManufacturerCSV(invoice, persistedRates.Rates, claims);

            exportableStream.Seek(0, SeekOrigin.Begin);

            var relativeExportPath = await exportStorage.WriteAsync($"warranty-claims-{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}.csv", exportableStream);

            // Frozen ordering: the CSV file is written BEFORE the claim/rates mutations are saved.
            await this.repository.SaveChangesAsync();

            return Ok(new ShiftEntityResponse<IEnumerable<WarrantyClaimListDTO>>()
            {
                Additional = new()
                {
                    ["Rates"] = persistedRates.RatesEcho,
                    ["CSVExportPath"] = relativeExportPath,
                    ["InvoiceNumber"] = invoice
                },
                Entity = claims.Select(x => new WarrantyClaimListDTO { })
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

    // [AllowAnonymous] is load-bearing: the export-result dialog opens this URL via window.open with
    // no bearer token. DELIBERATE SECURITY FIX vs the verbatim-move doctrine: the original host
    // concatenated the catch-all segment straight onto its content root (path traversal on an
    // anonymous endpoint). The storage seam's OpenAsync contract now requires canonicalizing the
    // path and confining it to the export root, treating escapes exactly like a missing file — an
    // OSS package must not ship a traversal. Legitimate paths under the export folder behave
    // byte-identically.
    [HttpGet("DownloadCSV/{*exportPath}")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadCSV([FromRoute] string exportPath)
    {
        var exportStorage = GetRequiredSeam<IWarrantyCsvExportStorage>();

        var memoryStream = new MemoryStream();

        using (var fileStream = await exportStorage.OpenAsync(exportPath))
        {
            fileStream.CopyTo(memoryStream);
        }

        memoryStream.Seek(0, SeekOrigin.Begin);

        return File(memoryStream, "text/csv; charset=utf-8", "warranty-claims.csv");
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
