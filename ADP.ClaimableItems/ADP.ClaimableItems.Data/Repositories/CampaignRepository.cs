using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.Campaign;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Repositories;

public class CampaignRepository : ShiftRepository<ShiftDbContext, Campaign, CampaignListDTO, CampaignDTO>
{
    public CampaignRepository(ShiftDbContext db) : base(db, i =>
    {
        i.IncludeRelatedEntitiesWithFindAsync(
            x => x.Include(x => x.ClaimableItems)
        );

        // The maps - the three id lists and the select-convention notes - are in Mappers/ClaimableItemsMapper.cs.
    })
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
