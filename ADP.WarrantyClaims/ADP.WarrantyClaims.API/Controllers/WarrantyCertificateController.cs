using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.WarrantyClaims.API.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

[Route("[controller]")]
public class WarrantyCertificateController : ShiftEntitySecureControllerAsync<WarrantyCertificateRepository, Certificate, CertificateListDTO, CertificateDTO>
{
    private readonly WarrantyCertificateRepository repository;
    private readonly ITypeAuthService typeAuthService;
    private readonly IOptions<WarrantyClaimsApiOptions> options;

    public WarrantyCertificateController(WarrantyCertificateRepository repository, ITypeAuthService typeAuthService, IOptions<WarrantyClaimsApiOptions> options)
        : base(options.Value.EnableWarrantyClaimsActionTreeAuthorization ? options.Value.CertificatesAction : null)
    {
        this.repository = repository;
        this.typeAuthService = typeAuthService;
        this.options = options;
    }

    [HttpPost("Invoice/{invoiceDate}")]
    public async Task<IActionResult> Invoice([FromBody] ShiftSoftware.ShiftEntity.Model.Dtos.SelectStateDTO<CertificateListDTO> selectedItems, [FromRoute] DateTime invoiceDate)
    {
        var items = await this.GetSelectedEntitiesAsync(selectedItems);

        try
        {
            await this.repository.Invoice(items, invoiceDate);

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

    /// <summary>
    /// Returns the selected certificates as one CSV file (one row for each claim). The file is sent in
    /// the response body. Nothing is stored on the server.
    /// Needs Read on the certificates action, the same permission that shows the certificates list.
    /// </summary>
    [HttpPost("ExportCSV")]
    public async Task<IActionResult> ExportCSV([FromBody] ShiftSoftware.ShiftEntity.Model.Dtos.SelectStateDTO<CertificateListDTO> selectedItems)
    {
        // GetSelectedEntitiesAsync also checks this action, but it returns an empty list instead of
        // refusing. Without this check, a user with no access would get 200 and an empty CSV.
        if (options.Value.EnableWarrantyClaimsActionTreeAuthorization &&
            options.Value.CertificatesAction is not null &&
            !typeAuthService.CanRead(options.Value.CertificatesAction))
        {
            return Forbid();
        }

        var items = await this.GetSelectedEntitiesAsync(selectedItems);

        var stream = await this.repository.ExportCSVAsync(items);

        return File(stream, "text/csv", $"warranty-certificates-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
