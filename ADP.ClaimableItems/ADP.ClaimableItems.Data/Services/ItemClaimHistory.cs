using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Cases.Data;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Services;

public sealed class ItemClaimHistory : ClaimHistory<ItemClaim, ItemClaimStatusHistory>
{
    public static ItemClaimHistory Instance { get; } = new();

    protected override bool IsDistributor(DbContext db) => ClaimableItemsCapability.IsDistributor(db);
}

public static class ItemClaimHistoryExtensions
{
    /// <summary>
    /// Records the history of the item claims that <typeparamref name="TDbContext"/> saves. The module's API
    /// registration calls it; a process that saves claims without that registration calls it itself.
    /// </summary>
    public static IServiceCollection AddItemClaimHistory<TDbContext>(this IServiceCollection services)
        where TDbContext : ShiftDbContext
        => services.ConfigureDbContext<TDbContext>(o => o.AddInterceptors(ItemClaimHistory.Instance));
}
