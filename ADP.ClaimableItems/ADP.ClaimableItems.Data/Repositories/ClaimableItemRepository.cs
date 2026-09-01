using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Repositories;

// SHENGEN008 fires on `Costs` and is a FALSE POSITIVE. The check pairs a view member with an entity
// member of the SAME NAME and compares their handling; here both are literally named `Costs` but
// they are different shapes - the DTO's is a List<ClaimableItemCostDTO>, the entity's is the raw
// JSON `string` column - and the generator cannot see through the serializer. Verified against the
// emitted code before suppressing, per conventions.md section 7: MapToEntityGenerated contains
// `existing.Costs = this.__ShiftMap.InvokeEntity<string>(...)`, so the member demonstrably does NOT
// "silently fail to save".
#pragma warning disable SHENGEN008
public class ClaimableItemRepository : ShiftRepository<ShiftDbContext, ClaimableItem, ClaimableItemListDTO, ClaimableItemDTO>
{
    public ClaimableItemRepository(ShiftDbContext db) : base(db, i =>
    {
        i.IncludeRelatedEntitiesWithFindAsync(
            x => x.Include(x => x.Campaign)
        );

        i.UseGeneratedMapper(map => map

            // ── LIST ───────────────────────────────────────────────────────────────
            // FIVE MEMBERS NO DIAGNOSTIC REPORTS, AND THE OLD PROFILE NEVER MENTIONED EITHER.
            //
            // AutoMapper flattens `Campaign.Name` onto `CampaignName` by name convention, with no
            // configuration at all - which is why the deleted profile has no ForMember for any of
            // these and why nothing here looks missing. The generated projection does NOT flatten,
            // and SHENGEN004 only reports unmapped members on the VIEW mapper, never on the list -
            // so dropping these produces no warning, no error, and five silently null columns.
            //
            // They were found by diffing the pre-migration parity baseline against the emitted
            // __shiftListProjection, and the baseline is the proof they were live: it carries
            // "CampaignName": "PARITY-CAMPAIGN parity campaign" on a row this projection would have
            // returned as null.
            //
            // The Campaign navigation is nullable here, so each one keeps the guard that
            // AutoMapper's own projection emitted; the two enums fall back to default(T) for the
            // same reason. `Validity`, `ValidityModeText`, `ActivationTriggerText` and
            // `ActivationTypeText` need nothing - they are computed getters on the DTO, and
            // restoring the two enum members below is what makes the last two correct again.
            .ForList(d => d.CampaignName, e => e.Campaign != null ? e.Campaign.Name : null!)
            .ForList(d => d.CampaignStartDate, e => e.Campaign != null ? e.Campaign.StartDate : null)
            .ForList(d => d.CampaignExpireDate, e => e.Campaign != null ? e.Campaign.ExpireDate : null)
            .ForList(d => d.CampaignActivationTrigger, e => e.Campaign != null ? e.Campaign.ActivationTrigger : default)
            .ForList(d => d.CampaignActivationType, e => e.Campaign != null ? e.Campaign.ActivationType : default)

            // ── VIEW ──────────────────────────────────────────────────────────────────────
            // The member SHENGEN004 names. A JSON column on the entity, a typed list on the DTO.
            //
            // THE SERIALIZER OPTIONS ARE THE POINT. Both directions pass a bare
            // `new JsonSerializerOptions { }` - DEFAULT options, deliberately NOT the framework's
            // configured ones. Default options are PascalCase; substituting a camelCase or
            // otherwise-configured instance would rewrite the property names inside the stored
            // column and silently orphan every cost row already in the database. Carried over
            // character-for-character from the deleted profile for that reason alone.
            //
            // The null/empty behaviour is likewise reproduced rather than improved: this is a
            // straight Deserialize with no guard, exactly as before. Making it null-safe here would
            // be a behaviour change smuggled in under a refactor.
            .ForView(d => d.Costs, e => JsonSerializer.Deserialize<List<ClaimableItemCostDTO>>(e.Costs, new JsonSerializerOptions { }))

            // ── ENTITY ────────────────────────────────────────────────────────────────────
            // Same options instance shape, same reasoning, opposite direction.
            .ForEntity(e => e.Costs, dto => JsonSerializer.Serialize(dto.Costs, new JsonSerializerOptions { })));

        // As on CampaignRepository, `Campaign` is left entirely to the convention - it is the
        // scalar ShiftEntitySelectDTO? that the removed .DefaultEntityToDtoAfterMap() /
        // .DefaultDtoToEntityAfterMap() pair used to carry, and `ToSelectDTO` /
        // `ToNullableForeignKey` now do it. SHENGEN004 not naming it is the proof. See
        // CampaignRepository for the two inherited behaviour deltas, which apply here too - and
        // apply to `Campaign` in particular, because a ClaimableItem may legitimately have a null
        // CampaignID.
    })
    {
    }
}
#pragma warning restore SHENGEN008
