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

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

[Route("[controller]")]
public class WarrantyCertificateController : ShiftEntitySecureControllerAsync<WarrantyCertificateRepository, Certificate, CertificateListDTO, CertificateDTO>
{
    private readonly WarrantyCertificateRepository repository;

    public WarrantyCertificateController(WarrantyCertificateRepository repository, IOptions<WarrantyClaimsApiOptions> options)
        : base(options.Value.EnableWarrantyClaimsActionTreeAuthorization ? options.Value.CertificatesAction : null)
    {
        this.repository = repository;
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
}
