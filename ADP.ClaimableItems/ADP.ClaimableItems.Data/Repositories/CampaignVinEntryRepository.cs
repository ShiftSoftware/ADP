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
    public CampaignVinEntryRepository(ShiftDbContext db) : base(db, i =>
    {
        i.IncludeRelatedEntitiesWithFindAsync(q => q.Include(e => e.Campaign));

        // The maps - the list's two Campaign flattenings - are in Mappers/ClaimableItemsMapper.cs.
    })
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

        // A VIN entry can also supply membership evidence to an inspection campaign's conditions.
        // It does not activate that campaign; the inspection remains the activation/claim identity.
        // Re-check server-side. Uses db.Set<Campaign>()
        // (not a module-specific DbSet) so the module works against any consumer ShiftDbContext.
        var campaignTrigger = await db.Set<Campaign>()
            .Where(x => x.ID == upserted.CampaignID)
            .Select(x => (ClaimableItemCampaignActivationTrigger?)x.ActivationTrigger)
            .FirstOrDefaultAsync();

        if (campaignTrigger is null || !Enum.IsDefined(campaignTrigger.Value))
        {
            throw new ShiftEntityException(new Message(
                "Error",
                "Select an existing campaign with a supported activation trigger."));
        }

        return upserted;
    }
}
