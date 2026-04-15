using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourRateMapping;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

public class LabourRateMappingRepository : ShiftRepository<ShiftDbContext, LabourRateMapping, LabourRateMappingListDTO, LabourRateMappingDTO>
{
    public LabourRateMappingRepository(ShiftDbContext db) : base(db)
    {
    }
}

