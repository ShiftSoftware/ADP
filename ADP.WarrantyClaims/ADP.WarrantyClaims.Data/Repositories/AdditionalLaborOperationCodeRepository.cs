using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.AdditionalLaborOperationCode;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

public class AdditionalLaborOperationCodeRepository : ShiftRepository<ShiftDbContext, AdditionalLaborOperationCode, AdditionalLaborOperationCodeListDTO, AdditionalLaborOperationCodeDTO>
{
    public AdditionalLaborOperationCodeRepository(ShiftDbContext db) : base(db)
    {
    }
}
