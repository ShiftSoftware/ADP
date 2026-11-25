using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Data.Services;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyCurrencyRate;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

public class WarrantyCurrencyRateRepository : ShiftRepository<ShiftDbContext, WarrantyCurrencyRate, WarrantyCurrencyRateListDTO, WarrantyCurrencyRateDTO>
{
    private readonly IWarrantyClaimsCapabilityProvider capabilityProvider;

    public WarrantyCurrencyRateRepository(ShiftDbContext db, IWarrantyClaimsCapabilityProvider capabilityProvider) : base(db)
    {
        this.capabilityProvider = capabilityProvider;
    }

    // The node a host gates the rates with can be one that dealers hold, and a rate decides a dealer's own payout.
    public override ValueTask<WarrantyCurrencyRate> UpsertAsync(WarrantyCurrencyRate entity, WarrantyCurrencyRateDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        this.capabilityProvider.RequireDistributor();

        return base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    public override ValueTask<WarrantyCurrencyRate> DeleteAsync(WarrantyCurrencyRate entity, long? userId, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        this.capabilityProvider.RequireDistributor();

        return base.DeleteAsync(entity, userId, disableDefaultDataLevelAccess, disableGlobalFilters);
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
