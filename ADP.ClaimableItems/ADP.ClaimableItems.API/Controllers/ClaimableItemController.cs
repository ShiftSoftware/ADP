using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.ClaimableItems.API.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.ClaimableItems.API.Controllers;

[Route("[controller]")]
public class ClaimableItemController : ShiftEntitySecureControllerAsync<ClaimableItemRepository, ClaimableItem, ClaimableItemListDTO, ClaimableItemDTO>
{
    public ClaimableItemController(IOptions<ClaimableItemsApiOptions> options)
        : base(options.Value.EnableClaimableItemsActionTreeAuthorization ? options.Value.ClaimableItemSetupAction : null)
    {
    }
}
