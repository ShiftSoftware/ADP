using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

// SHENGEN008 fires on ServiceIntervalGroups and is a false positive here. The check pairs a view member
// with an entity member of the SAME name; this DTO member is written back to a DIFFERENTLY named entity
// collection (ReplacementItemServiceIntervalGroups) from the AfterEntity hook below, which the generator
// cannot see into. The member does save — the reconciliation is the whole point of that hook.
#pragma warning disable SHENGEN008
public class ReplacementItemRepository : ShiftRepository<ShiftDbContext, ReplacementItem,ReplacementItemListDTO , ReplacementItemDTO>
{
    public ReplacementItemRepository(ShiftDbContext db) : base(db, x =>
    {
        x.IncludeRelatedEntitiesWithFindAsync(
            a=> a.Include(i=> i.ReplacementItemServiceIntervalGroups),
            a=> a.Include(i => i.StandaloneReplacementItemGroup)
        );

        x.UseGeneratedMapper(map => map

            // ── VIEW ──────────────────────────────────────────────────────────────────────────────
            // The M:N link rows become bare id selectors, soft-deleted links excluded. The convention
            // has no deep composition for this shape, so it must be written out.
            .ForView(d => d.ServiceIntervalGroups, e => e.ReplacementItemServiceIntervalGroups
                .Where(s => !s.IsDeleted)
                .Select(s => new ServiceIntervalGroupReplacaementItemDTO(s.ServiceIntervalGroupID.ToString()))
                .ToList())

            // StandaloneReplacementItemGroup needs nothing in either direction: the convention builds the
            // selector through MappingHelpers.ToSelectDTO from the foreign key, and fills Text from the
            // included navigation — which is what the list column wanted anyway.

            // ── ENTITY ────────────────────────────────────────────────────────────────────────────
            // The link rows are reconciled below, never replaced: ReplacementItemServiceIntervalGroups
            // carries a required non-nullable FK that ShiftEntity forces to Restrict, so severing one
            // throws a HandleConceptualNulls exception rather than deleting a row.
            .AfterEntity((dto, entity, ctx) =>
            {
                entity.ReplacementItemServiceIntervalGroups ??= [];

                // 1. Soft-delete links missing from the source. Physically removing them
                //    would sever a required non-nullable FK (ShiftEntity forces Restrict)
                //    and throw a HandleConceptualNulls exception. Soft-deleted links are
                //    filtered out of the forward map.
                var serviceIntervalItemsToRemove = entity.ReplacementItemServiceIntervalGroups
                    .Where(existing => !existing.IsDeleted
                        && !dto.ServiceIntervalGroups.Any(r => r.ID == existing.ServiceIntervalGroupID.ToString()))
                    .ToList();

                foreach (var item in serviceIntervalItemsToRemove)
                    item.IsDeleted = true;

                // 2. Add new items, reviving a previously soft-deleted link when present
                //    (the unique index on ReplacementItemID+ServiceIntervalGroupID has no
                //    IsDeleted filter, so we must reuse the existing row, not insert a duplicate).
                foreach (var item in dto.ServiceIntervalGroups)
                {
                    var existingItem = entity.ReplacementItemServiceIntervalGroups
                        .FirstOrDefault(r => r.ServiceIntervalGroupID.ToString() == item.ID);
                    if (existingItem == null)
                        entity.ReplacementItemServiceIntervalGroups.Add(new ReplacementItemServiceIntervalGroup { ServiceIntervalGroupID = item.ID.ToLong() });
                    else if (existingItem.IsDeleted)
                        existingItem.IsDeleted = false;
                }
            })
        );
    })
    {
    }
}
#pragma warning restore SHENGEN008
