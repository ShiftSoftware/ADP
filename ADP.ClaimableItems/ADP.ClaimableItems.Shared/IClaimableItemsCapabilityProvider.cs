// The namespace is the Blazor project's, where the interface was first published.
namespace ShiftSoftware.ADP.ClaimableItems.Web;

/// <summary>
/// Consumer capability seam: whether the user is the distributor. The consumer registers an adapter over its own
/// capability source. In the Blazor app it shows the certificate list's distributor-only batch actions (none registered:
/// hidden). In the API it decides who may run the distributor's item-claim actions (none registered: see
/// <c>ItemClaimRepository.IsDistributor</c>).
/// </summary>
public interface IClaimableItemsCapabilityProvider
{
    bool IsDistributor { get; }
}
