using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Cases.Data;
using ShiftSoftware.ADP.Cases.Data.Printing;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Services;

public sealed class WarrantyClaimHistory : ClaimHistory<WarrantyClaim, WarrantyClaimStatusHistory>
{
    public static WarrantyClaimHistory Instance { get; } = new();

    protected override bool IsDistributor(DbContext db) =>
        PrintingServices.GetService<IWarrantyClaimsCapabilityProvider>(db)?.IsDistributor == true;
}

public static class WarrantyClaimHistoryExtensions
{
    /// <summary>
    /// Records the history of the warranty claims that <typeparamref name="TDbContext"/> saves. The module's API
    /// registration calls it; a process that saves claims without that registration calls it itself.
    /// </summary>
    public static IServiceCollection AddWarrantyClaimHistory<TDbContext>(this IServiceCollection services)
        where TDbContext : ShiftDbContext
        => services.ConfigureDbContext<TDbContext>(o => o.AddInterceptors(WarrantyClaimHistory.Instance));
}
