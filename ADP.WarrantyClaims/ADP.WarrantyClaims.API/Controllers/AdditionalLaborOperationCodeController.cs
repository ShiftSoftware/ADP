using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.WarrantyClaims.API.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.AdditionalLaborOperationCode;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

[Route("[controller]")]
public class AdditionalLaborOperationCodeController : ShiftEntitySecureControllerAsync<AdditionalLaborOperationCodeRepository, Data.Entities.AdditionalLaborOperationCode, AdditionalLaborOperationCodeListDTO, AdditionalLaborOperationCodeDTO>
{
    public AdditionalLaborOperationCodeController(IOptions<WarrantyClaimsApiOptions> options)
        : base(options.Value.EnableWarrantyClaimsActionTreeAuthorization ? options.Value.AdditionalLaborOperationCodesAction : null)
    {
    }
}
