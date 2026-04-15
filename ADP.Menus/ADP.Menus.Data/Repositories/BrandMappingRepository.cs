using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.DTOs.BrandMapping;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

public class BrandMappingRepository : ShiftRepository<ShiftDbContext, BrandMapping, BrandMappingListDTO, BrandMappingDTO>
{
    public BrandMappingRepository(ShiftDbContext db) : base(db)
    {
    }

    public override async ValueTask<BrandMapping> UpsertAsync(BrandMapping entity, BrandMappingDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        var brandIDExists = await db.Set<BrandMapping>()
            .Where(x => !x.IsDeleted)
            .Where(x => x.BrandID == dto.Brand.Value.ToLong())
            .Where(x => x.ID != entity.ID)
            .AnyAsync();

        if (brandIDExists)
            throw new ShiftEntityException(new("Duplicate", "A mapping for the selected Brand already exists."));

        return await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
    }
}


