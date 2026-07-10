using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

/// <summary>
/// The warranty-rates repository. Moved from the original host application's SettingRepository and
/// renamed with its entity (Phase 3 Slice 3.6, D24) — the admin CRUD surface plus the
/// current-rates query the module's <see cref="Services.DefaultWarrantyRatesStore"/> and the
/// WarrantyRates controller serve.
/// </summary>
public class WarrantyRatesRepository : ShiftRepository<ShiftDbContext, WarrantyRates, WarrantyRatesListDTO, WarrantyRatesDTO>
{
    private readonly IMapper mapper;

    public WarrantyRatesRepository(ShiftDbContext db, IMapper mapper) : base(db)
    {
        this.mapper = mapper;
    }

    /// <summary>The latest non-deleted rates row by LastSaveDate (null when none exists yet).</summary>
    public async Task<WarrantyRatesDTO?> GetCurrentRatesAsync()
    {
        var rates = await db.Set<WarrantyRates>()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastSaveDate)
            .FirstOrDefaultAsync();

        return this.mapper.Map<WarrantyRatesDTO>(rates);
    }
}
