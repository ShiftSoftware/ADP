using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

public class ServiceIntervalRepository : ShiftRepository<ShiftDbContext, ServiceInterval, ServiceIntervalListDTO, ServiceIntervalDTO>
{
    public ServiceIntervalRepository(ShiftDbContext db) : base(db)
    {
    }
}
