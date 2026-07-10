using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.Campaign;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.ClaimableItems.API.Controllers;

// Route/EntitySet name "ServiceCampaign" is preserved from the original host implementation so the admin Blazor list keeps
// resolving. base(null): Campaign CRUD is not action-tree gated (matching the original behaviour).
[Route("[controller]")]
public class ServiceCampaignController : ShiftEntitySecureControllerAsync<CampaignRepository, Campaign, CampaignListDTO, CampaignDTO>
{
    public ServiceCampaignController() : base(null)
    {
    }
}
