using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

public class ReplacementItemRepository : ShiftRepository<ShiftDbContext, ReplacementItem,ReplacementItemListDTO , ReplacementItemDTO>
{
    public ReplacementItemRepository(ShiftDbContext db) : base(db, x=> x.IncludeRelatedEntitiesWithFindAsync(
            a=> a.Include(i=> i.ReplacementItemServiceIntervalGroups),
            a=> a.Include(i => i.StandaloneReplacementItemGroup)
        ))
    {
    }
}
