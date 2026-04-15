using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceIntervalGroup;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

public class ServiceIntervalGroupRepository : ShiftRepository<ShiftDbContext, ServiceIntervalGroup, ServiceIntervalGroupListDTO, ServiceIntervalGroupDTO>
{
    public ServiceIntervalGroupRepository(ShiftDbContext db) : base(db, x=> x.IncludeRelatedEntitiesWithFindAsync(i=> i.Include(s=> s.ServiceIntervals)))
    {
    }
}
