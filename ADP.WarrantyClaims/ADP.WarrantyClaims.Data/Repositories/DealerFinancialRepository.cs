using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Financial;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

public class DealerFinancialRepository : ShiftRepository<ShiftDbContext, WarrantyClaim, DealerFinancialListDTO, WarrantyClaimDTO>
{
    public DealerFinancialRepository(ShiftDbContext db) : base(db)
    {

    }
}
