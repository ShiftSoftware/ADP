using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Repositories;

public class ClaimableItemRepository : ShiftRepository<ShiftDbContext, ClaimableItem, ClaimableItemListDTO, ClaimableItemDTO>
{
    public ClaimableItemRepository(ShiftDbContext db) : base(db, x =>
        x.IncludeRelatedEntitiesWithFindAsync(x =>
            x.Include(x => x.Campaign)
    ))
    {
    }
}
