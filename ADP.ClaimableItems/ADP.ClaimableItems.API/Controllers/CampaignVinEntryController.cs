using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.ClaimableItems.API.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.CampaignVinEntry;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.ClaimableItems.API.Controllers;

// [AllowAnonymous] is intentional and preserved from the TCA original (the VIN-entry endpoint is exposed
// without authentication).
[Route("[controller]")]
[AllowAnonymous]
public class CampaignVinEntryController : ShiftEntitySecureControllerAsync<CampaignVinEntryRepository, CampaignVinEntry, CampaignVinEntryListDTO, CampaignVinEntryDTO>
{
    public CampaignVinEntryController(IOptions<ClaimableItemsApiOptions> options)
        : base(options.Value.EnableClaimableItemsActionTreeAuthorization ? options.Value.CampaignVinEntriesAction : null)
    {
    }
}
