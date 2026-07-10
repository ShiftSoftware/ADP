using EntityFrameworkCore.Triggered;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Triggers;

/// <summary>
/// Assigns the sequential per-(CompanyID, ClaimableItemID) ClaimNumber AFTER insert, with a second
/// SaveChanges. Moved VERBATIM from the original host application's Services.Data.Triggers.ItemClaimAfterSaveTrigger (Phase 2
/// Slice 5) — including the last-by-ID read-back and its concurrency race (D16: port bug-for-bug;
/// fixing it would change observable numbering behavior).
/// The consumer registers it: <c>AddTransient&lt;IAfterSaveTrigger&lt;ItemClaim&gt;, ItemClaimAfterSaveTrigger&gt;()</c>.
/// </summary>
public class ItemClaimAfterSaveTrigger : IAfterSaveTrigger<ItemClaim>
{
    private readonly ItemClaimRepository itemClaimRepository;

    public ItemClaimAfterSaveTrigger(ItemClaimRepository itemClaimRepository)
    {
        this.itemClaimRepository = itemClaimRepository;
    }

    public async Task AfterSave(ITriggerContext<ItemClaim> context, CancellationToken cancellationToken)
    {
        if (context.ChangeType == ChangeType.Added)
        {
            var q = await this.itemClaimRepository.GetIQueryable(asOf: null, includes: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);

            var lastClaimNumber = await q
            .Where(x => x.ID != 0)
            .Where(x => x.ID != context.Entity.ID)
            .Where(x => x.CompanyID == context.Entity.CompanyID)
            .Where(x => x.ClaimableItemID == context.Entity.ClaimableItemID)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.ID)
            .Select(x => x.ClaimNumber)
            .FirstOrDefaultAsync();

            context.Entity.ClaimNumber = lastClaimNumber + 1;

            await itemClaimRepository.SaveChangesAsync();
        }
    }
}
