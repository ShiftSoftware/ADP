using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.CampaignVinEntry;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Repositories;

public class CampaignVinEntryRepository
    : ShiftRepository<ShiftDbContext, CampaignVinEntry, CampaignVinEntryListDTO, CampaignVinEntryDTO>
{
    public CampaignVinEntryRepository(ShiftDbContext db) : base(
        db,
        x => x.IncludeRelatedEntitiesWithFindAsync(q => q.Include(e => e.Campaign))
    )
    {
    }

    public override async ValueTask<CampaignVinEntry> UpsertAsync(
        CampaignVinEntry entity,
        CampaignVinEntryDTO dto,
        ActionTypes actionType,
        long? userId,
        Guid? idempotencyKey,
        bool disableDefaultDataLevelAccess,
        bool disableGlobalFilters)
    {
        var upserted = await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);

        // Guard: only campaigns whose trigger is ManualVinEntry are valid targets here. The UI filters
        // the picker, but the API accepts any CampaignID, so re-check server-side. Uses db.Set<Campaign>()
        // (not a module-specific DbSet) so the module works against any consumer ShiftDbContext.
        var campaignTrigger = await db.Set<Campaign>()
            .Where(x => x.ID == upserted.CampaignID)
            .Select(x => (ClaimableItemCampaignActivationTrigger?)x.ActivationTrigger)
            .FirstOrDefaultAsync();

        if (campaignTrigger != ClaimableItemCampaignActivationTrigger.ManualVinEntry)
        {
            throw new ShiftEntityException(new Message(
                "Error",
                "The selected campaign's activation trigger is not ManualVinEntry."));
        }

        return upserted;
    }
}
