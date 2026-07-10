using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.WarrantyClaims.API.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Certificate;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

[Route("[controller]")]
public class WarrantyInvoiceController : ShiftEntitySecureControllerAsync<WarrantyCertificateRepository, Certificate, CertificateListDTO, CertificateDTO>
{
    public WarrantyInvoiceController(WarrantyCertificateRepository repository, IOptions<WarrantyClaimsApiOptions> options)
        : base(options.Value.EnableWarrantyClaimsActionTreeAuthorization ? options.Value.InvoicesAction : null)
    {
        repository.IsInvoiceMode = true;
    }
}
