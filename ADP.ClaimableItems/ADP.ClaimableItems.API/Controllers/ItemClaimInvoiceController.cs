using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.ClaimableItems.API.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaimCertificate;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.ClaimableItems.API.Controllers;

/// <summary>
/// Item-claim invoice surface — the certificate repository in invoice mode (an invoice is a
/// view-mode of Certificate, not a separate entity). Moved from the original host application (Phase 2 Slice 6).
/// </summary>
[Route("[controller]")]
public class ItemClaimInvoiceController : ShiftEntitySecureControllerAsync<ItemClaimCertificateRepository, Certificate, CertificateListDTO, ItemClaimCertificateDTO>
{
    public ItemClaimInvoiceController(ItemClaimCertificateRepository repository, IOptions<ClaimableItemsApiOptions> options)
        : base(options.Value.EnableClaimableItemsActionTreeAuthorization ? options.Value.InvoicingAction : null)
    {
        repository.IsInvoiceMode = true;
    }
}
