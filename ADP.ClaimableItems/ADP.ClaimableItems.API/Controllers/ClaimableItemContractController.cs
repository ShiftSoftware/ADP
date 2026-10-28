using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.ClaimableItems.API.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.ClaimableItems.API.Controllers;

[Route("[controller]")]
public class ClaimableItemContractController : ShiftEntitySecureControllerAsync<ClaimableItemContractRepository, ClaimableItemContract, ClaimableItemContractListDTO, ClaimableItemContractDTO>
{
    public ClaimableItemContractController(IOptions<ClaimableItemsApiOptions> options)
        : base(options.Value.EnableClaimableItemsActionTreeAuthorization ? options.Value.ClaimableItemContractsAction : null)
    {
    }
}
