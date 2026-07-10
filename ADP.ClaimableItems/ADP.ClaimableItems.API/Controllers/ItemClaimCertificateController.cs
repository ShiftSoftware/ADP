using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.ClaimableItems.API.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaimCertificate;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.ClaimableItems.API.Controllers;

/// <summary>
/// Item-claim certificate CRUD + the batch Invoice endpoint. Moved from the original host application (Phase 2 Slice 6);
/// routes/EntitySet name preserved via the controller name + route-prefix convention.
/// </summary>
[Route("[controller]")]
public class ItemClaimCertificateController : ShiftEntitySecureControllerAsync<ItemClaimCertificateRepository, Certificate, CertificateListDTO, ItemClaimCertificateDTO>
{
    private readonly ItemClaimCertificateRepository repository;

    public ItemClaimCertificateController(ItemClaimCertificateRepository repository, IOptions<ClaimableItemsApiOptions> options)
        : base(options.Value.EnableClaimableItemsActionTreeAuthorization ? options.Value.CertifyingAction : null)
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

            // NOTE: the original host implementation wrapped the response as WarrantyClaimListDTO instances (a wire
            // quirk); the Entity payload is a list of default-valued DTOs the client never reads.
            return Ok(new ShiftEntityResponse<IEnumerable<ItemClaimListDTO>>()
            {
                Entity = items.Select(x => new ItemClaimListDTO { })
            });
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse<ItemClaimCertificateDTO>
            {
                Message = ex.Message,
                Additional = ex.AdditionalData,
            });
        }
    }
}
