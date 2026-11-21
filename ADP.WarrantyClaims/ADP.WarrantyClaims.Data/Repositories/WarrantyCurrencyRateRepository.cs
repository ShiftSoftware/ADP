using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyCurrencyRate;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

public class WarrantyCurrencyRateRepository : ShiftRepository<ShiftDbContext, WarrantyCurrencyRate, WarrantyCurrencyRateListDTO, WarrantyCurrencyRateDTO>
{
    public WarrantyCurrencyRateRepository(ShiftDbContext db) : base(db)
    {
    }

    public Task<WarrantyCurrencyRate?> GetCurrentAsync(string currency) =>
        CurrentAsync(this.db.Set<WarrantyCurrencyRate>(), currency, DateTime.UtcNow);

    /// <summary>
    /// The rate in force at <paramref name="asOf"/>: the row of the currency with the latest effective date
    /// that has started, the newest row winning a tie.
    /// </summary>
    public static Task<WarrantyCurrencyRate?> CurrentAsync(IQueryable<WarrantyCurrencyRate> rates, string currency, DateTime asOf) =>
        rates
            .Where(x => !x.IsDeleted)
            .Where(x => x.Currency == currency)
            .Where(x => x.EffectiveDate <= asOf)
            .OrderByDescending(x => x.EffectiveDate)
            .ThenByDescending(x => x.ID)
            .FirstOrDefaultAsync();
}
