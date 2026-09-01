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

        i.UseGeneratedMapper(map => map

            // ── LIST ───────────────────────────────────────────────────────────────
            // Two flattenings through the Campaign navigation. The convention does not derive a
            // reach-through like this, so both are restated verbatim - including the null guard,
            // which is load-bearing: ForList is spliced into the SQL projection, and while EF would
            // translate a bare `e.Campaign.Name` today, the guard is what the old profile shipped
            // and what its behaviour is defined by.
            .ForList(d => d.CampaignName, e => e.Campaign != null ? e.Campaign.Name : null)
            .ForList(d => d.CampaignUniqueReference, e => e.Campaign != null ? e.Campaign.UniqueReference : null)

            // ── VIEW ─────────────────────────────────────────────────────────────
            // The member SHENGEN004 names, and it resolves to an ignore rather than a mapping.
            //
            // DisableVinValidation has NO entity source - CampaignVinEntry has no such column - and
            // it is [JsonIgnore], so it never crosses the wire in either direction. Its only reader
            // is CampaignVinEntryValidator (`.When(x => !x.DisableVinValidation)`): it exists purely
            // to let an in-process caller construct the DTO with VIN validation switched off.
            //
            // This is also the trap-3-write check item F calls for, and it comes back CLEAN: a
            // member the old reverse map never wrote could only become writable if the entity had a
            // matching target for the convention to find. It has none, so there is nothing for the
            // convention to write and no new behaviour to guard against.
            .IgnoreView(d => d.DisableVinValidation));
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
