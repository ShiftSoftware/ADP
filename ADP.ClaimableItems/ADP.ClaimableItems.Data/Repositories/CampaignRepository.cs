using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.Campaign;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Repositories;

public class CampaignRepository : ShiftRepository<ShiftDbContext, Campaign, CampaignListDTO, CampaignDTO>
{
    public CampaignRepository(ShiftDbContext db) : base(db, x => x.IncludeRelatedEntitiesWithFindAsync(
        x => x.Include(x => x.ClaimableItems)
    ))
    {
    }

    public override async ValueTask<Campaign> UpsertAsync(Campaign entity, CampaignDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        var upserted = await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);

        if (upserted.ActivationTrigger != ShiftSoftware.ADP.Models.Enums.ClaimableItemCampaignActivationTrigger.VehicleInspection)
        {
            upserted.VehicleInspectionTypeID = null;
        }

        return upserted;
    }
}
