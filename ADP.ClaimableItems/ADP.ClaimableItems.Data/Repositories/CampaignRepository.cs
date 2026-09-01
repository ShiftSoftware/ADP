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

        i.UseGeneratedMapper(map => map

            // ── VIEW ──────────────────────────────────────────────────────────────────────
            // The three members SHENGEN004 names. Their source is a plain `List<long>` on the
            // entity - NOT a navigation - so no convention reaches them: MappingHelpers.ToSelectDTO
            // applies to a foreign key plus its navigation, and there is neither here.
            //
            // Value-only is CORRECT, and is deliberately not a gap to be "fixed" later. The
            // convention fills Text from a navigation where one exists; these ids have no
            // navigation to read a name from, so there is nothing to put in Text. That also matches
            // what these members carried before the migration - see the note on the entity
            // direction below for why every select DTO in this group had a null Text.
            .ForView(d => d.Brands, e => e.Brands.Select(v => new ShiftEntitySelectDTO { Value = v.ToString() }).ToList())
            .ForView(d => d.Companies, e => e.Companies.Select(v => new ShiftEntitySelectDTO { Value = v.ToString() }).ToList())
            .ForView(d => d.Countries, e => e.Countries.Select(v => new ShiftEntitySelectDTO { Value = v.ToString() }).ToList())

            // ── ENTITY ────────────────────────────────────────────────────────────────────
            // Straight back to List<long>. `ToLong()` is carried over from the old reverse map
            // rather than replaced with long.Parse, so a malformed value keeps failing the way it
            // always did instead of failing in a new way.
            .ForEntity(e => e.Brands, dto => dto.Brands.Select(s => s.Value.ToLong()).ToList())
            .ForEntity(e => e.Companies, dto => dto.Companies.Select(s => s.Value.ToLong()).ToList())
            .ForEntity(e => e.Countries, dto => dto.Countries.Select(s => s.Value.ToLong()).ToList()));

        // NOTHING is configured for `VehicleInspectionType`, and that is the deliberate half of
        // SPIKE-11. It is a scalar ShiftEntitySelectDTO?, so the old framework-supplied
        // .DefaultEntityToDtoAfterMap() / .DefaultDtoToEntityAfterMap() pair used to carry it -
        // back-filling { Value = VehicleInspectionTypeID } on the way out and writing
        // VehicleInspectionTypeID = long.Parse(Value) on the way in. Both helpers are GONE at
        // 2026.8.30.1. The convention replaces them (`ToSelectDTO` / `ToNullableForeignKey`), which
        // is why SHENGEN004 does not name this member - restating it here would be noise.
        //
        // Two inherited behaviour changes ride along, and neither is caused by anything written
        // here (STATUS.md, SPIKE-11):
        //   READ  - a NULL FK now yields a NULL member, where the old helper produced { Value = "" }.
        //   WRITE - a blank/null select DTO now SETS THE FK TO NULL, where the old helper left the
        //           existing FK untouched. That one is data-loss-shaped; it is the reason the
        //           UPDATE parity cases matter more than usual for this group.
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
