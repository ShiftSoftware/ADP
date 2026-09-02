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
    public WarrantyRatesRepository(ShiftDbContext db) : base(db)
    {
    }

    /// <summary>The latest non-deleted rates row by LastSaveDate (null when none exists yet).</summary>
    public async Task<WarrantyRatesDTO?> GetCurrentRatesAsync()
    {
        var rates = await db.Set<WarrantyRates>()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastSaveDate)
            .FirstOrDefaultAsync();

        // Routed through this repository's OWN triple rather than a separate mapper - the
        // WarrantyRates -> WarrantyRatesDTO direction it needs is exactly this triple's view map.
        //
        // THE NULL GUARD IS REQUIRED, not defensive. The query is FirstOrDefaultAsync and the method
        // is documented to return null when no rates row exists yet; AutoMapper's Map<T>(null)
        // quietly returned null, whereas MapToView would dereference it. Without this the very first
        // call on a fresh database throws instead of returning null.
        return rates is null ? null : this.MapToView(rates);
    }
}
