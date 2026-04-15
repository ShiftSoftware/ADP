using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.DTOs.StandaloneReplacementItemGroup;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

public class StandaloneReplacementItemGroupRepository : ShiftRepository<ShiftDbContext, StandaloneReplacementItemGroup, StandaloneReplacementItemGroupListDTO, StandaloneReplacementItemGroupDTO>
{
    public StandaloneReplacementItemGroupRepository(ShiftDbContext db) : base(db)
    {
    }
}
