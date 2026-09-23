using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVersion;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

public class MenuVersionRepository : ShiftRepository<ShiftDbContext, MenuVersion, MenuVersionListDTO, MenuVersionDTO>
{
    public MenuVersionRepository(ShiftDbContext db) : base(db)
    {
    }

    public override async ValueTask<MenuVersion> UpsertAsync(MenuVersion entity, MenuVersionDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        if (actionType == ActionTypes.Update)
            throw new NotImplementedException();

        var maxVersion = await db.Set<MenuVersion>().AnyAsync() ? await db.Set<MenuVersion>().MaxAsync(s => s.Version) : 0;
        var newVersion = maxVersion + 1;

        var versionDateTime = DateTimeOffset.Now;

        var data = new MenuVersionDTO
        {
            Version = newVersion,
            VersionDateTime = versionDateTime
        };

        return await base.UpsertAsync(entity, data, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    public override async ValueTask<IQueryable<MenuVersionListDTO>> OdataList(IQueryable<MenuVersion>? queryable)
    {
        return await base.OdataList((await GetIQueryable(null,null,false,false)).OrderByDescending(x => x.Version));
    }
}
