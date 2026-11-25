using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Data.Services;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyLaborRate;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

/// <summary>
/// The labor rates. Distributor-only, reads included: a row holds what the distributor charges the manufacturer, and
/// the node a host gates the rates with can be one that dealers hold.
/// </summary>
public class WarrantyLaborRateRepository : ShiftRepository<ShiftDbContext, WarrantyLaborRate, WarrantyLaborRateListDTO, WarrantyLaborRateDTO>
{
    private readonly IWarrantyClaimsCapabilityProvider capabilityProvider;

    public WarrantyLaborRateRepository(ShiftDbContext db, IWarrantyClaimsCapabilityProvider capabilityProvider) : base(db)
    {
        this.capabilityProvider = capabilityProvider;
    }

    public override async ValueTask<IQueryable<WarrantyLaborRate>> GetIQueryable(DateTimeOffset? asOf, List<string>? includes, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        this.capabilityProvider.RequireDistributor();

        return await base.GetIQueryable(asOf, includes, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    public override ValueTask<WarrantyLaborRate> UpsertAsync(WarrantyLaborRate entity, WarrantyLaborRateDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        this.capabilityProvider.RequireDistributor();

        return base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    public override ValueTask<WarrantyLaborRate> DeleteAsync(WarrantyLaborRate entity, long? userId, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        this.capabilityProvider.RequireDistributor();

        return base.DeleteAsync(entity, userId, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    public override IQueryable<RevisionDTO> GetRevisionsAsync(long id)
    {
        this.capabilityProvider.RequireDistributor();

        return base.GetRevisionsAsync(id);
    }

    /// <summary>
    /// The rates in force at <paramref name="asOf"/>: the row of the currency with the latest effective date that has
    /// started, the newest row winning a tie.
    /// </summary>
    public static Task<WarrantyLaborRate?> CurrentAsync(IQueryable<WarrantyLaborRate> rates, string currency, DateTime asOf) =>
        rates
            .Where(x => !x.IsDeleted)
            .Where(x => x.Currency == currency)
            .Where(x => x.EffectiveDate <= asOf)
            .OrderByDescending(x => x.EffectiveDate)
            .ThenByDescending(x => x.ID)
            .FirstOrDefaultAsync();
}
