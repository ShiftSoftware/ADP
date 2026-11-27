using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared;

public static class WarrantyClaimAccess
{
    /// <summary>Whether a dealer may still edit a claim: its own drafts, and the claims returned to it.</summary>
    public static bool IsOpenToDealer(ClaimStatus? status) => status is ClaimStatus.Draft or ClaimStatus.RejectedWithError;

    /// <summary>The distributor notes on any claim, a dealer on one it may still edit.</summary>
    public static bool CanAddNote(ClaimStatus? status, bool isDistributor) => isDistributor || IsOpenToDealer(status);
}
