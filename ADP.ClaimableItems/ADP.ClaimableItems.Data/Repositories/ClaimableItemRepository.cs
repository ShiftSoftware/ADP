using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Repositories;

public class ClaimableItemRepository : ShiftRepository<ShiftDbContext, ClaimableItem, ClaimableItemListDTO, ClaimableItemDTO>
{
    public ClaimableItemRepository(ShiftDbContext db) : base(db, i =>
    {
        i.IncludeRelatedEntitiesWithFindAsync(
            x => x.Include(x => x.Campaign)
        );

        // The maps - the list's Campaign flattenings and the Costs JSON column - are in Mappers/ClaimableItemsMapper.cs.
    })
    {
    }
}
