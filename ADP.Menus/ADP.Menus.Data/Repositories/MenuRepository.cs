using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Brand;

using MenuEntity = global::ShiftSoftware.ADP.Menus.Data.Entities.Menu;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

public class MenuRepository : ShiftRepository<ShiftDbContext, MenuEntity, MenuListDTO, MenuDTO>
{
    public MenuRepository(ShiftDbContext db) : base(db, i =>
        {
            i.IncludeRelatedEntitiesWithFindAsync(
                x => x.Include(a => a.Variants),
                x => x.Include(a => a.VehicleModel)
            );

            i.FilterByTypeAuthValues(x => (x.ReadableTypeAuthValues != null && x.Entity.BrandID != null
                && x.ReadableTypeAuthValues.Contains(x.Entity.BrandID!.ToString()!))
                || x.WildCardRead)
            .ValueProvider<BrandDTO>(ShiftIdentityActions.DataLevelAccess.Brands);
        })
    {
    }

    public override async ValueTask<MenuEntity> UpsertAsync(MenuEntity entity, MenuDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        var vehicleModelID = dto.VehicleModel.Value.ToLong();

        if (actionType == ActionTypes.Update && entity.VehicleModelID != vehicleModelID)
            throw new ShiftEntityException(new("Conflict", "Vehicle model can not be changed after creation"));

        var vehicleModel = await db.Set<VehicleModel>()
            .Where(x => !x.IsDeleted && x.ID == vehicleModelID)
            .FirstOrDefaultAsync();
        if (vehicleModel is null)
            throw new ShiftEntityException(new("NotFound", "Vehicle Model not found"), 404);

        if (await db.Set<MenuEntity>().Where(x => !x.IsDeleted && x.ID != entity.ID).AnyAsync(x => x.BasicModelCode == dto.BasicModelCode))
            throw new ShiftEntityException(new("Conflict", "Basic model code should be unique"));

        entity.BrandID = vehicleModel.BrandID;

        var result = await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
        return result;
    }

    public override async ValueTask<MenuEntity> DeleteAsync(MenuEntity entity, bool isHardDelete, long? userId, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        if (!isHardDelete)
        {
            var variants = await db.Set<MenuVariant>()
                .Where(x => !x.IsDeleted && x.MenuID == entity.ID)
                .ToListAsync();

            foreach (var variant in variants)
                variant.IsDeleted = true;
        }

        return await base.DeleteAsync(entity, isHardDelete, userId, disableDefaultDataLevelAccess, disableGlobalFilters);
    }
}
