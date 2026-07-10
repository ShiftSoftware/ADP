namespace ShiftSoftware.ADP.ClaimableItems.Web;

/// <summary>
/// Consumer capability seam for the module's Blazor pages (Phase 2 Slice 7): the certificate list
/// shows distributor-only batch actions (e.g. Invoice) when <see cref="IsDistributor"/> is true.
/// The consumer registers an adapter over its own capability provider (host:
/// <c>ICapabilityProvider.IsDistributor</c> = a TypeAuth DistributorLevel check); when none is
/// registered, <see cref="AddClaimableItemsBlazorServices"/> falls back to a default that returns
/// false (dealer-safe).
/// </summary>
public interface IClaimableItemsCapabilityProvider
{
    bool IsDistributor { get; }
}

internal class DefaultClaimableItemsCapabilityProvider : IClaimableItemsCapabilityProvider
{
    public bool IsDistributor => false;
}
