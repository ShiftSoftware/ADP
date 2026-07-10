using ShiftSoftware.ADP.WarrantyClaims.Shared;

namespace ShiftSoftware.ADP.WarrantyClaims.Web;

/// <summary>
/// Dealer-safe fallback for <see cref="IWarrantyClaimsCapabilityProvider"/>: when the consumer does not
/// register its own adapter, <see cref="Extensions.WarrantyClaimsWebExtensions.AddWarrantyClaimsBlazorServices"/>
/// registers this default which reports the user as a non-distributor (no distributor-only UI shows).
/// </summary>
internal class DefaultWarrantyClaimsCapabilityProvider : IWarrantyClaimsCapabilityProvider
{
    public bool IsDistributor => false;
}
